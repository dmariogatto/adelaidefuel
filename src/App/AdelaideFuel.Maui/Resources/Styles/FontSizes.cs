namespace AdelaideFuel.Maui.Styles
{
    public class FontSizes : ResourceDictionary
    {
        public const string Default = nameof(Default);
        public const string Micro = nameof(Micro);
        public const string Small = nameof(Small);
        public const string Medium = nameof(Medium);
        public const string Large = nameof(Large);
        public const string Body = nameof(Body);
        public const string Header = nameof(Header);
        public const string Title = nameof(Title);
        public const string Subtitle = nameof(Subtitle);
        public const string Caption = nameof(Caption);

        public FontSizes()
        {
            if (DeviceInfo.Current.Platform == DevicePlatform.Android)
            {
                Add(Default, 15d);
                Add(Micro, 10d);
                Add(Small, 14d);
                Add(Medium, 17d);
                Add(Large, 22d);
                Add(Body, 16d);
                Add(Header, 14d);
                Add(Title, 24d);
                Add(Subtitle, 16d);
                Add(Caption, 12d);
            }
            else
            {
                Add(Default, 17d);
                Add(Micro, 12d);
                Add(Small, 14d);
                Add(Medium, 17d);
                Add(Large, 22d);
                Add(Body, 17d);
                Add(Header, 17d);
                Add(Title, 28d);
                Add(Subtitle, 22d);
                Add(Caption, 12d);
            }
        }
    }
}
