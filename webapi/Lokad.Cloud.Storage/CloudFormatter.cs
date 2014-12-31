#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Serialization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Lokad.Cloud.Storage
{
    /// <summary>
    /// Formatter based on <c>DataContractSerializer</c> and <c>NetDataContractSerializer</c>. 
    /// The formatter targets storage of persistent or transient data in the cloud storage.
    /// </summary>
    /// <remarks>
    /// If a <c>DataContract</c> attribute is present, then the <c>DataContractSerializer</c>
    /// is favored. If not, then the <c>NetDataContractSerializer</c> is used instead.
    /// This class is not <b>thread-safe</b>.
    /// </remarks>
    public class CloudFormatter : IIntermediateDataSerializer
    {
        static XmlObjectSerializer GetXmlSerializer(Type type)
        {
            // 'false' == do not inherit the attribute
            if (GetAttributes<DataContractAttribute>(type, false).Length > 0)
            {
                return new DataContractSerializer(type);
            }

            return new NetDataContractSerializer();
        }

        /// <summary>Serializes the object to the specified stream.</summary>
        /// <param name="instance">The instance.</param>
        /// <param name="destination">The destination stream.</param>
        /// <param name="type">The type of the object to serialize (can be a base type of the provided instance).</param>
        public void Serialize(object instance, Stream destination, Type type)
        {
            var serializer = GetXmlSerializer(type);

            using(var compressed = Compress(destination, true))
            using(var writer = XmlDictionaryWriter.CreateBinaryWriter(compressed, null, null, false))
            {
                serializer.WriteObject(writer, instance);
            }
        }

        /// <summary>Deserializes the object from specified source stream.</summary>
        /// <param name="source">The source stream.</param>
        /// <param name="type">The type of the object to deserialize.</param>
        /// <returns>deserialized object</returns>
        public object Deserialize(Stream source, Type type)
        {
            var serializer = GetXmlSerializer(type);

            using(var decompressed = Decompress(source, true))
            using(var reader = XmlDictionaryReader.CreateBinaryReader(decompressed, XmlDictionaryReaderQuotas.Max))
            {
                return serializer.ReadObject(reader);
            }
        }

        /// <remarks></remarks>
        public XElement UnpackXml(Stream source)
        {
            using(var decompressed = Decompress(source, true))
            using (var reader = XmlDictionaryReader.CreateBinaryReader(decompressed, XmlDictionaryReaderQuotas.Max))
            {
                return XElement.Load(reader);
            }
        }

        /// <remarks></remarks>
        public void RepackXml(XElement data, Stream destination)
        {
            using(var compressed = Compress(destination, true))
            using(var writer = XmlDictionaryWriter.CreateBinaryWriter(compressed, null, null, false))
            {
                data.Save(writer);
                writer.Flush();
                compressed.Flush();
            }
        }

        static GZipStream Compress(Stream stream, bool leaveOpen)
        {
            return new GZipStream(stream, CompressionMode.Compress, leaveOpen);
        }

        static GZipStream Decompress(Stream stream, bool leaveOpen)
        {
            return new GZipStream(stream, CompressionMode.Decompress, leaveOpen);
        }

        ///<summary>Retrieve attributes from the type.</summary>
        ///<param name="target">Type to perform operation upon</param>
        ///<param name="inherit"><see cref="MemberInfo.GetCustomAttributes(Type,bool)"/></param>
        ///<typeparam name="T">Attribute to use</typeparam>
        ///<returns>Empty array of <typeparamref name="T"/> if there are no attributes</returns>
        static T[] GetAttributes<T>(ICustomAttributeProvider target, bool inherit) where T : Attribute
        {
            if (target.IsDefined(typeof(T), inherit))
            {
                return target
                    .GetCustomAttributes(typeof(T), inherit)
                    .Select(a => (T)a).ToArray();
            }
            return new T[0];
        }
    }
}