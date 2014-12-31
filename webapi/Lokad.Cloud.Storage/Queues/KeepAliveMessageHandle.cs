#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Threading;

namespace Lokad.Cloud.Storage
{
    public class KeepAliveMessageHandle<T> : IDisposable
        where T : class
    {
        private readonly IQueueStorageProvider _storage;
        private readonly Timer _timer;

        public T Message { get; private set; }

        public KeepAliveMessageHandle(T message, IQueueStorageProvider storage, TimeSpan keepAliveAfter, TimeSpan keepAlivePeriod)
        {
            _storage = storage;
            Message = message;

            _timer = new Timer(state => _storage.KeepAlive(Message), null, keepAliveAfter, keepAlivePeriod);
        }

        public void Delete()
        {
            _storage.Delete(Message);
        }

        public void Abandon()
        {
            _storage.Abandon(Message);
        }

        public void ResumeLater()
        {
            _storage.ResumeLater(Message);
        }

        void IDisposable.Dispose()
        {
            _timer.Dispose();
            _storage.Abandon(Message);
        }
    }
}
