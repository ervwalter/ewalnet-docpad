#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable CheckNamespace
// ReSharper disable CSharpWarnings::CS1591

namespace Lokad.Cloud.Storage
{
    /// <summary>Helpers for the <see cref="IBlobStorageProvider"/>.</summary>
    public static class BlobStorageExtensions
    {
        /// <summary>
        /// List the blob names of all blobs matching the provided blob name prefix.
        /// </summary>
        /// <remarks>
        /// <para>This method is sideeffect-free, except for infrastructure effects like thread pool usage.</para>
        /// </remarks>
        public static IEnumerable<T> ListBlobNames<T>(this IBlobStorageProvider provider, T blobNamePrefix) where T : UntypedBlobName
        {
            return provider.ListBlobNames(blobNamePrefix.ContainerName, blobNamePrefix.ToString())
                .Select(UntypedBlobName.Parse<T>);
        }

        /// <summary>
        /// List the blob names of all blobs matching the provided blob name prefix.
        /// </summary>
        /// <remarks>
        /// <para>This method is sideeffect-free, except for infrastructure effects like thread pool usage.</para>
        /// </remarks>
        public static IEnumerable<T> ListBlobNames<T>(this IBlobStorageProvider provider, IBlobLocation locationPrefix) where T : UntypedBlobName
        {
            return provider.ListBlobNames(locationPrefix.ContainerName, locationPrefix.Path)
                .Select(UntypedBlobName.Parse<T>);
        }

        /// <summary>
        /// List the blob locations of all blobs matching the provided blob name prefix.
        /// </summary>
        /// <remarks>
        /// <para>This method is sideeffect-free, except for infrastructure effects like thread pool usage.</para>
        /// </remarks>
        public static IEnumerable<IBlobLocation> ListBlobLocations(this IBlobStorageProvider provider, string containerName, string blobNamePrefix = null)
        {
            return provider.ListBlobNames(containerName, blobNamePrefix)
                .Select(name => new BlobLocation(containerName, name));
        }

        /// <summary>
        /// List the blob locations of all blobs matching the provided blob name prefix.
        /// </summary>
        /// <remarks>
        /// <para>This method is sideeffect-free, except for infrastructure effects like thread pool usage.</para>
        /// </remarks>
        public static IEnumerable<IBlobLocation> ListBlobLocations(this IBlobStorageProvider provider, IBlobLocation blobLocationPrefix)
        {
            return provider.ListBlobNames(blobLocationPrefix.ContainerName, blobLocationPrefix.Path)
                .Select(name => new BlobLocation(blobLocationPrefix.ContainerName, name));
        }

        /// <summary>
        /// List the blob locations (with the provided type) of all blobs matching the provided blob name prefix.
        /// </summary>
        /// <remarks>
        /// <para>This method is sideeffect-free, except for infrastructure effects like thread pool usage.</para>
        /// </remarks>
        public static IEnumerable<IBlobLocationAndType<T>> ListBlobLocations<T>(this IBlobStorageProvider provider, string containerName, string blobNamePrefix = null)
        {
            return provider.ListBlobNames(containerName, blobNamePrefix)
                .Select(name => new BlobLocationAndType<T>(containerName, name));
        }

        /// <summary>
        /// List the blob locations (with the provided type) of all blobs matching the provided blob name prefix.
        /// </summary>
        /// <remarks>
        /// <para>This method is sideeffect-free, except for infrastructure effects like thread pool usage.</para>
        /// </remarks>
        public static IEnumerable<IBlobLocationAndType<T>> ListBlobLocations<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> blobLocationPrefix)
        {
            return provider.ListBlobNames(blobLocationPrefix.ContainerName, blobLocationPrefix.Path)
                .Select(name => new BlobLocationAndType<T>(blobLocationPrefix.ContainerName, name));
        }

