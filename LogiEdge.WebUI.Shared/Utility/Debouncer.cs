using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace LogiEdge.WebUI.Shared.Utility
{
    public sealed class Debouncer : IDisposable
    {
        public int DelayMilliseconds { get; }

        private readonly System.Timers.Timer saveTimer;

        private readonly ConcurrentQueue<Func<Task>> actionsQueue = [];

        public bool HasEnqueuedActions => !actionsQueue.IsEmpty;


        public Debouncer(int delayMilliseconds = 500)
        {
            DelayMilliseconds = delayMilliseconds;

            saveTimer = new System.Timers.Timer(DelayMilliseconds);
            saveTimer.Elapsed += OnSaveTimerElapsed;
            saveTimer.AutoReset = false;
        }

        public void Debounce(Func<Task> action)
        {
            actionsQueue.Enqueue(action);

            lock (saveTimer)
            {
                saveTimer.Stop();
                saveTimer.Start();
            }
        }

        // ReSharper disable once AsyncVoidMethod
        private async void OnSaveTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            lock (saveTimer)
            {
                saveTimer.Stop();
            }

            await InvokeActionsAsync();
        }

        private async Task InvokeActionsAsync()
        {
            while (actionsQueue.TryDequeue(out Func<Task>? action))
            {
                await action.Invoke();
            }
        }

        public void Dispose()
        {
            saveTimer.Dispose();

            // Any outstanding tasks may not be properly executed, but this is the best we can do in Dispose.
            InvokeActionsAsync().GetAwaiter().GetResult();
        }
    }
}
