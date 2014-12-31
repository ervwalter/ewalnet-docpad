#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace Lokad.Cloud.Storage
{
    /// <summary>
    /// Base class for untyped hierarchical blob names. Implementations should
    /// not inherit <see cref="UntypedBlobName"/>c> but <see cref="BlobName{T}"/> instead.
    /// </summary>
    [Serializable, DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/storage/2.0")]
    public abstract class UntypedBlobName : IBlobLocation
    {
        class InheritanceComparer : IComparer<Type>
        {
            public int Compare(Type x, Type y)
            {
                if (x.Equals(y)) return 0;
                return x.IsSubclassOf(y) ? 1 : -1;
            }
        }

        /// <summary>Sortable pattern for date times.</summary>
        /// <remarks>Hyphens can be eventually used to refine further the iteration.</remarks>
        public const string DateFormatInBlobName = "yyyy-MM-dd-HH-mm-ss";

        static readonly Dictionary<Type, Func<string, object>> Parsers = new Dictionary<Type, Func<string, object>>();
        static readonly Dictionary<Type, Func<object, string>> Printers = new Dictionary<Type, Func<object, string>>();

        /// <summary>
        /// Name of the container where the blob is located.
        /// </summary>
        public abstract string ContainerName { get; }

        /// <summary>
        /// Location of the blob inside of the container.
        /// </summary>
        public virtual string Path
        {
            get
            {
                return ToString();
            }
        }

        static UntypedBlobName()
        {
            // adding overrides

            // Guid: does not have default converter
            Printers.Add(typeof(Guid), o => ((Guid)o).ToString("N"));
            Parsers.Add(typeof(Guid), s => new Guid(s));

            // DateTime: sortable ascending;
            // NOTE: not time zone safe, users have to deal with that themselves
            Printers.Add(typeof(DateTime),
                o => ((DateTime)o).ToString(DateFormatInBlobName, CultureInfo.InvariantCulture));
            Parsers.Add(typeof(DateTime),
                s => DateTime.ParseExact(s, DateFormatInBlobName, CultureInfo.InvariantCulture));

            // DateTimeOffset: sortable ascending;
            // time zone safe, but always returned with UTC/zero offset (comparisons can deal with that)
            Printers.Add(typeof(DateTimeOffset),
                o => ((DateTimeOffset)o).UtcDateTime.ToString(DateFormatInBlobName, CultureInfo.InvariantCulture));
            Parsers.Add(typeof(DateTimeOffset),
                s => new DateTimeOffset(DateTime.SpecifyKind(DateTime.ParseExact(s, DateFormatInBlobName, CultureInfo.InvariantCulture), DateTimeKind.Utc)));
        }

        /// <summary>Syntactic equivalent to Print{T} with T being the current base type.</summary>
        public override string ToString()
        {
            // Invoke a Static Generic Method using Reflection
            // because type is programmatically defined
            var method = typeof(UntypedBlobName).GetMethod("Print", BindingFlags.Static | BindingFlags.Public);

            // Binding the method info to generic arguments
            method = method.MakeGenericMethod(new[] { GetType() });

            // Invoking the method and passing parameters
            // The null parameter is the object to call the method from. Since the method is static, pass null.
            return (string)method.Invoke(null, new object[] { this });
        }

        static object InternalParse(string value, Type type)
        {
            Func<string, object> parse;
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
            if (Parsers.TryGetValue(underlyingType, out parse))
            {
                return parse(value);
            }

            if (String.IsNullOrEmpty(value))
            {
                return null;
            }

            if (underlyingType.IsEnum)
            {
                return Enum.Parse(underlyingType, value);
            }

            return Convert.ChangeType(value, underlyingType);
        }

        static string InternalPrint(object value, Type type)
        {
            Func<object, string> print;
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
            if (Printers.TryGetValue(underlyingType, out print))
            {
                return print(value);
            }

            if (value == null)
            {
                return string.Empty;
            }

            return value.ToString();
        }

        class ConverterTypeCache<T>
        {
            static readonly MemberInfo[] Members; // either 'FieldInfo' or 'PropertyInfo'
            static readonly bool[] TreatDefaultAsNull;
            const string Delimeter = "/";

            static ConverterTypeCache()
            {
                // HACK: optimize this to IL code, if needed
                // NB: this approach could be used to generate F# style objects!
                Members = 
                    (typeof(T).GetFields().Select(f => (MemberInfo)f).Union(typeof(T).GetProperties()))
                    .Where(f => f.GetCustomAttributes(typeof(RankAttribute), true).Any())
                    // ordering always respect inheritance
                    .GroupBy(f => f.DeclaringType)
                    .OrderBy(g => g.Key, new InheritanceComparer())
                    .Select(g =>
                        g.OrderBy(f => ((RankAttribute)f.GetCustomAttributes(typeof(RankAttribute), true).First()).Index))
                    .SelectMany(f => f)
                    .ToArray();

                TreatDefaultAsNull = Members.Select(m =>
                    ((RankAttribute) (m.GetCustomAttributes(typeof (RankAttribute), true).First())).TreatDefaultAsNull).ToArray();
            }

            public static string Print(T instance)
            {
                var sb = new StringBuilder();
                for (int i = 0; i < Members.Length; i++)
                {
                    var info = Members[i];
                    var fieldInfo = info as FieldInfo;
                    var propInfo = info as PropertyInfo;

                    var memberType = (null != fieldInfo) ? fieldInfo.FieldType : propInfo.PropertyType;
                    var value = (null != fieldInfo) ? fieldInfo.GetValue(instance) : propInfo.GetValue(instance, new object[0]);
                    
                    if(null == value || (TreatDefaultAsNull[i] && IsDefaultValue(value, memberType)))
                    {
                        // Delimiter has to be appended here to avoid enumerating
                        // too many blog (names being prefix of each other).
                        //
                        // For example, without delimiter, the prefix 'foo/123' whould enumerate both
                        // foo/123/bar
                        // foo/1234/bar
                        //
                        // Then, we should not append a delimiter if prefix is entirely empty
                        // because it would not properly enumerate all blobs (semantic associated with
                        // empty prefix).
                        if (i > 0) sb.Append(Delimeter);
                        break;
                    }

                    var s = InternalPrint(value, memberType);
                    if (i > 0) sb.Append(Delimeter);
                    sb.Append(s);
                }
                return sb.ToString();
            }

            private static bool IsDefaultValue(object value, Type type)
            {
                if (type == typeof(string))
                {
                    return String.IsNullOrEmpty((string)value);
                }

                if (type.IsValueType)
                {
                    return Activator.CreateInstance(type).Equals(value);
                }

                return value == null;
            }

            public static T Parse(string value)
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                var split = value.Split(new[] { Delimeter }, StringSplitOptions.RemoveEmptyEntries);

                // In order to support parsing blob names also to blob name supper classes
                // in case of inheritance, we simply ignore supplementary items in the name
                if (split.Length < Members.Length)
                {
                    throw new ArgumentException("Number of items in the string is invalid. Are you missing something?", "value");
                }

                var parameters = new object[Members.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    var memberType = Members[i] is FieldInfo
                        ? ((FieldInfo) Members[i]).FieldType
                        : ((PropertyInfo) Members[i]).PropertyType;

                    parameters[i] = InternalParse(split[i], memberType);
                }

                // Initialization through reflection (no assumption on constructors)
                var name = (T)FormatterServices.GetUninitializedObject(typeof (T));
                for (int i = 0; i < Members.Length; i++)
                {
                    if (Members[i] is FieldInfo)
                    {
                        ((FieldInfo)Members[i]).SetValue(name, parameters[i]);
                    }
                    else
                    {
                        ((PropertyInfo)Members[i]).SetValue(name, parameters[i], new object[0]);
                    }
                }

                return name;
            }
        }

        /// <summary>Do not use directly, call <see cref="ToString"/> instead.</summary>
        public static string Print<T>(T instance) where T : UntypedBlobName
        {
            return ConverterTypeCache<T>.Print(instance);
        }

        /// <summary>Parse a hierarchical blob name.</summary>
        public static T Parse<T>(string value) where T : UntypedBlobName
        {
            return ConverterTypeCache<T>.Parse(value);
        }
    }
}