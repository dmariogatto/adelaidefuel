using System.ComponentModel;

namespace AdelaideFuel.Maui.Views
{
    public interface ISearchPage : INotifyPropertyChanged
    {
        string Query { get; set; }
        string Placeholder { get; set; }
    }
}