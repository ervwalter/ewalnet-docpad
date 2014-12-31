#region Copyright (c) Lokad 2011-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Xml.Linq;

namespace Lokad.Cloud.Storage.Instrumentation.Events
{
    /// <summary>
    /// Raised whenever a storage operation is retried.
    /// Useful for analyzing retry policy behavior.
    /// </summary>
    public class StorageOperationRetriedEvent : IStorageEvent
    {
        public StorageEventLevel Level { get { return StorageEventLevel.Trace; } }
        public Exception Exception { get; private set; }
        public string Policy { get; private set; }
        public int Trial { get; private set; }
        public TimeSpan Interval { get; private set; }
        public Guid TrialSequence { get; private set; }

        public StorageOperationRetriedEvent(Exception exception, string policy, int trial, TimeSpan interval, Guid trialSequence)
        {
            Exception = exception;
            Policy = policy;
            Trial = trial;
            Interval = interval;
            TrialSequence = trialSequence;
        }

        public string Describe()
        {
            return string.Format("Storage: Operation was retried on policy {0} ({1} trial): {2}",
                Policy, Trial, Exception != null ? Exception.Message : string.Empty);
        }

        public XElement DescribeMeta()
        {
            var meta = new XElement("Meta",
                new XElement("Component", "Lokad.Cloud.Storage"),
                new XElement("Event", "StorageOperationRetriedEvent"));

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
