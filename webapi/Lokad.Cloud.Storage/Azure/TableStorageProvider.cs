#region Copyright (c) Lokad 2010-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using Lokad.Cloud.Storage.Instrumentation;
using Lokad.Cloud.Storage.Instrumentation.Events;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Table.DataServices;
using Microsoft.WindowsAzure.Storage.Table.Protocol;

namespace Lokad.Cloud.Storage.Azure
{
    /// <summary>Implementation based on the Table Storage of Windows Azure.</summary>
    public class TableStorageProvider : ITableStorageProvider
    {
        // HACK: those tokens will probably be provided as constants in the StorageClient library
        const int MaxEntityTransactionCount = 50;

        // HACK: Lowering the maximal payload, to avoid corner cases #117 (ContentLengthExceeded)
        // [vermorel] 128kB is purely arbitrary, only taken as a reasonable safety margin
        const int MaxEntityTransactionPayload = 4 * 1024 * 1024 - 128 * 1024; // 4 MB - 128kB

        const string ContinuationNextRowKeyToken = "x-ms-continuation-NextRowKey";
        const string ContinuationNextPartitionKeyToken = "x-ms-continuation-NextPartitionKey";
        const string NextRowKeyToken = "NextRowKey";
        const string NextPartitionKeyToken = "NextPartitionKey";

        readonly CloudTableClient _tableStorage;
        readonly IDataSerializer _serializer;
        readonly IStorageObserver _observer;
        readonly RetryPolicies _policies;

        /// <summary>IoC constructor.</summary>
        /// <param name="observer">Can be <see langword="null"/>.</param>
        public TableStorageProvider(CloudTableClient tableStorage, IDataSerializer serializer = null, IStorageObserver observer = null)
        {
            _policies = new RetryPolicies(observer);
            _tableStorage = tableStorage;
            _serializer = serializer ?? new CloudFormatter();
            _observer = observer;
        }

        /// <remarks></remarks>
        public bool CreateTable(string tableName)
        {
            var flag = false;
            Retry.Do(_policies.SlowInstantiation(), CancellationToken.None,
                    () =>
                        {
                            var table = _tableStorage.GetTableReference(tableName);
                                flag = table.CreateIfNotExists();
                            });
            return flag;
        }

        /// <remarks></remarks>
        public bool DeleteTable(string tableName)
        {
            var flag = false;
            Retry.Do(_policies.SlowInstantiation(), CancellationToken.None,
                  () =>
                  {
                      var table = _tableStorage.GetTableReference(tableName);
                      flag = table.DeleteIfExists();
                  });

            return flag;
        }

        /// <remarks></remarks>
        public IEnumerable<string> GetTables()
        {
            return _tableStorage.ListTables().Select(x=>x.Name);
        }

        /// <remarks></remarks>
        public IEnumerable<CloudEntity<T>> Get<T>(string tableName)
        {
            if(null == tableName) throw new ArgumentNullException("tableName");

            var context = _tableStorage.GetTableServiceContext();
            return GetInternal<T>(context, tableName, Maybe<string>.Empty);
        }

        /// <remarks></remarks>
        public IEnumerable<CloudEntity<T>> Get<T>(string tableName, string partitionKey)
        {
            if(null == tableName) throw new ArgumentNullException("tableName");
            if(null == partitionKey) throw new ArgumentNullException("partitionKey");
            if (partitionKey.Contains("'"))
                throw new ArgumentOutOfRangeException("partitionKey", "Incorrect char in partitionKey.");

            var filter = string.Format("(PartitionKey eq '{0}')", HttpUtility.UrlEncode(partitionKey));

            var context = _tableStorage.GetTableServiceContext();
            return GetInternal<T>(context, tableName, filter);
        }

