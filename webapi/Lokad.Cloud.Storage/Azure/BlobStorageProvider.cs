#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Lokad.Cloud.Storage.Instrumentation;
using Lokad.Cloud.Storage.Instrumentation.Events;
using Lokad.Cloud.Storage.Shared.Threading;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Blob.Protocol;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;

// ReSharper disable CSharpWarnings::CS1591

namespace Lokad.Cloud.Storage.Azure
{
    /// <summary>Provides access to the Blob Storage.</summary>
    /// <remarks>
    /// All the methods of <see cref="BlobStorageProvider"/> are thread-safe.
    /// </remarks>
    public class BlobStorageProvider : IBlobStorageProvider
    {
        /// <summary>Custom meta-data used as a work-around of an issue of the StorageClient.</summary>
        /// <remarks>[vermorel 2010-11] The StorageClient for odds reasons do not enable the
        /// retrieval of the Content-MD5 property when performing a GET on blobs. In order to validate
        /// the integrity during the entire roundtrip, we need to apply a supplementary header
        /// used to perform the MD5 check.</remarks>
        private const string MetadataMD5Key = "LokadContentMD5";

        readonly CloudBlobClient _blobStorage;
        readonly IDataSerializer _defaultSerializer;
        readonly IStorageObserver _observer;
        readonly RetryPolicies _policies;

        /// <summary>IoC constructor.</summary>
        /// <param name="observer">Can be <see langword="null"/>.</param>
        public BlobStorageProvider(CloudBlobClient blobStorage, IDataSerializer defaultSerializer = null, IStorageObserver observer = null)
        {
            _policies = new RetryPolicies(observer);
            _blobStorage = blobStorage;
            _defaultSerializer = defaultSerializer ?? new CloudFormatter();
            _observer = observer;
        }

        public IEnumerable<string> ListContainers(string containerNamePrefix = null)
        {
            var enumerator = String.IsNullOrEmpty(containerNamePrefix)
                ? _blobStorage.ListContainers().GetEnumerator()
                : _blobStorage.ListContainers(containerNamePrefix,new ContainerListingDetails()).GetEnumerator();

            // TODO: Parallelize

            while (true)
            {
                if (!Retry.Get(_policies.TransientServerErrorBackOff(), CancellationToken.None, enumerator.MoveNext))
                {
                    yield break;
                }

                // removing /container/ from the blob name (dev storage: /account/container/)
                yield return enumerator.Current.Name;
            }
        }

        public bool CreateContainerIfNotExist(string containerName)
        {
            //workaround since Azure is presently returning OutOfRange exception when using a wrong name.
            if (!BlobStorageExtensions.IsContainerNameValid(containerName))
                throw new NotSupportedException("containerName is not compliant with azure constraints on container naming");

            var container = _blobStorage.GetContainerReference(containerName);
            try
            {
                Retry.Do(_policies.TransientServerErrorBackOff(), CancellationToken.None, () =>container.Create());
                return true;
            }
            catch(StorageException ex)
            {
                if(ex.RequestInformation.ExtendedErrorInformation.ErrorCode == StorageErrorCodeStrings.ContainerAlreadyExists
                    || ex.RequestInformation.ExtendedErrorInformation.ErrorCode == BlobErrorCodeStrings.BlobAlreadyExists)
                {
                    return false;
                }

                throw;
            }
        }

        public bool DeleteContainerIfExist(string containerName)
        {
            var container = _blobStorage.GetContainerReference(containerName);
            try
            {
                Retry.Do(_policies.TransientServerErrorBackOff(), CancellationToken.None, () => container.Delete());
                return true;
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.ExtendedErrorInformation.ErrorCode == StorageErrorCodeStrings.ContainerNotFound
                    || ex.RequestInformation.ExtendedErrorInformation.ErrorCode == StorageErrorCodeStrings.ResourceNotFound)
                {
                    return false;
                }

                throw;
            }
        }

