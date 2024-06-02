namespace AdelaideFuel.Maui.Dispatching
{
    public static class DispatcherExtensions
    {
        /// <summary>
        /// Starts a timer on the specified <see cref="IDispatcher"/> context.
        /// </summary>
        /// <param name="dispatcher">The <see cref="IDispatcher"/> instance this method is called on.</param>
        /// <param name="interval">Sets the amount of time between timer ticks.</param>
        /// <param name="callback">The callback on which the dispatcher returns when the event is dispatched.
        /// <returns>Dispatcher timer</returns>
        /// If the result of the callback is <see langword="true"/>, the timer will repeat, otherwise the timer stops.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="callback"/> is <see langword="null"/>.</exception>
        public static IDispatcherTimer CreateAndStartTimer(this IDispatcher dispatcher, TimeSpan interval, Func<bool> callback)
        {
            _ = callback ?? throw new ArgumentNullException(nameof(callback));

            var timer = dispatcher.CreateTimer();
            timer.Interval = interval;
            timer.IsRepeating = true;
            timer.Tick += OnTick;
            timer.Start();

            void OnTick(object sender, EventArgs e)
            {
                if (!callback())
                {
                    timer.Tick -= OnTick;
                    timer.Stop();
                }
            }

            return timer;
        }
    }
}