        /// <remarks></remarks>
        public IEnumerable<CloudEntity<T>> Get<T>(string tableName, string partitionKey, string startRowKey, string endRowKey)
        {
            if(null == tableName) throw new ArgumentNullException("tableName");
            if(null == partitionKey) throw new ArgumentNullException("partitionKey");
            if (partitionKey.Contains("'"))
                throw new ArgumentOutOfRangeException("partitionKey", "Incorrect char.");
            if(startRowKey != null && startRowKey.Contains("'"))
                throw new ArgumentOutOfRangeException("startRowKey", "Incorrect char.");
            if(endRowKey != null && endRowKey.Contains("'"))
                throw new ArgumentOutOfRangeException("endRowKey", "Incorrect char.");

            var filter = string.Format("(PartitionKey eq '{0}')", HttpUtility.UrlEncode(partitionKey));

            // optional starting range constraint
            if (!string.IsNullOrEmpty(startRowKey))
            {
                // ge = GreaterThanOrEqual (inclusive)
                filter += string.Format(" and (RowKey ge '{0}')", HttpUtility.UrlEncode(startRowKey));
            }

            if (!string.IsNullOrEmpty(endRowKey))
            {
                // lt = LessThan (exclusive)
                filter += string.Format(" and (RowKey lt '{0}')", HttpUtility.UrlEncode(endRowKey));
            }

            var context = _tableStorage.GetTableServiceContext();
            return GetInternal<T>(context, tableName, filter);
        }

        /// <remarks></remarks>
        public IEnumerable<CloudEntity<T>> Get<T>(string tableName, string partitionKey, IEnumerable<string> rowKeys)
        {
            if(null == tableName) throw new ArgumentNullException("tableName");
            if(null == partitionKey) throw new ArgumentNullException("partitionKey");
            if(partitionKey.Contains("'")) throw new ArgumentOutOfRangeException("partitionKey", "Incorrect char.");

            var context = _tableStorage.GetTableServiceContext();
			var slices = Slice(rowKeys, MaxEntityTransactionCount).ToArray();

            foreach (var slice in slices)
            {
                // work-around the limitation of ADO.NET that does not provide a native way
                // of query a set of specified entities directly.
                var builder = new StringBuilder();
                builder.Append(string.Format("(PartitionKey eq '{0}') and (", HttpUtility.UrlEncode(partitionKey)));
                for (int i = 0; i < slice.Length; i++)
                {
                    // in order to avoid SQL-injection-like problems 
                    if (slice[i].Contains("'")) throw new ArgumentOutOfRangeException("rowKeys", "Incorrect char.");

                    builder.Append(string.Format("(RowKey eq '{0}')", HttpUtility.UrlEncode(slice[i])));
                    if (i < slice.Length - 1)
                    {
                        builder.Append(" or ");
                    }
                }
                builder.Append(")");
				Debug.WriteLine(builder.ToString());

                foreach(var entity in GetInternal<T>(context, tableName, builder.ToString()))
                {
                    yield return entity;
                }
            }
        }

        /// <remarks></remarks>
        private IEnumerable<CloudEntity<T>> GetInternal<T>(TableServiceContext context, string tableName, Maybe<string> filter)
        {
            string continuationRowKey = null;
            string continuationPartitionKey = null;

            var stopwatch = Stopwatch.StartNew();

            context.MergeOption = MergeOption.AppendOnly;
            context.ResolveType = ResolveFatEntityType;

            do
            {
                var query = context.CreateQuery<FatEntity>(tableName);

                if (filter.HasValue)
                {
                    query = query.AddQueryOption("$filter", filter.Value);
                }

                if (null != continuationRowKey)
                {
                    query = query.AddQueryOption(NextRowKeyToken, continuationRowKey)
                        .AddQueryOption(NextPartitionKeyToken, continuationPartitionKey);
                }

                QueryOperationResponse response = null;
                FatEntity[] fatEntities = null;

                Retry.Do(_policies.TransientTableErrorBackOff(), CancellationToken.None, () =>
                    {
                        try
                        {
                            response = query.Execute() as QueryOperationResponse;
                            fatEntities = ((IEnumerable<FatEntity>)response).ToArray();
                        }
                        catch (DataServiceQueryException ex)
                        {
                            // if the table does not exist, there is nothing to return
                            var errorCode = RetryPolicies.GetErrorCode(ex);
                            if (TableErrorCodeStrings.TableNotFound == errorCode
                                || StorageErrorCodeStrings.ResourceNotFound == errorCode)
                            {
                                fatEntities = new FatEntity[0];
                                return;
                            }

                            throw;
                        }
                    });

                NotifySucceeded(StorageOperationType.TableQuery, stopwatch);

                foreach (var fatEntity in fatEntities)
                {
                    var etag = context.Entities.First(e => e.Entity == fatEntity).ETag;
                    context.Detach(fatEntity);
                    yield return FatEntity.Convert<T>(fatEntity, _serializer, etag);
                }

                Debug.Assert(context.Entities.Count == 0);

                if (null != response && response.Headers.ContainsKey(ContinuationNextRowKeyToken))
                {
                    continuationRowKey = response.Headers[ContinuationNextRowKeyToken];
                    continuationPartitionKey = response.Headers[ContinuationNextPartitionKeyToken];

                    stopwatch.Restart();
                }
                else
                {
                    continuationRowKey = null;
                    continuationPartitionKey = null;
                }

            } while (null != continuationRowKey);
        }

