#region Copyright (c) Lokad 2011-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Xml.Linq;

namespace Lokad.Cloud.Storage.Instrumentation.Events
{
    /// <summary>
    /// Raised whenever a storage operation has finally failed (maybe after giving up retrials).
    /// </summary>
    public class StorageOperationFailedEvent : IStorageEvent
    {
        public StorageEventLevel Level { get { return StorageEventLevel.Error; } }
        public StorageOperationType OperationType { get; private set; }
        public Exception Exception { get; private set; }

        public StorageOperationFailedEvent(StorageOperationType operationType, Exception exception)
        {
            OperationType = operationType;
            Exception = exception;
        }

        public string Describe()
        {
            return string.Format("Storage: {0} operation failed: {1}",
                OperationType, Exception != null ? Exception.Message : string.Empty);
        }

        public XElement DescribeMeta()
        {
            var meta = new XElement("Meta",
                new XElement("Component", "Lokad.Cloud.Storage"),
                new XElement("Event", "StorageOperationFailedEvent"));

            if (Exception != null)
            {
                var ex = Exception.GetBaseException();
                meta.Add(new XElement("Exception",
                    new XAttribute("typeName", ex.GetType().FullName),
                    new XAttribute("message", ex.Message),
                    ex.ToString()));
            }

            return meta;
        }
    }
}
