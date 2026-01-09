using AdelaideFuel.Maui.Extensions;
using Microsoft.Maui.Controls.Shapes;

namespace AdelaideFuel.Maui.Controls
{
    [ContentProperty(nameof(Children))]
    public class BottomTabControl : Grid
    {
        public static readonly BindableProperty SelectedIndexProperty =
          BindableProperty.Create(
              propertyName: nameof(SelectedIndex),
              returnType: typeof(int),
              declaringType: typeof(BottomTabControl),
              defaultValue: -1,
              defaultBindingMode: BindingMode.TwoWay,
              propertyChanged: OnSelectedIndexChanged);

        public static readonly BindableProperty PrimaryColorProperty =
            BindableProperty.Create(
                propertyName: nameof(PrimaryColor),
                returnType: typeof(Color),
                declaringType: typeof(BottomTabControl),
                defaultValue: Colors.Yellow,
                propertyChanged: null);

        private readonly List<View> _children = [];

        private readonly Border _selectedMarker;

        private View _selectedView;

        public BottomTabControl()
        {
            HorizontalOptions = LayoutOptions.Fill;
            VerticalOptions = LayoutOptions.Start;

            SafeAreaEdges = SafeAreaEdges.None;

            Margin = 0;
            Padding = 0;

            RowSpacing = 0;
            ColumnSpacing = 0;

            _selectedMarker = new Border
            {
                Padding = 0,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Fill
            };

            AutomationProperties.SetIsInAccessibleTree(_selectedMarker, false);
            _selectedMarker.SetBinding(BackgroundProperty, static (BottomTabControl i) => i.PrimaryColor, source: this);

            this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(3, GridUnitType.Absolute) });
            this.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _selectedMarker.HeightRequest = this.RowDefinitions[0].Height.Value;
            _selectedMarker.WidthRequest = 16;
            _selectedMarker.StrokeShape = new RoundRectangle
            {
                CornerRadius = (float)(_selectedMarker.HeightRequest / 2d)
            };

            Children.Add(_selectedMarker);

