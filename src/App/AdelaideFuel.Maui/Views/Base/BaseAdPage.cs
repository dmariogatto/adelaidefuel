using AdelaideFuel.ViewModels;

namespace AdelaideFuel.Maui.Views
{
    [ContentProperty(nameof(MainContent))]
    public class BaseAdPage<T> : BasePage<T> where T : BaseViewModel
    {
        private AdContentView _mainView;

        public BaseAdPage() : base()
        {
            Content = _mainView = new AdContentView();
        }

        public View MainContent
        {
            get => _mainView.Content;
            set =>  _mainView.Content = value;
        }

        public string AdUnitId
        {
            get => _mainView.AdUnitId;
            set => _mainView.AdUnitId = value;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            _mainView.OnAppearing();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            _mainView.OnDisappearing();
        }
    }
}