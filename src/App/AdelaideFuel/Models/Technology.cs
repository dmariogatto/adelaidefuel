using System;

namespace AdelaideFuel.Models
{
    public class Technology
    {
        private const string DefaultNuGetImageUrl = "https://www.nuget.org/Content/gallery/img/default-package-icon-256x256.png";
        private const string NuGetImageUrlFormat = "https://api.nuget.org/v3-flatcontainer/{0}/{1}/icon";

        public Technology() { }

        public Technology(string name, string description, string version, string url)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentOutOfRangeException(nameof(name));

            Name = name;
            Description = description;

            IconUrl = !string.IsNullOrEmpty(version) ? string.Format(NuGetImageUrlFormat, name, version) : DefaultNuGetImageUrl;
            PackageUrl = !string.IsNullOrEmpty(url) ? url : string.Empty;
        }

        public Technology(string name, string iconUrl, string url) : this(name, string.Empty, iconUrl, url) { }
        public Technology(string name, string url) : this(name, string.Empty, string.Empty, url) { }

        public string Name { get; set; }
        public string Description { get; set; }

        public string IconUrl { get; set; }
        public string PackageUrl { get; set; }
    }
}