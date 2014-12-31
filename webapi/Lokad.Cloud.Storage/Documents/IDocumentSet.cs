#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;

namespace Lokad.Cloud.Storage.Documents
{
    /// <summary>
    /// Represents a set of documents and how they are persisted.
    /// </summary>
    public interface IDocumentSet<TDocument, TKey>
    {
        /// <summary>
        /// Try to read the document, if it exists.
        /// </summary>
        bool TryGet(TKey key, out TDocument document);

        /// <summary>
        /// Delete the document, if it exists.
        /// </summary>
        bool DeleteIfExist(TKey key);

        /// <summary>
        /// Write the document. If it already exists, overwrite it.
        /// </summary>
        void InsertOrReplace(TKey key, TDocument document);

        /// <summary>
        /// If the document already exists, update it. If it does not exist yet, do nothing.
        /// </summary>
        TDocument UpdateIfExist(TKey key, Func<TDocument, TDocument> updateDocument);

        /// <summary>
        /// Load the current document, or create a default document if it does not exist yet.
        /// Then update the document with the provided update function and persist the result.
        /// </summary>
        TDocument Update(TKey key, Func<TDocument, TDocument> updateDocument, Func<TDocument> defaultIfNotExist);

        /// <summary>
        /// If the document already exists, update it with the provided update function.
        /// If the document does not exist yet, insert a new document with the provided insert function.
        /// </summary>
        TDocument UpdateOrInsert(TKey key, Func<TDocument, TDocument> updateDocument, Func<TDocument> insertDocument);

        /// <summary>
        /// List the keys of all documents. Not all document sets will support this,
        /// those that do not will throw a NotSupportedException.
        /// </summary>
        /// <exception cref="NotSupportedException" />
        IEnumerable<TKey> ListAllKeys();

        /// <summary>
        /// Read all documents matching the provided prefix.
        /// Not all document sets will support this, those that
        /// do not will throw a NotSupportedException.
        /// </summary>
        /// <exception cref="NotSupportedException" />
        IEnumerable<TDocument> GetAll();

        /// <summary>
        /// Delete all document matching the provided prefix.
        /// Not all document sets will support this, those that
        /// do not will throw a NotSupportedException.
        /// </summary>
        /// <exception cref="NotSupportedException" />
        void DeleteAll();
    }
}