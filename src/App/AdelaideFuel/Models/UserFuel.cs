using MvvmHelpers;
using System;
using System.Diagnostics;

namespace AdelaideFuel.Models
{
    [DebuggerDisplay("{Name}")]
    public class UserFuel : ObservableObject, IUserSortableEntity
    {
        private int _id;
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private int _sortOrder;
        public int SortOrder
        {
            get => _sortOrder;
            set => SetProperty(ref _sortOrder, value);
        }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public override bool Equals(object obj)
            => obj is UserFuel fuel &&
               Id == fuel.Id &&
               Name == fuel.Name &&
               SortOrder == fuel.SortOrder &&
               IsActive == fuel.IsActive;

        public override int GetHashCode()
            => HashCode.Combine(Id, Name, SortOrder, IsActive);
    }
}