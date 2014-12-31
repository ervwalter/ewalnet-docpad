#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace Lokad.Cloud.Storage
{
    /// <summary>
    /// Raw byte pass-through formatter, supporting byte-array, Stream, string (UTF-8) and XElement (Root of UTF-8 XDocument) only.
    /// </summary>
    public class RawFormatter : IDataSerializer
    {
        /// <remarks>Supports byte[], XElement, Stream and string only</remarks>
        public void Serialize(object instance, Stream destination, Type type)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }

            if (type == typeof(Stream) && instance is Stream)
            {
                var stream = (Stream)instance;
                stream.CopyTo(destination);
                return;
            }

            if (type == typeof(XElement) && instance is XElement)
            {
                var document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), (XElement)instance);
                document.Save(destination);
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

            destination.Write(bytes, 0, bytes.Length);
        }

        /// <remarks>Supports byte[], XElement, Stream and string only</remarks>
        public object Deserialize(Stream source, Type type)
        {
            if (type == typeof(Stream))
            {
                var stream = new MemoryStream();
                source.CopyTo(stream);
                return stream;
            }

            if (type == typeof(XElement))
            {
                return XDocument.Load(source).Root;
            }

            byte[] bytes;
            var memorySource = source as MemoryStream;
            if (memorySource != null)
            {
                // shortcut if source is already a memory stream
                bytes = memorySource.ToArray();
            }
            else
            {
                using (var memoryStream = new MemoryStream())
                {
                    source.CopyTo(memoryStream);
                    bytes = memoryStream.ToArray();
                }
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