#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

namespace Lokad.Cloud.Storage
{
    /// <summary>
    /// Blob reference, to be used a short hand
    /// while operating with the <see cref="IBlobStorageProvider"/>
    /// </summary>
    public interface IBlobLocation
    {
        /// <summary>
        /// Name of the container where the blob is located.
        /// </summary>
        string ContainerName { get; }

        /// <summary>
        /// Location of the blob inside of the container.
        /// </summary>
        string Path { get; }
    }
}
