using Microsoft.Maui.Controls.Platform;
using System.Reflection;
using UIKit;

namespace AdelaideFuel.Maui.Effects
{
    public class SafeAreaInsetPlatformEffect : PlatformEffect
    {
        private PaddingElement _paddingElement;
        private Thickness _originalPadding;
        private bool _registered;

        protected override void OnAttached()
        {
            _paddingElement = new PaddingElement(Element);

            _originalPadding = _paddingElement.Padding;
            ApplyInsets(_paddingElement, _originalPadding);

            _paddingElement.Element.SizeChanged += ElementSizeChanged;
            _registered = true;
        }

        protected override void OnDetached()
        {
            if (_registered)
            {
                _paddingElement.Element.SizeChanged -= ElementSizeChanged;
                _paddingElement.Padding = _originalPadding;
                _registered = false;
            }
        }

        private void ElementSizeChanged(object sender, EventArgs e)
        {
            // Handle orientation changes
            if (_registered)
            {
                ApplyInsets(_paddingElement, _originalPadding);
            }
        }

        private static void ApplyInsets(PaddingElement paddingElement, Thickness defaultPadding)
        {
            var safeInset = GetSafeAreaInset();

            // Get the attached property value so we can apply the appropriate padding
            var insetFlags = SafeAreaInsetEffect.GetInsets(paddingElement.Element);

            // Combine the safe inset with the view's current padding
            var newPadding = CombineInset(defaultPadding, safeInset, insetFlags);

            paddingElement.Padding = newPadding;
        }

        private static Thickness GetSafeAreaInset()
        {
            var edgeInsets = default(UIEdgeInsets);

            if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
            {
#pragma warning disable CA1416 // Validate platform compatibility
                edgeInsets = UIDevice.CurrentDevice.CheckSystemVersion(15, 0)
                    ? UIApplication.SharedApplication.ConnectedScenes
                        .OfType<UIWindowScene>()
                        .FirstOrDefault(i => i.KeyWindow is not null)
                        ?.KeyWindow?.SafeAreaInsets ?? UIEdgeInsets.Zero
                    : UIApplication.SharedApplication.Windows
                        .FirstOrDefault()
                        ?.SafeAreaInsets ?? UIEdgeInsets.Zero;
#pragma warning restore CA1416 // Validate platform compatibility
            }
            else
            {
                switch (UIApplication.SharedApplication.StatusBarOrientation)
                {
                    case UIInterfaceOrientation.Portrait:
                    case UIInterfaceOrientation.PortraitUpsideDown:
                        // Default padding for older iPhones (top status bar)
                        edgeInsets = new UIEdgeInsets(20, 0, 0, 0);
                        break;
                    default:
                        edgeInsets = UIEdgeInsets.Zero;
                        break;
                }
            }

            return new Thickness(edgeInsets.Left, edgeInsets.Top, edgeInsets.Right, edgeInsets.Bottom);
        }

        private static Thickness CombineInset(Thickness defaultPadding, Thickness safeInset, SafeAreaInsets safeAreaInsets)
        {
            var result = new Thickness(defaultPadding.Left, defaultPadding.Top, defaultPadding.Right, defaultPadding.Bottom);

            if (safeAreaInsets != SafeAreaInsets.None)
            {
                if (safeAreaInsets.HasFlag(SafeAreaInsets.All))
                {
                    result.Left += safeInset.Left;
                    result.Top += safeInset.Top;
                    result.Right += safeInset.Right;
                    result.Bottom += safeInset.Bottom;
                }
                else
                {
                    if (safeAreaInsets.HasFlag(SafeAreaInsets.Left))
                    {
                        result.Left += safeInset.Left;
                    }
                    if (safeAreaInsets.HasFlag(SafeAreaInsets.Top))
                    {
                        result.Top += safeInset.Top;
                    }
                    if (safeAreaInsets.HasFlag(SafeAreaInsets.Right))
                    {
                        result.Right += safeInset.Right;
                    }
                    if (safeAreaInsets.HasFlag(SafeAreaInsets.Bottom))
                    {
                        result.Bottom += safeInset.Bottom;
                    }
                }
            }

            return result;
        }

        private class PaddingElement
        {
            private readonly PropertyInfo _paddingInfo;

            public PaddingElement(object e)
            {
                // VisualElement provides the SizeChanged event handler
                if (e is VisualElement ve)
                {
                    Element = ve;

                    // Make sure we've got a view with Padding support
                    var type = Element.GetType();
                    if (type.GetProperty(nameof(Layout.Padding)) is PropertyInfo pi &&
                        pi.GetMethod is not null &&
                        pi.SetMethod is not null)
                    {
                        _paddingInfo = pi;
                    }
                    else
                    {
                        throw new ArgumentException($"Element must have a {nameof(Layout.Padding)} property with a public getter & setter!");
                    }
                }
                else
                {
                    throw new ArgumentException($"Element must inherit from {nameof(VisualElement)}");
                }
            }

            public VisualElement Element { get; private set; }

            public Thickness Padding
            {
                get => (Thickness)_paddingInfo.GetValue(Element);
                set => _paddingInfo.SetValue(Element, value);
            }
        }
    }
}