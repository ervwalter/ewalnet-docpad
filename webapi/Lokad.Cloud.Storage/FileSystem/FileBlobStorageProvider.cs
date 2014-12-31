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

namespace Lokad.Cloud.Storage.FileSystem
{
    public class FileBlobStorageProvider : IBlobStorageProvider
    {
        readonly string _root;
        readonly IDataSerializer _defaultSerializer;
        readonly RetryPolicies _policies;

        public FileBlobStorageProvider(string rootPath, IDataSerializer defaultSerializer = null)
        {
            _root = rootPath;
            _defaultSerializer = defaultSerializer ?? new CloudFormatter();
            _policies = new RetryPolicies();

            if (!Directory.Exists(rootPath))
            {
                Directory.CreateDirectory(rootPath);
            }
        }

        public IEnumerable<string> ListContainers(string prefix = null)
        {
            try
            {
                int offset = _root.Length + 1;
                if (!String.IsNullOrEmpty(prefix))
                {
                    return Directory.EnumerateDirectories(_root, prefix + "*")
                        .Select(path => path.Substring(offset));
                }

                return Directory.EnumerateDirectories(_root)
                        .Select(path => path.Substring(offset));
            }
            catch (DirectoryNotFoundException)
            {
                return Enumerable.Empty<string>();
            }
        }

        public bool CreateContainerIfNotExist(string containerName)
        {
            var container = Path.Combine(_root, containerName);
            if (Directory.Exists(container))
            {
                return false;
            }

            Directory.CreateDirectory(container);
            return true;
        }

        public bool DeleteContainerIfExist(string containerName)
        {
            var container = Path.Combine(_root, containerName);
            if (!Directory.Exists(container))
            {
                return false;
            }

            try
            {
                Retry.Do(_policies.OptimisticConcurrency(), CancellationToken.None, () => Directory.Delete(container, true));
                return true;
            }
            catch (DirectoryNotFoundException)
            {
                return false;
            }
        }

