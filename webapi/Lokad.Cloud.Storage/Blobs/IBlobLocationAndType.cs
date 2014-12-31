#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

namespace Lokad.Cloud.Storage
{
    /// <summary>
    /// Typed blob reference, to be used a short hand
    /// while operating with the <see cref="IBlobStorageProvider"/>
    /// </summary>
    public interface IBlobLocationAndType<T> : IBlobLocation
    {
    }
}
