using MvvmHelpers;
using System;

namespace AdelaideFuel.Models
{
    public class UserBrand : ObservableObject, IUserSortableEntity
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
            => obj is UserBrand brand &&
               Id == brand.Id &&
               Name == brand.Name &&
               SortOrder == brand.SortOrder &&
               IsActive == brand.IsActive;

        public override int GetHashCode()
            => HashCode.Combine(Id, Name, SortOrder, IsActive);
    }
}