#region Copyright (c) Lokad 2011-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Xml.Linq;

namespace Lokad.Cloud.Storage.Instrumentation.Events
{
    /// <summary>
    /// Raised whenever a message is quarantined because it could not be deserialized.
    /// Useful to monitor for serialization and data transport errors, alarm when it happens to often.
    /// </summary>
    public class MessageDeserializationFailedQuarantinedEvent : IStorageEvent
    {
        public StorageEventLevel Level { get { return StorageEventLevel.Warning; } }
        public AggregateException Exceptions { get; private set; }
        public string QueueName { get; private set; }
        public string QuarantineStoreName { get; private set; }
        public Type MessageType { get; private set; }
        public byte[] Data { get; private set; }

        public MessageDeserializationFailedQuarantinedEvent(AggregateException exceptions, string queueName, string storeName, Type messageType, byte[] data)
        {
            Exceptions = exceptions;
            QueueName = queueName;
            QuarantineStoreName = storeName;
            MessageType = messageType;
            Data = data;
        }

        public string Describe()
        {
            return string.Format("Storage: A message in queue {0} failed to deserialize to type {1} and has been quarantined.",
                QueueName, MessageType.Name);
        }

        public XElement DescribeMeta()
        {
            var meta = new XElement("Meta",
                new XElement("Component", "Lokad.Cloud.Storage"),
                new XElement("Event", "MessageDeserializationFailedQuarantinedEvent"));

            if (Exceptions != null)
            {
                var ex = Exceptions.GetBaseException();
                meta.Add(new XElement("Exception",
                    new XAttribute("typeName", ex.GetType().FullName),
                    new XAttribute("message", ex.Message),
                    ex.ToString()));
            }

            return meta;
        }
    }
}
