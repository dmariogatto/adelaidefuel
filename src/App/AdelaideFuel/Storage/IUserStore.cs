using System.Collections.Generic;

namespace AdelaideFuel.Storage
{
    public interface IUserStore
    {
        string Name { get; }
    }

    public interface IUserStore<T> : IUserStore where T : class
    {
        IList<T> All();
        T Get(int id);

        bool Upsert(T item);
        bool Update(T item);
        bool Remove(int id);

        int UpsertRange(IEnumerable<T> toUpsert);
        int RemoveRange(IEnumerable<T> toRemove);
    }
}