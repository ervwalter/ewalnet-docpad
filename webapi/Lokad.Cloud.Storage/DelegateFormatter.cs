#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;

namespace Lokad.Cloud.Storage
{
    /// <summary>
    /// Delegate formatter for ad-hoc scenarios
    /// </summary>
    public class DelegateFormatter : IDataSerializer
    {
        private readonly Action<object, Type, Stream> _serialize;
        private readonly Func<Type, Stream, object> _deserialize;

        public DelegateFormatter(Action<object, Type, Stream> serialize, Func<Type, Stream, object> deserialize)
        {
            _serialize = serialize;
            _deserialize = deserialize;
        }

        public void Serialize(object instance, Stream destinationStream, Type type)
        {
            _serialize(instance, type, destinationStream);
        }

        public object Deserialize(Stream sourceStream, Type type)
        {
            return _deserialize(type, sourceStream);
        }
    }
}
