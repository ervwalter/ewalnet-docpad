using System;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Storage
{
    public class ConcurrencyException : Exception
    {
        public ConcurrencyException()
        {
        }

        protected ConcurrencyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
