#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.IO;
using System.Xml.Linq;
using Lokad.Cloud.Storage.Shared;

namespace Lokad.Cloud.Storage
{
    /// <summary>
    /// Optional extension for custom formatters supporting an
    /// intermediate xml representation for inspection and recovery.
    /// </summary>
    /// <remarks>
    /// This extension can be implemented even when the serializer
    /// is not xml based but in a format that can be transformed
    /// to xml easily in a robust way (i.e. more robust than
    /// deserializing to a full object). Note that formatters
    /// should be registered in IoC as IBinaryFormatter, not by
    /// this extension interface.
    /// </remarks>
    public interface IIntermediateDataSerializer : IDataSerializer
    {
        /// <summary>Unpack and transform an object from a stream to xml.</summary>
        XElement UnpackXml(Stream source);

        /// <summary>Transform and repack an object from xml to a stream.</summary>
        void RepackXml(XElement data, Stream destination);
    }
}