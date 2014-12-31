#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Storage
{
    /// <summary>
    /// Blob reference, to be used a short hand
    /// while operating with the <see cref="IBlobStorageProvider"/>
    /// </summary>
    [Serializable, DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/storage/2.0")]
    public class BlobLocationAndType<T> : IBlobLocationAndType<T>
    {
        /// <summary>
        /// Name of the container where the blob is located.
        /// </summary>
        [DataMember(Order = 1)]
        public string ContainerName { get; private set; }

        /// <summary>
        /// Location of the blob inside of the container.
        /// </summary>
        [DataMember(Order = 2)]
        public string Path { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobLocationAndType{T}"/> class.
        /// </summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="path">The path.</param>
        public BlobLocationAndType(string containerName, string path)
        {
            ContainerName = containerName;
            Path = path;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobLocationAndType{T}"/> class,
        /// pointing to the same location (copy) as the provided location.
        /// </summary>
        public BlobLocationAndType(IBlobLocation fromLocation)
        {
            ContainerName = fromLocation.ContainerName;
            Path = fromLocation.Path;
        }
    }
}
