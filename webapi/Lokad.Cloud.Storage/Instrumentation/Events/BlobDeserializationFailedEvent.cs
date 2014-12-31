#region Copyright (c) Lokad 2011-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Xml.Linq;

namespace Lokad.Cloud.Storage.Instrumentation.Events
{
    /// <summary>
    /// Raised whenever a blob is ignored because it could not be deserialized.
    /// Useful to monitor for serialization and data transport errors, alarm when it happens to often.
    /// </summary>
    public class BlobDeserializationFailedEvent : IStorageEvent
    {
        public StorageEventLevel Level { get { return StorageEventLevel.Error; } }
        public Exception Exception { get; private set; }
        public string ContainerName { get; private set; }
        public string BlobName { get; private set; }

        public BlobDeserializationFailedEvent(Exception exception, string containerName, string blobName)
        {
            Exception = exception;
            ContainerName = containerName;
            BlobName = blobName;
        }

        public string Describe()
        {
            return string.Format("Storage: A blob was retrieved but failed to deserialize. The blob was ignored. Blob {0} in container {1}. Reason: {2}",
                BlobName, ContainerName, Exception != null ? Exception.Message : "unknown");
        }

        public XElement DescribeMeta()
        {
            var meta = new XElement("Meta",
                new XElement("Component", "Lokad.Cloud.Storage"),
                new XElement("Event", "BlobDeserializationFailedEvent"));

            if (Exception != null)
            {
                meta.Add(new XElement("Exception",
                    new XAttribute("typeName", Exception.GetType().FullName),
                    new XAttribute("message", Exception.Message),
                    Exception.ToString()));
            }

            return meta;
        }
    }
}
