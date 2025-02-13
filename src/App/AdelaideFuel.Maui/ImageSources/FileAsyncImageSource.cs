namespace AdelaideFuel.Maui.ImageSources
{
    public class FileAsyncImageSource : ImageSource, IFileAsyncImageSource
    {
        public override bool IsEmpty => File is null;

        public Func<CancellationToken, Task<string>> File { get; set; }

        async Task<string> IFileAsyncImageSource.GetFileAsync(CancellationToken userToken)
            => !IsEmpty ? await File.Invoke(userToken) : string.Empty;
    }
}
