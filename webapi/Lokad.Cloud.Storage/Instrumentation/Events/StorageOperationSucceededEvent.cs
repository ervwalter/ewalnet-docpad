#region Copyright (c) Lokad 2011-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Xml.Linq;

namespace Lokad.Cloud.Storage.Instrumentation.Events
{
    /// <summary>
    /// Raised whenever a storage operation has succeeded.
    /// Useful for collecting usage statistics.
    /// </summary>
    public class StorageOperationSucceededEvent : IStorageEvent
    {
        public StorageEventLevel Level { get { return StorageEventLevel.Trace; } }
        public StorageOperationType OperationType { get; private set; }
        public TimeSpan Duration { get; private set; }

        public StorageOperationSucceededEvent(StorageOperationType operationType, TimeSpan duration)
        {
            OperationType = operationType;
            Duration = duration;
        }

        public string Describe()
        {
            return string.Format("Storage: {0} operation succeeded in {1:0.00}s",
                OperationType, Duration.TotalSeconds);
        }

        public XElement DescribeMeta()
        {
            return new XElement("Meta",
                new XElement("Component", "Lokad.Cloud.Storage"),
                new XElement("Event", "StorageOperationSucceededEvent"));
        }
    }
}
