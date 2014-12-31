#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;

namespace Lokad.Cloud.Storage
{
    /// <summary>Abstraction for the Table Storage.</summary>
    /// <remarks>This provider represents a logical abstraction of the Table Storage,
    /// not the Table Storage itself. In particular, implementations handle paging
    /// and query splitting internally. Also, this provider implicitly relies on
    /// serialization to handle generic entities (not constrained by the few datatypes
    /// available to the Table Storage).</remarks>
    public interface ITableStorageProvider
    {
        /// <summary>Creates a new table if it does not exist already.</summary>
        /// <returns><c>true</c> if a new table has been created.
        /// <c>false</c> if the table already exists.
        /// </returns>
        bool CreateTable(string tableName);

        /// <summary>Deletes a table if it exists.</summary>
        /// <returns><c>true</c> if the table has been deleted.
        /// <c>false</c> if the table does not exist.
        /// </returns>
        bool DeleteTable(string tableName);

        /// <summary>Returns the list of all the tables that exist in the storage.</summary>
        IEnumerable<string> GetTables();

        /// <summary>Iterates through all entities of a given table.</summary>
        /// <remarks>The enumeration is typically expected to be lazy, iterating through
        /// all the entities with paged request. If the table does not exist, an
        /// empty enumeration is returned.
        /// </remarks>
        IEnumerable<CloudEntity<T>> Get<T>(string tableName);

        /// <summary>Iterates through all entities of a given table and partition.</summary>
        /// <remarks><para>The enumeration is typically expected to be lazy, iterating through
        /// all the entities with paged request. If the table does not exists, or if the partition
        /// does not exists, an empty enumeration is returned.</para>
        /// </remarks>
        IEnumerable<CloudEntity<T>> Get<T>(string tableName, string partitionKey);

        /// <summary>Iterates through a range of entities of a given table and partition.</summary>
        /// <param name="tableName">Name of the Table.</param>
        /// <param name="partitionKey">Name of the partition which can not be null.</param>
        /// <param name="startRowKey">Inclusive start row key. If <c>null</c>, no start range
        /// constraint is enforced.</param>
        /// <param name="endRowKey">Exclusive end row key. If <c>null</c>, no ending range
        /// constraint is enforced.</param>
        /// <remarks>
        /// The enumeration is typically expected to be lazy, iterating through
        /// all the entities with paged request.The enumeration is ordered by row key.
        /// If the table or the partition key does not exist, the returned enumeration is empty.
        /// </remarks>
        IEnumerable<CloudEntity<T>> Get<T>(string tableName, string partitionKey, string startRowKey, string endRowKey);

        /// <summary>Iterates through all entities specified by their row keys.</summary>
        /// <param name="tableName">The name of the table. This table should exists otherwise the method will fail.</param>
        /// <param name="partitionKey">Partition key (can not be null).</param>
        /// <param name="rowKeys">lazy enumeration of non null string representing rowKeys.</param>
        /// <remarks>The enumeration is typically expected to be lazy, iterating through
        /// all the entities with paged request. If the table or the partition key does not exist,
        /// the returned enumeration is empty.</remarks>
        IEnumerable<CloudEntity<T>> Get<T>(string tableName, string partitionKey, IEnumerable<string> rowKeys);

        /// <summary>Inserts a collection of new entities into the table storage.</summary>
        /// <remarks>
        /// <para>The call is expected to fail on the first encountered already-existing
        /// entity. Results are not garanteed if one or several entities already exist.
        /// </para>
        /// <para>There is no upper limit on the number of entities provided through
        /// the enumeration. The implementations are expected to lazily iterates
        /// and to create batch requests as the move forward.
        /// </para>
        /// <para>If the table does not exist then it should be created.</para>
        /// <warning>Idempotence is not enforced.</warning>
        /// </remarks>
        ///<exception cref="InvalidOperationException"> if an already existing entity has been encountered.</exception>
        void Insert<T>(string tableName, IEnumerable<CloudEntity<T>> entities);

        /// <summary>Updates a collection of existing entities into the table storage.</summary>
        /// <remarks>
        /// <para>The call is expected to fail on the first non-existing entity. 
        /// Results are not garanteed if one or several entities do not exist already.
        /// </para>
        /// <para>If <paramref name="force"/> is <c>false</c>, the call is expected to
        /// fail if one or several entities have changed in the meantime. If <c>true</c>,
        /// the entities are overwritten even if they've been changed remotely in the meantime.
        /// </para>
        /// <para>There is no upper limit on the number of entities provided through
        /// the enumeration. The implementations are expected to lazily iterates
        /// and to create batch requests as the move forward.
        /// </para>
        /// <para>Idempotence of the implementation is required.</para>
        /// </remarks>
        /// <exception cref="InvalidOperationException"> thrown if the table does not exist
        /// or an non-existing entity has been encountered.</exception>
        void Update<T>(string tableName, IEnumerable<CloudEntity<T>> entities, bool force);

        /// <summary>Updates or insert a collection of existing entities into the table storage.</summary>
        /// <remarks>
        /// <para>New entities will be inserted. Existing entities will be updated,
        /// even if they have changed remotely in the meantime.
        /// </para>
        /// <para>There is no upper limit on the number of entities provided through
        /// the enumeration. The implementations are expected to lazily iterates
        /// and to create batch requests as the move forward.
        /// </para>
        /// <para>If the table does not exist then it should be created.</para>
        /// <para>Idempotence of the implementation is required.</para>
        /// </remarks>
        void Upsert<T>(string tableName, IEnumerable<CloudEntity<T>> entities);

        /// <summary>Deletes all specified entities.</summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="partitionKey">The partition key (assumed to be non null).</param>
        /// <param name="rowKeys">Lazy enumeration of non null string representing the row keys.</param>
        /// <remarks>
        /// <para>
        /// The implementation is expected to lazily iterate through all row keys
        /// and send batch deletion request to the underlying storage.</para>
        /// <para>Idempotence of the method is required.</para>
        /// <para>The method should not fail if the table does not exist.</para>
        /// </remarks>
        void Delete<T>(string tableName, string partitionKey, IEnumerable<string> rowKeys);

        /// <summary>Deletes a collection of entities.</summary>
        /// <remarks>
        /// <para>
        /// The implementation is expected to lazily iterate through all row keys
        /// and send batch deletion request to the underlying storage.</para>
        /// <para>Idempotence of the method is required.</para>
        /// <para>The method should not fail if the table does not exist.</para>
        /// <para>If <paramref name="force"/> is <c>false</c>, the call is expected to
        /// fail if one or several entities have changed remotely in the meantime. If <c>true</c>,
        /// the entities are deleted even if they've been changed remotely in the meantime.
        /// </para>
        /// </remarks>
        void Delete<T>(string tableName, IEnumerable<CloudEntity<T>> entities, bool force);
    }
}