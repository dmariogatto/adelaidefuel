using Xamarin.Forms;

namespace AdelaideFuel.UI.Controls
{
    public enum AdLoadStatus
    {
        None,
        Loading,
        Loaded,
        Failed
    }

    public class AdSmartBanner : View
    {
        public static readonly BindableProperty AdUnitIdProperty =
          BindableProperty.Create(
              propertyName: nameof(AdUnitId),
              returnType: typeof(string),
              declaringType: typeof(AdSmartBanner),
              defaultValue: string.Empty);
        public static readonly BindableProperty AdStatusProperty =
          BindableProperty.Create(
              propertyName: nameof(AdStatus),
              returnType: typeof(AdLoadStatus),
              declaringType: typeof(AdSmartBanner),
              defaultValue: AdLoadStatus.None);

        public AdSmartBanner()
        {
            IsVisible = false;
            Margin = 0;
        }

        public string AdUnitId
        {
            get => (string)GetValue(AdUnitIdProperty);
            set => SetValue(AdUnitIdProperty, value);
        }

        public AdLoadStatus AdStatus
        {
            get => (AdLoadStatus)GetValue(AdStatusProperty);
            set => SetValue(AdStatusProperty, value);
        }
    }
}