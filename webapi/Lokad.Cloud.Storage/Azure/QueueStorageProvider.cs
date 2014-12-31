#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Lokad.Cloud.Storage.Instrumentation;
using Lokad.Cloud.Storage.Instrumentation.Events;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Queue.Protocol;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;

namespace Lokad.Cloud.Storage.Azure
{
    /// <summary>Provides access to the Queue Storage (plus the Blob Storage when
    /// messages are overflowing).</summary>
    /// <remarks>
    /// <para>
    /// Overflowing messages are stored in blob storage and normally deleted as with
    /// their originating correspondence in queue storage.
    /// </para>
    /// <para>All the methods of <see cref="QueueStorageProvider"/> are thread-safe.</para>
    /// </remarks>
    public class QueueStorageProvider : IQueueStorageProvider
    {
        internal const string OverflowingMessagesContainerName = "lokad-cloud-overflowing-messages";
        internal const string ResilientMessagesContainerName = "lokad-cloud-resilient-messages";
        internal const string ResilientLeasesContainerName = "lokad-cloud-resilient-leases";
        internal const string PoisonedMessagePersistenceStoreName = "failing-messages";
        private static readonly TimeSpan KeepAliveVisibilityTimeout = TimeSpan.FromSeconds(60);

        /// <summary>Root used to synchronize accesses to <c>_inprocess</c>. 
        /// Caution: do not hold the lock while performing operations on the cloud
        /// storage.</summary>
        readonly object _sync = new object();

        readonly CloudQueueClient _queueStorage;
        readonly IBlobStorageProvider _blobStorage;
        readonly IDataSerializer _defaultSerializer;
        readonly RetryPolicies _policies;
        readonly IStorageObserver _observer;

        // messages currently being processed (boolean property indicates if the message is overflowing)
        /// <summary>Mapping object --> Queue Message Id. Use to delete messages afterward.</summary>
        readonly Dictionary<object, InProcessMessage> _inProcessMessages;

        /// <summary>IoC constructor.</summary>
        /// <param name="blobStorage">Not null.</param>
        /// <param name="queueStorage">Not null.</param>
        /// <param name="defaultSerializer">Not null.</param>
        /// <param name="observer">Can be <see langword="null"/>.</param>
        public QueueStorageProvider(
            CloudQueueClient queueStorage,
            IBlobStorageProvider blobStorage,
            IDataSerializer defaultSerializer = null,
            IStorageObserver observer = null)
        {
            _policies = new RetryPolicies(observer);
            _queueStorage = queueStorage;
            _blobStorage = blobStorage;
            _defaultSerializer = defaultSerializer ?? new CloudFormatter();
            _observer = observer;

            _inProcessMessages = new Dictionary<object, InProcessMessage>(20, new IdentityComparer());
        }

        /// <remarks></remarks>
        public IEnumerable<string> List(string prefix)
        {
            return _queueStorage.ListQueues(prefix).Select(queue => queue.Name);
        }

        /// <remarks></remarks>
        public IEnumerable<T> Get<T>(string queueName, int count, TimeSpan visibilityTimeout, int maxProcessingTrials, IDataSerializer serializer = null)
        {
            var stopwatch = Stopwatch.StartNew();
            var dataSerializer = serializer ?? _defaultSerializer;

            var queue = _queueStorage.GetQueueReference(queueName);

            // 1. GET RAW MESSAGES

            IEnumerable<CloudQueueMessage> rawMessages;

            try
            {
                rawMessages = Retry.Get(_policies.TransientServerErrorBackOff(), CancellationToken.None, () => queue.GetMessages(count, visibilityTimeout));
            }
            catch (StorageException ex)
            {
                // if the queue does not exist return an empty collection.
                if (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    return new T[0];
                }

                throw;
            }

            // 2. SKIP EMPTY QUEUE

            if (null == rawMessages)
            {
                NotifySucceeded(StorageOperationType.QueueGet, stopwatch);
                return new T[0];
            }

            // 3. DESERIALIZE MESSAGE OR MESSAGE WRAPPER, CHECK-OUT

            var messages = new List<T>(count);
            var wrappedMessages = new List<MessageWrapper>();

            foreach (var rawMessage in rawMessages)
            {
                // 3.1. DESERIALIZE MESSAGE, CHECK-OUT, COLLECT WRAPPED MESSAGES TO BE UNWRAPPED LATER

                var data = rawMessage.AsBytes;
                var stream = new MemoryStream(data);
                try
                {
                    var dequeueCount = rawMessage.DequeueCount;

                    // 3.1.1 UNPACK ENVELOPE IF PACKED, UPDATE POISONING INDICATOR

                    var messageAsEnvelope = dataSerializer.TryDeserializeAs<MessageEnvelope>(stream);
                    if (messageAsEnvelope.IsSuccess)
                    {
                        stream.Dispose();
                        dequeueCount += messageAsEnvelope.Value.DequeueCount;
                        data = messageAsEnvelope.Value.RawMessage;
                        stream = new MemoryStream(data);
                    }

                    // 3.1.2 PERSIST POISONED MESSAGE, SKIP

                    if (dequeueCount > maxProcessingTrials)
                    {
                        // we want to persist the unpacked message (no envelope) but still need to drop
                        // the original message, that's why we pass the original rawMessage but the unpacked data
                        PersistRawMessage(rawMessage, data, queueName, PoisonedMessagePersistenceStoreName,
                            String.Format("Message was dequeued {0} times but failed processing each time.", dequeueCount - 1));

                        if (_observer != null)
                        {
                            _observer.Notify(new MessageProcessingFailedQuarantinedEvent(queueName, PoisonedMessagePersistenceStoreName, typeof(T), data));
                        }

                        continue;
                    }

                    // 3.1.3 DESERIALIZE MESSAGE IF POSSIBLE

                    var messageAsT = dataSerializer.TryDeserializeAs<T>(stream);
                    if (messageAsT.IsSuccess)
                    {
                        messages.Add(messageAsT.Value);
                        CheckOutMessage(messageAsT.Value, rawMessage, data, queueName, false, dequeueCount, dataSerializer);

                        continue;
                    }

                    // 3.1.4 DESERIALIZE WRAPPER IF POSSIBLE

                    var messageAsWrapper = dataSerializer.TryDeserializeAs<MessageWrapper>(stream);
                    if (messageAsWrapper.IsSuccess)
                    {
                        // we don't retrieve messages while holding the lock
                        wrappedMessages.Add(messageAsWrapper.Value);
                        CheckOutMessage(messageAsWrapper.Value, rawMessage, data, queueName, true, dequeueCount, dataSerializer);

                        continue;
                    }

                    // 3.1.5 PERSIST FAILED MESSAGE, SKIP

                    // we want to persist the unpacked message (no envelope) but still need to drop
                    // the original message, that's why we pass the original rawMessage but the unpacked data
                    PersistRawMessage(rawMessage, data, queueName, PoisonedMessagePersistenceStoreName,
                        String.Format("Message failed to deserialize:\r\nAs {0}:\r\n{1}\r\n\r\nAs MessageEnvelope:\r\n{2}\r\n\r\nAs MessageWrapper:\r\n{3}",
                            typeof (T).FullName, messageAsT.Error, messageAsEnvelope.IsSuccess ? "unwrapped" : messageAsEnvelope.Error.ToString(), messageAsWrapper.Error));

                    if (_observer != null)
                    {
                        var exceptions = new List<Exception> { messageAsT.Error, messageAsWrapper.Error };
                        if (!messageAsEnvelope.IsSuccess) { exceptions.Add(messageAsEnvelope.Error); }
                        _observer.Notify(new MessageDeserializationFailedQuarantinedEvent(new AggregateException(exceptions), queueName, PoisonedMessagePersistenceStoreName, typeof(T), data));
                    }
                }
                finally
                {
                    stream.Dispose();
                }
            }

            // 4. UNWRAP WRAPPED MESSAGES

            var unwrapStopwatch = new Stopwatch();
            foreach (var mw in wrappedMessages)
            {
                unwrapStopwatch.Restart();

                string ignored;
                var blobContent = _blobStorage.GetBlob(mw.ContainerName, mw.BlobName, typeof(T), out ignored);

                // blob may not exists in (rare) case of failure just before queue deletion
                // but after container deletion (or also timeout deletion).
                if (!blobContent.HasValue)
                {
                    CloudQueueMessage rawMessage;
                    lock (_sync)
                    {
                        rawMessage = _inProcessMessages[mw].RawMessages[0];
                        CheckInMessage(mw);
                    }

                    DeleteRawMessage(rawMessage, queue);

                    // skipping the message if it can't be unwrapped
                    continue;
                }

                T innerMessage = (T)blobContent.Value;

                // substitution: message wrapper replaced by actual item in '_inprocess' list
                CheckOutRelink(mw, innerMessage);

                messages.Add(innerMessage);
                NotifySucceeded(StorageOperationType.QueueUnwrap, unwrapStopwatch);
            }

            NotifySucceeded(StorageOperationType.QueueGet, stopwatch);

            // 5. RETURN LIST OF MESSAGES

            return messages;
        }

