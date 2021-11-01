using LiteDB;
using System;

namespace AdelaideFuel.Storage
{
    public class StoreItem<T> : IStoreItem
    {
        [BsonId]
        public string Id { get; set; }
        public T Contents { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateExpires { get; set; }

        public bool HasExpired() => DateExpires <= DateTime.UtcNow;
    }
}