        public IEnumerable<string> ListBlobNames(string containerName, string blobNamePrefix = null)
        {
            var container = Path.Combine(_root, containerName);

            try
            {
                int offset = container.Length + 1;
                if (!String.IsNullOrEmpty(blobNamePrefix))
                {
                    return Directory.EnumerateFiles(container, "*", SearchOption.AllDirectories)
                        .Select(path => path.Substring(offset))
                        .Where(p => p.StartsWith(blobNamePrefix));
                }

                return Directory.EnumerateFiles(container, "*", SearchOption.AllDirectories)
                        .Select(path => path.Substring(offset));
            }
            catch (DirectoryNotFoundException)
            {
                return Enumerable.Empty<string>();
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
            var path = Path.Combine(_root, containerName, blobName);
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                Retry.Do(_policies.OptimisticConcurrency(), CancellationToken.None, () => File.Delete(path));
                return true;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            
        }

        public void DeleteAllBlobs(string containerName, string blobNamePrefix = null)
        {
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
                .Convert(o => o is T ? (T)o : Maybe<T>.Empty, Maybe<T>.Empty);
        }

        public Maybe<object> GetBlob(string containerName, string blobName, Type type, out string etag, IDataSerializer serializer = null)
        {
            var path = Path.Combine(_root, containerName, blobName);
            try
            {
                var file = new FileInfo(path);
                if (!file.Exists)
                {
                    etag = null;
                    return Maybe<object>.Empty;
                }

                using (var stream = file.OpenRead())
                using (var epStream = new MetadataPrefixStream(stream))
                {
                    etag = epStream.ReadETag();
                    var deserialized = (serializer ?? _defaultSerializer).TryDeserialize(epStream, type);
                    return deserialized.IsSuccess ? new Maybe<object>(deserialized.Value) : Maybe<object>.Empty;
                }
            }
            catch (FileNotFoundException)
            {
                etag = null;
                return Maybe<object>.Empty;
            }
            catch (DirectoryNotFoundException)
            {
                etag = null;
                return Maybe<object>.Empty;
            }
        }

        public Task<BlobWithETag<object>> GetBlobAsync(string containerName, string blobName, Type type, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            // TODO: Implement native Task properly using FileSystem async api
            return Task.Factory.StartNew(
                () =>
                    {
                        string etag;
                        var blob = GetBlob(containerName, blobName, type, out etag, serializer);
                        return blob.Convert(o => new BlobWithETag<object> { Blob = o, ETag = etag }, () => default(BlobWithETag<object>));
                    });
        }

        public Maybe<XElement> GetBlobXml(string containerName, string blobName, out string etag, IDataSerializer serializer = null)
        {
            var formatter = (serializer ?? _defaultSerializer) as IIntermediateDataSerializer;
            if (formatter == null)
            {
                etag = null;
                return Maybe<XElement>.Empty;
            }

            var path = Path.Combine(_root, containerName, blobName);
            try
            {
                var file = new FileInfo(path);
                if (!file.Exists)
                {
                    etag = null;
                    return Maybe<XElement>.Empty;
                }

                using (var stream = file.OpenRead())
                using (var epStream = new MetadataPrefixStream(stream))
                {
                    etag = epStream.ReadETag();
                    var unpacked = formatter.TryUnpackXml(epStream);
                    return unpacked.IsSuccess ? new Maybe<XElement>(unpacked.Value) : Maybe<XElement>.Empty;
                }
            }
            catch (FileNotFoundException)
            {
                etag = null;
                return Maybe<XElement>.Empty;
            }
            catch (DirectoryNotFoundException)
            {
                etag = null;
                return Maybe<XElement>.Empty;
            }
        }

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

        public Maybe<T> GetBlobIfModified<T>(string containerName, string blobName, string oldEtag, out string newEtag, IDataSerializer serializer = null)
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

        public string GetBlobEtag(string containerName, string blobName)
        {
            var path = Path.Combine(_root, containerName, blobName);
            try
            {
                var file = new FileInfo(path);
                if (!file.Exists)
                {
                    return null;
                }

                using (var stream = file.OpenRead())
                using (var epStream = new MetadataPrefixStream(stream))
                {
                    return epStream.ReadETag();
                }
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }
        }

        public Task<string> GetBlobEtagAsync(string containerName, string blobName, CancellationToken cancellationToken)
        {
            // TODO: Implement native Task properly using FileSystem async api
            return TaskAsyncHelper.FromMethod(() => GetBlobEtag(containerName, blobName));
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
            string ignored;
            return PutBlob(containerName, blobName, item, typeof (T), true, expectedEtag, out ignored, serializer);
        }

        public bool PutBlob(string containerName, string blobName, object item, Type type, bool overwrite, out string etag, IDataSerializer serializer = null)
        {
            return PutBlob(containerName, blobName, item, type, overwrite, null, out etag, serializer);
        }

        public bool PutBlob(string containerName, string blobName, object item, Type type, bool overwrite, string expectedEtag, out string etag, IDataSerializer serializer = null)
        {
            var path = Path.Combine(_root, containerName, blobName);
            var folder = Path.GetDirectoryName(path);
            if (folder != null && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var file = new FileInfo(path);
            if (overwrite)
            {
                // retry in case it is currently locked by another operation
                var optimisticPolicy = _policies.OptimisticConcurrency();
                int retryCount = 0;
                while (true)
                {
                    try
                    {
                        using (var stream = file.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                        using (var epStream = new MetadataPrefixStream(stream))
                        {
                            if (!string.IsNullOrEmpty(expectedEtag) && epStream.ReadETag() != expectedEtag)
                            {
                                etag = null;
                                return false;
                            }

                            etag = WriteToStream(epStream, item, type, serializer);
                        }

                        return true;
                    }
                    catch (IOException exception)
                    {
                        TimeSpan retryInterval;
                        if (!optimisticPolicy.ShouldRetry(retryCount++, 0,exception, out retryInterval,null))
                        {
                            throw;
                        }

                        // Retry
                        Thread.Sleep(retryInterval);
                    }
                }
            }

            // no need to retry in the non-overwrite case, since being locked implies the file already exist anyway
            try
            {
                using (var stream = file.Open(FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var epStream = new MetadataPrefixStream(stream))
                {
                    etag = WriteToStream(epStream, item, type, serializer);
                }

                return true;
            }
            catch (IOException)
            {
                etag = null;
                return false;
            }
        }

        public Task<string> PutBlobAsync(string containerName, string blobName, object item, Type type, bool overwrite, string expectedEtag, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            // TODO: Implement native Task properly using FileSystem async api
            return Task.Factory.StartNew(
                () =>
                    {
                        string etag;
                        PutBlob(containerName, blobName, item, type, overwrite, expectedEtag, out etag, serializer);
                        return etag;
                    });
        }

        public Maybe<T> UpdateBlobIfExist<T>(string containerName, string blobName, Func<T, T> update, IDataSerializer serializer = null)
        {
            return UpsertBlobOrSkip(containerName, blobName, () => Maybe<T>.Empty, t => update(t), serializer);
        }

        public Maybe<T> UpdateBlobIfExistOrSkip<T>(string containerName, string blobName, Func<T, Maybe<T>> update, IDataSerializer serializer = null)
        {
            return UpsertBlobOrSkip(containerName, blobName, () => Maybe<T>.Empty, update, serializer);
        }

        public Maybe<T> UpdateBlobIfExistOrDelete<T>(string containerName, string blobName, Func<T, Maybe<T>> update, IDataSerializer serializer = null)
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
            var path = Path.Combine(_root, containerName, blobName);
            var folder = Path.GetDirectoryName(path);
            if (folder != null && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var optimisticPolicy = _policies.OptimisticConcurrency();
            int retryCount = 0;
            while(true)
            {
                try
                {
                    using (var file = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                    using (var epStream = new MetadataPrefixStream(file))
                    {
                        var input = Maybe<T>.Empty;
                        if (epStream.Length != 0)
                        {
                            var deserialized = (serializer ?? _defaultSerializer).TryDeserializeAs<T>(epStream);
                            if (deserialized.IsSuccess)
                            {
                                input = deserialized.Value;
                            }
                        }

                        var output = input.HasValue ? update(input.Value) : insert();
                        if (output.HasValue)
                        {
                            WriteToStream(epStream, output.Value, typeof(T), serializer);
                            return output.Value;
                        }

                        if (!input.HasValue)
                        {
                            epStream.Close();
                            file.Close();
                            File.Delete(path);
                        }

                        return Maybe<T>.Empty;
                    }
                }
                catch (IOException exception)
                {
                    TimeSpan retryInterval;
                    if (!optimisticPolicy.ShouldRetry(retryCount++,0, exception, out retryInterval,null))
                    {
                        throw;
                    }

                    // Retry
                    Thread.Sleep(retryInterval);
                }
            }
        }

        public Task<BlobWithETag<T>> UpsertBlobOrSkipAsync<T>(string containerName, string blobName,
            Func<Maybe<T>> insert, Func<T, Maybe<T>> update, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            return Retry.TaskAsTask(_policies.OptimisticConcurrency(), cancellationToken,
                () => GetBlobAsync(containerName, blobName, typeof(T), cancellationToken, serializer)
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
                            return putTask.Then(etag =>
                                {
                                    if (etag == null)
                                    {
                                        throw new ConcurrencyException();
                                    }

                                    return new BlobWithETag<T> { Blob = output.Value, ETag = etag };
                                });
                        }));
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

        public bool IsBlobLocked(string containerName, string blobName)
        {
            var path = Path.Combine(_root, containerName, blobName);
            return new FileInfo(path).IsReadOnly;
        }

        public Result<string> TryAcquireLease(string containerName, string blobName)
        {
            var path = Path.Combine(_root, containerName, blobName);
            try
            {
                var file = new FileInfo(path);
                if (!file.Exists)
                {
                    return null;
                }

                using (var stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                using (var epStream = new MetadataPrefixStream(stream))
                {
                    var flags = epStream.ReadFlags();
                    if ((flags & 0x1) == 0x1)
                    {
                        // already locked, conflict
                        return Result<string>.CreateError("Conflict");
                    }

                    epStream.WriteFlags((byte)(flags | 0x1));
                    return Result.CreateSuccess(epStream.ReadETag());
                }
            }
            catch (FileNotFoundException)
            {
                return Result<string>.CreateError("NotFound");
            }
            catch (DirectoryNotFoundException)
            {
                return Result<string>.CreateError("NotFound");
            }
        }

        public Result<string> TryReleaseLease(string containerName, string blobName, string leaseId)
        {
            var path = Path.Combine(_root, containerName, blobName);
            try
            {
                var file = new FileInfo(path);
                if (!file.Exists)
                {
                    return null;
                }

                using (var stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                using (var epStream = new MetadataPrefixStream(stream))
                {
                    var flags = epStream.ReadFlags();
                    if ((flags & 0x1) == 0x0)
                    {
                        // not locked
                        return Result<string>.CreateError("NotFound");
                    }

                    if (leaseId != epStream.ReadETag())
                    {
                        // locked by another leaseId, conflict
                        return Result<string>.CreateError("Conflict");
                    }

                    epStream.WriteFlags((byte)(flags & ~0x1));
                    return Result.CreateSuccess("OK");
                }
            }
            catch (FileNotFoundException)
            {
                return Result<string>.CreateError("NotFound");
            }
            catch (DirectoryNotFoundException)
            {
                return Result<string>.CreateError("NotFound");
            }
        }

        public Result<string> TryRenewLease(string containerName, string blobName, string leaseId)
        {
            var path = Path.Combine(_root, containerName, blobName);
            try
            {
                var file = new FileInfo(path);
                if (!file.Exists)
                {
                    return null;
                }

                using (var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                using (var epStream = new MetadataPrefixStream(stream))
                {
                    var flags = epStream.ReadFlags();
                    if ((flags & 0x1) == 0x0)
                    {
                        // not locked
                        return Result<string>.CreateError("NotFound");
                    }

                    if (leaseId != epStream.ReadETag())
                    {
                        // locked by another leaseId, conflict
                        return Result<string>.CreateError("Conflict");
                    }

                    return Result.CreateSuccess("OK");
                }
            }
            catch (FileNotFoundException)
            {
                return Result<string>.CreateError("NotFound");
            }
            catch (DirectoryNotFoundException)
            {
                return Result<string>.CreateError("NotFound");
            }
        }

        /// <returns>New ETag</returns>
        private string WriteToStream(MetadataPrefixStream stream, object item, Type type, IDataSerializer serializer = null)
        {
            byte[] result;
            using (var resultStream = new MemoryStream())
            {
                (serializer ?? _defaultSerializer).Serialize(item, resultStream, type);
                result = resultStream.ToArray();
            }

            stream.Seek(0, SeekOrigin.Begin);
            stream.Write(result, 0, result.Length);
            stream.SetLength(result.Length);
            return stream.WriteNewETag();
        }
    }
}
