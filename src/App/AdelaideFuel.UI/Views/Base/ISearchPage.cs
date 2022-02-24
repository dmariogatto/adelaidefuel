using System;
using System.ComponentModel;

namespace AdelaideFuel.UI.Views
{
    public interface ISearchPage : INotifyPropertyChanged
    {
        string Query { get; set; }
        string Placeholder { get; set; }
    }
}