        public IEnumerable<string> ListBlobNames(string containerName, string blobNamePrefix = null)
        {
            // Enumerated blobs do not have a "name" property,
            // thus the name must be extracted from their URI
            // http://social.msdn.microsoft.com/Forums/en-US/windowsazure/thread/c5e36676-8d07-46cc-b803-72621a0898b0/?prof=required

            if (blobNamePrefix == null)
            {
                blobNamePrefix = string.Empty;
            }

            var container = _blobStorage.GetContainerReference(containerName);


            // if no prefix is provided, then enumerate the whole container
            IEnumerator<IListBlobItem> enumerator;
            if (string.IsNullOrEmpty(blobNamePrefix))
            {
                enumerator = container.ListBlobs(useFlatBlobListing:true).GetEnumerator();
            }
            else
            {
                // 'CloudBlobDirectory' must be used for prefixed enumeration
                var directory = container.GetDirectoryReference(blobNamePrefix);

                // HACK: [vermorel] very ugly override, but otherwise an "/" separator gets forcibly added
                typeof(CloudBlobDirectory).GetField("prefix", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(directory, blobNamePrefix);

                enumerator = directory.ListBlobs(useFlatBlobListing: true).GetEnumerator();
            }

            // TODO: Parallelize

            while (true)
            {
                try
                {
                    if (!Retry.Get(_policies.TransientServerErrorBackOff(), CancellationToken.None, enumerator.MoveNext))
                    {
                        yield break;
                    }
                }
                catch (StorageException ex)
                {
                    // if the container does not exist, empty enumeration
                    if (ex.RequestInformation.ExtendedErrorInformation.ErrorCode == StorageErrorCodeStrings.ContainerNotFound)
                    {
                        yield break;
                    }
                    throw;
                }

                // removing /container/ from the blob name (dev storage: /account/container/)
                yield return Uri.UnescapeDataString(enumerator.Current.Uri.AbsolutePath.Substring(container.Uri.LocalPath.Length + 1));
            }
        }

        public IEnumerable<T> ListBlobs<T>(string containerName, string blobNamePrefix = null, int skip = 0, IDataSerializer serializer = null)
        {
            var names = ListBlobNames(containerName, blobNamePrefix);

            if (skip > 0)
            {
                names = names.Skip(skip);
            }

            return names.Select(name => GetBlob<T>(containerName, name, serializer))
                .Where(blob => blob.HasValue)
                .Select(blob => blob.Value);
        }

        public bool DeleteBlobIfExist(string containerName, string blobName)
        {
            var stopwatch = Stopwatch.StartNew();

            var container = _blobStorage.GetContainerReference(containerName);

            try
            {
                var blob = container.GetBlockBlobReference(blobName);
                Retry.Do(_policies.TransientServerErrorBackOff(), CancellationToken.None, () => blob.Delete());

                NotifySucceeded(StorageOperationType.BlobDelete, stopwatch);
                return true;
            }
            catch (StorageException ex) // no such container, return false
            {
                if (IsNotFoundException(ex))
                {
                    // success anyway since the condition was not met
                    NotifySucceeded(StorageOperationType.BlobDelete, stopwatch);
                    return false;
                }

                throw;
            }
        }

        public void DeleteAllBlobs(string containerName, string blobNamePrefix = null)
        {
            // TODO: Parallelize
            foreach (var blobName in ListBlobNames(containerName, blobNamePrefix))
            {
                DeleteBlobIfExist(containerName, blobName);
            }
        }

        public Maybe<T> GetBlob<T>(string containerName, string blobName, IDataSerializer serializer = null)
        {
            string ignoredEtag;
            return GetBlob<T>(containerName, blobName, out ignoredEtag, serializer);
        }

        public Maybe<T> GetBlob<T>(string containerName, string blobName, out string etag, IDataSerializer serializer = null)
        {
            return GetBlob(containerName, blobName, typeof(T), out etag, serializer)
                .Convert(o => (T)o, Maybe<T>.Empty);
        }

        public Maybe<object> GetBlob(string containerName, string blobName, Type type, out string etag, IDataSerializer serializer = null)
        {
            var stopwatch = Stopwatch.StartNew();

            var container = _blobStorage.GetContainerReference(containerName);
            var blob = container.GetBlockBlobReference(blobName);

            using (var stream = new MemoryStream())
            {
                etag = null;

                // if no such container, return empty
                try
                {
                    Retry.Do(_policies.TransientServerErrorBackOff(), CancellationToken.None, () =>
                        {
                            stream.Seek(0, SeekOrigin.Begin);
                            blob.DownloadToStream(stream);
                            VerifyContentHash(blob, stream, containerName, blobName);
                        });

                    etag = blob.Properties.ETag;
                }
                catch (StorageException ex)
                {
                    if (IsNotFoundException(ex))
                    {
                        return Maybe<object>.Empty;
                    }

                    throw;
                }

                stream.Seek(0, SeekOrigin.Begin);
                var deserialized = (serializer ?? _defaultSerializer).TryDeserialize(stream, type);

                if (_observer != null)
                {
                    if (!deserialized.IsSuccess)
                    {
                        _observer.Notify(new BlobDeserializationFailedEvent(deserialized.Error, containerName, blobName));
                    }
                    else
                    {
                        NotifySucceeded(StorageOperationType.BlobGet, stopwatch);
                    }
                }

                return deserialized.IsSuccess ? new Maybe<object>(deserialized.Value) : Maybe<object>.Empty;
            }
        }

        public Task<BlobWithETag<object>> GetBlobAsync(string containerName, string blobName, Type type, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            var stopwatch = Stopwatch.StartNew();
            var completionSource = new TaskCompletionSource<BlobWithETag<object>>();

            var container = _blobStorage.GetContainerReference(containerName);
            var blob = container.GetBlockBlobReference(blobName);

            var stream = new MemoryStream();
            completionSource.Task.ContinueWith(t => stream.Dispose());

            Retry.Task(_policies.TransientServerErrorBackOff(), cancellationToken,
                () =>
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        return Task.Factory.FromAsync(blob.BeginDownloadToStream, blob.EndDownloadToStream, stream, null)
                            .Then(() => VerifyContentHash(blob, stream, containerName, blobName));
                    },
                () =>
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        var deserialized = (serializer ?? _defaultSerializer).TryDeserialize(stream, type);
                        if (deserialized.IsSuccess)
                        {
                            NotifySucceeded(StorageOperationType.BlobGet, stopwatch);
                            completionSource.TrySetResult(new BlobWithETag<object>
                                {
                                    ETag = blob.Properties.ETag,
                                    Blob = deserialized.Value
                                });
                        }
                        else
                        {
                            if (_observer != null)
                            {
                                _observer.Notify(new BlobDeserializationFailedEvent(deserialized.Error, containerName, blobName));
                            }
                            completionSource.TrySetResult(null);
                        }
                    },
                exception =>
                    {
                        if (IsNotFoundException(exception))
                        {
                            completionSource.TrySetResult(null);
                        }
                        else
                        {
                            NotifyFailed(StorageOperationType.BlobGet, exception);
                            completionSource.TrySetException(exception);
                        }
                    },
                () => completionSource.TrySetCanceled());

