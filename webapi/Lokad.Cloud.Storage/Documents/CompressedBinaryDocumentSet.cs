#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.IO.Compression;

namespace Lokad.Cloud.Storage.Documents
{
    /// <summary>
    /// Base class for a set of documents to be serialized using a BinaryWriter
    /// </summary>
    public abstract class CompressedBinaryDocumentSet<TDocument, TKey> : DocumentSet<TDocument, TKey>, IDataSerializer
        where TDocument : class
    {
        public CompressedBinaryDocumentSet(
            IBlobStorageProvider blobs,
            Func<TKey, IBlobLocation> locationOfKey,
            Func<IBlobLocation> commonPrefix = null)
            : base(blobs, locationOfKey, commonPrefix)
        {
            Serializer = this;
        }

        protected abstract void Serialize(TDocument document, BinaryWriter writer);
        protected abstract TDocument Deserialize(BinaryReader reader);

        void IDataSerializer.Serialize(object instance, Stream destinationStream, Type type)
        {
            var document = instance as TDocument;
            if (document == null)
            {
                throw new NotSupportedException();
            }

            using (var compressed = new GZipStream(destinationStream, CompressionMode.Compress, true))
            using (var buffered = new BufferedStream(compressed, 4 * 1024))
            using (var writer = new BinaryWriter(buffered))
            {
                Serialize(document, writer);

                writer.Flush();
                buffered.Flush();
                compressed.Flush();
                compressed.Close();
            }
        }

        object IDataSerializer.Deserialize(Stream sourceStream, Type type)
        {
            using (var decompressed = new GZipStream(sourceStream, CompressionMode.Decompress, true))
            using (var reader = new BinaryReader(decompressed))
            {
                return Deserialize(reader);
            }
        }
    }
}