            ChildAdded += (sender, args) =>
            {
                var child = (View)args.Element;
                child.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(() => SelectedIndex = _children.IndexOf(child)),
                });

                _children.Add((View)args.Element);
                Redraw();
                UpdateSelectedBorderPosition();
            };
            ChildRemoved += (sender, args) =>
            {
                _children.Remove((View)args.Element);
                Redraw();
                UpdateSelectedBorderPosition();
            };
        }

        public event EventHandler<IndexChangedArgs> IndexChanged;

        public int SelectedIndex
        {
            get => (int)GetValue(SelectedIndexProperty);
            set => SetValue(SelectedIndexProperty, value);
        }

        public Color PrimaryColor
        {
            get => (Color)GetValue(PrimaryColorProperty);
            set => SetValue(PrimaryColorProperty, value);
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            if (width <= 0 || height <= 0)
                return;

            UpdateSelectedBorderPosition();
        }

        private void Redraw()
        {
            ColumnDefinitions.Clear();

            for (var i = 0; i < _children.Count; i++)
            {
                ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

                Grid.SetRow(_children[i], 1);
                Grid.SetColumn(_children[i], i);

                var selected = i == SelectedIndex;
                if (_children[i] is BottomTabItem tabItem)
                    tabItem.SetSelected(selected);
                if (selected)
                    _selectedView = _children[i];
            }
        }

        private void UpdateSelectedBorderPosition()
        {
            const int animationLengthMs = 350;

            if (_children.Count == 0)
                return;
            if (Width <= 0)
                return;

            _selectedMarker.CancelAnimations();

            if (_selectedView is null)
            {
                _selectedMarker.TranslationX = 0;
                _selectedMarker.IsVisible = false;
                return;
            }

            _selectedMarker.IsVisible = true;

            var columnWidth = Width / _children.Count;
            var markerWidth = _selectedMarker.Width > 0
                ? _selectedMarker.Width
                : _selectedMarker.WidthRequest;

            var tranX = columnWidth * SelectedIndex;
            tranX += (columnWidth / 2d) - (markerWidth / 2d);
            tranX = Math.Max(0, tranX);

            if (_selectedMarker.Width <= 0)
            {
                _selectedMarker.TranslationX = tranX;
                _selectedMarker.ScaleX = 1d;
                return;
            }

            _ = _selectedMarker.TranslateToAsync(Math.Max(0, tranX), 0, animationLengthMs, Easing.CubicOut);
            _ = _selectedMarker.ScaleXToAsync(3d, animationLengthMs / 2, Easing.CubicOut)
                .ContinueWith(r =>
                {
                    if (r.IsCompletedSuccessfully)
                        _ = _selectedMarker.ScaleXToAsync(1d, animationLengthMs / 2, Easing.CubicInOut);
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void SelectedIndexChanged(int oldIdx, int newIdx)
        {
            View getItem(int idx) => idx >= 0 && idx < _children.Count ? _children[idx] : null;

            var newSelected = getItem(newIdx);

            (_selectedView as BottomTabItem)?.SetSelected(false);
            _selectedView = newSelected;
            (_selectedView as BottomTabItem)?.SetSelected(true);

            if (oldIdx != newIdx && _selectedView is BottomTabItem item)
            {
                SemanticScreenReader.Announce($"'{item.Text}' tab selected");
            }

            IndexChanged?.Invoke(this, new IndexChangedArgs(oldIdx, newIdx));
        }

        private static void OnSelectedIndexChanged(BindableObject sender, object oldValue, object newValue)
        {
            var tabBar = (BottomTabControl)sender;

            var oldIdx = (int)oldValue;
            var newIdx = (int)newValue;

            tabBar.UpdateSelectedBorderPosition();
            tabBar.SelectedIndexChanged(oldIdx, newIdx);
        }
    }

    public class BottomTabItem : VerticalStackLayout
    {
        public static readonly BindableProperty UnselectedColorProperty =
            BindableProperty.Create(
                propertyName: nameof(UnselectedColor),
                returnType: typeof(Color),
                declaringType: typeof(BottomTabItem),
                defaultValue: Colors.Blue);

        public static readonly BindableProperty SelectedColorProperty =
            BindableProperty.Create(
                propertyName: nameof(SelectedColor),
                returnType: typeof(Color),
                declaringType: typeof(BottomTabItem),
                defaultValue: Colors.Red);

        public static readonly BindableProperty IconSourceProperty =
            BindableProperty.Create(
                propertyName: nameof(IconSource),
                returnType: typeof(ImageSource),
                declaringType: typeof(BottomTabItem),
                defaultValue: null);

        public static readonly BindableProperty TextProperty =
            BindableProperty.Create(
                propertyName: nameof(Text),
                returnType: typeof(string),
                declaringType: typeof(BottomTabItem),
                defaultValue: null);

        public static readonly BindableProperty IconSizeProperty =
            BindableProperty.Create(
                propertyName: nameof(IconSize),
                returnType: typeof(double),
                declaringType: typeof(BottomTabItem),
                defaultValue: 28d);

        public static readonly BindableProperty TextSizeProperty =
            BindableProperty.Create(
                propertyName: nameof(TextSize),
                returnType: typeof(double),
                declaringType: typeof(BottomTabItem),
                defaultValue: -1d);

        private readonly TintImage _image = new TintImage();
        private readonly Label _label = new Label();

        private bool _selected;

        public BottomTabItem()
        {
            AutomationProperties.SetIsInAccessibleTree(this, true);

            SemanticProperties.SetDescription(this, "Tab");
            this.SetBinding(
                SemanticProperties.HintProperty,
                static (BottomTabItem i) => i.Text,
                stringFormat: "Tap to select '{0}' tab",
                source: this);

            SafeAreaEdges = SafeAreaEdges.None;

            HorizontalOptions = LayoutOptions.Fill;
            VerticalOptions = LayoutOptions.Fill;
            Padding = App.Current.FindResource<Thickness>(Styles.Keys.XSmallThickness);

            _label.SetDynamicResource(Label.StyleProperty, Styles.Keys.LabelStyle);
            this.SetDynamicResource(BottomTabItem.TextSizeProperty, Styles.FontSizes.Micro);

            AutomationProperties.SetIsInAccessibleTree(_image, false);

            _image.HorizontalOptions = LayoutOptions.Fill;
            _image.VerticalOptions = LayoutOptions.Start;
            _image.Aspect = Aspect.AspectFit;

            _label.HorizontalOptions = LayoutOptions.Center;
            _label.VerticalOptions = LayoutOptions.Start;
            SemanticProperties.SetHeadingLevel(_label, SemanticHeadingLevel.Level1);

            _image.SetBinding(Image.SourceProperty, static (BottomTabItem i) => i.IconSource, source: this);
            _image.SetBinding(Image.HeightRequestProperty, static (BottomTabItem i) => i.IconSize, source: this);

            _label.SetBinding(Label.TextProperty, static (BottomTabItem i) => i.Text, source: this);
            _label.SetBinding(Label.FontSizeProperty, static (BottomTabItem i) => i.TextSize, source: this);

            Children.Add(_image);
            Children.Add(_label);
        }

        public bool Selected => _selected;

        public Color UnselectedColor
        {
            get => (Color)GetValue(UnselectedColorProperty);
            set => SetValue(UnselectedColorProperty, value);
        }

        public Color SelectedColor
        {
            get => (Color)GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        public ImageSource IconSource
        {
            get => (ImageSource)GetValue(IconSourceProperty);
            set => SetValue(IconSourceProperty, value);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        public double TextSize
        {
            get => (double)GetValue(TextSizeProperty);
            set => SetValue(TextSizeProperty, value);
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;

            if (selected)
            {
                _image.SetBinding(TintImage.TintColorProperty, static (BottomTabItem i) => i.SelectedColor, source: this);
                _label.SetBinding(Label.TextColorProperty, static (BottomTabItem i) => i.SelectedColor, source: this);
            }
            else
            {
                _image.SetBinding(TintImage.TintColorProperty, static (BottomTabItem i) => i.UnselectedColor, source: this);
                _label.SetBinding(Label.TextColorProperty, static (BottomTabItem i) => i.UnselectedColor, source: this);
            }
        }
    }
}