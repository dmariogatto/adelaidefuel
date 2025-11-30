using AdelaideFuel.ViewModels;

namespace AdelaideFuel.Maui.Views
{
    public interface IBaseTabView : IContentView
    {
        string Title { get; set; }
        ImageSource IconImageSource { get; set; }

        void OnAppearing();
        void OnDisappearing();
        void OnDestroy();
    }

    public class BaseTabView<T> : ContentView, IBaseTabView where T : BaseViewModel
    {
        public static readonly BindableProperty TitleProperty =
            BindableProperty.Create(
                propertyName: nameof(Title),
                returnType: typeof(string),
                declaringType: typeof(BaseTabView<T>),
                defaultValue: null);

        public static readonly BindableProperty IconImageSourceProperty =
            BindableProperty.Create(
                propertyName: nameof(IconImageSource),
                returnType: typeof(ImageSource),
                declaringType: typeof(BaseTabView<T>),
                defaultValue: null);

        public T ViewModel => BindingContext as T;

        public BaseTabView() : base()
        {
            var vm = IoC.Resolve<T>();
            BindingContext = vm ?? throw new ArgumentNullException(typeof(T).Name);

            ViewModel.OnCreate();
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public ImageSource IconImageSource
        {
            get => (ImageSource)GetValue(IconImageSourceProperty);
            set => SetValue(IconImageSourceProperty, value);
        }

        public virtual void OnDestroy()
        {
            ViewModel.OnDestroy();
        }

        public virtual void OnAppearing()
        {
            ViewModel.OnAppearing();
        }

        public virtual void OnDisappearing()
        {
            ViewModel.OnDisappearing();
        }
    }
}