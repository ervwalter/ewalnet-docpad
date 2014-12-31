#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Lokad.Cloud.Storage
{
    /// <summary>Abstraction for the Blob Storage.</summary>
    /// <remarks>
    /// This provider represents a <em>logical</em> blob storage, not the actual
    /// Blob Storage. In particular, this provider deals with overflowing buffers
    /// that need to be split in smaller chunks to be uploaded.
    /// </remarks>
    public interface IBlobStorageProvider
    {
        /// <summary>
        /// List the names of all containers, matching the optional prefix if provided.
        /// </summary>
        IEnumerable<string> ListContainers(string containerNamePrefix = null);

        /// <summary>Creates a new blob container.</summary>
        /// <returns><c>true</c> if the container was actually created and <c>false</c> if
        /// the container already exists.</returns>
        /// <remarks>This operation is idempotent.</remarks>
        bool CreateContainerIfNotExist(string containerName);

        /// <summary>Delete a container.</summary>
        /// <returns><c>true</c> if the container has actually been deleted.</returns>
        /// <remarks>This operation is idempotent.</remarks>
        bool DeleteContainerIfExist(string containerName);

        /// <summary>
        /// List the blob names of all blobs matching both the provided container name and the optional blob name prefix.
        /// </summary>
        /// <remarks>
        /// <para>This method is sideeffect-free, except for infrastructure effects like thread pool usage.</para>
        /// </remarks>
        IEnumerable<string> ListBlobNames(string containerName, string blobNamePrefix = null);

        /// <summary>
        /// List and get all blobs matching both the provided container name and the optional blob name prefix.
        /// </summary>
        /// <remarks>
        /// <para>This method is sideeffect-free, except for infrastructure effects like thread pool usage.</para>
        /// </remarks>
        IEnumerable<T> ListBlobs<T>(string containerName, string blobNamePrefix = null, int skip = 0, IDataSerializer serializer = null);

        /// <summary>
        /// Deletes a blob if it exists.
        /// </summary>
        /// <remarks>
        /// <para>This method is idempotent.</para>
        /// </remarks>
        bool DeleteBlobIfExist(string containerName, string blobName);

        /// <summary>
        /// Delete all blobs matching the provided blob name prefix.
        /// </summary>
        /// <remarks>
        /// <para>This method is idempotent.</para>
        /// </remarks>
        void DeleteAllBlobs(string containerName, string blobNamePrefix = null);

        /// <summary>Gets a blob.</summary>
        /// <returns>
        /// If there is no such blob, the returned object
        /// has its property HasValue set to <c>false</c>.
        /// </returns>
        Maybe<T> GetBlob<T>(string containerName, string blobName, IDataSerializer serializer = null);

        /// <summary>Gets a blob.</summary>
        /// <typeparam name="T">Blob type.</typeparam>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="blobName">Name of the blob.</param>
        /// <param name="etag">Identifier assigned by the storage to the blob
        ///   that can be used to distinguish be successive version of the blob 
        ///   (useful to check for blob update).</param>
        /// <returns>
        /// If there is no such blob, the returned object
        /// has its property HasValue set to <c>false</c>.
        /// </returns>
        Maybe<T> GetBlob<T>(string containerName, string blobName, out string etag, IDataSerializer serializer = null);

        /// <summary>Gets a blob.</summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="blobName">Name of the blob.</param>
        /// <param name="type">The type of the blob.</param>
        /// <param name="etag">Identifier assigned by the storage to the blob
        ///   that can be used to distinguish be successive version of the blob 
        ///   (useful to check for blob update).</param>
        /// <returns>
        /// If there is no such blob, the returned object
        /// has its property HasValue set to <c>false</c>.
        /// </returns>
        /// <remarks>This method should only be used when the caller does not know the type of the
        /// object stored in the blob at compile time, but it can only be determined at run time.
        /// In all other cases, you should use the generic overloads of the method.</remarks>
        Maybe<object> GetBlob(string containerName, string blobName, Type type, out string etag, IDataSerializer serializer = null);

        /// <summary>ASYNC: Gets a blob.</summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="blobName">Name of the blob.</param>
        /// <param name="type">The type of the blob.</param>
        Task<BlobWithETag<object>> GetBlobAsync(string containerName, string blobName, Type type,
            CancellationToken cancellationToken, IDataSerializer serializer = null);

        /// <summary>
        /// Gets a blob in intermediate XML representation for inspection and recovery,
        /// if supported by the serialization formatter.
        /// </summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="blobName">Name of the blob.</param>
        /// <param name="etag">Identifier assigned by the storage to the blob
        ///   that can be used to distinguish be successive version of the blob 
        ///   (useful to check for blob update).</param>
        /// <returns>
        /// If there is no such blob or the formatter supports no XML representation,
        /// the returned object has its property HasValue set to <c>false</c>.
        /// </returns>
        Maybe<XElement> GetBlobXml(string containerName, string blobName, out string etag, IDataSerializer serializer = null);

        /// <summary>
        /// Gets a range of blobs.
        /// </summary>
        /// <typeparam name="T">Blob type.</typeparam>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="blobNames">Names of the blobs.</param>
        /// <param name="etags">Etag identifiers for all returned blobs.</param>
        /// <returns>For each requested blob, an element in the array is returned in the same order.
        /// If a specific blob was not found, the corresponding <b>etags</b> array element is <c>null</c>.</returns>
        Maybe<T>[] GetBlobRange<T>(string containerName, string[] blobNames, out string[] etags, IDataSerializer serializer = null);

        /// <summary>Gets a blob only if the etag has changed meantime.</summary>
        /// <typeparam name="T">Type of the blob.</typeparam>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="blobName">Name of the blob.</param>
        /// <param name="oldEtag">Old etag value. If this value is <c>null</c>, the blob will always
        ///   be retrieved (except if the blob does not exist anymore).</param>
        /// <param name="newEtag">New etag value. Will be <c>null</c> if the blob no more exist,
        ///   otherwise will be set to the current etag value of the blob.</param>
        /// <returns>
        /// If the blob has not been modified or if there is no such blob,
        /// then the returned object has its property HasValue set to <c>false</c>.
        /// </returns>
        Maybe<T> GetBlobIfModified<T>(string containerName, string blobName, string oldEtag, out string newEtag, IDataSerializer serializer = null);

        /// <summary>
        /// Gets the current etag of the blob, or <c>null</c> if the blob does not exists.
        /// </summary>
        string GetBlobEtag(string containerName, string blobName);

        /// <summary>
        /// ASYNC: Gets the current etag of the blob, or <c>null</c> if the blob does not exists.
        /// </summary>
        Task<string> GetBlobEtagAsync(string containerName, string blobName, CancellationToken cancellationToken);

        /// <summary>Puts a blob (overwrite if the blob already exists).</summary>
        /// <remarks>Creates the container if it does not exist beforehand.</remarks>
        void PutBlob<T>(string containerName, string blobName, T item, IDataSerializer serializer = null);

        /// <summary>Puts a blob and optionally overwrite.</summary>
        /// <remarks>Creates the container if it does not exist beforehand.</remarks>
        /// <returns><c>true</c> if the blob has been put and <c>false</c> if the blob already
        /// exists but could not be overwritten.</returns>
        bool PutBlob<T>(string containerName, string blobName, T item, bool overwrite, IDataSerializer serializer = null);

        /// <summary>Puts a blob and optionally overwrite.</summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="blobName">Name of the blob.</param>
        /// <param name="item">Item to be put.</param>
        /// <param name="overwrite">Indicates whether existing blob should be overwritten
        /// if it exists.</param>
        /// <param name="etag">New etag (identifier used to track for blob change) if
        /// the blob is written, or <c>null</c> if no blob is written.</param>
        /// <remarks>Creates the container if it does not exist beforehand.</remarks>
        /// <returns><c>true</c> if the blob has been put and <c>false</c> if the blob already
        /// exists but could not be overwritten.</returns>
        bool PutBlob<T>(string containerName, string blobName, T item, bool overwrite, out string etag, IDataSerializer serializer = null);

        /// <summary>Puts a blob only if etag given in argument is matching blob's etag in blobStorage.</summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="blobName">Name of the blob.</param>
        /// <param name="item">Item to be put.</param>
        /// <param name="expectedEtag">etag that should be matched inside BlobStorage.</param>
        /// <remarks>Creates the container if it does not exist beforehand.</remarks>
        /// <returns><c>true</c> if the blob has been put and <c>false</c> if the blob already
        /// exists but version were not matching.</returns>
        bool PutBlob<T>(string containerName, string blobName, T item, string expectedEtag, IDataSerializer serializer = null);

        /// <summary>Puts a blob and optionally overwrite.</summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="blobName">Name of the blob.</param>
        /// <param name="item">Item to be put.</param>
        /// <param name="type">The type of the blob.</param>
        /// <param name="overwrite">Indicates whether existing blob should be overwritten
        /// if it exists.</param>
        /// <param name="etag">New etag (identifier used to track for blob change) if
        /// the blob is written, or <c>null</c> if no blob is written.</param>
        /// <remarks>Creates the container if it does not exist beforehand.</remarks>
        /// <returns><c>true</c> if the blob has been put and <c>false</c> if the blob already
        /// exists but could not be overwritten.</returns>
        /// <remarks>This method should only be used when the caller does not know the type of the
        /// object stored in the blob at compile time, but it can only be determined at run time.
        /// In all other cases, you should use the generic overloads of the method.</remarks>
        bool PutBlob(string containerName, string blobName, object item, Type type, bool overwrite, out string etag, IDataSerializer serializer = null);

        /// <summary>ASYNC: Puts a blob and optionally overwrite.</summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="blobName">Name of the blob.</param>
        /// <param name="item">Item to be put.</param>
        /// <param name="type">The type of the blob.</param>
        /// <param name="overwrite">Indicates whether existing blob should be overwritten
        /// if it exists.</param>
        /// <param name="etag">New etag (identifier used to track for blob change) if
        /// the blob is written, or <c>null</c> if no blob is written.</param>
        /// <remarks>Creates the container if it does not exist beforehand.</remarks>
        Task<string> PutBlobAsync(string containerName, string blobName,
            object item, Type type, bool overwrite, string expectedEtag,
            CancellationToken cancellationToken, IDataSerializer serializer = null);

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
        Maybe<T> UpdateBlobIfExist<T>(string containerName, string blobName, Func<T, T> update, IDataSerializer serializer = null);

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
        Maybe<T> UpdateBlobIfExistOrSkip<T>(string containerName, string blobName, Func<T, Maybe<T>> update, IDataSerializer serializer = null);

        /// <summary>
        /// ASYNC: Updates a blob if it already exists.
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
        Task<BlobWithETag<T>> UpsertBlobOrSkipAsync<T>(string containerName, string blobName,
            Func<Maybe<T>> insert, Func<T, Maybe<T>> update, CancellationToken cancellationToken, IDataSerializer serializer = null);

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
        Maybe<T> UpdateBlobIfExistOrDelete<T>(string containerName, string blobName, Func<T, Maybe<T>> update, IDataSerializer serializer = null);

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
        T UpsertBlob<T>(string containerName, string blobName, Func<T> insert, Func<T, T> update, IDataSerializer serializer = null);

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
        Maybe<T> UpsertBlobOrSkip<T>(string containerName, string blobName, Func<Maybe<T>> insert, Func<T, Maybe<T>> update, IDataSerializer serializer = null);

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
        Maybe<T> UpsertBlobOrDelete<T>(string containerName, string blobName, Func<Maybe<T>> insert, Func<T, Maybe<T>> update, IDataSerializer serializer = null);

        /// <summary>Query whether a blob is locked by a blob lease.</summary>
        bool IsBlobLocked(string containerName, string blobName);

        /// <summary>Requests a new lease on the blob and returns its new lease ID</summary>
        Result<string> TryAcquireLease(string containerName, string blobName);

        /// <summary>Releases the lease of the blob if the provided lease ID matches.</summary>
        Result<string> TryReleaseLease(string containerName, string blobName, string leaseId);

        /// <summary>Renews the lease of the blob if the provided lease ID matches.</summary>
        Result<string> TryRenewLease(string containerName, string blobName, string leaseId);

    }
}