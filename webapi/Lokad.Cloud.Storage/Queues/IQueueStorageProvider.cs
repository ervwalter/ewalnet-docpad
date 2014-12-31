#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Lokad.Cloud.Storage
{
    /// <summary>Abstraction of the Queue Storage.</summary>
    /// <remarks>
    /// This provider represents a <em>logical</em> queue, not the actual
    /// Queue Storage. In particular, the provider implementation deals
    /// with overflowing messages (that is to say messages larger than 8kb)
    /// on its own.
    /// </remarks>
    public interface IQueueStorageProvider
    {
        /// <summary>Gets the list of queues whose name start with the specified prefix.</summary>
        /// <param name="prefix">If <c>null</c> or empty, returns all queues.</param>
        IEnumerable<string> List(string prefix);

        /// <summary>Gets messages from a queue.</summary>
        /// <typeparam name="T">Type of the messages.</typeparam>
        /// <param name="queueName">Identifier of the queue to be pulled.</param>
        /// <param name="count">Maximal number of messages to be retrieved.</param>
        /// <param name="visibilityTimeout">
        /// The visibility timeout, indicating when the not yet deleted message should
        /// become visible in the queue again.
        /// </param>
        /// <param name="maxProcessingTrials">
        /// Maximum number of message processing trials, before the message is considered as
        /// being poisonous, removed from the queue and persisted to the 'failing-messages' store.
        /// </param>
        /// <returns>Enumeration of messages, possibly empty.</returns>
        IEnumerable<T> Get<T>(string queueName, int count, TimeSpan visibilityTimeout, int maxProcessingTrials, IDataSerializer serializer = null);

        /// <summary>Put a message on a queue.</summary>
        /// <typeparam name="T">Type of the messages.</typeparam>
        /// <param name="queueName">Identifier of the queue where messages are put.</param>
        /// <param name="message">Message to be put.</param>
        /// <remarks>If the queue does not exist, it gets created.</remarks>
        void Put<T>(string queueName, T message, TimeSpan timeToLive = default(TimeSpan), TimeSpan delay = default(TimeSpan), IDataSerializer serializer = null);

        /// <summary>Put messages on a queue.</summary>
        /// <typeparam name="T">Type of the messages.</typeparam>
        /// <param name="queueName">Identifier of the queue where messages are put.</param>
        /// <param name="messages">Messages to be put.</param>
        /// <remarks>If the queue does not exist, it gets created.</remarks>
        void PutRange<T>(string queueName, IEnumerable<T> messages, TimeSpan timeToLive = default(TimeSpan), TimeSpan delay = default(TimeSpan), IDataSerializer serializer = null);

        /// <summary>
        /// Puts messages on a queue. Uses Tasks to increase thouroughput dramatically.
        /// </summary>
        /// <typeparam name="T">Type of the messages.</typeparam>
        /// <param name="queueName">Identifier of the queue where messages are put.</param>
        /// <param name="messages">Messages to be put.</param>
        /// <remarks>If the queue does not exist, it gets created.</remarks>
        void PutRangeParallel<T>(string queueName, IEnumerable<T> messages, TimeSpan timeToLive = default(TimeSpan), TimeSpan delay = default(TimeSpan), IDataSerializer serializer = null);

        /// <summary>Clear all the messages from the specified queue.</summary>
        void Clear(string queueName);

        /// <summary>Keep the message alive for another period.</summary>
        /// <returns>The new visibility timeout</returns>
        TimeSpan KeepAlive<T>(T message) where T : class;

        /// <summary>Revive messages that are no longer kept alive.</summary>
        int ReviveMessages(TimeSpan timeToLive = default(TimeSpan), TimeSpan delay = default(TimeSpan));

        /// <summary>Deletes a message being processed from the queue.</summary>
        /// <returns><c>True</c> if the message has been deleted.</returns>
        /// <remarks>Message must have first been retrieved through <see cref="Get{T}"/>.</remarks>
        bool Delete<T>(T message);

        /// <summary>Deletes messages being processed from the queue.</summary>
        /// <typeparam name="T">Type of the messages.</typeparam>
        /// <param name="messages">Messages to be removed.</param>
        /// <returns>The number of messages actually deleted.</returns>
        /// <remarks>Messages must have first been retrieved through <see cref="Get{T}"/>.</remarks>
        int DeleteRange<T>(IEnumerable<T> messages);

        /// <summary>
        /// Abandon a message being processed and put it visibly back on the queue.
        /// </summary>
        /// <typeparam name="T">Type of the message.</typeparam>
        /// <param name="message">Message to be abandoned.</param>
        /// <returns><c>True</c> if the original message has been deleted.</returns>
        /// <remarks>Message must have first been retrieved through <see cref="Get{T}"/>.</remarks>
        bool Abandon<T>(T message, TimeSpan timeToLive = default(TimeSpan), TimeSpan delay = default(TimeSpan));

        /// <summary>
        /// Abandon a set of messages being processed and put them visibly back on the queue.
        /// </summary>
        /// <typeparam name="T">Type of the messages.</typeparam>
        /// <param name="messages">Messages to be abandoned.</param>
        /// <returns>The number of original messages actually deleted.</returns>
        /// <remarks>Messages must have first been retrieved through <see cref="Get{T}"/>.</remarks>
        int AbandonRange<T>(IEnumerable<T> messages, TimeSpan timeToLive = default(TimeSpan), TimeSpan delay = default(TimeSpan));

        /// <summary>
        /// Abandon all messages still being processed. This is recommended to call e.g. when forcing a worker to shutting.
        /// </summary>
        /// <returns>The number of original messages actually deleted.</returns>
        int AbandonAll();

        /// <summary>
        /// Resume a message being processed later and put it visibly back on the queue, without decreasing the poison detection dequeue count.
        /// </summary>
        /// <typeparam name="T">Type of the message.</typeparam>
        /// <param name="message">Message to be resumed later.</param>
        /// <returns><c>True</c> if the original message has been deleted.</returns>
        /// <remarks>Message must have first been retrieved through <see cref="Get{T}"/>.</remarks>
        bool ResumeLater<T>(T message, TimeSpan timeToLive = default(TimeSpan), TimeSpan delay = default(TimeSpan));

        /// <summary>
        /// Resume a set of messages being processed latern and put them visibly back on the queue, without decreasing the poison detection dequeue count.
        /// </summary>
        /// <typeparam name="T">Type of the messages.</typeparam>
        /// <param name="messages">Messages to be resumed later.</param>
        /// <returns>The number of original messages actually deleted.</returns>
        /// <remarks>Messages must have first been retrieved through <see cref="Get{T}"/>.</remarks>
        int ResumeLaterRange<T>(IEnumerable<T> messages, TimeSpan timeToLive = default(TimeSpan), TimeSpan delay = default(TimeSpan));

        /// <summary>
        /// Persist a message being processed to a store and remove it from the queue.
        /// </summary>
        /// <typeparam name="T">Type of the message.</typeparam>
        /// <param name="message">Message to be persisted.</param>
        /// <param name="storeName">Name of the message persistence store.</param>
        /// <param name="reason">Optional reason text on why the message has been taken out of the queue.</param>
        void Persist<T>(T message, string storeName, string reason);

        /// <summary>
        /// Persist a set of messages being processed to a store and remove them from the queue.
        /// </summary>
        /// <typeparam name="T">Type of the messages.</typeparam>
        /// <param name="messages">Messages to be persisted.</param>
        /// <param name="storeName">Name of the message persistence store.</param>
        /// <param name="reason">Optional reason text on why the messages have been taken out of the queue.</param>
        void PersistRange<T>(IEnumerable<T> messages, string storeName, string reason);

        /// <summary>
        /// Enumerate the keys of all persisted messages of the provided store.
        /// </summary>
        /// <param name="storeName">Name of the message persistence store.</param>
        IEnumerable<string> ListPersisted(string storeName);

        /// <summary>
        /// Get details of a persisted message for inspection and recovery.
        /// </summary>
        /// <param name="storeName">Name of the message persistence store.</param>
        /// <param name="key">Unique key of the persisted message as returned by ListPersisted.</param>
        Maybe<PersistedMessage> GetPersisted(string storeName, string key, IDataSerializer serializer = null);

        /// <summary>
        /// Delete a persisted message.
        /// </summary>
        /// <param name="storeName">Name of the message persistence store.</param>
        /// <param name="key">Unique key of the persisted message as returned by ListPersisted.</param>
        void DeletePersisted(string storeName, string key, IDataSerializer serializer = null);

        /// <summary>
        /// Put a persisted message back to the queue and delete it.
        /// </summary>
        /// <param name="storeName">Name of the message persistence store.</param>
        /// <param name="key">Unique key of the persisted message as returned by ListPersisted.</param>
        void RestorePersisted(string storeName, string key, TimeSpan timeToLive = default(TimeSpan), TimeSpan delay = default(TimeSpan));

        /// <summary>Deletes a queue.</summary>
        /// <returns><c>true</c> if the queue name has been actually deleted.</returns>
        bool DeleteQueue(string queueName);

        /// <summary>Gets the approximate number of items in this queue.</summary>
        int GetApproximateCount(string queueName);

        /// <summary>Gets the approximate age of the top message of this queue.</summary>
        Maybe<TimeSpan> GetApproximateLatency(string queueName);
    }

    /// <summary>
    /// Persisted message details for inspection and recovery.
    /// </summary>
    public class PersistedMessage
    {
        /// <summary>Identifier of the originating message queue.</summary>
        public string QueueName { get; internal set; }
        /// <summary>Name of the message persistence store.</summary>
        public string StoreName { get; internal set; }
        /// <summary>Unique key of the persisted message as returned by ListPersisted.</summary>
        public string Key { get; internal set; }

        /// <summary>Time when the message was inserted into the message queue.</summary>
        public DateTimeOffset InsertionTime { get; internal set; }
        /// <summary>Time when the message was persisted and removed from the message queue.</summary>
        public DateTimeOffset PersistenceTime { get; internal set; }
        /// <summary>The number of times the message has been dequeued.</summary>
        public int DequeueCount { get; internal set; }
        /// <summary>Optional reason text why the message was persisted.</summary>
        public string Reason { get; internal set; }

        /// <summary>XML representation of the message, if possible and supported by the serializer</summary>
        public Maybe<XElement> DataXml { get; internal set; }

        /// <summary>True if the raw message data is available and can be restored.</summary>
        /// <remarks>Can be true even if DataXML is not available.</remarks>
        public bool IsDataAvailable { get; internal set; }
    }
}