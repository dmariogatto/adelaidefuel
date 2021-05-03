using AdelaideFuel.Services;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace AdelaideFuel.UI
{
    [Preserve(AllMembers = true)]
    public static class FaderBehavior
    {
        private const string Visible = nameof(Visible);
        private const string Toggle = nameof(Toggle);

        public static readonly BindableProperty VisibleProperty = BindableProperty.Create(
            propertyName: Visible,
            returnType: typeof(bool),
            declaringType: typeof(FaderBehavior),
            defaultValue: null,
            defaultBindingMode: BindingMode.OneWay,
            propertyChanged: VisiblePropertyChanged);

        public static bool GetVisible(BindableObject obj) => (bool)obj.GetValue(VisibleProperty);
        public static void SetVisible(BindableObject obj, bool value) => obj.SetValue(VisibleProperty, value);

        public static readonly BindableProperty ToggleProperty = BindableProperty.Create(
            propertyName: Toggle,
            returnType: typeof(bool),
            declaringType: typeof(FaderBehavior),
            defaultValue: null,
            defaultBindingMode: BindingMode.OneWay,
            propertyChanged: TogglePropertyChanged);

        public static bool GetToggle(BindableObject obj) => (bool)obj.GetValue(ToggleProperty);
        public static void SetToggle(BindableObject obj, bool value) => obj.SetValue(ToggleProperty, value);

        private static void VisiblePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is View view)
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        var visible = GetVisible(view);

                        if (visible && view.Opacity < 1)
                        {
                            AutomationProperties.SetIsInAccessibleTree(view, visible);

                            ViewExtensions.CancelAnimations(view);
                            await view.FadeTo(1, 250);
                        }
                        else if (!visible)
                        {
                            AutomationProperties.SetIsInAccessibleTree(view, visible);

                            ViewExtensions.CancelAnimations(view);
                            view.Opacity = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        IoC.Resolve<ILogger>().Error(ex);
                    }
                });
            }
        }

        private static void TogglePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is View view)
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        var toggleOn = GetToggle(view);

                        ViewExtensions.CancelAnimations(view);

                        if (toggleOn)
                        {
                            await Task.WhenAll(view.FadeTo(1, 250), view.ScaleTo(1.125, 250));
                        }
                        else
                        {
                            await Task.WhenAll(view.FadeTo(0.75, 250), view.ScaleTo(1, 250));
                        }
                    }
                    catch (Exception ex)
                    {
                        IoC.Resolve<ILogger>().Error(ex);
                    }
                });
            }
        }
    }
}