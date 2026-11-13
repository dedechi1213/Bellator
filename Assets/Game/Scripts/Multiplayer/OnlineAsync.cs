using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bellator.Multiplayer
{
    /// <summary>Stops waiting without pretending to cancel an SDK operation that has no token API.</summary>
    public static class OnlineAsync
    {
        public static async Task WaitAsync(Task operation, CancellationToken cancellation)
        {
            if(operation==null) throw new ArgumentNullException(nameof(operation));
            cancellation.ThrowIfCancellationRequested();
            if(!operation.IsCompleted)
            {
                var interrupted=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                using(cancellation.Register(()=>interrupted.TrySetResult(true)))
                {
                    if(await Task.WhenAny(operation,interrupted.Task)!=operation)
                    {
                        // Init/auth may complete in the background, but cannot advance into room creation.
                        _=operation.ContinueWith(faulted=> { _=faulted.Exception; },CancellationToken.None,
                            TaskContinuationOptions.OnlyOnFaulted,TaskScheduler.Default);
                        throw new OperationCanceledException(cancellation);
                    }
                }
            }
            cancellation.ThrowIfCancellationRequested();
            await operation;
        }
    }
}
