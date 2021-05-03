using System;

namespace AdelaideFuel.ViewModels
{
    public interface IViewModel
    {
        void OnCreate();
        void OnAppearing();
        void OnDisappearing();
        void OnDestory();
    }
}