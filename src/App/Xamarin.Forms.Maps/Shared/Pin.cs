using System.ComponentModel;

namespace Xamarin.Forms.Maps
{
    public class Pin : Element
    {
        public static readonly BindableProperty TintColorProperty =
            BindableProperty.Create(nameof(TintColor), typeof(Color), typeof(Pin), Color.Transparent);

        public static readonly BindableProperty PositionProperty =
            BindableProperty.Create(nameof(Position), typeof(Position), typeof(Pin), default(Position));

        public static readonly BindableProperty AddressProperty =
            BindableProperty.Create(nameof(Address), typeof(string), typeof(Pin), default(string));

        public static readonly BindableProperty LabelProperty =
            BindableProperty.Create(nameof(Label), typeof(string), typeof(Pin), default(string));

        public static readonly BindableProperty AnchorProperty =
            BindableProperty.Create(nameof(Anchor), typeof(Point), typeof(Pin), new Point(0.5, 1.0));

        public static readonly BindableProperty FileImageSourceProperty =
            BindableProperty.Create(nameof(FileImage), typeof(string), typeof(Pin));

        public static readonly BindableProperty ZIndexProperty =
            BindableProperty.Create(nameof(ZIndex), typeof(int), typeof(Pin));

        private object _markerId;

        public Color TintColor
        {
            get => (Color)GetValue(TintColorProperty);
            set => SetValue(TintColorProperty, value);
        }

        public string Address
        {
            get => (string)GetValue(AddressProperty);
            set => SetValue(AddressProperty, value);
        }

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public Position Position
        {
            get => (Position)GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
        }

        public Point Anchor
        {
            get => (Point)GetValue(AnchorProperty);
            set => SetValue(AnchorProperty, value);
        }

        public string FileImage
        {
            get => (string)GetValue(FileImageSourceProperty);
            set => SetValue(FileImageSourceProperty, value);
        }

        public int ZIndex
        {
            get => (int)GetValue(ZIndexProperty);
            set => SetValue(ZIndexProperty, value);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public object MarkerId
        {
            get => _markerId;
            set => _markerId = value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((Pin)obj);
        }

        public override int GetHashCode() => (TintColor, Label, Address, Position, ZIndex).GetHashCode();

        public static bool operator ==(Pin left, Pin right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Pin left, Pin right)
        {
            return !Equals(left, right);
        }

        private bool Equals(Pin other) => other is Pin pin &&
            TintColor == pin.TintColor &&
            Label == pin.Label &&
            Address == pin.Address &&
            Position == pin.Position &&
            ZIndex == pin.ZIndex;
    }
}