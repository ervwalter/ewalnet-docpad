#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Storage
{
    /// <summary>The purpose of the <see cref="MessageWrapper"/> is to gracefully
    /// handle messages that are too large of the queue storage (or messages that 
    /// happen to be already stored in the Blob Storage).</summary>
    [DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/storage/2.0"), Serializable]
    internal sealed class MessageWrapper
    {
        [DataMember(Order = 1)]
        public string ContainerName { get; set; }

        [DataMember(Order = 2)]
        public string BlobName { get; set; }
    }
}