        /// <summary>
        /// List and get all blobs matching the provided blob name prefix.
        /// </summary>
        /// <remarks>
        /// <para>This method is sideeffect-free, except for infrastructure effects like thread pool usage.</para>
        /// </remarks>
        public static IEnumerable<T> ListBlobs<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> locationPrefix, int skip = 0, IDataSerializer serializer = null)
        {
            return provider.ListBlobs<T>(locationPrefix.ContainerName, locationPrefix.Path, skip, serializer);
        }

        /// <summary>
        /// List and get all blobs matching the provided blob name prefix.
        /// </summary>
        /// <remarks>
        /// <para>This method is sideeffect-free, except for infrastructure effects like thread pool usage.</para>
        /// </remarks>
        public static IEnumerable<T> ListBlobs<T>(this IBlobStorageProvider provider, IBlobLocation locationPrefix, int skip = 0, IDataSerializer serializer = null)
        {
            return provider.ListBlobs<T>(locationPrefix.ContainerName, locationPrefix.Path, skip, serializer);
        }

        /// <summary>
        /// Deletes a blob if it exists.
        /// </summary>
        /// <remarks>
        /// <para>This method is idempotent.</para>
        /// </remarks>
        public static bool DeleteBlobIfExist(this IBlobStorageProvider provider, IBlobLocation location)
        {
            return provider.DeleteBlobIfExist(location.ContainerName, location.Path);
        }

        /// <summary>
        /// Delete all blobs matching the provided blob name prefix.
        /// </summary>
        /// <remarks>
        /// <para>This method is idempotent.</para>
        /// </remarks>
        public static void DeleteAllBlobs(this IBlobStorageProvider provider, IBlobLocation locationPrefix)
        {
            provider.DeleteAllBlobs(locationPrefix.ContainerName, locationPrefix.Path);
        }

        public static Maybe<T> GetBlob<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> location, IDataSerializer serializer = null)
        {
            return provider.GetBlob<T>(location.ContainerName, location.Path, serializer);
        }

        public static Maybe<T> GetBlob<T>(this IBlobStorageProvider provider, IBlobLocation location, IDataSerializer serializer = null)
        {
            return provider.GetBlob<T>(location.ContainerName, location.Path, serializer);
        }

        public static Maybe<T> GetBlob<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> location, out string etag, IDataSerializer serializer = null)
        {
            return provider.GetBlob<T>(location.ContainerName, location.Path, out etag, serializer);
        }

        public static string GetBlobEtag(this IBlobStorageProvider provider, IBlobLocation location)
        {
            return provider.GetBlobEtag(location.ContainerName, location.Path);
        }

        public static void PutBlob<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> location, T item, IDataSerializer serializer = null)
        {
            provider.PutBlob(location.ContainerName, location.Path, item, serializer);
        }

        public static void PutBlob<T>(this IBlobStorageProvider provider, IBlobLocation location, T item, IDataSerializer serializer = null)
        {
            provider.PutBlob(location.ContainerName, location.Path, item, serializer);
        }

        public static bool PutBlob<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> location, T item, bool overwrite, IDataSerializer serializer = null)
        {
            return provider.PutBlob(location.ContainerName, location.Path, item, overwrite, serializer);
        }

        public static bool PutBlob<T>(this IBlobStorageProvider provider, IBlobLocation location, T item, bool overwrite, IDataSerializer serializer = null)
        {
            return provider.PutBlob(location.ContainerName, location.Path, item, overwrite, serializer);
        }

        /// <summary>Push the blob only if etag is matching the etag of the blob in BlobStorage</summary>
        public static bool PutBlob<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> location, T item, string etag, IDataSerializer serializer = null)
        {
            return provider.PutBlob(location.ContainerName, location.Path, item, etag, serializer);
        }

        /// <summary>
        /// Updates a blob if it already exists.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The provided lambdas can be executed multiple times in case of
        /// concurrency-related retrials, so be careful with side-effects
        /// (like incrementing a counter in them).
        /// </para>
        /// <para>This method is idempotent if and only if the provided lambdas are idempotent.</para>
        /// </remarks>
        /// <returns>The value returned by the lambda, or empty if the blob did not exist.</returns>
        public static Maybe<T> UpdateBlobIfExist<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> location, Func<T, T> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkip(location.ContainerName, location.Path, () => Maybe<T>.Empty, t => update(t), serializer);
        }

        /// <summary>
        /// Updates a blob if it already exists.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The provided lambdas can be executed multiple times in case of
        /// concurrency-related retrials, so be careful with side-effects
        /// (like incrementing a counter in them).
        /// </para>
        /// <para>This method is idempotent if and only if the provided lambdas are idempotent.</para>
        /// </remarks>
        /// <returns>The value returned by the lambda, or empty if the blob did not exist.</returns>
        public static Maybe<T> UpdateBlobIfExist<T>(this IBlobStorageProvider provider, IBlobLocation location, Func<T, T> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkip(location.ContainerName, location.Path, () => Maybe<T>.Empty, t => update(t), serializer);
        }


        /// <summary>
        /// Updates a blob if it already exists.
        /// If the insert or update lambdas return empty, the blob will not be changed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The provided lambdas can be executed multiple times in case of
        /// concurrency-related retrials, so be careful with side-effects
        /// (like incrementing a counter in them).
        /// </para>
        /// <para>This method is idempotent if and only if the provided lambdas are idempotent.</para>
        /// </remarks>
        /// <returns>The value returned by the lambda, or empty if the blob did not exist or no change was applied.</returns>
        public static Maybe<T> UpdateBlobIfExistOrSkip<T>(
            this IBlobStorageProvider provider, IBlobLocationAndType<T> location, Func<T, Maybe<T>> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkip(location.ContainerName, location.Path, () => Maybe<T>.Empty, update, serializer);
        }

        /// <summary>
        /// Updates a blob if it already exists.
        /// If the insert or update lambdas return empty, the blob will be deleted.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The provided lambdas can be executed multiple times in case of
        /// concurrency-related retrials, so be careful with side-effects
        /// (like incrementing a counter in them).
        /// </para>
        /// <para>This method is idempotent if and only if the provided lambdas are idempotent.</para>
        /// </remarks>
        /// <returns>The value returned by the lambda, or empty if the blob did not exist or was deleted.</returns>
        public static Maybe<T> UpdateBlobIfExistOrDelete<T>(
            this IBlobStorageProvider provider, IBlobLocationAndType<T> location, Func<T, Maybe<T>> update, IDataSerializer serializer = null)
        {
            return provider.UpdateBlobIfExistOrDelete(location.ContainerName, location.Path, update, serializer);
        }

        /// <summary>
        /// Inserts or updates a blob depending on whether it already exists or not.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The provided lambdas can be executed multiple times in case of
        /// concurrency-related retrials, so be careful with side-effects
        /// (like incrementing a counter in them).
        /// </para>
        /// <para>
        /// This method is idempotent if and only if the provided lambdas are idempotent
        /// and if the object returned by the insert lambda is an invariant to the update lambda
        /// (if the second condition is not met, it is idempotent after the first successful call).
        /// </para>
        /// </remarks>
        /// <returns>The value returned by the lambda.</returns>
        public static T UpsertBlob<T>(this IBlobStorageProvider provider, IBlobLocationAndType<T> location, Func<T> insert, Func<T, T> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkip<T>(location.ContainerName, location.Path, () => insert(), t => update(t), serializer).Value;
        }

        /// <summary>
        /// Inserts or updates a blob depending on whether it already exists or not.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The provided lambdas can be executed multiple times in case of
        /// concurrency-related retrials, so be careful with side-effects
        /// (like incrementing a counter in them).
        /// </para>
        /// <para>
        /// This method is idempotent if and only if the provided lambdas are idempotent
        /// and if the object returned by the insert lambda is an invariant to the update lambda
        /// (if the second condition is not met, it is idempotent after the first successful call).
        /// </para>
        /// </remarks>
        /// <returns>The value returned by the lambda.</returns>
        public static T UpsertBlob<T>(this IBlobStorageProvider provider, IBlobLocation location, Func<T> insert, Func<T, T> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkip<T>(location.ContainerName, location.Path, () => insert(), t => update(t), serializer).Value;
        }

        /// <summary>
        /// Inserts or updates a blob depending on whether it already exists or not.
        /// If the insert or update lambdas return empty, the blob will not be changed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The provided lambdas can be executed multiple times in case of
        /// concurrency-related retrials, so be careful with side-effects
        /// (like incrementing a counter in them).
        /// </para>
        /// <para>
        /// This method is idempotent if and only if the provided lambdas are idempotent
        /// and if the object returned by the insert lambda is an invariant to the update lambda
        /// (if the second condition is not met, it is idempotent after the first successful call).
        /// </para>
        /// </remarks>
        /// <returns>The value returned by the lambda. If empty, then no change was applied.</returns>
        public static Maybe<T> UpsertBlobOrSkip<T>(this IBlobStorageProvider provider,
            IBlobLocationAndType<T> location, Func<Maybe<T>> insert, Func<T, Maybe<T>> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrSkip(location.ContainerName, location.Path, insert, update, serializer);
        }

        /// <summary>
        /// Inserts or updates a blob depending on whether it already exists or not.
        /// If the insert or update lambdas return empty, the blob will be deleted (if it exists).
        /// </summary>
        /// <remarks>
        /// <para>
        /// The provided lambdas can be executed multiple times in case of
        /// concurrency-related retrials, so be careful with side-effects
        /// (like incrementing a counter in them).
        /// </para>
        /// <para>
        /// This method is idempotent if and only if the provided lambdas are idempotent
        /// and if the object returned by the insert lambda is an invariant to the update lambda
        /// (if the second condition is not met, it is idempotent after the first successful call).
        /// </para>
        /// </remarks>
        /// <returns>The value returned by the lambda. If empty, then the blob has been deleted.</returns>
        public static Maybe<T> UpsertBlobOrDelete<T>(
            this IBlobStorageProvider provider, IBlobLocationAndType<T> location, Func<Maybe<T>> insert, Func<T, Maybe<T>> update, IDataSerializer serializer = null)
        {
            return provider.UpsertBlobOrDelete(location.ContainerName, location.Path, insert, update, serializer);
        }

        /// <summary>Checks that containerName is a valid DNS name, as requested by Azure</summary>
        public static bool IsContainerNameValid(string containerName)
        {
            return (Regex.IsMatch(containerName, @"(^([a-z]|\d))((-([a-z]|\d)|([a-z]|\d))+)$")
                && (3 <= containerName.Length) && (containerName.Length <= 63));
        }
    }
}
