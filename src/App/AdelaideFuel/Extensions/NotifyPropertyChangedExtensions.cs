using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel
{
    public static class NotifyPropertyChangedExtensions
    {
        public static void When<T>(this T obj, Func<T, bool> predicate, Action action, int timeoutMs = 3000) where T : INotifyPropertyChanged
        {
            if (obj is null || predicate is null || action is null)
                return;

            if (predicate.Invoke(obj))
            {
                action.Invoke();
            }
            else
            {
                var cts = new CancellationTokenSource();
                var tok = cts.Token;

                void onPropertyChanged(object sender, PropertyChangedEventArgs args)
                {
                    if (sender is T npc && predicate.Invoke(npc))
                    {
                        cts.Cancel();
                        npc.PropertyChanged -= onPropertyChanged;
                        action.Invoke();
                    }
                }

                obj.PropertyChanged += onPropertyChanged;

                if (timeoutMs > 0)
                {
                    _ = Task.Delay(timeoutMs, tok).ContinueWith(r =>
                    {
                        if (!r.IsCanceled)
                            obj.PropertyChanged -= onPropertyChanged;
                    });
                }
            }
        }
    }
}