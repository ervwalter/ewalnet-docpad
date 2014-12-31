#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable CheckNamespace
// ReSharper disable CSharpWarnings::CS1591

namespace Lokad.Cloud.Storage
{
    /// <summary>Async Helpers for the <see cref="IBlobStorageProvider"/>.</summary>
    public static class BlobStorageAsyncExtensions
    {
        // GetBlobAsync

        public static Task<BlobWithETag<T>> GetBlobAsync<T>(this IBlobStorageProvider provider, string containerName, string blobName, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            return provider.GetBlobAsync(containerName, blobName, typeof(T), cancellationToken, serializer)
                .Then(b => b == null ? null : new BlobWithETag<T> { Blob = (T)b.Blob, ETag = b.ETag });
        }

        public static Task<BlobWithETag<T>> GetBlobAsync<T>(this IBlobStorageProvider provider, string containerName, string blobName, IDataSerializer serializer = null)
        {
            return provider.GetBlobAsync(containerName, blobName, typeof(T), CancellationToken.None, serializer)
                .Then(b => b == null ? null : new BlobWithETag<T> { Blob = (T)b.Blob, ETag = b.ETag });
        }

        public static Task<BlobWithETag<T>> GetBlobAsync<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> location, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            return provider.GetBlobAsync<T>(location.ContainerName, location.Path, cancellationToken, serializer);
        }

