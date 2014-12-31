#region Copyright (c) Lokad 2011-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Lokad.Cloud.Storage.Instrumentation.Events
{
    /// <summary>
    /// Raised whenever one or more messages have been revived
    /// (e.g. from kee-alive messages that were no longer kept alive).
    /// </summary>
    public class MessagesRevivedEvent : IStorageEvent
    {
        public StorageEventLevel Level { get { return StorageEventLevel.Warning; } }
        public Dictionary<string, int> MessageCountByQueueName { get; private set; }

        public MessagesRevivedEvent(Dictionary<string, int> messageCountByQueueName)
        {
            MessageCountByQueueName = messageCountByQueueName;
        }

        public string Describe()
        {
            return string.Format("Storage: Messages have been revived: {0}.", string.Join(", ", MessageCountByQueueName.Select(p => string.Format("{0} from {1}", p.Value, p.Key))));
        }

        public XElement DescribeMeta()
        {
            return new XElement("Meta",
                new XElement("Component", "Lokad.Cloud.Storage"),
                new XElement("Event", "MessagesRevivedEvent"));
        }
    }
}
