using System;

namespace AdelaideFuel.Models
{
    public interface IUserEntity
    {
        int Id { get; set; }
        string Name { get; set; }
        bool IsActive { get; set; }

        IUserEntity Clone();
    }
}