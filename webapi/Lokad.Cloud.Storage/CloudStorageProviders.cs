#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

namespace Lokad.Cloud.Storage
{
    /// <summary>Storage providers and runtime providers.</summary>
    public class CloudStorageProviders
    {
        /// <summary>Blob Storage Abstraction.</summary>
        public IBlobStorageProvider BlobStorage { get; private set; }

        /// <summary>Queue Storage Abstraction.</summary>
        public IQueueStorageProvider QueueStorage { get; private set; }

        /// <summary>Table Storage Abstraction.</summary>
        public ITableStorageProvider TableStorage { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudStorageProviders"/> class.
        /// </summary>
        /// <param name="blobStorage">The blob storage provider.</param>
        /// <param name="queueStorage">The queue storage provider.</param>
        /// <param name="tableStorage">The table storage provider.</param>
        public CloudStorageProviders(
            IBlobStorageProvider blobStorage,
            IQueueStorageProvider queueStorage,
            ITableStorageProvider tableStorage)
        {
            BlobStorage = blobStorage;
            QueueStorage = queueStorage;
            TableStorage = tableStorage;
        }
    }
}
