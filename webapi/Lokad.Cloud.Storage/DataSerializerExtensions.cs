#region Copyright (c) Lokad 2010-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml.Linq;

namespace Lokad.Cloud.Storage
{
    internal static class DataSerializerExtensions
    {
        public static Result<T, Exception> TryDeserializeAs<T>(this IDataSerializer serializer, Stream source)
        {
            var position = source.Position;
            try
            {
                var result = serializer.Deserialize(source, typeof(T));
                if (result == null)
                {
                    return Result<T, Exception>.CreateError(new SerializationException("Serializer returned null"));
                }

                if (!(result is T))
                {
                    return Result<T, Exception>.CreateError(new InvalidCastException(
                        String.Format("Source was expected to be of type {0} but was of type {1}.",
                            typeof (T).Name,
                            result.GetType().Name)));
                }

                return Result<T, Exception>.CreateSuccess((T)result);
            }
            catch (Exception e)
            {
                return Result<T, Exception>.CreateError(e);
            }
            finally
            {
                source.Position = position;
            }
        }

        public static Result<object, Exception> TryDeserialize(this IDataSerializer serializer, Stream source, Type type)
        {
            var position = source.Position;
            try
            {
                var result = serializer.Deserialize(source, type);
                if (result == null)
                {
                    return Result<object, Exception>.CreateError(new SerializationException("Serializer returned null"));
                }

                var actualType = result.GetType();
                if (!type.IsAssignableFrom(actualType))
                {
                    return Result<object, Exception>.CreateError(new InvalidCastException(
                        String.Format("Source was expected to be of type {0} but was of type {1}.",
                            type.Name,
                            actualType.Name)));
                }

                return Result<object, Exception>.CreateSuccess(result);
            }
            catch (Exception e)
            {
                return Result<object, Exception>.CreateError(e);
            }
            finally
            {
                source.Position = position;
            }
        }

        public static Result<T, Exception> TryDeserializeAs<T>(this IDataSerializer serializer, byte[] source)
        {
            using (var stream = new MemoryStream(source))
            {
                return TryDeserializeAs<T>(serializer, stream);
            }
        }

        public static Result<XElement, Exception> TryUnpackXml(this IIntermediateDataSerializer serializer, Stream source)
        {
            var position = source.Position;
            try
            {
                var result = serializer.UnpackXml(source);
                if (result == null)
                {
                    return Result<XElement, Exception>.CreateError(new SerializationException("Serializer returned null"));
                }

                return Result<XElement, Exception>.CreateSuccess(result);
            }
            catch (Exception e)
            {
                return Result<XElement, Exception>.CreateError(e);
            }
            finally
            {
                source.Position = position;
            }
        }
    }
}
