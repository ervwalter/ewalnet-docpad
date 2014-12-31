#region Copyright (c) Lokad 2011-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Xml.Linq;

namespace Lokad.Cloud.Storage.Instrumentation.Events
{
    /// <summary>
    /// Generic task error.
    /// </summary>
    public class TaskFailedEvent : IStorageEvent
    {
        public StorageEventLevel Level { get { return StorageEventLevel.Warning; } }
        public Exception Exception { get; private set; }

        public TaskFailedEvent(Exception exception)
        {
            Exception = exception;
        }

        public string Describe()
        {
            return string.Format("Storage: generic task failure: {0}",
                Exception != null ? Exception.Message : string.Empty);
        }

        public XElement DescribeMeta()
        {
            var meta = new XElement("Meta",
                new XElement("Component", "Lokad.Cloud.Storage"),
                new XElement("Event", "TaskFailedEvent"));

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
