using AdelaideFuel.Localisation;
using AdelaideFuel.Services;

namespace AdelaideFuel.Maui.Services
{
    public class DialogService : IDialogService
    {
        private static Page MainPage
        {
            get
            {
                if (Application.Current.Windows.Count == 0)
                    throw new InvalidOperationException("Application does not have any Windows initialised");
                return Application.Current.Windows[0].Page;
            }
        }

        public DialogService()
        {
        }

        public void Alert(string message, string title = null, string ok = null)
        {
            MainPage.Dispatcher.DispatchAsync(() => AlertAsync(message, title, ok));
        }

        public Task AlertAsync(string message, string title = null, string ok = null)
        {
            return MainPage.DisplayAlertAsync(title, message, ok ?? Resources.OK);
        }

        public Task<bool> ConfirmAsync(string message, string title = null, string ok = null, string cancel = null)
        {
            return MainPage.DisplayAlertAsync(title, message, ok ?? Resources.OK, cancel ?? Resources.Cancel);
        }

        public Task<string> PromptAsync(string message, string title = null, string ok = null, string cancel = null, string placeholder = null, int maxLength = -1, KeyboardType keyboard = KeyboardType.Default, string initialValue = null)
        {
            return MainPage.DisplayPromptAsync(title, message, ok ?? Resources.OK, cancel ?? Resources.Cancel, placeholder, maxLength, KeyboardTypeToKeyboard(keyboard), initialValue ?? string.Empty);
        }

        public Task<string> ActionSheetAsync(string title, string cancel, string destruction, params string[] buttons)
        {
            return MainPage.DisplayActionSheetAsync(title, cancel, destruction, buttons);
        }

        private static Keyboard KeyboardTypeToKeyboard(KeyboardType type)
         => type switch
         {
             KeyboardType.Default => Keyboard.Default,
             KeyboardType.Plain => Keyboard.Plain,
             KeyboardType.Email => Keyboard.Email,
             KeyboardType.Text => Keyboard.Text,
             KeyboardType.Url => Keyboard.Url,
             KeyboardType.Numeric => Keyboard.Numeric,
             KeyboardType.Telephone => Keyboard.Telephone,
             KeyboardType.Chat => Keyboard.Chat,
             KeyboardType.Date => Keyboard.Date,
             KeyboardType.Time => Keyboard.Time,
             KeyboardType.Password => Keyboard.Password,
             _ => Keyboard.Default,
         };
    }
}