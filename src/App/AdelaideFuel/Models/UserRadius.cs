using MvvmHelpers;
using System;
using System.Diagnostics;

namespace AdelaideFuel.Models
{
    [DebuggerDisplay("{Name}")]
    public class UserRadius : ObservableObject, IUserSortableEntity
    {
        private int _id;
        public int Id
        {
            get => _id;
            set
            {
                SetProperty(ref _id, value);
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(SortOrder));
            }
        }

        public string Name
        {
            get => Id != int.MaxValue
                ? string.Format(Localisation.Resources.ItemKm, Id)
                : string.Format(Localisation.Resources.CheapestInItem, Localisation.Resources.SA);
            set { }
        }

        public int SortOrder
        {
            get => Id;
            set { }
        }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public IUserEntity Clone() => new UserRadius()
        {
            Id = this.Id,
            IsActive = this.IsActive
        };

        public override bool Equals(object obj)
            => obj is UserRadius radius &&
               Id == radius.Id &&
               IsActive == radius.IsActive;

        public override int GetHashCode()
            => HashCode.Combine(Id, IsActive);
    }
}