using System;

namespace AdelaideFuel.Functions.Services
{
    public interface ICacheService
    {
        bool TryGetValue<T>(object key, out T value);
        void SetAbsolute<T>(object key, T value, TimeSpan expires);
        void SetSliding<T>(object key, T value, TimeSpan sliding);
    }
}
