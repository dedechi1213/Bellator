using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bellator.Multiplayer;
using UnityEditor;
using UnityEngine;

namespace Bellator.Editor
{
    [InitializeOnLoad]
    public static class OnlineAsyncValidation
    {
        static Task checks;
        static OnlineAsyncValidation() { EditorApplication.update+=Poll; }
        static void Poll()
        {
            if(checks!=null && checks.IsCompleted)
            {
                try { checks.GetAwaiter().GetResult(); File.WriteAllText("Logs/Resume-async-validation.txt","PASS: cancelled pending task; deadline on stalled task; late completion cannot advance connection; success and failure propagation."); }
                catch(Exception ex) { File.WriteAllText("Logs/Resume-async-validation.txt","FAIL: "+ex); Debug.LogException(ex); }
                checks=null;
            }
            if(checks!=null || EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists("Temp/OnlineAsync.test")) return;
            File.Delete("Temp/OnlineAsync.test"); checks=RunChecks();
        }
        static async Task RunChecks()
        {
            await OnlineAsync.WaitAsync(Task.CompletedTask,CancellationToken.None);
            bool failed=false;
            try { await OnlineAsync.WaitAsync(Task.FromException(new InvalidOperationException("expected")),CancellationToken.None); }
            catch(InvalidOperationException) { failed=true; }
            Require(failed,"SDK failure was swallowed");
            var never=new TaskCompletionSource<bool>();
            using(var cancelled=new CancellationTokenSource())
            {
                var waiter=OnlineAsync.WaitAsync(never.Task,cancelled.Token);
                cancelled.Cancel(); await RequireCancelled(waiter);
                never.SetResult(true);
                Require(waiter.IsCanceled,"Late initialization advanced the cancelled attempt");
                await RequireCancelled(OnlineAsync.WaitAsync(Task.CompletedTask,cancelled.Token));
            }
            using(var deadline=new CancellationTokenSource(TimeSpan.FromMilliseconds(30)))
                await RequireCancelled(OnlineAsync.WaitAsync(new TaskCompletionSource<bool>().Task,deadline.Token));
        }
        static async Task RequireCancelled(Task operation)
        {
            try { await operation; throw new Exception("A pending operation ignored cancellation"); }
            catch(OperationCanceledException) { }
        }
        static void Require(bool success,string message) { if(!success) throw new Exception(message); }
    }
}
