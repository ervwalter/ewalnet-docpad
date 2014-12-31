#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Lokad.Cloud.Storage.InMemory
{
    /// <summary>Mock in-memory Blob Storage.</summary>
    /// <remarks>
    /// All the methods of <see cref="MemoryBlobStorageProvider"/> are thread-safe.
    /// Note that the blob lease implementation is simplified such that leases do not time out.
    /// </remarks>
    public class MemoryBlobStorageProvider : IBlobStorageProvider
    {
        /// <summary> Containers Property.</summary>
        Dictionary<string, MockContainer> Containers { get { return _containers;} }
        readonly Dictionary<string, MockContainer> _containers;
        
        /// <summary>naive global lock to make methods thread-safe.</summary>
        readonly object _syncRoot;

        internal IDataSerializer DefaultSerializer { get; set; }

        /// <remarks></remarks>
        public MemoryBlobStorageProvider(IDataSerializer defaultSerializer = null)
        {
            _containers = new Dictionary<string, MockContainer>();
            _syncRoot = new object();
            DefaultSerializer = defaultSerializer ?? new CloudFormatter();
        }

        /// <remarks></remarks>
        public IEnumerable<string> ListContainers(string prefix = null)
        {
            lock (_syncRoot)
            {
                if (String.IsNullOrEmpty(prefix))
                {
                    return Containers.Keys;
                }

                return Containers.Keys.Where(key => key.StartsWith(prefix));
            }
        }

        /// <remarks></remarks>
        public bool CreateContainerIfNotExist(string containerName)
        {
            lock (_syncRoot)
            {
                if (!BlobStorageExtensions.IsContainerNameValid(containerName))
                {
                    throw new NotSupportedException("the containerName is not compliant with azure constraints on container names");
                }

                if (Containers.Keys.Contains(containerName))
                {
                    return false;
                }
                
                Containers.Add(containerName, new MockContainer());
                return true;
            }	
        }

        /// <remarks></remarks>
        public bool DeleteContainerIfExist(string containerName)
        {
            lock (_syncRoot)
            {
                if (!Containers.Keys.Contains(containerName))
                {
                    return false;
                }

                Containers.Remove(containerName);
                return true;
            }
        }

        /// <remarks></remarks>
        public IEnumerable<string> ListBlobNames(string containerName, string blobNamePrefix = null)
        {
            lock (_syncRoot)
            {
                if (!Containers.Keys.Contains(containerName))
                {
                    return Enumerable.Empty<string>();
                }

                var names = Containers[containerName].BlobNames;
                return String.IsNullOrEmpty(blobNamePrefix) ? names : names.Where(name => name.StartsWith(blobNamePrefix));
            }
        }

        /// <remarks></remarks>
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

        /// <remarks></remarks>
        public bool DeleteBlobIfExist(string containerName, string blobName)
        {
            lock (_syncRoot)
            {
                if (!Containers.Keys.Contains(containerName) || !Containers[containerName].BlobNames.Contains(blobName))
                {
                    return false;
                }

                Containers[containerName].RemoveBlob(blobName);
                return true;
            }
        }

        /// <remarks></remarks>
        public void DeleteAllBlobs(string containerName, string blobNamePrefix = null)
        {
            foreach (var blobName in ListBlobNames(containerName, blobNamePrefix))
            {
                DeleteBlobIfExist(containerName, blobName);
            }
        }

        /// <remarks></remarks>
        public Maybe<T> GetBlob<T>(string containerName, string blobName, IDataSerializer serializer = null)
        {
            string ignoredEtag;
            return GetBlob<T>(containerName, blobName, out ignoredEtag, serializer);
        }

        /// <remarks></remarks>
        public Maybe<T> GetBlob<T>(string containerName, string blobName, out string etag, IDataSerializer serializer = null)
        {
            return GetBlob(containerName, blobName, typeof(T), out etag, serializer)
                .Convert(o => o is T ? (T)o : Maybe<T>.Empty, Maybe<T>.Empty);
        }

        /// <remarks></remarks>
        public Maybe<object> GetBlob(string containerName, string blobName, Type type, out string etag, IDataSerializer serializer = null)
        {
            lock (_syncRoot)
            {
                if (!Containers.ContainsKey(containerName)
                    || !Containers[containerName].BlobNames.Contains(blobName))
                {
                    etag = null;
                    return Maybe<object>.Empty;
                }

                etag = Containers[containerName].BlobsEtag[blobName];
                return Containers[containerName].GetBlob(blobName);
            }
        }

        public Task<BlobWithETag<object>> GetBlobAsync(string containerName, string blobName, Type type, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            return TaskAsyncHelper.FromMethod(
                () =>
                    {
                        string etag;
                        var blob = GetBlob(containerName, blobName, type, out etag, serializer);
                        return blob.Convert(o => new BlobWithETag<object> { Blob = o, ETag = etag }, () => default(BlobWithETag<object>));
                    });
        }

        /// <remarks></remarks>
        public Maybe<XElement> GetBlobXml(string containerName, string blobName, out string etag, IDataSerializer serializer = null)
        {
            etag = null;

            var formatter = (serializer ?? DefaultSerializer) as IIntermediateDataSerializer;
            if (formatter == null)
            {
                return Maybe<XElement>.Empty;
            }

            object data;
            lock (_syncRoot)
            {
                if (!Containers.ContainsKey(containerName)
                    || !Containers[containerName].BlobNames.Contains(blobName))
                {
                    return Maybe<XElement>.Empty;
                }

                etag = Containers[containerName].BlobsEtag[blobName];
                data = Containers[containerName].GetBlob(blobName);
            }

            using (var stream = new MemoryStream())
            {
                formatter.Serialize(data, stream, data.GetType());
                stream.Position = 0;
                return formatter.UnpackXml(stream);
            }
        }

        /// <remarks></remarks>
        public Maybe<T>[] GetBlobRange<T>(string containerName, string[] blobNames, out string[] etags, IDataSerializer serializer = null)
        {
            var tempResult = blobNames.Select(blobName =>
            {
                string etag;
                var blob = GetBlob<T>(containerName, blobName, out etag);
                return new Tuple<Maybe<T>, string>(blob, etag);
            }).ToArray();

            etags = new string[blobNames.Length];
            var result = new Maybe<T>[blobNames.Length];

            for (int i = 0; i < tempResult.Length; i++)
            {
                result[i] = tempResult[i].Item1;
                etags[i] = tempResult[i].Item2;
            }

            return result;
        }

        /// <remarks></remarks>
        public Maybe<T> GetBlobIfModified<T>(string containerName, string blobName, string oldEtag, out string newEtag, IDataSerializer serializer = null)
        {
            lock (_syncRoot)
            {
                string currentEtag = GetBlobEtag(containerName, blobName);

                if (currentEtag == oldEtag)
                {
                    newEtag = null;
                    return Maybe<T>.Empty;
                }

                newEtag = currentEtag;
                return GetBlob<T>(containerName, blobName, serializer);
            }
        }

        /// <remarks></remarks>
        public string GetBlobEtag(string containerName, string blobName)
        {
            lock (_syncRoot)
            {
                return (Containers.ContainsKey(containerName) && Containers[containerName].BlobNames.Contains(blobName))
                    ? Containers[containerName].BlobsEtag[blobName]
                    : null;
            }
        }

        public Task<string> GetBlobEtagAsync(string containerName, string blobName, CancellationToken cancellationToken)
        {
            return TaskAsyncHelper.FromMethod(() => GetBlobEtag(containerName, blobName));
        }

        /// <remarks></remarks>
        public void PutBlob<T>(string containerName, string blobName, T item, IDataSerializer serializer = null)
        {
            PutBlob(containerName, blobName, item, true, serializer);
        }

        /// <remarks></remarks>
        public bool PutBlob<T>(string containerName, string blobName, T item, bool overwrite, IDataSerializer serializer = null)
        {
            string ignored;
            return PutBlob(containerName, blobName, item, overwrite, out ignored, serializer);
        }

        /// <remarks></remarks>
        public bool PutBlob<T>(string containerName, string blobName, T item, bool overwrite, out string etag, IDataSerializer serializer = null)
        {
            return PutBlob(containerName, blobName, item, typeof(T), overwrite, out etag, serializer);
        }

        /// <remarks></remarks>
        public bool PutBlob<T>(string containerName, string blobName, T item, string expectedEtag, IDataSerializer serializer = null)
        {
            string ignored;
            return PutBlob(containerName, blobName, item, typeof (T), true, expectedEtag, out ignored, serializer);
        }

        /// <remarks></remarks>
        public bool PutBlob(string containerName, string blobName, object item, Type type, bool overwrite, out string etag, IDataSerializer serializer = null)
        {
            return PutBlob(containerName, blobName, item, type, overwrite, null, out etag, serializer);
        }

        /// <remarks></remarks>
        public bool PutBlob(string containerName, string blobName, object item, Type type, bool overwrite, string expectedEtag, out string etag, IDataSerializer serializer = null)
        {
            var dataSerializer = serializer ?? DefaultSerializer;
            lock(_syncRoot)
            {
                etag = null;
                if(Containers.ContainsKey(containerName))
                {
                    if(Containers[containerName].BlobNames.Contains(blobName))
                    {
                        if(!overwrite || expectedEtag != null && expectedEtag != Containers[containerName].BlobsEtag[blobName])
                        {
                            return false;
                        }

                        // Just verify that we can serialize
                        using (var stream = new MemoryStream())
                        {
                            dataSerializer.Serialize(item, stream, type);
                        }

                        Containers[containerName].SetBlob(blobName, item);
                        etag = Containers[containerName].BlobsEtag[blobName];
                        return true;
                    }

                    Containers[containerName].AddBlob(blobName, item);
                    etag = Containers[containerName].BlobsEtag[blobName];
                    return true;
                }

                if (!BlobStorageExtensions.IsContainerNameValid(containerName))
                {
                    throw new NotSupportedException("the containerName is not compliant with azure constraints on container names");
                }

                Containers.Add(containerName, new MockContainer());

                using (var stream = new MemoryStream())
                {
                    dataSerializer.Serialize(item, stream, type);
                }

                Containers[containerName].AddBlob(blobName, item);
                etag = Containers[containerName].BlobsEtag[blobName];
                return true;
            }
        }

        public Task<string> PutBlobAsync(string containerName, string blobName, object item, Type type, bool overwrite, string expectedEtag, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            return TaskAsyncHelper.FromMethod(
                () =>
                    {
                        string etag;
                        PutBlob(containerName, blobName, item, type, overwrite, expectedEtag, out etag, serializer);
                        return etag;
                    });
        }

        /// <remarks></remarks>
        public Maybe<T> UpdateBlobIfExist<T>(string containerName, string blobName, Func<T, T> update, IDataSerializer serializer = null)
        {
            return UpsertBlobOrSkip(containerName, blobName, () => Maybe<T>.Empty, t => update(t), serializer);
        }

        /// <remarks></remarks>
        public Maybe<T> UpdateBlobIfExistOrSkip<T>(string containerName, string blobName, Func<T, Maybe<T>> update, IDataSerializer serializer = null)
        {
            return UpsertBlobOrSkip(containerName, blobName, () => Maybe<T>.Empty, update, serializer);
        }

        /// <remarks></remarks>
        public Maybe<T> UpdateBlobIfExistOrDelete<T>(string containerName, string blobName, Func<T, Maybe<T>> update, IDataSerializer serializer = null)
        {
            var result = UpsertBlobOrSkip(containerName, blobName, () => Maybe<T>.Empty, update, serializer);
            if (!result.HasValue)
            {
                DeleteBlobIfExist(containerName, blobName);
            }

            return result;
        }

        /// <remarks></remarks>
        public T UpsertBlob<T>(string containerName, string blobName, Func<T> insert, Func<T, T> update, IDataSerializer serializer = null)
        {
            return UpsertBlobOrSkip<T>(containerName, blobName, () => insert(), t => update(t), serializer).Value;
        }

        /// <remarks></remarks>
        public Maybe<T> UpsertBlobOrSkip<T>(
            string containerName, string blobName, Func<Maybe<T>> insert, Func<T, Maybe<T>> update, IDataSerializer serializer = null)
        {
            lock (_syncRoot)
            {
                Maybe<T> input;
                if (Containers.ContainsKey(containerName))
                {
                    if (Containers[containerName].BlobNames.Contains(blobName))
                    {
                        var blobData = Containers[containerName].GetBlob(blobName);
                        input = blobData == null ? Maybe<T>.Empty : (T)blobData;
                    }
                    else
                    {
                        input = Maybe<T>.Empty;
                    }
                }
                else
                {
                    Containers.Add(containerName, new MockContainer());
                    input = Maybe<T>.Empty;
                }

                var output = input.HasValue ? update(input.Value) : insert();

                if (output.HasValue)
                {
                    Containers[containerName].SetBlob(blobName, output.Value);
                }

                return output;
            }
        }

        public Task<BlobWithETag<T>> UpsertBlobOrSkipAsync<T>(string containerName, string blobName,
            Func<Maybe<T>> insert, Func<T, Maybe<T>> update, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            lock (_syncRoot)
            {
                return GetBlobAsync(containerName, blobName, typeof(T), cancellationToken, serializer)
                    .Then(b =>
                    {
                        var output = (b == null) ? insert() : update((T)b.Blob);
                        if (!output.HasValue)
                        {
                            return TaskAsyncHelper.FromResult(default(BlobWithETag<T>));
                        }

                        var putTask = (b == null)
                            ? PutBlobAsync(containerName, blobName, output.Value, typeof(T), false, null, cancellationToken, serializer)
                            : PutBlobAsync(containerName, blobName, output.Value, typeof(T), true, b.ETag, cancellationToken, serializer);
                        return putTask.Then(etag => new BlobWithETag<T> { Blob = output.Value, ETag = etag });
                    });
            }
        }

        /// <remarks></remarks>
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

        class MockContainer
        {
            readonly Dictionary<string, object> _blobSet;
            readonly Dictionary<string, string> _blobsEtag;
            readonly Dictionary<string, string> _blobsLeases;

            public string[] BlobNames { get { return _blobSet.Keys.ToArray(); } }

            public Dictionary<string, string> BlobsEtag { get { return _blobsEtag; } }
            public Dictionary<string, string> BlobsLeases { get { return _blobsLeases; } }

            public MockContainer()
            {
                _blobSet = new Dictionary<string, object>();
                _blobsEtag = new Dictionary<string, string>();
                _blobsLeases = new Dictionary<string, string>();
            }

            public void SetBlob(string blobName, object item)
            {
                _blobSet[blobName] = item;
                _blobsEtag[blobName] = Guid.NewGuid().ToString();
            }

            public object GetBlob(string blobName)
            {
                return _blobSet[blobName];
            }

            public void AddBlob(string blobName, object item)
            {
                _blobSet.Add(blobName, item);
                _blobsEtag.Add(blobName, Guid.NewGuid().ToString());
            }

            public void RemoveBlob(string blobName)
            {
                _blobSet.Remove(blobName);
                _blobsEtag.Remove(blobName);
                _blobsLeases.Remove(blobName);
            }
        }

        /// <remarks></remarks>
        public bool IsBlobLocked(string containerName, string blobName)
        {
            lock (_syncRoot)
            {
                return (Containers.ContainsKey(containerName)
                    && Containers[containerName].BlobNames.Contains(blobName))
                    && Containers[containerName].BlobsLeases.ContainsKey(blobName);
            }
        }

        /// <remarks></remarks>
        public Result<string> TryAcquireLease(string containerName, string blobName)
        {
            lock (_syncRoot)
            {
                if (!Containers[containerName].BlobsLeases.ContainsKey(blobName))
                {
                    var leaseId = Guid.NewGuid().ToString("N");
                    Containers[containerName].BlobsLeases[blobName] = leaseId;
                    return Result.CreateSuccess(leaseId);
                }

                return Result<string>.CreateError("Conflict");
            }
        }

        /// <remarks></remarks>
        public Result<string> TryReleaseLease(string containerName, string blobName, string leaseId)
        {
            lock (_syncRoot)
            {
                string actualLeaseId;
                if (!Containers[containerName].BlobsLeases.TryGetValue(blobName, out actualLeaseId))
                {
                    return Result<string>.CreateError("NotFound");
                }
                if (actualLeaseId != leaseId)
                {
                    return Result<string>.CreateError("Conflict");
                }

                Containers[containerName].BlobsLeases.Remove(blobName);
                return Result.CreateSuccess("OK");
            }
        }

        /// <remarks></remarks>
        public Result<string> TryRenewLease(string containerName, string blobName, string leaseId)
        {
            lock (_syncRoot)
            {
                string actualLeaseId;
                if (!Containers[containerName].BlobsLeases.TryGetValue(blobName, out actualLeaseId))
                {
                    return Result<string>.CreateError("NotFound");
                }
                if (actualLeaseId != leaseId)
                {
                    return Result<string>.CreateError("Conflict");
                }

                return Result.CreateSuccess("OK");
            }
        }
    }
}
