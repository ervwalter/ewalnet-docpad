#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tu = System.Tuple<string, object, System.Collections.Generic.List<byte[]>>;

namespace Lokad.Cloud.Storage.InMemory
{
    /// <summary>Mock in-memory Queue Storage.</summary>
    public class MemoryQueueStorageProvider : IQueueStorageProvider
    {
        /// <summary>Root used to synchronize accesses to <c>_inprocess</c>.</summary>
        private readonly object _sync = new object();

        private readonly Dictionary<string, Queue<byte[]>> _queues;
        private readonly Dictionary<object, Tu> _inProcessMessages;
        private readonly HashSet<Tuple<string, string, string, byte[]>> _persistedMessages;

        internal IDataSerializer DefaultSerializer { get; set; }
        
        /// <summary>Default constructor.</summary>
        public MemoryQueueStorageProvider(IDataSerializer defaultSerializer = null)
        {
            _queues = new Dictionary<string, Queue<byte[]>>();
            _inProcessMessages = new Dictionary<object, Tu>();
            _persistedMessages = new HashSet<Tuple<string, string, string, byte[]>>();
            DefaultSerializer = defaultSerializer ?? new CloudFormatter();
        }

        /// <remarks></remarks>
        public IEnumerable<string> List(string prefix)
        {
            return _queues.Keys.Where(e => e.StartsWith(prefix));
        }

        /// <remarks></remarks>
        public IEnumerable<T> Get<T>(string queueName, int count, TimeSpan visibilityTimeout, int maxProcessingTrials, IDataSerializer serializer = null)
        {
            var dataSerializer = serializer ?? DefaultSerializer;
            lock (_sync)
            {
                var items = new List<T>(count);
                for (int i = 0; i < count; i++)
                {
                    if (_queues.ContainsKey(queueName) && _queues[queueName].Any())
                    {
                        var messageBytes = _queues[queueName].Dequeue();
                        object message;
                        using (var stream = new MemoryStream(messageBytes))
                        {
                            message = dataSerializer.Deserialize(stream, typeof (T));
                        }

                        Tu inProcess;
                        if (!_inProcessMessages.TryGetValue(message, out inProcess))
                        {
                            inProcess = new Tu(queueName, message, new List<byte[]>());
                            _inProcessMessages.Add(message, inProcess);
                        }

                        inProcess.Item3.Add(messageBytes);
                        items.Add((T)message);
                    }
                }
                return items;
            }
        }

        /// <remarks></remarks>
        public void Put<T>(string queueName, T message, TimeSpan timeToLive = default(TimeSpan), TimeSpan delay = default(TimeSpan), IDataSerializer serializer = null)
        {
            var dataSerializer = serializer ?? DefaultSerializer;
            lock (_sync)
            {
                byte[] messageBytes;
                using (var stream = new MemoryStream())
                {
                    dataSerializer.Serialize(message, stream, typeof(T));
                    messageBytes = stream.ToArray();
                }

                if (!_queues.ContainsKey(queueName))
                {
                    _queues.Add(queueName, new Queue<byte[]>());
                }

                _queues[queueName].Enqueue(messageBytes);
            }
        }

        /// <remarks></remarks>
        public void PutRange<T>(string queueName, IEnumerable<T> messages, TimeSpan timeToLive = default(TimeSpan), TimeSpan delay = default(TimeSpan), IDataSerializer serializer = null)
        {
            var dataSerializer = serializer ?? DefaultSerializer;
            lock (_sync)
            {
                foreach(var message in messages)
                {
                    Put(queueName, message, timeToLive, delay, dataSerializer);
                }
            }
        }

        public void PutRangeParallel<T>(string queueName, IEnumerable<T> messages, TimeSpan timeToLive = default(TimeSpan), TimeSpan delay = default(TimeSpan), IDataSerializer serializer = null)
        {
            PutRange(queueName, messages, timeToLive, delay, serializer);
        }

        /// <remarks></remarks>
        public void Clear(string queueName)
        {
            lock (_sync)
            {
                _queues[queueName].Clear();

                var toDelete = _inProcessMessages.Where(pair => pair.Value.Item1 == queueName).ToList();
                foreach(var pair in toDelete)
                {
                    _inProcessMessages.Remove(pair.Key);
                }
            }
        }

        public TimeSpan KeepAlive<T>(T message)
             where T : class
        {
            return TimeSpan.FromMinutes(5);
        }

        public int ReviveMessages(TimeSpan timeToLive = default(TimeSpan), TimeSpan delay = default(TimeSpan))
        {
            return 0;
        }

        /// <remarks></remarks>
        public bool Delete<T>(T message)
        {
            lock (_sync)
            {
                Tu inProcess;
                if (!_inProcessMessages.TryGetValue(message, out inProcess))
                {
                    return false;
                }

                inProcess.Item3.RemoveAt(0);
                if (inProcess.Item3.Count == 0)
                {
                    _inProcessMessages.Remove(inProcess.Item2);
                }

                return true;
            }
        }

        /// <remarks></remarks>
        public int DeleteRange<T>(IEnumerable<T> messages)
        {
            lock (_sync)
            {
                return messages.Where(Delete).Count();
            }
        }

