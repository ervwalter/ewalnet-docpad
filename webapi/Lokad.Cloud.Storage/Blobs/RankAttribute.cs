#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

namespace Lokad.Cloud.Storage
{
    /// <summary>Used to specify the field position in the blob name.</summary>
    /// <remarks>The name (chosen as the abbreviation of "field position")
    /// is made compact not to make client code too verbose.</remarks>
    public class RankAttribute : Attribute
    {
        /// <summary>Index of the property within the generated blob name.</summary>
        public readonly int Index;

        /// <summary>Indicates whether the default value (for value types)
        /// should be treated as 'null'. Not relevant for class types.
        /// </summary>
        public readonly bool TreatDefaultAsNull;

        /// <summary>Position v</summary>
        public RankAttribute(int index)
        {
            Index = index;
        }

        /// <summary>Position v, and default behavior.</summary>
        public RankAttribute(int index, bool treatDefaultAsNull)
        {
            Index = index;
            TreatDefaultAsNull = treatDefaultAsNull;
        }
    }
}