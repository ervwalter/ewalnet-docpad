#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

namespace Lokad.Cloud.Storage
{
    public class BlobWithETag<T>
    {
        public T Blob { get; set; }
        public string ETag { get; set; }
    }
}