        /// <remarks></remarks>
        public bool Abandon<T>(T message, TimeSpan timeToLive = default(TimeSpan), TimeSpan delay = default(TimeSpan))
        {
            lock (_sync)
            {
                Tu inProcess;
                if (!_inProcessMessages.TryGetValue(message, out inProcess))
                {
                    return false;
                }

                // Add back to queue
                if (!_queues.ContainsKey(inProcess.Item1))
                {
                    _queues.Add(inProcess.Item1, new Queue<byte[]>());
                }

                _queues[inProcess.Item1].Enqueue(inProcess.Item3[0]);

                // Remove from invisible queue
                inProcess.Item3.RemoveAt(0);
                if (inProcess.Item3.Count == 0)
                {
                    _inProcessMessages.Remove(inProcess.Item2);
                }

                return true;
            }
        }

        /// <remarks></remarks>
        public int AbandonRange<T>(IEnumerable<T> messages, TimeSpan timeToLive = default(TimeSpan), TimeSpan delay = default(TimeSpan))
        {
            lock (_sync)
            {
                return messages.Count(m => Abandon(m, timeToLive, delay));
            }
        }

        public int AbandonAll()
        {
            int count = 0;
            lock (_sync)
            {
                while (_inProcessMessages.Count > 0)
                {
                    count += AbandonRange(_inProcessMessages.Keys.ToList());
                }
            }
            return count;
        }

        /// <remarks></remarks>
        public bool ResumeLater<T>(T message, TimeSpan timeToLive = default(TimeSpan), TimeSpan delay = default(TimeSpan))
        {
            // same as abandon as the InMemory provider applies no poison detection
            return Abandon(message);
        }

        /// <remarks></remarks>
        public int ResumeLaterRange<T>(IEnumerable<T> messages, TimeSpan timeToLive = default(TimeSpan), TimeSpan delay = default(TimeSpan))
        {
            // same as abandon as the InMemory provider applies no poison detection
            return AbandonRange(messages);
        }

        /// <remarks></remarks>
        public void Persist<T>(T message, string storeName, string reason)
        {
            lock (_sync)
            {
                Tu inProcess;
                if (!_inProcessMessages.TryGetValue(message, out inProcess))
                {
                    return;
                }

                // persist
                var key = Guid.NewGuid().ToString("N");
                _persistedMessages.Add(Tuple.Create(storeName, key, inProcess.Item1, inProcess.Item3[0]));

                // Remove from invisible queue
                inProcess.Item3.RemoveAt(0);
                if (inProcess.Item3.Count == 0)
                {
                    _inProcessMessages.Remove(inProcess.Item2);
                }
            }
        }

        /// <remarks></remarks>
        public void PersistRange<T>(IEnumerable<T> messages, string storeName, string reason)
        {
            lock (_sync)
            {
                foreach(var message in messages)
                {
                    Persist(message, storeName, reason);
                }
            }
        }

        /// <remarks></remarks>
        public IEnumerable<string> ListPersisted(string storeName)
        {
            lock (_sync)
            {
                return _persistedMessages
                    .Where(x => x.Item1 == storeName)
                    .Select(x => x.Item2)
                    .ToArray();
            }
        }

        /// <remarks></remarks>
        public Maybe<PersistedMessage> GetPersisted(string storeName, string key, IDataSerializer serializer = null)
        {
            var intermediateDataSerializer = (serializer ?? DefaultSerializer) as IIntermediateDataSerializer;
            var xmlProvider = intermediateDataSerializer != null
                ? new Maybe<IIntermediateDataSerializer>(intermediateDataSerializer)
                : Maybe<IIntermediateDataSerializer>.Empty;

            lock (_sync)
            {
                var tuple = _persistedMessages.FirstOrDefault(x => x.Item1 == storeName && x.Item2 == key);
                if(null != tuple)
                {
                    return new PersistedMessage
                        {
                            QueueName = tuple.Item3,
                            StoreName = tuple.Item1,
                            Key = tuple.Item2,
                            IsDataAvailable = true,
                            DataXml = xmlProvider.Convert(s => s.UnpackXml(new MemoryStream(tuple.Item4)))
                        };
                }
                
                return Maybe<PersistedMessage>.Empty;
            }
        }

        /// <remarks></remarks>
        public void DeletePersisted(string storeName, string key, IDataSerializer serializer = null)
        {
            lock (_sync)
            {
                _persistedMessages.RemoveWhere(x => x.Item1 == storeName && x.Item2 == key);
            }
        }

        /// <remarks></remarks>
        public void RestorePersisted(string storeName, string key, TimeSpan timeToLive = default(TimeSpan), TimeSpan delay = default(TimeSpan))
        {
            lock (_sync)
            {
                var item = _persistedMessages.First(x => x.Item1 == storeName && x.Item2 == key);
                _persistedMessages.Remove(item);

                if (!_queues.ContainsKey(item.Item3))
                {
                    _queues.Add(item.Item3, new Queue<byte[]>());
                }

                _queues[item.Item3].Enqueue(item.Item4);

            }
        }

        /// <remarks></remarks>
        public bool DeleteQueue(string queueName)
        {
            lock (_sync)
            {
                if (!_queues.ContainsKey(queueName))
                {
                    return false;
                }

                _queues.Remove(queueName);

                var toDelete = _inProcessMessages.Where(pair => pair.Value.Item1 == queueName).ToList();
                foreach (var pair in toDelete)
                {
                    _inProcessMessages.Remove(pair.Key);
                }

                return true;
            }
        }

        /// <remarks></remarks>
        public int GetApproximateCount(string queueName)
        {
            lock (_sync)
            {
                Queue<byte[]> queue;
                return _queues.TryGetValue(queueName, out queue)
                    ? queue.Count : 0;
            }
        }

        /// <remarks></remarks>
        public Maybe<TimeSpan> GetApproximateLatency(string queueName)
        {
            return Maybe<TimeSpan>.Empty;
        }
    }
}
