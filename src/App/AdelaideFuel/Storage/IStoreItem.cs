using System;

namespace AdelaideFuel.Storage
{
    public interface IStoreItem
    {
        public string Id { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateExpires { get; set; }
    }
}