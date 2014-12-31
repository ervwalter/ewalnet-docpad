#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.RetryPolicies;

namespace Lokad.Cloud.Storage.FileSystem
{
    /// <summary>
    /// Azure retry policies for corner-situation and server errors.
    /// </summary>
    internal class RetryPolicies
    {
        internal RetryPolicies()
        {
        }

        /// <summary>
        /// Retry policy for optimistic concurrency retrials.
        /// </summary>
        public IRetryPolicy OptimisticConcurrency()
        {
            return new OptimisticConcurrencyRetry();
        }

        internal class OptimisticConcurrencyRetry : IRetryPolicy
        {
            public IRetryPolicy CreateInstance()
            {
                return new OptimisticConcurrencyRetry();
            }

            public bool ShouldRetry(int currentRetryCount, int statusCode, Exception lastException, out TimeSpan retryInterval,
                                    OperationContext operationContext)
            {
                var random = new Random();
                if (lastException is AggregateException)
                {
                    lastException = lastException.GetBaseException();
                }

                if (currentRetryCount >= 30 || !(lastException is IOException) && !(lastException is ConcurrencyException))
                {
                    retryInterval = TimeSpan.Zero;
                    return false;
                }

                retryInterval = TimeSpan.FromMilliseconds(random.Next(Math.Min(1000, 5 + currentRetryCount * currentRetryCount * 5)));
                return true;
            }
        }
    }
}