        /// <remarks></remarks>
        public void Insert<T>(string tableName, IEnumerable<CloudEntity<T>> entities)
        {
            foreach (var g in entities.GroupBy(e => e.PartitionKey))
            {
                InsertInternal(tableName, g);
            }
        }

        /// <remarks></remarks>
        void InsertInternal<T>(string tableName, IEnumerable<CloudEntity<T>> entities)
        {
            var context = _tableStorage.GetTableServiceContext();
            context.MergeOption = MergeOption.AppendOnly;
            context.ResolveType = ResolveFatEntityType;

            var stopwatch = new Stopwatch();

            var fatEntities = entities.Select(e => Tuple.Create(FatEntity.Convert(e, _serializer), e));

            var noBatchMode = false;

            foreach (var slice in SliceEntities(fatEntities, e => e.Item1.GetPayload()))
            {
                stopwatch.Restart();

                var cloudEntityOfFatEntity = new Dictionary<object, CloudEntity<T>>();
                foreach (var fatEntity in slice)
                {
                    context.AddObject(tableName, fatEntity.Item1);
                    cloudEntityOfFatEntity.Add(fatEntity.Item1, fatEntity.Item2);
                }

                Retry.Do(_policies.TransientTableErrorBackOff(), CancellationToken.None, () =>
                    {
                        try
                        {
                            // HACK: nested try/catch
                            try
                            {
                                context.SaveChanges(noBatchMode ? SaveChangesOptions.None : SaveChangesOptions.Batch);
                                ReadETagsAndDetach(context, (entity, etag) => cloudEntityOfFatEntity[entity].ETag = etag);
                            }
                                // special casing the need for table instantiation
                            catch (DataServiceRequestException ex)
                            {
                                var errorCode = RetryPolicies.GetErrorCode(ex);
                                if (errorCode == TableErrorCodeStrings.TableNotFound
                                    || errorCode == StorageErrorCodeStrings.ResourceNotFound)
                                {
                                    Retry.Do(_policies.SlowInstantiation(), CancellationToken.None, () =>
                                        {
                                            try
                                            {
                                                var table = _tableStorage.GetTableReference(tableName);
                                                table.CreateIfNotExists();
                                            }
                                            // HACK: incorrect behavior of the StorageClient (2010-09)
                                            // Fails to behave properly in multi-threaded situations
                                            catch (StorageException cex)
                                            {
                                                if (cex.RequestInformation.ExtendedErrorInformation == null
                                                    || cex.RequestInformation.ExtendedErrorInformation.ErrorCode != TableErrorCodeStrings.TableAlreadyExists)
                                                {
                                                    throw;
                                                }
                                            }
                                            context.SaveChanges(noBatchMode ? SaveChangesOptions.None : SaveChangesOptions.Batch);
                                            ReadETagsAndDetach(context, (entity, etag) => cloudEntityOfFatEntity[entity].ETag = etag);
                                        });
                                }
                                else
                                {
                                    throw;
                                }
                            }
                        }
                        catch (DataServiceRequestException ex)
                        {
                            var errorCode = RetryPolicies.GetErrorCode(ex);

                            if (errorCode == StorageErrorCodeStrings.OperationTimedOut)
                            {
                                // if batch does not work, then split into elementary requests
                                // PERF: it would be better to split the request in two and retry
                                context.SaveChanges();
                                ReadETagsAndDetach(context, (entity, etag) => cloudEntityOfFatEntity[entity].ETag = etag);
                                noBatchMode = true;
                            }
                                // HACK: undocumented code returned by the Table Storage
                            else if (errorCode == "ContentLengthExceeded")
                            {
                                context.SaveChanges();
                                ReadETagsAndDetach(context, (entity, etag) => cloudEntityOfFatEntity[entity].ETag = etag);
                                noBatchMode = true;
                            }
                            else
                            {
                                throw;
                            }
                        }
                        catch (DataServiceQueryException ex)
                        {
                            // HACK: code duplicated

                            var errorCode = RetryPolicies.GetErrorCode(ex);

                            if (errorCode == StorageErrorCodeStrings.OperationTimedOut)
                            {
                                // if batch does not work, then split into elementary requests
                                // PERF: it would be better to split the request in two and retry
                                context.SaveChanges();
                                ReadETagsAndDetach(context, (entity, etag) => cloudEntityOfFatEntity[entity].ETag = etag);
                                noBatchMode = true;
                            }
                            else
                            {
                                throw;
                            }
                        }
                    });

                NotifySucceeded(StorageOperationType.TableInsert, stopwatch);
            }
        }

