using System;
using System.Threading;
using System.Threading.Tasks;

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

        /*
        public static bool IsSuccess(this Task t)
        {
            return !t.IsFaulted; // TODO && t.IsCompleted?
        }
        */
    }
}
