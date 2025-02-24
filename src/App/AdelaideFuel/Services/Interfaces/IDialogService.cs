using System.Threading.Tasks;

namespace AdelaideFuel.Services
{
    public enum KeyboardType
    {
        Default,
        Plain,
        Email,
        Text,
        Url,
        Numeric,
        Telephone,
        Chat,
        Date,
        Time,
        Password,
    }

    public interface IDialogService
    {
        void Alert(string message, string title = null, string ok = null);
        Task AlertAsync(string message, string title = null, string ok = null);
        Task<bool> ConfirmAsync(string message, string title = null, string ok = null, string cancel = null);
        Task<string> PromptAsync(string message, string title = null, string ok = null, string cancel = null, string placeholder = null, int maxLength = -1, KeyboardType keyboard = KeyboardType.Default, string initialValue = null);
        Task<string> ActionSheetAsync(string title, string cancel, string destruction, params string[] buttons);
    }
}