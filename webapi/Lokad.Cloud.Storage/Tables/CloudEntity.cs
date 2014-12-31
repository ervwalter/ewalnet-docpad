#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

namespace Lokad.Cloud.Storage
{
    /// <summary>Entity to be stored by the <see cref="ITableStorageProvider"/>.</summary>
    /// <typeparam name="T">Type of the value carried by the entity.</typeparam>
    /// <remarks>Once serialized the <c>CloudEntity.Value</c> should weight less
    /// than 720KB to be compatible with Table Storage limitations on entities.</remarks>
    public class CloudEntity<T>
    {
        /// <summary>Indexed system property.</summary>
        public string RowKey { get; set; }

        /// <summary>Indexed system property.</summary>
        public string PartitionKey { get; set; }

        /// <summary>Flag indicating last update. Populated by the Table Storage.</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>ETag. Indicates changes. Populated by the Table Storage.</summary>
        public string ETag { get; set; }

        /// <summary>Value carried by the entity.</summary>
        public T Value { get; set; }
    }
}