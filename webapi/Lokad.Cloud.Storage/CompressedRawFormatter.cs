#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace Lokad.Cloud.Storage
{
    /// <summary>
    /// Gzip byte pass-through formatter, supporting byte-array, Stream, string (UTF-8) and XElement (Root of UTF-8 XDocument) only.
    /// </summary>
    public class CompressedRawFormatter : IDataSerializer
    {
        /// <remarks>Supports byte[], XElement, Stream and string only</remarks>
        public void Serialize(object instance, Stream destination, Type type)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }

            using (var compressed = new GZipStream(destination, CompressionMode.Compress, true))
            {
                if (type == typeof(Stream) && instance is Stream)
                {
                    var stream = (Stream)instance;
                    stream.CopyTo(compressed);
                    compressed.Close();
                    return;
                }

                if (type == typeof(XElement) && instance is XElement)
                {
                    var document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), (XElement)instance);
                    using (var buffered = new BufferedStream(compressed, 4 * 1024))
                    {
                        document.Save(buffered);
                        buffered.Flush();
                    }
                    compressed.Close();
                    return;
                }

                byte[] bytes;

                if (type == typeof(byte[]) && instance is byte[])
                {
                    bytes = (byte[])instance;
                }
                else if (type == typeof(string) && instance is string)
                {
                    bytes = Encoding.UTF8.GetBytes((string)instance);
                }
                else
                {
                    throw new NotSupportedException();
                }

                compressed.Write(bytes, 0, bytes.Length);
                compressed.Close();
            }
        }

        /// <remarks>Supports byte[], XElement, Stream and string only</remarks>
        public object Deserialize(Stream source, Type type)
        {
            using (var decompressed = new GZipStream(source, CompressionMode.Decompress, true))
            {
                if (type == typeof(Stream))
                {
                    var stream = new MemoryStream();
                    decompressed.CopyTo(stream);
                    return stream;
                }

                if (type == typeof(XElement))
                {
                    return XDocument.Load(decompressed).Root;
                }

                byte[] bytes;
                using (var memoryStream = new MemoryStream())
                {
                    decompressed.CopyTo(memoryStream);
                    bytes = memoryStream.ToArray();
                }

                if (type == typeof(byte[]))
                {
                    return bytes;
                }

                if (type == typeof(string))
                {
                    return Encoding.UTF8.GetString(bytes);
                }

                throw new NotSupportedException();
            }
        }
    }
}