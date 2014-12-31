#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Storage
{
    /// <summary>
    /// The purpose of the <see cref="MessageEnvelope"/> is to provide
    /// additional metadata for a message.
    /// </summary>
    [DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/storage/2.0"), Serializable]
    internal sealed class MessageEnvelope
    {
        [DataMember(Order = 1)]
        public int DequeueCount { get; set; }

        [DataMember(Order = 2)]
        public byte[] RawMessage { get; set; }
    }
}