        /// <remarks></remarks>
        public void Update<T>(string tableName, IEnumerable<CloudEntity<T>> entities, bool force)
        {
            foreach (var g in entities.GroupBy(e => e.PartitionKey))
            {
                UpdateInternal(tableName, g, force);
            }
        }

        /// <remarks></remarks>
        void UpdateInternal<T>(string tableName, IEnumerable<CloudEntity<T>> entities, bool force)
        {
            var context = _tableStorage.GetTableServiceContext();
            context.MergeOption = MergeOption.AppendOnly;
            context.ResolveType = ResolveFatEntityType;

            var stopwatch = new Stopwatch();

            var fatEntities = entities.Select(e => Tuple.Create(FatEntity.Convert(e, _serializer), e));

            var noBatchMode = false;

            foreach (var slice in SliceEntities(fatEntities, e => e.Item1.GetPayload()))
            {
                stopwatch.Restart();

                var cloudEntityOfFatEntity = new Dictionary<object, CloudEntity<T>>();
                foreach (var fatEntity in slice)
                {
                    // entities should be updated in a single round-trip
                    context.AttachTo(tableName, fatEntity.Item1, MapETag(fatEntity.Item2.ETag, force));
                    context.UpdateObject(fatEntity.Item1);
                    cloudEntityOfFatEntity.Add(fatEntity.Item1, fatEntity.Item2);
                }

                Retry.Do(_policies.TransientTableErrorBackOff(), CancellationToken.None, () =>
                    {
                        try
                        {
                            context.SaveChanges(noBatchMode ? SaveChangesOptions.None : SaveChangesOptions.Batch);
                            ReadETagsAndDetach(context, (entity, etag) => cloudEntityOfFatEntity[entity].ETag = etag);
                        }
                        catch (DataServiceRequestException ex)
                        {
                            var errorCode = RetryPolicies.GetErrorCode(ex);

                            if (errorCode == StorageErrorCodeStrings.OperationTimedOut)
                            {
                                // if batch does not work, then split into elementary requests
                                // PERF: it would be better to split the request in two and retry
                                context.SaveChanges();
                                ReadETagsAndDetach(context, (entity, etag) => cloudEntityOfFatEntity[entity].ETag = etag);
                                noBatchMode = true;
                            }
                            else if (errorCode == TableErrorCodeStrings.TableNotFound)
                            {
                                Retry.Do(_policies.SlowInstantiation(), CancellationToken.None, () =>
                                    {
                                        try
                                        {
                                            var table = _tableStorage.GetTableReference(tableName);
                                            table.CreateIfNotExists();
                                        }
                                        // HACK: incorrect behavior of the StorageClient (2010-09)
                                        // Fails to behave properly in multi-threaded situations
                                        catch (StorageException cex)
                                        {
                                            if (cex.RequestInformation.ExtendedErrorInformation.ErrorCode != TableErrorCodeStrings.TableAlreadyExists)
                                            {
                                                throw;
                                            }
                                        }
                                        context.SaveChanges(noBatchMode ? SaveChangesOptions.None : SaveChangesOptions.Batch);
                                        ReadETagsAndDetach(context, (entity, etag) => cloudEntityOfFatEntity[entity].ETag = etag);
                                    });
                            }
                            else if (errorCode == StorageErrorCodeStrings.ResourceNotFound)
                            {
                                throw new InvalidOperationException("Cannot call update on a resource that does not exist", ex);
                            }
                            else
                            {
                                throw;
                            }
                        }
                        catch (DataServiceQueryException ex)
                        {
                            // HACK: code duplicated

                            var errorCode = RetryPolicies.GetErrorCode(ex);

                            if (errorCode == StorageErrorCodeStrings.OperationTimedOut)
                            {
                                // if batch does not work, then split into elementary requests
                                // PERF: it would be better to split the request in two and retry
                                context.SaveChanges();
                                ReadETagsAndDetach(context, (entity, etag) => cloudEntityOfFatEntity[entity].ETag = etag);
                                noBatchMode = true;
                            }
                            else
                            {
                                throw;
                            }
                        }
                    });

                NotifySucceeded(StorageOperationType.TableUpdate, stopwatch);
            }
        }

