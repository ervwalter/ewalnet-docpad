#region (c)2009-2011 Lokad - New BSD license
// Company: http://www.lokad.com
// This code is released under the terms of the new BSD licence
#endregion

using System;
using System.Diagnostics;
using System.Threading;

namespace Lokad.Cloud.Storage.Shared.Threading
{
    ///<summary>
    /// Quick alternatives to PLinq with minimal overhead and simple implementations.
    ///</summary>
    internal static class ParallelExtensions
    {
        static int ThreadCount = Environment.ProcessorCount;

        /// <summary>Executes the specified function in parallel over an array.</summary>
        /// <param name="input">Input array to processed in parallel.</param>
        /// <param name="func">The action to perform. Parameters and all the members should be immutable.</param>
        /// <remarks>Threads are recycled. Synchronization overhead is minimal.</remarks>
        public static TResult[] SelectInParallel<TItem, TResult>(this TItem[] input, Func<TItem, TResult> func)
        {
            return SelectInParallel(input, func, ThreadCount);
        }

        /// <summary>
        /// Executes the specified function in parallel over an array, using the provided number of threads.
        /// </summary>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="input">Input array to processed in parallel.</param>
        /// <param name="func">The action to perform. Parameters and all the members should be immutable.</param>
        /// <param name="threadCount">The thread count.</param>
        /// <returns></returns>
        /// <remarks>Threads are recycled. Synchronization overhead is minimal.</remarks>
        public static TResult[] SelectInParallel<TItem, TResult>(this TItem[] input, Func<TItem, TResult> func,
            int threadCount)
        {
            if (input == null) throw new ArgumentNullException("input");
            if (func == null) throw new ArgumentNullException("func");

            if (threadCount < 1)
                throw new ArgumentOutOfRangeException("threadCount");


            if (input.Length == 0)
                return new TResult[0];

            var results = new TResult[input.Length];

            if (threadCount == 1 || input.Length == 1)
            {
                for (int i = 0; i < input.Length; i++)
                {
                    try
                    {
                        results[i] = func(input[i]);
                    }
                    catch (Exception ex)
                    {
                        WrapAndThrow(ex);
                    }
                }

                return results;
            }

            // perf: no more threads than items in collection
            var actualThreadCount = Math.Min(threadCount, input.Length);

            // perf: start by syncless process, then finish with light index-based sync
            // to adjust varying execution time of the various threads.
            var threshold = Math.Max(0, input.Length - (int)Math.Sqrt(input.Length) - 2 * actualThreadCount);
            var workingIndex = threshold - 1;

            var sync = new object();

            Exception exception = null;

            int completedCount = 0;
            WaitCallback worker = index =>
            {
                try
                {
                    // no need for lock - disjoint processing
                    for (var i = (int)index; i < threshold; i += actualThreadCount)
                    {
                        results[i] = func(input[i]);
                    }

                    // joint processing
                    int j;
                    while ((j = Interlocked.Increment(ref workingIndex)) < input.Length)
                    {
                        results[j] = func(input[j]);
                    }

                    var r = Interlocked.Increment(ref completedCount);

                    // perf: only the terminating thread actually acquires a lock.
                    if (r == actualThreadCount && (int)index != 0)
                    {
                        lock (sync) Monitor.Pulse(sync);
                    }
                }
                catch (Exception ex)
                {
                    exception = ex;
                    lock (sync) Monitor.Pulse(sync);
                }
            };

            for (int i = 1; i < actualThreadCount; i++)
            {
                ThreadPool.QueueUserWorkItem(worker, i);
            }
            worker((object)0); // perf: recycle current thread

            // waiting until completion or failure
            while (completedCount < actualThreadCount && exception == null)
            {
                // CAUTION: limit on wait time is needed because if threads
                // have terminated 
                // - AFTER the test of the 'while' loop, and
                // - BEFORE the inner 'lock' 
                // then, there is no one left to call for 'Pulse'.
                lock (sync) Monitor.Wait(sync, TimeSpan.FromMilliseconds(10));
            }

            if (exception != null)
            {
                WrapAndThrow(exception);
            }

            return results;
        }

        [DebuggerNonUserCode]
        static void WrapAndThrow(Exception exception)
        {
            throw new Exception("Exception caught in SelectInParallel", exception);
        }
    }
}
