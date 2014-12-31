#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.RetryPolicies;

namespace Lokad.Cloud.Storage
{
    internal static class Retry
    {
        public static void Do(this IRetryPolicy retryPolicy, CancellationToken cancellationToken, Action action)
        {
            var policy = retryPolicy;
            int retryCount = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    action();
                    return;
                }
                catch (Exception exception)
                {
                    TimeSpan delay;
                    if (policy.ShouldRetry(retryCount, 0,exception, out delay,null))
                    {
                        retryCount++;
                        if (delay > TimeSpan.Zero)
                        {
                            Thread.Sleep(delay);
                        }

                        continue;
                    }

                    throw;
                }
            }
        }

        public static void Do(this IRetryPolicy firstPolicy, IRetryPolicy secondPolicy, CancellationToken cancellationToken, Action action)
        {
            var first = firstPolicy;
            var second = secondPolicy;
            int retryCount = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    action();
                    return;
                }
                catch (Exception exception)
                {
                    TimeSpan delay;
                    if (first.ShouldRetry(retryCount, 0, exception, out delay, null))
                    {
                        retryCount++;
                        if (delay > TimeSpan.Zero)
                        {
                            Thread.Sleep(delay);
                        }

                        continue;
                    }

                    if (second.ShouldRetry(retryCount, 0, exception, out delay, null))
                    {
                        retryCount++;
                        if (delay > TimeSpan.Zero)
                        {
                            Thread.Sleep(delay);
                        }

                        continue;
                    }

                    throw;
                }
            }
        }

        public static T Get<T>(this IRetryPolicy retryPolicy, CancellationToken cancellationToken, Func<T> action)
        {
            var policy = retryPolicy;
            int retryCount = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var result = action();
                    return result;
                }
                catch (Exception exception)
                {
                    TimeSpan delay;
                    if (policy.ShouldRetry(retryCount, 0, exception, out delay, null))
                    {
                        retryCount++;
                        if (delay > TimeSpan.Zero)
                        {
                            Thread.Sleep(delay);
                        }

                        continue;
                    }

                    throw;
                }
            }
        }

        public static T Get<T>(this IRetryPolicy firstPolicy, IRetryPolicy secondPolicy, CancellationToken cancellationToken, Func<T> action)
        {
            var first = firstPolicy;
            var second = secondPolicy;
            int retryCount = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var result = action();
                    return result;
                }
                catch (Exception exception)
                {
                    TimeSpan delay;
                    if (first.ShouldRetry(retryCount, 0, exception, out delay, null))
                    {
                        retryCount++;
                        if (delay > TimeSpan.Zero)
                        {
                            Thread.Sleep(delay);
                        }

                        continue;
                    }

                    if (second.ShouldRetry(retryCount, 0, exception, out delay, null))
                    {
                        retryCount++;
                        if (delay > TimeSpan.Zero)
                        {
                            Thread.Sleep(delay);
                        }

                        continue;
                    }

                    throw;
                }
            }
        }

        /// <remarks>Policy must support exceptions being null.</remarks>
        public static void DoUntilTrue(this IRetryPolicy retryPolicy, CancellationToken cancellationToken, Func<bool> action)
        {
            var policy = retryPolicy;
            int retryCount = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    if (action())
                    {
                        return;
                    }

                    TimeSpan delay;
                    if (policy.ShouldRetry(retryCount, 0, null, out delay, null))
                    {
                        retryCount++;
                        if (delay > TimeSpan.Zero)
                        {
                            Thread.Sleep(delay);
                        }

                        continue;
                    }

                    throw new TimeoutException("Failed to reach a successful result in a limited number of retrials");
                }
                catch (Exception exception)
                {
                    TimeSpan delay;
                    if (policy.ShouldRetry(retryCount, 0, exception, out delay, null))
                    {
                        retryCount++;
                        if (delay > TimeSpan.Zero)
                        {
                            Thread.Sleep(delay);
                        }

                        continue;
                    }

                    throw;
                }
            }
        }

        public static void Task(this IRetryPolicy retryPolicy, CancellationToken cancellationToken,
            Func<Task> action, Action onSuccess = null, Action<Exception> onFinalError = null, Action onCancel = null)
        {
            RetryTask(retryPolicy, 0, cancellationToken, action, onSuccess, onFinalError, onCancel);
        }

        public static void Task<T>(this IRetryPolicy retryPolicy, CancellationToken cancellationToken,
            Func<Task<T>> action, Action<T> onSuccess = null, Action<Exception> onFinalError = null, Action onCancel = null)
        {
            RetryTask(retryPolicy, 0, cancellationToken, action, onSuccess, onFinalError, onCancel);
        }

        public static Task TaskAsTask(this IRetryPolicy retryPolicy, CancellationToken cancellationToken, Func<Task> action)
        {
            var tcs = new TaskCompletionSource<object>();
            RetryTask(retryPolicy, 0, cancellationToken, action, () => tcs.TrySetResult(null), e => tcs.TrySetException(e), () => tcs.TrySetCanceled());
            return tcs.Task;
        }

        public static Task<TOut> TaskAsTask<TOut>(this IRetryPolicy retryPolicy, CancellationToken cancellationToken, Func<Task> action, Func<TOut> mapSuccess)
        {
            var tcs = new TaskCompletionSource<TOut>();
            RetryTask(retryPolicy, 0, cancellationToken, action, () => tcs.TrySetResult(mapSuccess()), e => tcs.TrySetException(e), () => tcs.TrySetCanceled());
            return tcs.Task;
        }

        public static Task<T> TaskAsTask<T>(this IRetryPolicy retryPolicy, CancellationToken cancellationToken, Func<Task<T>> action)
        {
            var tcs = new TaskCompletionSource<T>();
            RetryTask(retryPolicy, 0, cancellationToken, action, s => tcs.TrySetResult(s), e => tcs.TrySetException(e), () => tcs.TrySetCanceled());
            return tcs.Task;
        }

        public static Task<TOut> TaskAsTask<TIn, TOut>(this IRetryPolicy retryPolicy, CancellationToken cancellationToken, Func<Task<TIn>> action, Func<TIn, TOut> mapSuccess)
        {
            var tcs = new TaskCompletionSource<TOut>();
            RetryTask(retryPolicy, 0, cancellationToken, action, s => tcs.TrySetResult(mapSuccess(s)), e => tcs.TrySetException(e), () => tcs.TrySetCanceled());
            return tcs.Task;
        }

        static void RetryTask<T>(IRetryPolicy retryPolicy, int retryCount, CancellationToken cancellationToken,
            Func<Task<T>> action, Action<T> onSuccess = null, Action<Exception> onFinalError = null, Action onCancel = null)
        {
            Task<T> mainTask;
            try
            {
                mainTask = action();
            }
            catch (Exception exception)
            {
                if (onFinalError != null) onFinalError(exception);
                else throw;
                return;
            }

            mainTask.ContinueWith(task =>
                {
                    try
                    {
                        bool isCancellationRequested = cancellationToken.IsCancellationRequested;

                        if (task.IsFaulted)
                        {
                            var baseException = task.Exception.GetBaseException();

                            if (isCancellationRequested || baseException is TaskCanceledException)
                            {
                                if (onCancel != null) onCancel();
                                return;
                            }

                            // Fail task if we don't retry
                            TimeSpan retryDelay;
                            if (retryPolicy == null || !retryPolicy.ShouldRetry(retryCount, 0, baseException, out retryDelay,null))
                            {
                                if (onFinalError != null) onFinalError(baseException);
                                return;
                            }

                            // Retry immediately
                            if (retryDelay <= TimeSpan.Zero)
                            {
                                RetryTask(retryPolicy, retryCount + 1, cancellationToken, action, onSuccess, onFinalError, onCancel);
                                return;
                            }

                            new Timer(self =>
                                {
                                    // Consider to use TaskEx.Delay instead once available
                                    ((IDisposable)self).Dispose();

                                    // Do not retry oif cancelled in the meantime
                                    if (cancellationToken.IsCancellationRequested)
                                    {
                                        if (onCancel != null) onCancel();
                                        return;
                                    }

                                    RetryTask(retryPolicy, retryCount + 1, cancellationToken, action, onSuccess, onFinalError, onCancel);
                                }).Change(retryDelay, TimeSpan.FromMilliseconds(-1));

                            return;
                        }

                        if (task.IsCanceled || isCancellationRequested)
                        {
                            if (onCancel != null) onCancel();
                            return;
                        }

                        if (onSuccess != null) onSuccess(task.Result);
                    }
                    catch (Exception e)
                    {
                        // we do not retry if the exception happened in the continuation
                        if (onFinalError != null) onFinalError(e);
                    }
                });
        }

        static void RetryTask(IRetryPolicy retryPolicy, int retryCount, CancellationToken cancellationToken,
            Func<Task> action, Action onSuccess = null, Action<Exception> onFinalError = null, Action onCancel = null)
        {
            Task mainTask;
            try
            {
                mainTask = action();
            }
            catch (Exception exception)
            {
                if (onFinalError != null) onFinalError(exception);
                else throw;
                return;
            }

            mainTask.ContinueWith(task =>
            {
                try
                {
                    bool isCancellationRequested = cancellationToken.IsCancellationRequested;

                    if (task.IsFaulted)
                    {
                        var baseException = task.Exception.GetBaseException();

                        if (isCancellationRequested || baseException is TaskCanceledException)
                        {
                            if (onCancel != null) onCancel();
                            return;
                        }

                        // Fail task if we don't retry
                        TimeSpan retryDelay;
                        if (retryPolicy == null || !retryPolicy.ShouldRetry(retryCount, 0,baseException, out retryDelay, null))
                        {
                            if (onFinalError != null) onFinalError(baseException);
                            return;
                        }

                        // Retry immediately
                        if (retryDelay <= TimeSpan.Zero)
                        {
                            RetryTask(retryPolicy, retryCount + 1, cancellationToken, action, onSuccess, onFinalError, onCancel);
                            return;
                        }

                        new Timer(self =>
                        {
                            // Consider to use TaskEx.Delay instead once available
                            ((IDisposable)self).Dispose();

                            // Do not retry oif cancelled in the meantime
                            if (cancellationToken.IsCancellationRequested)
                            {
                                if (onCancel != null) onCancel();
                                return;
                            }

                            RetryTask(retryPolicy, retryCount + 1, cancellationToken, action, onSuccess, onFinalError, onCancel);
                        }).Change(retryDelay, TimeSpan.FromMilliseconds(-1));

                        return;
                    }

                    if (task.IsCanceled || isCancellationRequested)
                    {
                        if (onCancel != null) onCancel();
                        return;
                    }

                    if (onSuccess != null) onSuccess();
                }
                catch (Exception e)
                {
                    // we do not retry if the exception happened in the continuation
                    if (onFinalError != null) onFinalError(e);
                }
            });
        }
    }
}