        /// <remarks></remarks>
        public void Upsert<T>(string tableName, IEnumerable<CloudEntity<T>> entities)
        {
            foreach (var g in entities.GroupBy(e => e.PartitionKey))
            {
                UpsertInternal(tableName, g);
            }
        }

        /// <remarks>Upsert is making several storage calls to emulate the 
        /// missing semantic from the Table Storage.</remarks>
        void UpsertInternal<T>(string tableName, IEnumerable<CloudEntity<T>> entities)
        {
            var context = _tableStorage.GetTableServiceContext();
            context.MergeOption = MergeOption.AppendOnly;
            context.ResolveType = ResolveFatEntityType;

            var stopwatch = new Stopwatch();

            var fatEntities = entities.Select(e => Tuple.Create(FatEntity.Convert(e, _serializer), e));

            var noBatchMode = false;

            foreach (var slice in SliceEntities(fatEntities, e => e.Item1.GetPayload()))
            {
                stopwatch.Restart();

                var cloudEntityOfFatEntity = new Dictionary<object, CloudEntity<T>>();
                foreach (var fatEntity in slice)
                {
                    // entities should be updated in a single round-trip
                    context.AttachTo(tableName, fatEntity.Item1);
                    context.UpdateObject(fatEntity.Item1);
                    cloudEntityOfFatEntity.Add(fatEntity.Item1, fatEntity.Item2);
                }

                Retry.Do(_policies.TransientTableErrorBackOff(), CancellationToken.None, () =>
                {
                    try
                    {
                        context.SaveChanges(noBatchMode ? SaveChangesOptions.ReplaceOnUpdate : SaveChangesOptions.ReplaceOnUpdate | SaveChangesOptions.Batch);
                        ReadETagsAndDetach(context, (entity, etag) => cloudEntityOfFatEntity[entity].ETag = etag);
                    }
                    catch (DataServiceRequestException ex)
                    {
                        var errorCode = RetryPolicies.GetErrorCode(ex);

                        if (errorCode == StorageErrorCodeStrings.OperationTimedOut)
                        {
                            // if batch does not work, then split into elementary requests
                            // PERF: it would be better to split the request in two and retry
                            context.SaveChanges(SaveChangesOptions.ReplaceOnUpdate);
                            ReadETagsAndDetach(context, (entity, etag) => cloudEntityOfFatEntity[entity].ETag = etag);
                            noBatchMode = true;
                        }
                        else if (errorCode == TableErrorCodeStrings.TableNotFound)
                        {
                            Retry.Do(_policies.SlowInstantiation(), CancellationToken.None, () =>
                            {
                                try
                                {
                                    var table = _tableStorage.GetTableReference(tableName);
                                    table.CreateIfNotExists();
                                }
                                // HACK: incorrect behavior of the StorageClient (2010-09)
                                // Fails to behave properly in multi-threaded situations
                                catch (StorageException cex)
                                {
                                    if (cex.RequestInformation.ExtendedErrorInformation.ErrorCode != TableErrorCodeStrings.TableAlreadyExists)
                                    {
                                        throw;
                                    }
                                }
                                context.SaveChanges(noBatchMode ? SaveChangesOptions.ReplaceOnUpdate : SaveChangesOptions.ReplaceOnUpdate | SaveChangesOptions.Batch);
                                ReadETagsAndDetach(context, (entity, etag) => cloudEntityOfFatEntity[entity].ETag = etag);
                            });
                        }
                        else if (errorCode == StorageErrorCodeStrings.ResourceNotFound)
                        {
                            throw new InvalidOperationException("Cannot call update on a resource that does not exist", ex);
                        }
                        else
                        {
                            throw;
                        }
                    }
                    catch (DataServiceQueryException ex)
                    {
                        // HACK: code duplicated

                        var errorCode = RetryPolicies.GetErrorCode(ex);

                        if (errorCode == StorageErrorCodeStrings.OperationTimedOut)
                        {
                            // if batch does not work, then split into elementary requests
                            // PERF: it would be better to split the request in two and retry
                            context.SaveChanges(SaveChangesOptions.ReplaceOnUpdate);
                            ReadETagsAndDetach(context, (entity, etag) => cloudEntityOfFatEntity[entity].ETag = etag);
                            noBatchMode = true;
                        }
                        else
                        {
                            throw;
                        }
                    }
                });

                NotifySucceeded(StorageOperationType.TableUpsert, stopwatch);
            }
        }

