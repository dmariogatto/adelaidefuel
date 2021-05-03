using System;

namespace AdelaideFuel.Models
{
    public interface IUserSortableEntity : IUserEntity
    {
        int SortOrder { get; set; }
    }
}