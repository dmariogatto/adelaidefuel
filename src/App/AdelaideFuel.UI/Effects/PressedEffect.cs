using System.Windows.Input;
using Xamarin.Forms;

namespace AdelaideFuel.UI.Effects
{
    public class PressedEffect : RoutingEffect
    {
        public PressedEffect() : base($"AdelaideFuel.Effects.{nameof(PressedEffect)}")
        {
        }

        public static readonly BindableProperty CommandProperty =
            BindableProperty.CreateAttached(
                "Command",
                typeof(ICommand),
                typeof(PressedEffect),
                (object)null);

        public static ICommand GetCommand(BindableObject view)
        {
            return (ICommand)view.GetValue(CommandProperty);
        }

        public static void SetCommand(BindableObject view, ICommand value)
        {
            view.SetValue(CommandProperty, value);
        }

        public static readonly BindableProperty CommandParameterProperty =
            BindableProperty.CreateAttached(
                "CommandParameter",
                typeof(object),
                typeof(PressedEffect),
                (object)null);

        public static object GetCommandParameter(BindableObject view)
        {
            return view.GetValue(CommandParameterProperty);
        }

        public static void SetCommandParameter(BindableObject view, object value)
        {
            view.SetValue(CommandParameterProperty, value);
        }

        public static readonly BindableProperty CommandLongProperty =
            BindableProperty.CreateAttached(
                "CommandLong",
                typeof(ICommand),
                typeof(PressedEffect),
                (object)null);

        public static ICommand GetCommandLong(BindableObject view)
        {
            return (ICommand)view.GetValue(CommandLongProperty);
        }

        public static void SetCommandLong(BindableObject view, ICommand value)
        {
            view.SetValue(CommandLongProperty, value);
        }

        public static readonly BindableProperty CommandLongParameterProperty =
            BindableProperty.CreateAttached(
                "CommandLongParameter",
                typeof(object),
                typeof(PressedEffect),
                (object)null);

        public static object GetCommandLongParameter(BindableObject view)
        {
            return view.GetValue(CommandParameterProperty);
        }

        public static void SetCommandLongParameter(BindableObject view, object value)
        {
            view.SetValue(CommandParameterProperty, value);
        }
    }
}