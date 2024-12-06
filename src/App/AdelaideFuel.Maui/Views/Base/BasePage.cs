using AdelaideFuel.ViewModels;

namespace AdelaideFuel.Maui.Views
{
    public interface IBasePage : IContentView
    {
        void OnDestroy();
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

        public virtual void OnDestroy()
        {
            ViewModel.OnDestroy();
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