using System;
using System.IO;

namespace Lokad.Cloud.Storage.FileSystem
{
    public class MetadataPrefixStream : Stream
    {
        const int ETagOffset = 0;
        const int ETagLength = 16;
        const int FlagsOffset = ETagOffset + ETagLength;
        const int FlagsLength = 4;
        const int DataOffset = FlagsOffset + FlagsLength;

        readonly Stream _inner;

        public MetadataPrefixStream(Stream inner)
        {
            _inner = inner;
            _inner.Seek(DataOffset, SeekOrigin.Begin);
        }

        public string WriteNewETag()
        {
            if (_inner.Length < DataOffset)
            {
                _inner.SetLength(DataOffset);
            }

            var guid = Guid.NewGuid();
            var oldPosition = _inner.Position;
            _inner.Position = ETagOffset;
            _inner.Write(Guid.NewGuid().ToByteArray(), 0, ETagLength);
            _inner.Position = oldPosition;
            return guid.ToString("N");
        }

        public string ReadETag()
        {
            if (_inner.Length < DataOffset)
            {
                return null;
            }

            var oldPosition = _inner.Position;
            _inner.Position = ETagOffset;
            var guid = new byte[ETagLength];
            for (int k = 0; k < ETagLength; k += _inner.Read(guid, k, ETagLength - k)) { }
            _inner.Position = oldPosition;
            return new Guid(guid).ToString("N");
        }

        public void WriteFlags(byte flags)
        {
            if (_inner.Length < DataOffset)
            {
                _inner.SetLength(DataOffset);
            }

            var oldPosition = _inner.Position;
            _inner.Position = FlagsOffset;
            _inner.WriteByte(flags);
            _inner.Position = oldPosition;
        }

        public byte ReadFlags()
        {
            if (_inner.Length < DataOffset)
            {
                return 0;
            }

            var oldPosition = _inner.Position;
            _inner.Position = FlagsOffset;
            int flags = _inner.ReadByte();
            _inner.Position = oldPosition;
            return (byte)flags;
        }

        public override void Flush()
        {
            _inner.Flush();
        }

        public override long Length
        {
            get { return Math.Max(0, _inner.Length - DataOffset); }
        }

        public override void SetLength(long value)
        {
            _inner.SetLength(value + DataOffset);
        }

        public override long Position
        {
            get { return Math.Max(0, _inner.Position - DataOffset); }
            set { _inner.Position = value + DataOffset; }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    return _inner.Seek(offset + DataOffset, origin);
                default:
                    return _inner.Seek(offset, origin);
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _inner.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _inner.Write(buffer, offset, count);
        }

        public override bool CanRead
        {
            get { return _inner.CanRead; }
        }

        public override bool CanSeek
        {
            get { return _inner.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return _inner.CanWrite; }
        }
    }
}