        public static Task<BlobWithETag<T>> GetBlobAsync<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> location, IDataSerializer serializer = null)
        {
            return provider.GetBlobAsync<T>(location.ContainerName, location.Path, CancellationToken.None, serializer);
        }

        public static Task<BlobWithETag<T>> GetBlobAsync<T>(this IBlobStorageProvider provider, IBlobLocation location, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            return provider.GetBlobAsync<T>(location.ContainerName, location.Path, cancellationToken, serializer);
        }

        public static Task<BlobWithETag<T>> GetBlobAsync<T>(this IBlobStorageProvider provider, IBlobLocation location, IDataSerializer serializer = null)
        {
            return provider.GetBlobAsync<T>(location.ContainerName, location.Path, CancellationToken.None, serializer);
        }

        // GetBlobEtagAsync

        public static Task<string> GetBlobEtagAsync(this IBlobStorageProvider provider, string containerName, string blobName, CancellationToken cancellationToken)
        {
            return provider.GetBlobEtagAsync(containerName, blobName, cancellationToken);
        }

        public static Task<string> GetBlobEtagAsync(this IBlobStorageProvider provider, string containerName, string blobName)
        {
            return provider.GetBlobEtagAsync(containerName, blobName, CancellationToken.None);
        }

        // PutBlobAsync

        public static Task<string> PutBlobAsync<T>(this IBlobStorageProvider provider, string containerName, string blobName, T item, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            return provider.PutBlobAsync(containerName, blobName, item, typeof(T), true, null, cancellationToken, serializer);
        }

        public static Task<string> PutBlobAsync<T>(this IBlobStorageProvider provider, string containerName, string blobName, T item, IDataSerializer serializer = null)
        {
            return provider.PutBlobAsync(containerName, blobName, item, typeof(T), true, null, CancellationToken.None, serializer);
        }

        public static Task<string> PutBlobAsync<T>(this IBlobStorageProvider provider, string containerName, string blobName, T item, bool overwrite, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            return provider.PutBlobAsync(containerName, blobName, item, typeof(T), overwrite, null, cancellationToken, serializer);
        }

        public static Task<string> PutBlobAsync<T>(this IBlobStorageProvider provider, string containerName, string blobName, T item, bool overwrite, IDataSerializer serializer = null)
        {
            return provider.PutBlobAsync(containerName, blobName, item, typeof(T), overwrite, null, CancellationToken.None, serializer);
        }

        public static Task<string> PutBlobAsync<T>(this IBlobStorageProvider provider, string containerName, string blobName, T item, string expectedEtag, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            return provider.PutBlobAsync(containerName, blobName, item, typeof(T), true, expectedEtag, cancellationToken, serializer);
        }

        public static Task<string> PutBlobAsync<T>(this IBlobStorageProvider provider, string containerName, string blobName, T item, string expectedEtag, IDataSerializer serializer = null)
        {
            return provider.PutBlobAsync(containerName, blobName, item, typeof(T), true, expectedEtag, CancellationToken.None, serializer);
        }

        public static Task<string> PutBlobAsync(this IBlobStorageProvider provider, string containerName, string blobName, object item, Type type, bool overwrite, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            return provider.PutBlobAsync(containerName, blobName, item, type, overwrite, null, cancellationToken, serializer);
        }

        public static Task<string> PutBlobAsync(this IBlobStorageProvider provider, string containerName, string blobName, object item, Type type, bool overwrite, IDataSerializer serializer = null)
        {
            return provider.PutBlobAsync(containerName, blobName, item, type, overwrite, null, CancellationToken.None, serializer);
        }

        public static Task<string> PutBlobAsync<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> location, T item, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            return provider.PutBlobAsync(location.ContainerName, location.Path, item, typeof(T), true, null, cancellationToken, serializer);
        }

        public static Task<string> PutBlobAsync<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> location, T item, IDataSerializer serializer = null)
        {
            return provider.PutBlobAsync(location.ContainerName, location.Path, item, typeof(T), true, null, CancellationToken.None, serializer);
        }

        public static Task<string> PutBlobAsync<T>(this IBlobStorageProvider provider, IBlobLocation location, T item, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            return provider.PutBlobAsync(location.ContainerName, location.Path, item, typeof(T), true, null, cancellationToken, serializer);
        }

        public static Task<string> PutBlobAsync<T>(this IBlobStorageProvider provider, IBlobLocation location, T item, IDataSerializer serializer = null)
        {
            return provider.PutBlobAsync(location.ContainerName, location.Path, item, typeof(T), true, null, CancellationToken.None, serializer);
        }

        public static Task<string> PutBlobAsync<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> location, T item, bool overwrite, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            return provider.PutBlobAsync(location.ContainerName, location.Path, item, typeof(T), overwrite, null, cancellationToken, serializer);
        }

        public static Task<string> PutBlobAsync<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> location, T item, bool overwrite, IDataSerializer serializer = null)
        {
            return provider.PutBlobAsync(location.ContainerName, location.Path, item, typeof(T), overwrite, null, CancellationToken.None, serializer);
        }

        public static Task<string> PutBlobAsync<T>(this IBlobStorageProvider provider, IBlobLocation location, T item, bool overwrite, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            return provider.PutBlobAsync(location.ContainerName, location.Path, item, typeof(T), overwrite, null, cancellationToken, serializer);
        }

        public static Task<string> PutBlobAsync<T>(this IBlobStorageProvider provider, IBlobLocation location, T item, bool overwrite, IDataSerializer serializer = null)
        {
            return provider.PutBlobAsync(location.ContainerName, location.Path, item, typeof(T), overwrite, null, CancellationToken.None, serializer);
        }

        public static Task<string> PutBlobAsync<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> location, T item, string expectedEtag, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            return provider.PutBlobAsync(location.ContainerName, location.Path, item, typeof(T), true, expectedEtag, cancellationToken, serializer);
        }

        public static Task<string> PutBlobAsync<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> location, T item, string expectedEtag, IDataSerializer serializer = null)
        {
            return provider.PutBlobAsync(location.ContainerName, location.Path, item, typeof(T), true, expectedEtag, CancellationToken.None, serializer);
        }

        // UpsertBlobOrSkipAsync

        public static Task<BlobWithETag<T>> UpsertBlobOrSkipAsync<T>(this IBlobStorageProvider provider, string containerName, string blobName,
            Func<Maybe<T>> insert, Func<T, Maybe<T>> update, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync(containerName, blobName, insert, update, cancellationToken, serializer);
        }

        public static Task<BlobWithETag<T>> UpsertBlobOrSkipAsync<T>(this IBlobStorageProvider provider, string containerName, string blobName,
            Func<Maybe<T>> insert, Func<T, Maybe<T>> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync(containerName, blobName, insert, update, CancellationToken.None, serializer);
        }

        public static Task<BlobWithETag<T>> UpsertBlobOrSkipAsync<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> location,
            Func<Maybe<T>> insert, Func<T, Maybe<T>> update, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync(location.ContainerName, location.Path, insert, update, cancellationToken, serializer);
        }

        public static Task<BlobWithETag<T>> UpsertBlobOrSkipAsync<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> location,
            Func<Maybe<T>> insert, Func<T, Maybe<T>> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync(location.ContainerName, location.Path, insert, update, CancellationToken.None, serializer);
        }

        public static Task<BlobWithETag<T>> UpsertBlobOrSkipAsync<T>(this IBlobStorageProvider provider, IBlobLocation location,
            Func<Maybe<T>> insert, Func<T, Maybe<T>> update, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync(location.ContainerName, location.Path, insert, update, cancellationToken, serializer);
        }

        public static Task<BlobWithETag<T>> UpsertBlobOrSkipAsync<T>(this IBlobStorageProvider provider, IBlobLocation location,
            Func<Maybe<T>> insert, Func<T, Maybe<T>> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync(location.ContainerName, location.Path, insert, update, CancellationToken.None, serializer);
        }

        // UpsertBlobAsync

        public static Task<BlobWithETag<T>> UpsertBlobAsync<T>(this IBlobStorageProvider provider, string containerName, string blobName,
            Func<T> insert, Func<T, T> update, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync<T>(containerName, blobName, () => insert(), t => update(t), cancellationToken, serializer);
        }

        public static Task<BlobWithETag<T>> UpsertBlobAsync<T>(this IBlobStorageProvider provider, string containerName, string blobName,
            Func<T> insert, Func<T, T> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync<T>(containerName, blobName, () => insert(), t => update(t), CancellationToken.None, serializer);
        }

        public static Task<BlobWithETag<T>> UpsertBlobAsync<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> location,
            Func<T> insert, Func<T, T> update, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync<T>(location.ContainerName, location.Path, () => insert(), t => update(t), cancellationToken, serializer);
        }

        public static Task<BlobWithETag<T>> UpsertBlobAsync<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> location,
            Func<T> insert, Func<T, T> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync<T>(location.ContainerName, location.Path, () => insert(), t => update(t), CancellationToken.None, serializer);
        }

        public static Task<BlobWithETag<T>> UpsertBlobAsync<T>(this IBlobStorageProvider provider, IBlobLocation location,
            Func<T> insert, Func<T, T> update, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync<T>(location.ContainerName, location.Path, () => insert(), t => update(t), cancellationToken, serializer);
        }

        public static Task<BlobWithETag<T>> UpsertBlobAsync<T>(this IBlobStorageProvider provider, IBlobLocation location,
            Func<T> insert, Func<T, T> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync<T>(location.ContainerName, location.Path, () => insert(), t => update(t), CancellationToken.None, serializer);
        }

        // UpsertBlobOrDeleteAsync

        public static Task<BlobWithETag<T>> UpsertBlobOrDeleteAsync<T>(this IBlobStorageProvider provider, string containerName, string blobName,
            Func<Maybe<T>> insert, Func<T, Maybe<T>> update, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync(containerName, blobName, insert, update, cancellationToken, serializer)
                .Then(b =>
                    {
                        if (b == null)
                        {
                            provider.DeleteBlobIfExist(containerName, blobName);
                        }

                        return b;
                    });
        }

        public static Task<BlobWithETag<T>> UpsertBlobOrDeleteAsync<T>(this IBlobStorageProvider provider, string containerName, string blobName,
            Func<Maybe<T>> insert, Func<T, Maybe<T>> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrDeleteAsync(containerName, blobName, insert, update, CancellationToken.None, serializer);
        }

        public static Task<BlobWithETag<T>> UpsertBlobOrDeleteAsync<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> location,
            Func<Maybe<T>> insert, Func<T, Maybe<T>> update, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrDeleteAsync(location.ContainerName, location.Path, insert, update, cancellationToken, serializer);
        }

        public static Task<BlobWithETag<T>> UpsertBlobOrDeleteAsync<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> location,
            Func<Maybe<T>> insert, Func<T, Maybe<T>> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrDeleteAsync(location.ContainerName, location.Path, insert, update, CancellationToken.None, serializer);
        }

        public static Task<BlobWithETag<T>> UpsertBlobOrDeleteAsync<T>(this IBlobStorageProvider provider, IBlobLocation location,
            Func<Maybe<T>> insert, Func<T, Maybe<T>> update, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrDeleteAsync(location.ContainerName, location.Path, insert, update, cancellationToken, serializer);
        }

        public static Task<BlobWithETag<T>> UpsertBlobOrDeleteAsync<T>(this IBlobStorageProvider provider, IBlobLocation location,
            Func<Maybe<T>> insert, Func<T, Maybe<T>> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrDeleteAsync(location.ContainerName, location.Path, insert, update, CancellationToken.None, serializer);
        }

        // UpdateBlobIfExistOrSkipAsync

        public static Task<BlobWithETag<T>> UpdateBlobIfExistOrSkipAsync<T>(this IBlobStorageProvider provider, string containerName, string blobName,
            Func<T, Maybe<T>> update, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync(containerName, blobName, () => Maybe<T>.Empty, update, cancellationToken, serializer);
        }

        public static Task<BlobWithETag<T>> UpdateBlobIfExistOrSkipAsync<T>(this IBlobStorageProvider provider, string containerName, string blobName,
            Func<T, Maybe<T>> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync(containerName, blobName, () => Maybe<T>.Empty, update, CancellationToken.None, serializer);
        }

        public static Task<BlobWithETag<T>> UpdateBlobIfExistOrSkipAsync<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> location,
            Func<T, Maybe<T>> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync(location.ContainerName, location.Path, () => Maybe<T>.Empty, update, CancellationToken.None, serializer);
        }

        public static Task<BlobWithETag<T>> UpdateBlobIfExistOrSkipAsync<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> location,
            Func<T, Maybe<T>> update, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync(location.ContainerName, location.Path, () => Maybe<T>.Empty, update, cancellationToken, serializer);
        }

        public static Task<BlobWithETag<T>> UpdateBlobIfExistOrSkipAsync<T>(this IBlobStorageProvider provider, IBlobLocation location,
            Func<T, Maybe<T>> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync(location.ContainerName, location.Path, () => Maybe<T>.Empty, update, CancellationToken.None, serializer);
        }

        public static Task<BlobWithETag<T>> UpdateBlobIfExistOrSkipAsync<T>(this IBlobStorageProvider provider, IBlobLocation location,
            Func<T, Maybe<T>> update, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync(location.ContainerName, location.Path, () => Maybe<T>.Empty, update, cancellationToken, serializer);
        }

        // UpdateBlobIfExistAsync

        public static Task<BlobWithETag<T>> UpdateBlobIfExistAsync<T>(this IBlobStorageProvider provider, string containerName, string blobName,
            Func<T, T> update, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync(containerName, blobName, () => Maybe<T>.Empty, t => update(t), cancellationToken, serializer);
        }

        public static Task<BlobWithETag<T>> UpdateBlobIfExistAsync<T>(this IBlobStorageProvider provider, string containerName, string blobName,
            Func<T, T> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync(containerName, blobName, () => Maybe<T>.Empty, t => update(t), CancellationToken.None, serializer);
        }

        public static Task<BlobWithETag<T>> UpdateBlobIfExistAsync<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> location,
            Func<T, T> update, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync(location.ContainerName, location.Path, () => Maybe<T>.Empty, t => update(t), cancellationToken, serializer);
        }

        public static Task<BlobWithETag<T>> UpdateBlobIfExistAsync<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> location,
            Func<T, T> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync(location.ContainerName, location.Path, () => Maybe<T>.Empty, t => update(t), CancellationToken.None, serializer);
        }

        public static Task<BlobWithETag<T>> UpdateBlobIfExistAsync<T>(this IBlobStorageProvider provider, IBlobLocation location,
            Func<T, T> update, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync(location.ContainerName, location.Path, () => Maybe<T>.Empty, t => update(t), cancellationToken, serializer);
        }

        public static Task<BlobWithETag<T>> UpdateBlobIfExistAsync<T>(this IBlobStorageProvider provider, IBlobLocation location,
            Func<T, T> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync(location.ContainerName, location.Path, () => Maybe<T>.Empty, t => update(t), CancellationToken.None, serializer);
        }

        // UpdateBlobIfExistOrDeleteAsync

        public static Task<BlobWithETag<T>> UpdateBlobIfExistOrDeleteAsync<T>(this IBlobStorageProvider provider, string containerName, string blobName,
            Func<T, Maybe<T>> update, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkipAsync(containerName, blobName, () => Maybe<T>.Empty, update, cancellationToken, serializer)
                .Then(b =>
                    {
                        if (b == null)
                        {
                            provider.DeleteBlobIfExist(containerName, blobName);
                        }

                        return b;
                    });
        }

        public static Task<BlobWithETag<T>> UpdateBlobIfExistOrDeleteAsync<T>(this IBlobStorageProvider provider, string containerName, string blobName,
            Func<T, Maybe<T>> update, IDataSerializer serializer = null)
        {
            return provider.UpdateBlobIfExistOrDeleteAsync(containerName, blobName, update, CancellationToken.None, serializer);
        }

        public static Task<BlobWithETag<T>> UpdateBlobIfExistOrDeleteAsync<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> location,
            Func<T, Maybe<T>> update, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            return provider.UpdateBlobIfExistOrDeleteAsync(location.ContainerName, location.Path, update, cancellationToken, serializer);
        }

        public static Task<BlobWithETag<T>> UpdateBlobIfExistOrDeleteAsync<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> location,
            Func<T, Maybe<T>> update, IDataSerializer serializer = null)
        {
            return provider.UpdateBlobIfExistOrDeleteAsync(location.ContainerName, location.Path, update, CancellationToken.None, serializer);
        }

        public static Task<BlobWithETag<T>> UpdateBlobIfExistOrDeleteAsync<T>(this IBlobStorageProvider provider, IBlobLocation location,
            Func<T, Maybe<T>> update, CancellationToken cancellationToken, IDataSerializer serializer = null)
        {
            return provider.UpdateBlobIfExistOrDeleteAsync(location.ContainerName, location.Path, update, cancellationToken, serializer);
        }

        public static Task<BlobWithETag<T>> UpdateBlobIfExistOrDeleteAsync<T>(this IBlobStorageProvider provider, IBlobLocation location,
            Func<T, Maybe<T>> update, IDataSerializer serializer = null)
        {
            return provider.UpdateBlobIfExistOrDeleteAsync(location.ContainerName, location.Path, update, CancellationToken.None, serializer);
        }
    }
}
