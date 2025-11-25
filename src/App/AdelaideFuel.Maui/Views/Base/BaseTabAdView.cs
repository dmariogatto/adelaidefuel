using AdelaideFuel.ViewModels;

namespace AdelaideFuel.Maui.Views
{
    [ContentProperty(nameof(MainContent))]
    public class BaseTabAdView<T> : BaseTabView<T> where T : BaseViewModel
    {
        private AdContentView _mainView;

        public BaseTabAdView() : base()
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

        public override void OnAppearing()
        {
            base.OnAppearing();

            _mainView.OnAppearing();
        }

        public override void OnDisappearing()
        {
            base.OnDisappearing();

            _mainView.OnDisappearing();
        }
    }
}