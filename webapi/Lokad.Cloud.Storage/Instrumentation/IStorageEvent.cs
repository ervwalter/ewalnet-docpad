#region Copyright (c) Lokad 2011-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Xml.Linq;

namespace Lokad.Cloud.Storage.Instrumentation
{
    public interface IStorageEvent
    {
        StorageEventLevel Level { get; }
        string Describe();
        XElement DescribeMeta();
    }
}
