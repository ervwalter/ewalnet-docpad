#region Copyright (c) Lokad 2011-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Xml.Linq;

namespace Lokad.Cloud.Storage.Instrumentation.Events
{
    /// <summary>
    /// Raised whenever a message is quarantined because it failed to be processed multiple times.
    /// </summary>
    public class MessageProcessingFailedQuarantinedEvent : IStorageEvent
    {
        public StorageEventLevel Level { get { return StorageEventLevel.Warning; } }
        public string QueueName { get; private set; }
        public string QuarantineStoreName { get; private set; }
        public Type MessageType { get; private set; }
        public byte[] Data { get; private set; }

        public MessageProcessingFailedQuarantinedEvent(string queueName, string storeName, Type messageType, byte[] data)
        {
            QueueName = queueName;
            QuarantineStoreName = storeName;
            MessageType = messageType;
            Data = data;
        }

        public string Describe()
        {
            return string.Format("Storage: A message of type {0} in queue {1} failed to process repeatedly and has been quarantined.",
                MessageType.Name, QueueName);
        }

        public XElement DescribeMeta()
        {
            return new XElement("Meta",
                new XElement("Component", "Lokad.Cloud.Storage"),
                new XElement("Event", "MessageProcessingFailedQuarantinedEvent"));
        }
    }
}