        /// <summary>Slice entities according the payload limitation of
        /// the transaction group, plus the maximal number of entities to
        /// be embedded into a single transaction.</summary>
        static IEnumerable<T[]> SliceEntities<T>(IEnumerable<T> entities, Func<T, int> getPayload)
        {
            var accumulator = new List<T>(100);
            var payload = 0;
            foreach (var entity in entities)
            {
                var entityPayLoad = getPayload(entity);

                if (accumulator.Count >= MaxEntityTransactionCount ||
                    payload + entityPayLoad >= MaxEntityTransactionPayload)
                {
                    yield return accumulator.ToArray();
                    accumulator.Clear();
                    payload = 0;
                }

                accumulator.Add(entity);
                payload += entityPayLoad;
            }

            if (accumulator.Count > 0)
            {
                yield return accumulator.ToArray();
            }
        }

        /// <remarks></remarks>
        public void Delete<T>(string tableName, string partitionKey, IEnumerable<string> rowKeys)
        {
            DeleteInternal<T>(tableName, partitionKey, rowKeys.Select(k => Tuple.Create(k, "*")), true);
        }

        /// <remarks></remarks>
        public void Delete<T>(string tableName, IEnumerable<CloudEntity<T>> entities, bool force)
        {
            foreach (var g in entities.GroupBy(e => e.PartitionKey))
            {
                DeleteInternal<T>(tableName, 
                    g.Key, g.Select(e => Tuple.Create(e.RowKey, MapETag(e.ETag, force))), force);
            }
        }

