using System;
using System.Threading;
using System.Threading.Tasks;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Extensions
{
    public static class AsyncExtensions
    {
        /// <summary>
        /// This extension allows to make tasks cancellable even if the original API has no
        /// CancellationToken parameter.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken ct)
        {
            // from http://stackoverflow.com/questions/19404199/how-to-to-make-udpclient-receiveasync-cancelable

            var tcs = new TaskCompletionSource<bool>();
            using (ct.Register(o => ((TaskCompletionSource<bool>) o).TrySetResult(true), tcs))
            {
                if (task != await Task.WhenAny(task, tcs.Task))
                {
                    throw new OperationCanceledException(ct);
                }
            }
            return task.Result;
        }

        public static Exception TryGetException(this Task task)
        {
            if (task.IsCompleted)
            {
                if (task.Exception != null)
                {
                    return task.Exception;
                }
                return new TaskFailedException(String.Format("{0} failed.", task));
            }
            return new TaskFailedException("This task has not yet completed.");
        }

        /*
        public static bool IsSuccess(this Task t)
        {
            return !t.IsFaulted; // TODO && t.IsCompleted?
        }
        */
    }
}