        /// <remarks></remarks>
        public void Put<T>(string queueName, T message, TimeSpan timeToLive = default(TimeSpan), TimeSpan delay = default(TimeSpan), IDataSerializer serializer = null)
        {
            PutRange(queueName, new[] { message }, timeToLive, delay, serializer);
        }

        /// <remarks></remarks>
        public void PutRange<T>(string queueName, IEnumerable<T> messages, TimeSpan timeToLive = default(TimeSpan), TimeSpan delay = default(TimeSpan), IDataSerializer serializer = null)
        {
            var dataSerializer = serializer ?? _defaultSerializer;
            var queue = _queueStorage.GetQueueReference(queueName);
            var stopwatch = new Stopwatch();

            foreach (var message in messages)
            {
                stopwatch.Restart();

                var queueMessage = SerializeCloudQueueMessage(queueName, message, dataSerializer);

                PutRawMessage(queueMessage, queue, timeToLive, delay);

                NotifySucceeded(StorageOperationType.QueuePut, stopwatch);
            }
        }

        /// <remarks></remarks>
        public void PutRangeParallel<T>(string queueName, IEnumerable<T> messages, TimeSpan timeToLive = default(TimeSpan), TimeSpan delay = default(TimeSpan), IDataSerializer serializer = null)
        {
            var dataSerializer = serializer ?? _defaultSerializer;
            var queue = _queueStorage.GetQueueReference(queueName);
            var stopwatch = new Stopwatch();

            var tasks = new List<Task>();

            foreach (var message in messages)
            {
                stopwatch.Restart();

                var queueMessage = SerializeCloudQueueMessage(queueName, message, dataSerializer);

                var task = Task.Factory.StartNew(() => PutRawMessage(queueMessage, queue, timeToLive, delay));
                task.ContinueWith(obj => NotifySucceeded(StorageOperationType.QueuePut, stopwatch), TaskContinuationOptions.OnlyOnRanToCompletion);

                tasks.Add(task);
            }

            try
            {
                Task.WaitAll(tasks.ToArray());
            }
            catch (AggregateException ae)
            {
                throw ae.Flatten();
            }
        }

        private CloudQueueMessage SerializeCloudQueueMessage<T>(string queueName, T message, IDataSerializer serializer)
        {
            CloudQueueMessage queueMessage;
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(message, stream, typeof (T));

                // Caution: MaxMessageSize is not related to the number of bytes
                // but the number of characters when Base64-encoded:

                if (stream.Length >= (CloudQueueMessage.MaxMessageSize - 1)/4*3)
                {
                    queueMessage = new CloudQueueMessage(PutOverflowingMessageAndWrap(queueName, message, serializer));
                }
                else
                {
                    try
                    {
                        queueMessage = new CloudQueueMessage(stream.ToArray());
                    }
                    catch (ArgumentException)
                    {
                        queueMessage = new CloudQueueMessage(PutOverflowingMessageAndWrap(queueName, message, serializer));
                    }
                }
            }
            return queueMessage;
        }