            return completionSource.Task;
        }

        public Maybe<XElement> GetBlobXml(string containerName, string blobName, out string etag, IDataSerializer serializer = null)
        {
            etag = null;

            var formatter = (serializer ?? _defaultSerializer) as IIntermediateDataSerializer;
            if (formatter == null)
            {
                return Maybe<XElement>.Empty;
            }

            var container = _blobStorage.GetContainerReference(containerName);
            var blob = container.GetBlockBlobReference(blobName);

            using (var stream = new MemoryStream())
            {
                // if no such container, return empty
                try
                {
                    Retry.Do(_policies.TransientServerErrorBackOff(), CancellationToken.None, () =>
                        {
                            stream.Seek(0, SeekOrigin.Begin);
                            blob.DownloadToStream(stream);
                            VerifyContentHash(blob, stream, containerName, blobName);
                        });

                    etag = blob.Properties.ETag;
                }
                catch (StorageException ex)
                {
                    if (IsNotFoundException(ex))
                    {
                        return Maybe<XElement>.Empty;
                    }

                    throw;
                }

                stream.Seek(0, SeekOrigin.Begin);
                var unpacked = formatter.TryUnpackXml(stream);
                return unpacked.IsSuccess ? unpacked.Value : Maybe<XElement>.Empty;
            }
        }

        /// <summary>As many parallel requests than there are blob names.</summary>
        public Maybe<T>[] GetBlobRange<T>(string containerName, string[] blobNames, out string[] etags, IDataSerializer serializer = null)
        {
            var dataSerializer = serializer ?? _defaultSerializer;
            var tempResult = blobNames.SelectInParallel(blobName =>
            {
                string etag;
                var blob = GetBlob<T>(containerName, blobName, out etag, dataSerializer);
                return new Tuple<Maybe<T>, string>(blob, etag);
            }, blobNames.Length);

            etags = new string[blobNames.Length];
            var result = new Maybe<T>[blobNames.Length];

            for (int i = 0; i < tempResult.Length; i++)
            {
                result[i] = tempResult[i].Item1;
                etags[i] = tempResult[i].Item2;
            }

            return result;
        }

        public Maybe<T> GetBlobIfModified<T>(string containerName, string blobName, string oldEtag, out string newEtag, IDataSerializer serializer = null)
        {
            var dataSerializer = serializer ?? _defaultSerializer;

            // 'oldEtag' is null, then behavior always match simple 'GetBlob'.
            if (null == oldEtag)
            {
                return GetBlob<T>(containerName, blobName, out newEtag, dataSerializer);
            }

            var stopwatch = Stopwatch.StartNew();

            newEtag = null;

            var container = _blobStorage.GetContainerReference(containerName);
            var blob = container.GetBlockBlobReference(blobName);

            try
            {
                using (var stream = new MemoryStream())
                {
                    Retry.Do(_policies.TransientServerErrorBackOff(), CancellationToken.None, () =>
                        {
                            stream.Seek(0, SeekOrigin.Begin);
                            blob.DownloadToStream(stream, accessCondition:AccessCondition.GenerateIfNoneMatchCondition(oldEtag));
                            VerifyContentHash(blob, stream, containerName, blobName);
                        });

                    newEtag = blob.Properties.ETag;

                    stream.Seek(0, SeekOrigin.Begin);
                    var deserialized = dataSerializer.TryDeserializeAs<T>(stream);

                    if (_observer != null)
                    {
                        if (!deserialized.IsSuccess)
                        {
                            _observer.Notify(new BlobDeserializationFailedEvent(deserialized.Error, containerName, blobName));
                        }
                        else
                        {
                            NotifySucceeded(StorageOperationType.BlobGetIfModified, stopwatch);
                        }
                    }

                    return deserialized.IsSuccess ? deserialized.Value : Maybe<T>.Empty;
                }
            }
            catch (StorageException ex)
            {
                // call fails because blob has not been modified (usual case)
                if (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotModified ||
                    // HACK: BUG in StorageClient 1.0 
                    // see http://social.msdn.microsoft.com/Forums/en-US/windowsazure/thread/4817cafa-12d8-4979-b6a7-7bda053e6b21
                    ex.Message == @"The condition specified using HTTP conditional header(s) is not met.")
                {
                    return Maybe<T>.Empty;
                }

                if (IsNotFoundException(ex))
                {
                    return Maybe<T>.Empty;
                }

                throw;
            }
        }

        public string GetBlobEtag(string containerName, string blobName)
        {
            var container = _blobStorage.GetContainerReference(containerName);

            try
            {
                var blob = container.GetBlockBlobReference(blobName);
                Retry.Do(_policies.TransientServerErrorBackOff(), CancellationToken.None, () => blob.FetchAttributes());
                return blob.Properties.ETag;
            }
            catch (StorageException ex)
            {
                if (IsNotFoundException(ex))
                {
                    return null;
                }

                throw;
            }
        }

        public Task<string> GetBlobEtagAsync(string containerName, string blobName, CancellationToken cancellationToken)
        {
            var completionSource = new TaskCompletionSource<string>();

            var container = _blobStorage.GetContainerReference(containerName);
            var blob = container.GetBlockBlobReference(blobName);

            Retry.Task(_policies.TransientServerErrorBackOff(), cancellationToken,
                () => Task.Factory.FromAsync(blob.BeginFetchAttributes, blob.EndFetchAttributes, null),
                () => completionSource.TrySetResult(blob.Properties.ETag),
                exception =>
                    {
                        if (IsNotFoundException(exception))
                        {
                            completionSource.TrySetResult(null);
                        }
                        else
                        {
                            NotifyFailed(StorageOperationType.BlobGet, exception);
                            completionSource.TrySetException(exception);
                        }
                    },
                () => completionSource.TrySetCanceled());

            return completionSource.Task;
        }

        public void PutBlob<T>(string containerName, string blobName, T item, IDataSerializer serializer = null)
        {
            PutBlob(containerName, blobName, item, true, serializer);
        }

        public bool PutBlob<T>(string containerName, string blobName, T item, bool overwrite, IDataSerializer serializer = null)
        {
            string ignored;
            return PutBlob(containerName, blobName, item, overwrite, out ignored, serializer);
        }

        public bool PutBlob<T>(string containerName, string blobName, T item, bool overwrite, out string etag, IDataSerializer serializer = null)
        {
            return PutBlob(containerName, blobName, item, typeof(T), overwrite, out etag, serializer);
        }

        public bool PutBlob<T>(string containerName, string blobName, T item, string expectedEtag, IDataSerializer serializer = null)
        {
            string outEtag;
            return PutBlob(containerName, blobName, item, typeof (T), true, expectedEtag, out outEtag, serializer);
        }

        public bool PutBlob(string containerName, string blobName, object item, Type type, bool overwrite, out string outEtag, IDataSerializer serializer = null)
        {
            return PutBlob(containerName, blobName, item, type, overwrite, null, out outEtag, serializer);
        }

        public bool PutBlob(string containerName, string blobName, object item, Type type, bool overwrite, string expectedEtag, out string outEtag, IDataSerializer serializer = null)
        {
            var stopwatch = Stopwatch.StartNew();

            using (var stream = new MemoryStream())
            {
                (serializer ?? _defaultSerializer).Serialize(item, stream, type);

                var container = _blobStorage.GetContainerReference(containerName);

                Func<Maybe<string>> doUpload = () =>
                {
                    var blob = container.GetBlockBlobReference(blobName);

                    // single remote call
                    var result = UploadBlobContent(blob, stream, overwrite, expectedEtag);

                    return result;
                };

                try
                {
                    var result = doUpload();
                    if (!result.HasValue)
                    {
                        outEtag = null;
                        return false;
                    }

                    outEtag = result.Value;
                    NotifySucceeded(StorageOperationType.BlobPut, stopwatch);
                    return true;
                }
                catch (StorageException ex)
                {
                    // if the container does not exist, it gets created
                    if (ex.RequestInformation.ExtendedErrorInformation.ErrorCode == BlobErrorCodeStrings.ContainerNotFound)
                    {
                        // caution: the container might have been freshly deleted
                        // (multiple retries are needed in such a situation)
                        var tentativeEtag = Maybe<string>.Empty;
                        Retry.Do(_policies.SlowInstantiation(), CancellationToken.None, () =>
                            {
                                Retry.Get(_policies.TransientServerErrorBackOff(), CancellationToken.None, () =>container.CreateIfNotExists());

                                tentativeEtag = doUpload();
                            });

                        if (!tentativeEtag.HasValue)
                        {
                            outEtag = null;
                            // success because it behaved as excpected - the expected etag was not matching so it was not overwritten
                            NotifySucceeded(StorageOperationType.BlobPut, stopwatch);
                            return false;
                        }

                        outEtag = tentativeEtag.Value;
                        NotifySucceeded(StorageOperationType.BlobPut, stopwatch);
                        return true;
                    }

                    if (ex.RequestInformation.ExtendedErrorInformation.ErrorCode == BlobErrorCodeStrings.BlobAlreadyExists && !overwrite)
                    {
                        // See http://social.msdn.microsoft.com/Forums/en-US/windowsazure/thread/fff78a35-3242-4186-8aee-90d27fbfbfd4
                        // and http://social.msdn.microsoft.com/Forums/en-US/windowsazure/thread/86b9f184-c329-4c30-928f-2991f31e904b/

                        outEtag = null;
                        // success because it behaved as excpected - the expected etag was not matching so it was not overwritten
                        NotifySucceeded(StorageOperationType.BlobPut, stopwatch);
                        return false;
                    }

                    var result = doUpload();
                    if (!result.HasValue)
                    {
                        outEtag = null;
                        // success because it behaved as excpected - the expected etag was not matching so it was not overwritten
                        NotifySucceeded(StorageOperationType.BlobPut, stopwatch);
                        return false;
                    }

                    outEtag = result.Value;
                    NotifySucceeded(StorageOperationType.BlobPut, stopwatch);
                    return true;
                }
            }
        }

        /// <returns>Task with the resulting ETag (or null if not written).</returns>
        public Task<string> PutBlobAsync(string containerName, string blobName, object item, Type type, bool overwrite, string expectedEtag, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            var stopwatch = Stopwatch.StartNew();
            var completionSource = new TaskCompletionSource<string>();

            var container = _blobStorage.GetContainerReference(containerName);
            var blob = container.GetBlockBlobReference(blobName);

            var stream = new MemoryStream();
            completionSource.Task.ContinueWith(t => stream.Dispose());

            (serializer ?? _defaultSerializer).Serialize(item, stream, type);
            ApplyContentHash(blob, stream);

            BlobRequestOptions options;
            AccessCondition accessCondition;
            if (!overwrite) // no overwrite authorized, blob must NOT exists
            {
                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(DateTime.MinValue);
            }
            else // overwrite is OK
            {
                accessCondition = string.IsNullOrEmpty(expectedEtag)
                                      ? // case with no etag constraint
                                      AccessCondition.GenerateIfNoneMatchCondition(expectedEtag)
                                      : // case with etag constraint
                                      AccessCondition.GenerateIfMatchCondition(expectedEtag);
            }

            Retry.Task(_policies.TransientServerErrorBackOff(), cancellationToken,
                () =>
                    {
                        stream.Seek(0, SeekOrigin.Begin);

                        var runWithAccessCondition = blob.BeginUploadFromStream(stream, accessCondition, null, null, null, null);
                        return Task.Factory.FromAsync(runWithAccessCondition, blob.EndUploadFromStream);
                    },
                () =>
                    {
                        // "return true"
                        NotifySucceeded(StorageOperationType.BlobPut, stopwatch);
                        completionSource.TrySetResult(blob.Properties.ETag);
                    },
                exception =>
                    {
                        if (exception is AggregateException)
                        {
                            exception = exception.GetBaseException();
                        }

                        var storageClientException = exception as StorageException;
                        if (storageClientException != null)
                        {
                            if (storageClientException.RequestInformation.ExtendedErrorInformation.ErrorCode == StorageErrorCodeStrings.ConditionNotMet)
                            {
                                // "return false"; success because it behaved as excpected - the expected etag was not matching so it was not overwritten
                                completionSource.TrySetResult(null);
                                NotifySucceeded(StorageOperationType.BlobPut, stopwatch);
                                return;
                            }

                            if (storageClientException.RequestInformation.ExtendedErrorInformation.ErrorCode == BlobErrorCodeStrings.BlobAlreadyExists
                                && !overwrite)
                            {
                                // See http://social.msdn.microsoft.com/Forums/en-US/windowsazure/thread/fff78a35-3242-4186-8aee-90d27fbfbfd4
                                // and http://social.msdn.microsoft.com/Forums/en-US/windowsazure/thread/86b9f184-c329-4c30-928f-2991f31e904b/

                                // "return false"; success because it behaved as excpected - the expected etag was not matching so it was not overwritten
                                completionSource.TrySetResult(null);
                                NotifySucceeded(StorageOperationType.BlobPut, stopwatch);
                                return;
                            }

                            // if the container does not exist, it gets created
                            if (storageClientException.RequestInformation.ExtendedErrorInformation.ErrorCode == BlobErrorCodeStrings.ContainerNotFound)
                            {
                                Retry.Task(_policies.SlowInstantiation(), cancellationToken, () =>
                                    {
                                        Retry.Get(_policies.TransientServerErrorBackOff(), CancellationToken.None, () => container.CreateIfNotExists());
                                        return Retry.TaskAsTask(_policies.TransientServerErrorBackOff(), cancellationToken,
                                            () =>
                                                {
                                                    stream.Seek(0, SeekOrigin.Begin);
                                                    return Task.Factory.FromAsync(blob.BeginUploadFromStream, blob.EndUploadFromStream, stream, accessCondition);
                                                },
                                            () => blob.Properties.ETag);
                                    },
                                    etag =>
                                        {
                                            completionSource.TrySetResult(etag);
                                            NotifySucceeded(StorageOperationType.BlobPut, stopwatch);
                                        },
                                    e =>
                                        {
                                            if (e is AggregateException)
                                            {
                                                e = e.GetBaseException();
                                            }

                                            var sce = e as StorageException;
                                            if (sce != null && sce.RequestInformation.ExtendedErrorInformation.ErrorCode 
                                                == StorageErrorCodeStrings.ConditionNotMet)
                                            {
                                                // "return false"; success because it behaved as excpected - the expected etag was not matching so it was not overwritten
                                                completionSource.TrySetResult(null);
                                                NotifySucceeded(StorageOperationType.BlobPut, stopwatch);
                                            }
                                            else
                                            {
                                                completionSource.TrySetException(e);
                                                NotifyFailed(StorageOperationType.BlobPut, e);
                                            }
                                        },
                                    () => completionSource.TrySetCanceled());
                                return;
                            }
                        }

                        completionSource.TrySetException(exception);
                        NotifyFailed(StorageOperationType.BlobPut, exception);
                    },
                () => completionSource.TrySetCanceled());

            return completionSource.Task;
        }

        /// <param name="blob"></param>
        /// <param name="stream"></param>
        /// <param name="overwrite">If <c>false</c>, then no write happens if the blob already exists.</param>
        /// <param name="expectedEtag">When specified, no writing occurs unless the blob etag
        /// matches the one specified as argument.</param>
        /// <returns>The ETag of the written blob, if it was written.</returns>
        Maybe<string> UploadBlobContent(CloudBlockBlob blob, Stream stream, bool overwrite, string expectedEtag)
        {
            AccessCondition accessCondition;

            if (!overwrite) // no overwrite authorized, blob must NOT exists
            {
                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(DateTime.MinValue);
            }
            else // overwrite is OK
            {
                accessCondition = string.IsNullOrEmpty(expectedEtag) ?
                    // case with no etag constraint
                    AccessCondition.GenerateIfNoneMatchCondition(expectedEtag) :
                                                                                        // case with etag constraint
                    AccessCondition.GenerateIfMatchCondition(expectedEtag);
            }

            ApplyContentHash(blob, stream);

            try
            {
                Retry.Do(_policies.TransientServerErrorBackOff(), CancellationToken.None, () =>
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        blob.UploadFromStream(stream, accessCondition);
                    });
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.ExtendedErrorInformation.ErrorCode == StorageErrorCodeStrings.ConditionNotMet)
                {
                    return Maybe<string>.Empty;
                }

                throw;
            }

            return blob.Properties.ETag;
        }

        public Maybe<T> UpdateBlobIfExist<T>(
            string containerName, string blobName, Func<T, T> update, IDataSerializer serializer = null)
        {
            return UpsertBlobOrSkip(containerName, blobName, () => Maybe<T>.Empty, t => update(t), serializer);
        }

        public Maybe<T> UpdateBlobIfExistOrSkip<T>(
            string containerName, string blobName, Func<T, Maybe<T>> update, IDataSerializer serializer = null)
        {
            return UpsertBlobOrSkip(containerName, blobName, () => Maybe<T>.Empty, update, serializer);
        }

        public Maybe<T> UpdateBlobIfExistOrDelete<T>(
            string containerName, string blobName, Func<T, Maybe<T>> update, IDataSerializer serializer = null)
        {
            var result = UpsertBlobOrSkip(containerName, blobName, () => Maybe<T>.Empty, update, serializer);
            if (!result.HasValue)
            {
                DeleteBlobIfExist(containerName, blobName);
            }

            return result;
        }

        public T UpsertBlob<T>(string containerName, string blobName, Func<T> insert, Func<T, T> update, IDataSerializer serializer = null)
        {
            return UpsertBlobOrSkip<T>(containerName, blobName, () => insert(), t => update(t), serializer).Value;
        }

        public Maybe<T> UpsertBlobOrSkip<T>(
            string containerName, string blobName, Func<Maybe<T>> insert, Func<T, Maybe<T>> update, IDataSerializer serializer = null)
        {
            var stopwatch = Stopwatch.StartNew();
            var dataSerializer = serializer ?? _defaultSerializer;

            var container = _blobStorage.GetContainerReference(containerName);
            var blob = container.GetBlockBlobReference(blobName);

            Maybe<T> output;

            var optimisticPolicy = _policies.OptimisticConcurrency();
            TimeSpan retryInterval = TimeSpan.Zero;
            int retryCount = 0;
            do
            {
                // 0. IN CASE OF RETRIAL, WAIT UNTIL NEXT TRIAL (retry policy)

                if (retryInterval > TimeSpan.Zero)
                {
                    Thread.Sleep(retryInterval);
                }

                // 1. DOWNLOAD EXISTING INPUT BLOB, IF IT EXISTS

                Maybe<T> input;
                bool inputBlobExists = false;
                string inputETag = null;

                try
                {
                    using (var readStream = new MemoryStream())
                    {
                        Retry.Do(_policies.TransientServerErrorBackOff(), CancellationToken.None, () =>
                            {
                                readStream.Seek(0, SeekOrigin.Begin);
                                blob.DownloadToStream(readStream);
                                VerifyContentHash(blob, readStream, containerName, blobName);
                            });

                        inputETag = blob.Properties.ETag;
                        inputBlobExists = !String.IsNullOrEmpty(inputETag);

                        readStream.Seek(0, SeekOrigin.Begin);

                        var deserialized = dataSerializer.TryDeserializeAs<T>(readStream);
                        if (!deserialized.IsSuccess && _observer != null)
                        {
                            _observer.Notify(new BlobDeserializationFailedEvent(deserialized.Error, containerName, blobName));
                        }

                        input = deserialized.IsSuccess ? deserialized.Value : Maybe<T>.Empty;
                    }
                }
                catch (StorageException ex)
                {
                    // creating the container when missing
                    if (IsNotFoundException(ex))
                    {
                        input = Maybe<T>.Empty;

                        // caution: the container might have been freshly deleted
                        // (multiple retries are needed in such a situation)
                        Retry.Get(_policies.SlowInstantiation(), _policies.TransientServerErrorBackOff(), CancellationToken.None, () => container.CreateIfNotExists());
                    }
                    else
                    {
                        throw;
                    }
                }

                // 2. APPLY UPADTE OR INSERT (DEPENDING ON INPUT)

                output = input.HasValue ? update(input.Value) : insert();

                // 3. IF EMPTY OUTPUT THEN WE CAN SKIP THE WHOLE OPERATION

                if (!output.HasValue)
                {
                    NotifySucceeded(StorageOperationType.BlobUpsertOrSkip, stopwatch);
                    return output;
                }

                // 4. TRY TO INSERT OR UPDATE BLOB

                using (var writeStream = new MemoryStream())
                {
                    dataSerializer.Serialize(output.Value, writeStream, typeof(T));
                    writeStream.Seek(0, SeekOrigin.Begin);

                    // Semantics:
                    // Insert: Blob must not exist -> do not overwrite
                    // Update: Blob must exists -> overwrite and verify matching ETag

                    bool succeeded = inputBlobExists
                        ? UploadBlobContent(blob, writeStream, true, inputETag).HasValue
                        : UploadBlobContent(blob, writeStream, false, null).HasValue;

                    if (succeeded)
                    {
                        NotifySucceeded(StorageOperationType.BlobUpsertOrSkip, stopwatch);
                        return output;
                    }
                }
            } while (optimisticPolicy.ShouldRetry(retryCount++, 0, null, out retryInterval,null));

            throw new TimeoutException("Failed to resolve optimistic concurrency errors within a limited number of retrials");
        }

        public Task<BlobWithETag<T>> UpsertBlobOrSkipAsync<T>(string containerName, string blobName,
            Func<Maybe<T>> insert, Func<T, Maybe<T>> update, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            var stopwatch = Stopwatch.StartNew();
            return Retry.TaskAsTask(_policies.OptimisticConcurrency(), cancellationToken,
                () => GetBlobAsync(containerName, blobName, typeof(T), cancellationToken, serializer)
                    .Then(b =>
                        {
                            var output = (b == null) ? insert() : update((T)b.Blob);
                            if (!output.HasValue)
                            {
                                NotifySucceeded(StorageOperationType.BlobUpsertOrSkip, stopwatch);
                                return TaskAsyncHelper.FromResult(default(BlobWithETag<T>));
                            }

                            var putTask = (b == null)
                                ? PutBlobAsync(containerName, blobName, output.Value, typeof(T), false, null, cancellationToken, serializer)
                                : PutBlobAsync(containerName, blobName, output.Value, typeof(T), true, b.ETag, cancellationToken, serializer);
                            return putTask.Then(etag =>
                                {
                                    if (etag == null)
                                    {
                                        throw new ConcurrencyException();
                                    }

                                    NotifySucceeded(StorageOperationType.BlobUpsertOrSkip, stopwatch);
                                    return new BlobWithETag<T> { Blob = output.Value, ETag = etag };
                                });
                        }));
        }

        public Maybe<T> UpsertBlobOrDelete<T>(
            string containerName, string blobName, Func<Maybe<T>> insert, Func<T, Maybe<T>> update, IDataSerializer serializer = null)
        {
            var result = UpsertBlobOrSkip(containerName, blobName, insert, update, serializer);
            if (!result.HasValue)
            {
                DeleteBlobIfExist(containerName, blobName);
            }

            return result;
        }

        private static string ComputeContentHash(Stream source)
        {
            byte[] hash;
            source.Seek(0, SeekOrigin.Begin);
            using (var md5 = MD5.Create())
            {
                hash = md5.ComputeHash(source);
            }

            source.Seek(0, SeekOrigin.Begin);
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Apply a content hash to the blob to verify upload and roundtrip consistency.
        /// </summary>
        private static void ApplyContentHash(CloudBlockBlob blob, Stream stream)
        {
            var hash = ComputeContentHash(stream);

            // HACK: [Vermorel 2010-11] StorageClient does not apply MD5 on smaller blobs.
            // Reflector indicates that the behavior threshold is at 32MB
            // so manually disable hasing for larger blobs
            if (stream.Length < 0x2000000L)
            {
                blob.Properties.ContentMD5 = hash;
            }

            // HACK: [vermorel 2010-11] StorageClient does not provide a way to retrieve
            // MD5 so we add our own MD5 check which let perform our own validation when
            // downloading the blob (full roundtrip validation). 
            blob.Metadata[MetadataMD5Key] = hash;
        }

        /// <summary>
        /// Throws a DataCorruptionException if the content hash is available but doesn't match.
        /// </summary>
        private static void VerifyContentHash(CloudBlockBlob blob, Stream stream, string containerName, string blobName)
        {
            var expectedHash = blob.Metadata[MetadataMD5Key];
            if (string.IsNullOrEmpty(expectedHash))
            {
                return;
            }

            if (expectedHash != ComputeContentHash(stream))
            {
                throw new DataCorruptionException(
                    string.Format("MD5 mismatch on blob retrieval {0}/{1}.", containerName, blobName));
            }
        }

        public bool IsBlobLocked(string containerName, string blobName)
        {
            var container = _blobStorage.GetContainerReference(containerName);

            try
            {
                var blob = container.GetBlockBlobReference(blobName);
                Retry.Do(_policies.TransientServerErrorBackOff(), CancellationToken.None, () => blob.FetchAttributes());
                return blob.Properties.LeaseStatus == LeaseStatus.Locked;
            }
            catch (StorageException ex)
            {
                if (IsNotFoundException(ex))
                {
                    return false;
                }

                throw;
            }
        }

        public Result<string> TryAcquireLease(string containerName, string blobName)
        {
            var container = _blobStorage.GetContainerReference(containerName);
            var blob = container.GetBlockBlobReference(blobName);
            // Lease Time for 1.8 Max is 60 seconds
            var leaseTime = TimeSpan.FromSeconds(60);

            string leaseId;
            try
            {
                leaseId = blob.AcquireLease(leaseTime, null);
            }
            catch (StorageException se)
            {
                var statusCode = (HttpStatusCode)se.RequestInformation.HttpStatusCode ;

                switch (statusCode)
                {
                    case HttpStatusCode.Conflict:
                    case HttpStatusCode.NotFound:
                    case HttpStatusCode.RequestTimeout:
                    case HttpStatusCode.InternalServerError:
                        return Result<string>.CreateError(statusCode.ToString());
                    default:
                        throw;
                }
            }
            return Result<string>.CreateSuccess(leaseId);
        }

        public Result<string> TryReleaseLease(string containerName, string blobName, string leaseId)
        {
            return TryLeaseAction(containerName, blobName, LeaseAction.Release, leaseId);
        }

        public Result<string> TryRenewLease(string containerName, string blobName, string leaseId)
        {
            return TryLeaseAction(containerName, blobName, LeaseAction.Renew, leaseId);
        }

        private Result<string> TryLeaseAction(string containerName, string blobName, LeaseAction action, string leaseId = null)
        {
            var container = _blobStorage.GetContainerReference(containerName);
            var blob = container.GetBlockBlobReference(blobName);
            var ninetySeconds = TimeSpan.FromSeconds(90);
            try
            {
                if (action == LeaseAction.Release)
                {
                    blob.ReleaseLease(AccessCondition.GenerateLeaseCondition(leaseId), new BlobRequestOptions { ServerTimeout = ninetySeconds });
                }
                else if (action == LeaseAction.Renew)
                {
                    blob.RenewLease(AccessCondition.GenerateLeaseCondition(leaseId), new BlobRequestOptions { ServerTimeout = ninetySeconds });
                }
            }
            catch (StorageException se)
            {
                var statusCode = (HttpStatusCode)se.RequestInformation.HttpStatusCode;

                switch(statusCode)
                {
                    case HttpStatusCode.Conflict:
                    case HttpStatusCode.NotFound:
                    case HttpStatusCode.RequestTimeout:
                    case HttpStatusCode.InternalServerError:
                        return Result<string>.CreateError(statusCode.ToString());
                    default:
                        throw;
                }
            }
            return Result<string>.CreateSuccess("OK");
        }

        private static bool IsNotFoundException(Exception exception)
        {
            if (exception is AggregateException)
            {
                exception = exception.GetBaseException();
            }

            var sce = exception as StorageException;

            return sce != null &&
                (
                    sce.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound &&
                    sce.RequestInformation.HttpStatusMessage.Contains("The specified blob does not exist.") == true
                )
                ||
                (
                    sce.RequestInformation.ExtendedErrorInformation.ErrorCode == StorageErrorCodeStrings.ContainerNotFound ||
                    sce.RequestInformation.ExtendedErrorInformation.ErrorCode == BlobErrorCodeStrings.BlobNotFound ||
                    sce.RequestInformation.ExtendedErrorInformation.ErrorCode == StorageErrorCodeStrings.ResourceNotFound
                );
        }

        private void NotifySucceeded(StorageOperationType operationType, Stopwatch stopwatch)
        {
            if (_observer != null)
            {
                _observer.Notify(new StorageOperationSucceededEvent(operationType, stopwatch.Elapsed));
            }
        }

        private void NotifyFailed(StorageOperationType operationType, Exception exception)
        {
            if (_observer != null)
            {
                _observer.Notify(new StorageOperationFailedEvent(operationType, exception));
            }
        }
    }
}