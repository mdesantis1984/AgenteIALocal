using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AgenteIALocal.Core.Logging;

namespace AgenteIALocal.Infrastructure.LoggingV2
{
    // NUEVO COMPONENTE UiLogBuffer - ID: 20260118_190200
    public sealed class UiLogBuffer
    {
        private readonly object gate = new object();
        private readonly int capacity;
        private readonly Queue<string> queue;
        public System.Collections.ObjectModel.ObservableCollection<string> Items { get; }
        private readonly Action<Action> uiInvoker;

        public UiLogBuffer(Action<Action> uiInvoker, int capacity = 250)
        {
            this.uiInvoker = uiInvoker ?? throw new ArgumentNullException(nameof(uiInvoker));
            this.capacity = capacity > 0 ? capacity : 250;
            queue = new Queue<string>(this.capacity + 4);
            Items = new ObservableCollection<string>();
        }

        public void Publish(string line, LogEntry entry)
        {
            if (line == null) return;
            lock (gate)
            {
                queue.Enqueue(line);
                string removed = null;
                if (queue.Count > capacity)
                {
                    removed = queue.Dequeue();
                }

                // apply diffs on UI thread
                try
                {
                    uiInvoker(() =>
                    {
                        try
                        {
                            if (removed != null)
                            {
                                if (Items.Count > 0)
                                {
                                    try { Items.RemoveAt(0); } catch { }
                                }
                            }
                            Items.Add(line);
                        }
                        catch { }
                    });
                }
                catch { }
            }
        }
    }
}