        byte[] PutOverflowingMessageAndWrap<T>(string queueName, T message, IDataSerializer serializer)
        {
            var stopwatch = Stopwatch.StartNew();

            var blobRef = OverflowingMessageBlobName<T>.GetNew(queueName);

            // HACK: In this case serialization is performed another time (internally)
            _blobStorage.PutBlob(blobRef, message);

            var mw = new MessageWrapper
                {
                    ContainerName = blobRef.ContainerName,
                    BlobName = blobRef.ToString()
                };

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(mw, stream, typeof(MessageWrapper));
                var serializerWrapper = stream.ToArray();

                NotifySucceeded(StorageOperationType.QueueWrap, stopwatch);

                return serializerWrapper;
            }
        }

        /// <remarks></remarks>
        void DeleteOverflowingMessages(string queueName)
        {
            _blobStorage.DeleteAllBlobs(OverflowingMessagesContainerName, queueName);
        }

        /// <remarks></remarks>
        public void Clear(string queueName)
        {
            try
            {
                // caution: call 'DeleteOverflowingMessages' first (BASE).
                DeleteOverflowingMessages(queueName);
                var queue = _queueStorage.GetQueueReference(queueName);
                Action action = () => queue.Clear();
                Retry.Do(_policies.TransientServerErrorBackOff(), CancellationToken.None, action);
            }
            catch (StorageException ex)
            {
                // if the queue does not exist do nothing
                if (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    return;
                }
                throw;
            }
        }

        public TimeSpan KeepAlive<T>(T message)
             where T : class
        {
            if (!IdentityComparer.CanDifferentiateInstances(typeof(T)))
            {
                throw new NotSupportedException("KeepAlive supports neither strings nor value types");
            }

            CloudQueueMessage rawMessage;
            string queueName;
            byte[] data;
            string blobName;
            string blobLease;

            lock (_sync)
            {
                InProcessMessage inProcMsg;
                if (!_inProcessMessages.TryGetValue(message, out inProcMsg) || inProcMsg.CommitStarted)
                {
                    // CASE: the message has already been handled => we ignore the request
                    return TimeSpan.Zero;
                }

                rawMessage = inProcMsg.RawMessages[0];
                queueName = inProcMsg.QueueName;
                data = inProcMsg.Data;
                blobName = inProcMsg.KeepAliveBlobName;
                blobLease = inProcMsg.KeepAliveBlobLease;

                if (blobName == null)
                {
                    // CASE: this is the first call to KeepAlive.
                    // => choose a name and set the initial invisibility time; continue
                    blobName = inProcMsg.KeepAliveBlobName = Guid.NewGuid().ToString("N");
                    inProcMsg.KeepAliveTimeout = DateTimeOffset.UtcNow + KeepAliveVisibilityTimeout;
                }
                else if (blobLease == null)
                {
                    // CASE: the message is already being initialized. This can happen
                    // e.g. on two calls to KeepAlive form different threads (race).
                    // => do nothing, but only return the remaining invisibility time
                    return inProcMsg.KeepAliveTimeout - DateTimeOffset.UtcNow;
                }

                // ELSE CASE: this is a successive call; continue
            }

            if (blobLease != null)
            {
                // CASE: this is a successive call, the message is already resilient
                // => just renew the lease

                bool messageAlreadyHandled = false;
                Retry.DoUntilTrue(_policies.OptimisticConcurrency(), CancellationToken.None, () =>
                    {
                        var result = _blobStorage.TryRenewLease(ResilientLeasesContainerName, blobName, blobLease);
                        if (result.IsSuccess)
                        {
                            // CASE: success
                            return true;
                        }

                        if (result.Error == "NotFound")
                        {
                            // CASE: we managed to loose our lease file, meaning that we must have lost our lease
                            // (maybe because we didn't renew in time) and the message was handled in the meantime.
                            // => do nothing
                            messageAlreadyHandled = true;
                            return true;
                        }

                        if (result.Error == "Conflict")
                        {
                            // CASE: we managed to loose our lease and someone acquired it in the meantime
                            // => try to re-aquire a new lease

                            var newLease = _blobStorage.TryAcquireLease(ResilientLeasesContainerName, blobName);
                            if (newLease.IsSuccess)
                            {
                                // CASE: we managed to re-acquire the lost lease.
                                // However, if the message blob is no longer present then the message was already handled and we need to retreat

                                if (_blobStorage.GetBlobEtag(ResilientMessagesContainerName, blobName) == null)
                                {
                                    Retry.DoUntilTrue(_policies.OptimisticConcurrency(), CancellationToken.None, () =>
                                        {
                                            var retreatResult = _blobStorage.TryReleaseLease(ResilientLeasesContainerName, blobName, newLease.Value);
                                            return retreatResult.IsSuccess || result.Error == "NotFound";
                                        });

                                    messageAlreadyHandled = true;
                                    return true;
                                }

                                blobLease = newLease.Value;
                                return true;
                            }

                            if (newLease.Error == "NotFound")
                            {
                                // CASE: we managed to loose our lease file, meaning that we must have lost our lease
                                // (maybe because we didn't renew in time) and the message was handled in the meantime.
                                // => do nothing
                                messageAlreadyHandled = true;
                                return true;
                            }

                            // still conflict or transient error, retry
                            return false;
                        }

                        return false;
                    });

                if (messageAlreadyHandled)
                {
                    return TimeSpan.Zero;
                }

                lock (_sync)
                {
                    InProcessMessage inProcMsg;
                    if (!_inProcessMessages.TryGetValue(message, out inProcMsg) || inProcMsg.CommitStarted)
                    {
                        // CASE: Renew worked, but in the meantime the message has already be handled
                        // => do nothing
                        return TimeSpan.Zero;
                    }

                    // CASE: renew succeeded, or we managed to acquire a new lease
                    inProcMsg.KeepAliveTimeout = DateTimeOffset.UtcNow - KeepAliveVisibilityTimeout;
                    inProcMsg.KeepAliveBlobLease = blobLease;
                    return KeepAliveVisibilityTimeout;
                }
            }

            // CASE: this is the first call to KeepAlive

            // 1. CREATE LEASE OBJECT

            _blobStorage.PutBlob(ResilientLeasesContainerName, blobName, new ResilientLeaseData { QueueName = queueName, BlobName = blobName });

            // 2. TAKE LEASE ON LEASE OBJECT

            Retry.DoUntilTrue(_policies.OptimisticConcurrency(), CancellationToken.None, () =>
                {
                    var lease = _blobStorage.TryAcquireLease(ResilientLeasesContainerName, blobName);
                    if (lease.IsSuccess)
                    {
                        blobLease = lease.Value;
                        return true;
                    }

                    if (lease.Error == "NotFound")
                    {
                        // CASE: lease blob has been deleted before we could acquire the lease
                        // => recreate the blob, then retry
                        _blobStorage.PutBlob(ResilientLeasesContainerName, blobName, new ResilientLeaseData { QueueName = queueName, BlobName = blobName });
                        return false;
                    }

                    // CASE: conflict (e.g. because ReviveMessages is running), or transient error
                    // => retry
                    return false;
                });

            // 3. PUT MESSAGE TO BLOB

            _blobStorage.PutBlob(ResilientMessagesContainerName, blobName, new ResilientMessageData { QueueName = queueName, Data = data });

            // 4. UPDATE IN-PROCESS-MESSAGE

            bool rollback = false;
            lock (_sync)
            {
                InProcessMessage inProcMsg;
                if (!_inProcessMessages.TryGetValue(message, out inProcMsg) || inProcMsg.CommitStarted)
                {
                    rollback = true;
                }
                else
                {
                    inProcMsg.KeepAliveBlobLease = blobLease;
                }
            }

            // 5. ROLLBACK IF MESSAGE HAS BEEN HANDLED IN THE MEANTIME

            if (rollback)
            {
                // CASE: The message has been handled in the meantime (so this call should be ignored)
                // => Drop all the blobs we created and exit

                _blobStorage.DeleteBlobIfExist(ResilientMessagesContainerName, blobName);

                Retry.DoUntilTrue(_policies.OptimisticConcurrency(), CancellationToken.None, () =>
                    {
                        var result = _blobStorage.TryReleaseLease(ResilientLeasesContainerName, blobName, blobLease);
                        if (result.IsSuccess)
                        {
                            _blobStorage.DeleteBlobIfExist(ResilientLeasesContainerName, blobName);
                            return true;
                        }
                        return result.Error == "NotFound";
                    });

                return TimeSpan.Zero;
            }

            // 6. DELETE MESSAGE FROM THE QUEUE

            var queue = _queueStorage.GetQueueReference(queueName);
            DeleteRawMessage(rawMessage, queue);

            return KeepAliveVisibilityTimeout;
        }

        public int ReviveMessages(TimeSpan timeToLive = default(TimeSpan), TimeSpan delay = default(TimeSpan))
        {
            var candidates = _blobStorage.ListBlobNames(ResilientLeasesContainerName)
                .Where(name => !_blobStorage.IsBlobLocked(ResilientLeasesContainerName, name))
                .Take(50).ToList();

            var messagesByQueue = new Dictionary<string, int>();
            foreach (var blobName in candidates)
            {
                var lease = _blobStorage.TryAcquireLease(ResilientLeasesContainerName, blobName);
                if (!lease.IsSuccess)
                {
                    continue;
                }

                try
                {
                    var messageBlob = _blobStorage.GetBlob<ResilientMessageData>(ResilientMessagesContainerName, blobName);
                    if (!messageBlob.HasValue)
                    {
                        continue;
                    }

                    // CASE: we were able to acquire a lease and can read the original message blob.
                    // => Restore the message

                    var messageData = messageBlob.Value;
                    var queue = _queueStorage.GetQueueReference(messageData.QueueName);
                    var rawMessage = new CloudQueueMessage(messageData.Data);
                    PutRawMessage(rawMessage, queue, timeToLive, delay);

                    if (DeleteKeepAliveMessage(blobName, lease.Value))
                    {
                        int oldCount;
                        if (messagesByQueue.TryGetValue(messageData.QueueName, out oldCount))
                        {
                            messagesByQueue[messageData.QueueName] = oldCount + 1;
                        }
                        else
                        {
                            messagesByQueue[messageData.QueueName] = 1;
                        }
                    }
                }
                finally
                {
                    Retry.DoUntilTrue(_policies.OptimisticConcurrency(), CancellationToken.None, () =>
                        {
                            var result = _blobStorage.TryReleaseLease(ResilientLeasesContainerName, blobName, lease.Value);
                            return result.IsSuccess || result.Error == "NotFound" || result.Error == "Conflict";
                        });
                }
            }

            if (_observer != null && messagesByQueue.Count > 0)
            {
                _observer.Notify(new MessagesRevivedEvent(messagesByQueue));
            }

            return messagesByQueue.Sum(p => p.Value);
        }

        /// <remarks></remarks>
        public bool Delete<T>(T message)
        {
            var stopwatch = new Stopwatch();

            // 1. GET RAW MESSAGE & QUEUE, OR SKIP IF NOT AVAILABLE/ALREADY DELETED

            CloudQueueMessage rawMessage;
            string queueName;
            bool isOverflowing;
            byte[] data;
            IDataSerializer dataSerializer;
            string keepAliveBlobName;
            string keepAliveBlobLease;

            lock (_sync)
            {
                // ignoring message if already deleted
                InProcessMessage inProcMsg;
                if (!_inProcessMessages.TryGetValue(message, out inProcMsg) || (IdentityComparer.CanDifferentiateInstances(typeof(T)) && inProcMsg.CommitStarted))
                {
                    return false;
                }

                rawMessage = inProcMsg.RawMessages[0];
                isOverflowing = inProcMsg.IsOverflowing;
                queueName = inProcMsg.QueueName;
                data = inProcMsg.Data;
                dataSerializer = inProcMsg.Serializer;
                keepAliveBlobName = inProcMsg.KeepAliveBlobName;
                keepAliveBlobLease = inProcMsg.KeepAliveBlobLease;

                inProcMsg.CommitStarted = true;
            }

            // 2. DELETING THE OVERFLOW BLOB, IF WRAPPED

            if (isOverflowing)
            {
                var messageWrapper = dataSerializer.TryDeserializeAs<MessageWrapper>(data);
                if (messageWrapper.IsSuccess)
                {
                    _blobStorage.DeleteBlobIfExist(messageWrapper.Value.ContainerName, messageWrapper.Value.BlobName);
                }
            }

            // 3. DELETE THE MESSAGE FROM THE QUEUE

            bool deleted;
            if (keepAliveBlobName != null && keepAliveBlobLease != null)
            {
                // CASE: in resilient mode
                // => release locks, delete blobs
                deleted = DeleteKeepAliveMessage(keepAliveBlobName, keepAliveBlobLease);
            }
            else
            {
                // CASE: normal mode (or keep alive in progress)
                // => just delete the message
                var queue = _queueStorage.GetQueueReference(queueName);
                deleted = DeleteRawMessage(rawMessage, queue);
            }

            // 4. REMOVE THE RAW MESSAGE

            CheckInMessage(message);

            if (deleted)
            {
                NotifySucceeded(StorageOperationType.QueueDelete, stopwatch);
                return true;
            }
            
            return false;
        }

        /// <remarks></remarks>
        public int DeleteRange<T>(IEnumerable<T> messages)
        {
            return messages.Count(Delete);
        }

        /// <remarks></remarks>
        public bool Abandon<T>(T message, TimeSpan timeToLive = default(TimeSpan), TimeSpan delay = default(TimeSpan))
        {
            var stopwatch = new Stopwatch();

            // 1. GET RAW MESSAGE & QUEUE, OR SKIP IF NOT AVAILABLE/ALREADY DELETED

            CloudQueueMessage oldRawMessage;
            string queueName;
            int dequeueCount;
            byte[] data;
            IDataSerializer dataSerializer;
            string keepAliveBlobName;
            string keepAliveBlobLease;

            lock (_sync)
            {
                // ignoring message if already deleted
                InProcessMessage inProcMsg;
                if (!_inProcessMessages.TryGetValue(message, out inProcMsg) || (IdentityComparer.CanDifferentiateInstances(typeof(T)) && inProcMsg.CommitStarted))
                {
                    return false;
                }

                queueName = inProcMsg.QueueName;
                dequeueCount = inProcMsg.DequeueCount;
                oldRawMessage = inProcMsg.RawMessages[0];
                data = inProcMsg.Data;
                dataSerializer = inProcMsg.Serializer;
                keepAliveBlobName = inProcMsg.KeepAliveBlobName;
                keepAliveBlobLease = inProcMsg.KeepAliveBlobLease;

                inProcMsg.CommitStarted = true;
            }

            var queue = _queueStorage.GetQueueReference(queueName);

            // 2. CLONE THE MESSAGE AND PUT IT TO THE QUEUE
            // we always use an envelope here since the dequeue count
            // is always >0, which we should continue to track in order
            // to make poison detection possible at all.

            var envelope = new MessageEnvelope
            {
                DequeueCount = dequeueCount,
                RawMessage = data
            };

            CloudQueueMessage newRawMessage = null;
            using (var stream = new MemoryStream())
            {
                dataSerializer.Serialize(envelope, stream, typeof(MessageEnvelope));
                if (stream.Length < (CloudQueueMessage.MaxMessageSize - 1) / 4 * 3)
                {
                    try
                    {
                        newRawMessage = new CloudQueueMessage(stream.ToArray());
                    }
                    catch (ArgumentException) { }
                }

                if (newRawMessage == null)
                {
                    envelope.RawMessage = PutOverflowingMessageAndWrap(queueName, message, dataSerializer);
                    using (var wrappedStream = new MemoryStream())
                    {
                        dataSerializer.Serialize(envelope, wrappedStream, typeof(MessageEnvelope));
                        newRawMessage = new CloudQueueMessage(wrappedStream.ToArray());
                    }
                }
            }
            PutRawMessage(newRawMessage, queue, timeToLive, delay);

            // 3. DELETE THE OLD MESSAGE FROM THE QUEUE

            bool deleted;
            if (keepAliveBlobName != null && keepAliveBlobLease != null)
            {
                // CASE: in resilient mode
                // => release locks, delete blobs
                deleted = DeleteKeepAliveMessage(keepAliveBlobName, keepAliveBlobLease);
            }
            else
            {
                // CASE: normal mode (or keep alive in progress)
                // => just delete the message
                deleted = DeleteRawMessage(oldRawMessage, queue);
            }

            // 4. REMOVE THE RAW MESSAGE

            CheckInMessage(message);

            if (deleted)
            {
                NotifySucceeded(StorageOperationType.QueueAbandon, stopwatch);
                return true;
            }

            return false;
        }

        /// <remarks></remarks>
        public int AbandonRange<T>(IEnumerable<T> messages, TimeSpan timeToLive = default(TimeSpan), TimeSpan delay = default(TimeSpan))
        {
            return messages.Count(m => Abandon(m, timeToLive, delay));
        }

        public int AbandonAll()
        {
            int count = 0;
            while(true)
            {
                List<object> messages;
                lock (_sync)
                {
                    messages = _inProcessMessages.Keys.ToList();
                }

                if (messages.Count == 0)
                {
                    return count;
                }

                count += AbandonRange(messages);
            }
        }

        /// <remarks></remarks>
        public bool ResumeLater<T>(T message, TimeSpan timeToLive = default(TimeSpan), TimeSpan delay = default(TimeSpan))
        {
            string queueName;

            lock (_sync)
            {
                // ignoring message if already deleted
                InProcessMessage inProcMsg;
                if (!_inProcessMessages.TryGetValue(message, out inProcMsg))
                {
                    return false;
                }

                queueName = inProcMsg.QueueName;
            }

            Put(queueName, message, timeToLive, delay);
            return Delete(message);
        }

        /// <remarks></remarks>
        public int ResumeLaterRange<T>(IEnumerable<T> messages, TimeSpan timeToLive = default(TimeSpan), TimeSpan delay = default(TimeSpan))
        {
            return messages.Count(m => ResumeLater(m, timeToLive, delay));
        }

        /// <remarks></remarks>
        public void Persist<T>(T message, string storeName, string reason)
        {
            // 1. GET MESSAGE FROM CHECK-OUT, SKIP IF NOT AVAILABLE/ALREADY DELETED

            CloudQueueMessage rawMessage;
            string queueName;
            byte[] data;

            lock (_sync)
            {
                // ignoring message if already deleted
                InProcessMessage inProcessMessage;
                if (!_inProcessMessages.TryGetValue(message, out inProcessMessage))
                {
                    return;
                }

                queueName = inProcessMessage.QueueName;
                rawMessage = inProcessMessage.RawMessages[0];
                data = inProcessMessage.Data;
            }

            // 2. PERSIST MESSAGE AND DELETE FROM QUEUE

            PersistRawMessage(rawMessage, data, queueName, storeName, reason);

            // 3. REMOVE MESSAGE FROM CHECK-OUT

            CheckInMessage(message);
        }

        /// <remarks></remarks>
        public void PersistRange<T>(IEnumerable<T> messages, string storeName, string reason)
        {
            foreach (var message in messages)
            {
                Persist(message, storeName, reason);
            }
        }

        /// <remarks></remarks>
        public IEnumerable<string> ListPersisted(string storeName)
        {
            var blobPrefix = PersistedMessageBlobName.GetPrefix(storeName);
            return _blobStorage.ListBlobNames(blobPrefix).Select(blobReference => blobReference.Key);
        }

        /// <remarks></remarks>
        public Maybe<PersistedMessage> GetPersisted(string storeName, string key, IDataSerializer serializer = null)
        {
            var dataSerializer = serializer ?? _defaultSerializer;

            // 1. GET PERSISTED MESSAGE BLOB

            var blobReference = new PersistedMessageBlobName(storeName, key);
            var blob = _blobStorage.GetBlob(blobReference);
            if (!blob.HasValue)
            {
                return Maybe<PersistedMessage>.Empty;
            }

            var persistedMessage = blob.Value;
            var data = persistedMessage.Data;
            var dataXml = Maybe<XElement>.Empty;

            // 2. IF WRAPPED, UNWRAP; UNPACK XML IF SUPPORTED

            bool dataForRestorationAvailable;
            var messageWrapper = dataSerializer.TryDeserializeAs<MessageWrapper>(data);
            if (messageWrapper.IsSuccess)
            {
                string ignored;
                dataXml = _blobStorage.GetBlobXml(messageWrapper.Value.ContainerName, messageWrapper.Value.BlobName, out ignored);
                
                // We consider data to be available only if we can access its blob's data
                // Simplification: we assume that if we can get the data as xml, then we can also get its binary data
                dataForRestorationAvailable = dataXml.HasValue;
            }
            else
            {
                var intermediateSerializer = dataSerializer as IIntermediateDataSerializer;
                if (intermediateSerializer != null)
                {
                    using (var stream = new MemoryStream(data))
                    {
                        var unpacked = intermediateSerializer.TryUnpackXml(stream);
                        dataXml = unpacked.IsSuccess ? unpacked.Value : Maybe<XElement>.Empty;
                    }
                }

                // The message is not wrapped (or unwrapping it failed).
                // No matter whether we can get the xml, we do have access to the binary data
                dataForRestorationAvailable = true;
            }

            // 3. RETURN

            return new PersistedMessage
                {
                    QueueName = persistedMessage.QueueName,
                    StoreName = storeName,
                    Key = key,
                    InsertionTime = persistedMessage.InsertionTime,
                    PersistenceTime = persistedMessage.PersistenceTime,
                    DequeueCount = persistedMessage.DequeueCount,
                    Reason = persistedMessage.Reason,
                    DataXml = dataXml,
                    IsDataAvailable = dataForRestorationAvailable,
                };
        }

        /// <remarks></remarks>
        public void DeletePersisted(string storeName, string key, IDataSerializer serializer = null)
        {
            var dataSerializer = serializer ?? _defaultSerializer;

            // 1. GET PERSISTED MESSAGE BLOB

            var blobReference = new PersistedMessageBlobName(storeName, key);
            var blob = _blobStorage.GetBlob(blobReference);
            if (!blob.HasValue)
            {
                return;
            }

            var persistedMessage = blob.Value;

            // 2. IF WRAPPED, UNWRAP AND DELETE BLOB

            var messageWrapper = dataSerializer.TryDeserializeAs<MessageWrapper>(persistedMessage.Data);
            if (messageWrapper.IsSuccess)
            {
                _blobStorage.DeleteBlobIfExist(messageWrapper.Value.ContainerName, messageWrapper.Value.BlobName);
            }

            // 3. DELETE PERSISTED MESSAGE

            _blobStorage.DeleteBlobIfExist(blobReference);
        }

        /// <remarks></remarks>
        public void RestorePersisted(string storeName, string key, TimeSpan timeToLive = default(TimeSpan), TimeSpan delay = default(TimeSpan))
        {
            // 1. GET PERSISTED MESSAGE BLOB

            var blobReference = new PersistedMessageBlobName(storeName, key);
            var blob = _blobStorage.GetBlob(blobReference);
            if(!blob.HasValue)
            {
                return;
            }

            var persistedMessage = blob.Value;

            // 2. PUT MESSAGE TO QUEUE

            var queue = _queueStorage.GetQueueReference(persistedMessage.QueueName);
            var rawMessage = new CloudQueueMessage(persistedMessage.Data);
            PutRawMessage(rawMessage, queue, timeToLive, delay);

            // 3. DELETE PERSISTED MESSAGE

            _blobStorage.DeleteBlobIfExist(blobReference);
        }

        void PersistRawMessage(CloudQueueMessage message, byte[] data, string queueName, string storeName, string reason)
        {
            var stopwatch = Stopwatch.StartNew();

            var queue = _queueStorage.GetQueueReference(queueName);

            // 1. PERSIST MESSAGE TO BLOB

            var blobReference = PersistedMessageBlobName.GetNew(storeName);
            var persistedMessage = new PersistedMessageData
                {
                    QueueName = queueName,
                    InsertionTime = message.InsertionTime.Value,
                    PersistenceTime = DateTimeOffset.UtcNow,
                    DequeueCount = message.DequeueCount,
                    Reason = reason,
                    Data = data,
                };

            _blobStorage.PutBlob(blobReference, persistedMessage);

            // 2. DELETE MESSAGE FROM QUEUE

            DeleteRawMessage(message, queue);

            NotifySucceeded(StorageOperationType.QueuePersist, stopwatch);
        }

        bool DeleteRawMessage(CloudQueueMessage message, CloudQueue queue)
        {
            try
            {
                Retry.Do(_policies.TransientServerErrorBackOff(), CancellationToken.None, () => queue.DeleteMessage(message));
                return true;
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    return false;
                }

                var info = ex.RequestInformation.ExtendedErrorInformation;
                if (info == null)
                {
                    throw;
                }

                if (info.ErrorCode == QueueErrorCodeStrings.PopReceiptMismatch)
                {
                    return false;
                }

                if (info.ErrorCode == QueueErrorCodeStrings.QueueNotFound)
                {
                    return false;
                }

                throw;
            }
        }

        bool DeleteKeepAliveMessage(string blobName, string blobLease)
        {
            bool deleted = false;
            _blobStorage.DeleteBlobIfExist(ResilientMessagesContainerName, blobName);
            Retry.DoUntilTrue(_policies.OptimisticConcurrency(), CancellationToken.None, () =>
                {
                    var result = _blobStorage.TryReleaseLease(ResilientLeasesContainerName, blobName, blobLease);
                    if (result.IsSuccess)
                    {
                        deleted = _blobStorage.DeleteBlobIfExist(ResilientLeasesContainerName, blobName);
                        return true;
                    }

                    if (result.Error == "NotFound")
                    {
                        return true;
                    }

                    if (result.Error == "Conflict")
                    {
                        // CASE: either conflict by another lease (e.g. ReviveMessages), or because it is not leased anymore
                        // => try to delete and retry.
                        //    -> if it is not leased anymore, then delete will work and we're done;if not, we need to retry anyway
                        //    -> if it is locked by another lease, then the delete will fail with a storage exception, causing a retry
                        deleted = _blobStorage.DeleteBlobIfExist(ResilientLeasesContainerName, blobName);
                        return false;
                    }

                    return false;
                });

            return deleted;
        }

        void PutRawMessage(CloudQueueMessage message, CloudQueue queue, TimeSpan timeToLive, TimeSpan delay)
        {
            var ttlOrNot = timeToLive < CloudQueueMessage.MaxTimeToLive && timeToLive > TimeSpan.Zero ? timeToLive : new TimeSpan?();
            var delayOrNot = delay < CloudQueueMessage.MaxTimeToLive && delay > TimeSpan.Zero ? delay : new TimeSpan?();

            try
            {
                Retry.Do(_policies.TransientServerErrorBackOff(), CancellationToken.None, () => queue.AddMessage(message, ttlOrNot, delayOrNot));
            }
            catch (StorageException ex)
            {
                // HACK: not storage status error code yet
                if (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    // It usually takes time before the queue gets available
                    // (the queue might also have been freshly deleted).
                    Retry.Do(_policies.SlowInstantiation(), CancellationToken.None, () =>
                        {
                            queue.Create();
                            queue.AddMessage(message);
                        });
                }
                else
                {
                    throw;
                }
            }
        }

        void CheckOutMessage(object message, CloudQueueMessage rawMessage, byte[] data, string queueName, bool isOverflowing, int dequeueCount, IDataSerializer serializer)
        {
            lock (_sync)
            {
                // If T is a value type, _inprocess could already contain the message
                // (not the same exact instance, but an instance that is value-equal to this one)
                InProcessMessage inProcessMessage;
                if (!_inProcessMessages.TryGetValue(message, out inProcessMessage))
                {
                    inProcessMessage = new InProcessMessage
                        {
                            QueueName = queueName,
                            RawMessages = new List<CloudQueueMessage> {rawMessage},
                            Serializer = serializer,
                            Data = data,
                            IsOverflowing = isOverflowing,
                            DequeueCount = dequeueCount
                        };
                    _inProcessMessages.Add(message, inProcessMessage);
                }
                else
                {
                    inProcessMessage.RawMessages.Add(rawMessage);
                }
            }
        }

        void CheckOutRelink(object originalMessage, object newMessage)
        {
            lock (_sync)
            {
                var inProcessMessage = _inProcessMessages[originalMessage];
                _inProcessMessages.Remove(originalMessage);
                _inProcessMessages.Add(newMessage, inProcessMessage);
            }
        }

        void CheckInMessage(object message)
        {
            lock (_sync)
            {
                var inProcessMessage = _inProcessMessages[message];
                inProcessMessage.RawMessages.RemoveAt(0);

                if (0 == inProcessMessage.RawMessages.Count)
                {
                    _inProcessMessages.Remove(message);
                }
            }
        }

        /// <summary>
        /// Deletes a queue.
        /// </summary>
        /// <returns><c>true</c> if the queue name has been actually deleted.</returns>
        /// <remarks>
        /// This implementation takes care of deleting overflowing blobs as
        /// well.
        /// </remarks>
        public bool DeleteQueue(string queueName)
        {
            try
            {
                // Caution: call to 'DeleteOverflowingMessages' comes first (BASE).
                DeleteOverflowingMessages(queueName);
                var queue = _queueStorage.GetQueueReference(queueName);
                Retry.Do(_policies.TransientServerErrorBackOff(), CancellationToken.None, () => queue.Delete());
                return true;
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    return false;
                }

                throw;
            }
        }

        /// <summary>
        /// Gets the approximate number of items in this queue.
        /// </summary>
        public int GetApproximateCount(string queueName)
        {
            try
            {
                var queue = _queueStorage.GetQueueReference(queueName);
                queue.FetchAttributes();
                return Retry.Get(_policies.TransientServerErrorBackOff(), CancellationToken.None, () =>
                                                                                                      {
                                                                                                          if (queue.ApproximateMessageCount.HasValue)
                                                                                                          {
                                                                                                              return queue.ApproximateMessageCount.Value;
                                                                                                          }
                                                                                                          else
                                                                                                          {
                                                                                                              return 0;
                                                                                                          }
                                                                                                      });
            }
            catch (StorageException ex)
            {
                // if the queue does not exist, return 0 (no queue)
                if (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    return 0;
                }

                throw;
            }
        }

        /// <summary>
        /// Gets the approximate age of the top message of this queue.
        /// </summary>
        public Maybe<TimeSpan> GetApproximateLatency(string queueName)
        {
            var queue = _queueStorage.GetQueueReference(queueName);
            CloudQueueMessage rawMessage;

            try
            {
                rawMessage = Retry.Get(_policies.TransientServerErrorBackOff(), CancellationToken.None, ()=>queue.PeekMessage());
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.ExtendedErrorInformation.ErrorCode == StorageErrorCodeStrings.ResourceNotFound
                    || ex.RequestInformation.ExtendedErrorInformation.ErrorCode == QueueErrorCodeStrings.QueueNotFound)
                {
                    return Maybe<TimeSpan>.Empty;
                }

                throw;
            }

            if(rawMessage == null || !rawMessage.InsertionTime.HasValue)
            {
                return Maybe<TimeSpan>.Empty;
            }

            var latency = DateTimeOffset.UtcNow - rawMessage.InsertionTime.Value;

            // don't return negative values when clocks are slightly out of sync 
            return latency > TimeSpan.Zero ? latency : TimeSpan.Zero;
        }

        private void NotifySucceeded(StorageOperationType operationType, Stopwatch stopwatch)
        {
            if (_observer != null)
            {
                _observer.Notify(new StorageOperationSucceededEvent(operationType, stopwatch.Elapsed));
            }
        }
    }

    internal class IdentityComparer : IEqualityComparer<object>
    {
        public static bool CanDifferentiateInstances(Type type)
        {
            return type != typeof(string) && type.IsClass;
        }

        public bool Equals(object x, object y)
        {
            if (x == null)
            {
                return y == null;
            }

            if (y == null)
            {
                return false;
            }

            return x.GetType().IsClass ? ReferenceEquals(x, y) : x.Equals(y);
        }

        public int GetHashCode(object obj)
        {
            return obj == null ? 0 : obj.GetHashCode();
        }
    }


    /// <summary>Represents a set of value-identical messages that are being processed by workers, 
    /// i.e. were hidden from the queue because of calls to Get{T}.</summary>
    internal class InProcessMessage
    {
        /// <summary>Name of the queue where messages are originating from.</summary>
        public string QueueName { get; set; }

        /// <summary>
        /// The multiple, different raw <see cref="CloudQueueMessage" /> 
        /// objects as returned from the queue storage.
        /// </summary>
        public List<CloudQueueMessage> RawMessages { get; set; }

        /// <summary>
        /// Serializer used for this message.
        /// </summary>
        public IDataSerializer Serializer { get; set; }

        /// <summary>
        /// The unpacked message data. Can still be a message wrapper, but never an envelope.
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// A flag indicating whether the original message was bigger than the max 
        /// allowed size and was therefore wrapped in <see cref="MessageWrapper" />.
        /// </summary>
        public bool IsOverflowing { get; set; }

        /// <summary>
        /// The number of times this message has already been dequeued,
        /// so we can track it safely even when abandoning it later
        /// </summary>
        public int DequeueCount { get; set; }

        /// <summary>
        /// True if Delete, Abandon or ResumeNext has been requested.
        /// </summary>
        public bool CommitStarted { get; set; }

        public DateTimeOffset KeepAliveTimeout { get; set; }
        public string KeepAliveBlobName { get; set; }
        public string KeepAliveBlobLease { get; set; }
    }

    internal class OverflowingMessageBlobName<T> : BlobName<T>
    {
        public override string ContainerName
        {
            get { return QueueStorageProvider.OverflowingMessagesContainerName; }
        }

        /// <summary>Indicates the name of the queue where the message has been originally pushed.</summary>
        [Rank(0)]
        public string QueueName;

        /// <summary>Message identifiers as specified by the queue storage itself.</summary>
        [Rank(1)]
        public Guid MessageId;

        OverflowingMessageBlobName(string queueName, Guid guid)
        {
            QueueName = queueName;
            MessageId = guid;
        }

        /// <summary>Used to iterate over all the overflowing messages 
        /// associated to a queue.</summary>
        public static OverflowingMessageBlobName<T> GetNew(string queueName)
        {
            return new OverflowingMessageBlobName<T>(queueName, Guid.NewGuid());
        }
    }

    [DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/storage/2.0"), Serializable]
    internal class PersistedMessageData
    {
        [DataMember(Order = 1)]
        public string QueueName { get; set; }

        [DataMember(Order = 2)]
        public DateTimeOffset InsertionTime { get; set; }

        [DataMember(Order = 3)]
        public DateTimeOffset PersistenceTime { get; set; }

        [DataMember(Order = 4)]
        public int DequeueCount { get; set; }

        [DataMember(Order = 5, IsRequired = false)]
        public string Reason { get; set; }

        [DataMember(Order = 6)]
        public byte[] Data { get; set; }
    }

    [DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/storage/2.0"), Serializable]
    internal class ResilientMessageData
    {
        [DataMember(Order = 1)]
        public string QueueName { get; set; }

        [DataMember(Order = 2)]
        public byte[] Data { get; set; }
    }

    [DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/storage/2.0")]
    internal class ResilientLeaseData
    {
        [DataMember(Order = 1)]
        public string QueueName { get; set; }

        [DataMember(Order = 2)]
        public string BlobName { get; set; }
    }

    internal class PersistedMessageBlobName : BlobName<PersistedMessageData>
    {
        public override string ContainerName
        {
            get { return "lokad-cloud-persisted-messages"; }
        }

        /// <summary>Indicates the name of the swap out store where the message is persisted.</summary>
        [Rank(0)]
        public string StoreName;

        [Rank(1)]
        public string Key;

        public PersistedMessageBlobName(string storeName, string key)
        {
            StoreName = storeName;
            Key = key;
        }

        public static PersistedMessageBlobName GetNew(string storeName, string key)
        {
            return new PersistedMessageBlobName(storeName, key);
        }

        public static PersistedMessageBlobName GetNew(string storeName)
        {
            return new PersistedMessageBlobName(storeName, Guid.NewGuid().ToString("N"));
        }

        public static PersistedMessageBlobName GetPrefix(string storeName)
        {
            return new PersistedMessageBlobName(storeName, null);
        }
    }
}