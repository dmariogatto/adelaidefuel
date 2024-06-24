using AdelaideFuel.ViewModels;
using MemoryToolkit.Maui;

namespace AdelaideFuel.Maui.Views
{
    public interface IBasePage : IContentView
    {
        void OnDestory();
    }

    public class BasePage<T> : ContentPage, IBasePage where T : BaseViewModel
    {
        public T ViewModel => BindingContext as T;

        public BasePage() : base()
        {
            var vm = IoC.Resolve<T>();
            BindingContext = vm ?? throw new ArgumentNullException(typeof(T).Name);

            ViewModel.OnCreate();
        }

        public virtual void OnDestory()
        {
            ViewModel.OnDestory();
            //this.TearDown();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ViewModel.OnAppearing();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            ViewModel.OnDisappearing();
        }
    }
}