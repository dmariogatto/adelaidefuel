namespace AdelaideFuel.Maui.Extensions
{
    public static class ResourceDictionaryExtensions
    {
        public static T FindResource<T>(this Application application, string resourceKey)
            => FindResource(application, resourceKey, default(T));

        public static T FindResource<T>(this Application application, string resourceKey, T defaultValue)
            => FindResource(application?.Resources, resourceKey, defaultValue);

        public static T FindResource<T>(this VisualElement visualElement, string resourceKey)
            => FindResource(visualElement, resourceKey, default(T));

        public static T FindResource<T>(this VisualElement visualElement, string resourceKey, T defaultValue)
            => FindResource(visualElement?.Resources, resourceKey, defaultValue);

        public static T FindResource<T>(this ResourceDictionary resourceDictionary, string resourceKey)
            => FindResource(resourceDictionary, resourceKey, default(T));

        public static T FindResource<T>(this ResourceDictionary resourceDictionary, string resourceKey, T defaultValue)
        {
            var resource = FindResource(resourceDictionary, resourceKey);
            return resource is T typedResource ? typedResource : defaultValue;
        }

        public static object FindResource(this ResourceDictionary resourceDictionary, string resourceKey)
        {
            ArgumentNullException.ThrowIfNull(resourceDictionary);
            ArgumentException.ThrowIfNullOrEmpty(resourceKey);

            if (resourceDictionary.TryGetValue(resourceKey, out var resource))
                return resource;

            foreach (var md in resourceDictionary.MergedDictionaries)
            {
                if (md.TryGetValue(resourceKey, out resource))
                    return resource;
            }

            throw new KeyNotFoundException($"The resource '{resourceKey}' is not present in the dictionary");
        }
    }
}