        /// <remarks></remarks>
        void DeleteInternal<T>(string tableName, string partitionKey, IEnumerable<Tuple<string,string>> rowKeysAndETags, bool force)
        {
            var context = _tableStorage.GetTableServiceContext();

            var stopwatch = new Stopwatch();

            // CAUTION: make sure to get rid of potential duplicate in rowkeys.
            // (otherwise insertion in 'context' is likely to fail)
            foreach (var s in Slice(rowKeysAndETags
                                    // Similar effect than 'Distinct' based on 'RowKey'
                                    .ToLookup(p => p.Item1, p => p).Select(g => g.First()), 
                                    MaxEntityTransactionCount))
            {
                stopwatch.Restart();

                var slice = s;

                DeletionStart: // 'slice' might have been refreshed if some entities were already deleted

                foreach (var rowKeyAndETag in slice)
                {
                    // Deleting entities in 1 roundtrip
                    // http://blog.smarx.com/posts/deleting-entities-from-windows-azure-without-querying-first
                    var mock = new FatEntity
                        {
                            PartitionKey = partitionKey,
                            RowKey = rowKeyAndETag.Item1
                        };

                    context.AttachTo(tableName, mock, rowKeyAndETag.Item2);
                    context.DeleteObject(mock);

                }

                try // HACK: [vermorel] if a single entity is missing, then the whole batch operation is aborded
                {

                    try // HACK: nested try/catch to handle the special case where the table is missing
                    {
                        Retry.Do(_policies.TransientTableErrorBackOff(), CancellationToken.None, () => context.SaveChanges(SaveChangesOptions.Batch));
                    }
                    catch (DataServiceRequestException ex)
                    {
                        // if the table is missing, no need to go on with the deletion
                        var errorCode = RetryPolicies.GetErrorCode(ex);
                        if (TableErrorCodeStrings.TableNotFound == errorCode)
                        {
                            NotifySucceeded(StorageOperationType.TableDelete, stopwatch);
                            return;
                        }

                        throw;
                    }
                }
                    // if some entities exist
                catch (DataServiceRequestException ex)
                {
                    var errorCode = RetryPolicies.GetErrorCode(ex);

                    // HACK: Table Storage both implement a bizarre non-idempotent semantic
                    // but in addition, it throws a non-documented exception as well. 
                    if (errorCode != "ResourceNotFound")
                    {
                        throw;
                    }

                    slice = Get<T>(tableName, partitionKey, slice.Select(p => p.Item1))
                        .Select(e => Tuple.Create(e.RowKey, MapETag(e.ETag, force))).ToArray();

                    // entities with same name will be added again
                    context = _tableStorage.GetTableServiceContext();

                    // HACK: [vermorel] yes, gotos are horrid, but other solutions are worst here.
                    goto DeletionStart;
                }

                NotifySucceeded(StorageOperationType.TableDelete, stopwatch);
            }
        }

        static Type ResolveFatEntityType(string name)
        {
            return typeof (FatEntity);
        }

        static string MapETag(string etag, bool force)
        {
            return force || string.IsNullOrEmpty(etag)
                ? "*"
                : etag;
        }

        static void ReadETagsAndDetach(DataServiceContext context, Action<object, string> write)
        {
            foreach (var entity in context.Entities)
            {
                write(entity.Entity, entity.ETag);
                context.Detach(entity.Entity);
            }
        }

        /// <summary>
        /// Performs lazy splitting of the provided collection into collections of <paramref name="sliceLength"/>
        /// </summary>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="sliceLength">Maximum length of the slice.</param>
        /// <returns>lazy enumerator of the collection of arrays</returns>
        public static IEnumerable<TItem[]> Slice<TItem>(IEnumerable<TItem> source, int sliceLength)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (sliceLength <= 0)
                throw new ArgumentOutOfRangeException("sliceLength", "value must be greater than 0");

            var list = new List<TItem>(sliceLength);
            foreach (var item in source)
            {
                list.Add(item);
                if (sliceLength == list.Count)
                {
                    yield return list.ToArray();
                    list.Clear();
                }
            }

            if (list.Count > 0)
                yield return list.ToArray();
        }

        private void NotifySucceeded(StorageOperationType operationType, Stopwatch stopwatch)
        {
            if (_observer != null)
            {
                _observer.Notify(new StorageOperationSucceededEvent(operationType, stopwatch.Elapsed));
            }
        }
    }
}