namespace AdelaideFuel.Maui.ImageSources
{
    public interface IFileAsyncImageSource : IImageSource
    {
        Task<string> GetFileAsync(CancellationToken cancellationToken = default);
    }
}
