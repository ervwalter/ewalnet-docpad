#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Storage.Azure
{
    /// <summary>
    /// Exception indicating that received data has been detected to be corrupt or altered.
    /// </summary>
    [Serializable]
    public class DataCorruptionException : Exception
    {
        /// <remarks></remarks>
        public DataCorruptionException() { }

        /// <remarks></remarks>
        public DataCorruptionException(string message) : base(message) { }

        /// <remarks></remarks>
        public DataCorruptionException(string message, Exception inner) : base(message, inner) { }

        /// <remarks></remarks>
        protected DataCorruptionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
