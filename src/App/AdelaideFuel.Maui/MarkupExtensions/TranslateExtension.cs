using AdelaideFuel.Localisation;
using AdelaideFuel.Services;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace AdelaideFuel.Maui
{
    [ContentProperty(nameof(Text))]
    public class TranslateExtension : IMarkupExtension
    {
        private const string ResourceId = "AdelaideFuel.Localisation.Resources";

        private static readonly Lazy<ResourceManager> ResMgr = new Lazy<ResourceManager>(() =>
            new ResourceManager(ResourceId, typeof(Resources).GetTypeInfo().Assembly));
        private static readonly Lazy<ILocalise> Localise = new Lazy<ILocalise>(() =>
            IoC.Resolve<ILocalise>());

        private readonly CultureInfo _ci;

        public TranslateExtension()
        {
            _ci = Localise.Value.GetCurrentCultureInfo();
        }

        public string Text { get; set; }

        public TextTransform Transform { get; set; }

        public object ProvideValue(IServiceProvider serviceProvider)
        {
            if (string.IsNullOrWhiteSpace(Text))
                return string.Empty;

            var translation = ResMgr.Value.GetString(Text, _ci);

            if (translation is null)
            {
#if DEBUG
                throw new ArgumentException(
                    $"Key '{Text}' was not found in resources '{ResourceId}' for culture '{_ci.Name}'.",
                    nameof(Text));
#else
                translation = Text; // returns the key, which GETS DISPLAYED TO THE USER
#endif
            }

            translation = Transform switch
            {
                TextTransform.Uppercase => translation.ToUpper(),
                TextTransform.Lowercase => translation.ToLower(),
                _ => translation
            };

            return translation;
        }
    }
}