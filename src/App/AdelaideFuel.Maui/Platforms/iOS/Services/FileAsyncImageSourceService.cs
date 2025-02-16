using AdelaideFuel.Maui.ImageSources;
using Microsoft.Extensions.Logging;
using UIKit;

namespace AdelaideFuel.Services
{
    public class FileAsyncImageSourceService : ImageSourceService, IImageSourceService<IFileAsyncImageSource>
    {
        private readonly IImageSourceService<FileImageSource> _fileImageSourceService;

        public FileAsyncImageSourceService(IImageSourceService<FileImageSource> fileImageSourceService)
            : this(fileImageSourceService, null)
        {
        }

        public FileAsyncImageSourceService(IImageSourceService<FileImageSource> fileImageSourceService, ILogger<FileAsyncImageSourceService> logger = null)
            : base(logger)
        {
            _fileImageSourceService = fileImageSourceService ?? throw new ArgumentNullException(nameof(fileImageSourceService));
        }

        public override async Task<IImageSourceServiceResult<UIImage>> GetImageAsync(IImageSource imageSource, float scale = 1, CancellationToken cancellationToken = default)
        {
            var fileAsyncImageSource = (IFileAsyncImageSource)imageSource;

            if (!fileAsyncImageSource.IsEmpty)
            {
                var file = string.Empty;

                try
                {
                    file = await fileAsyncImageSource.GetFileAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    Logger?.LogWarning(ex, "Unable to load image path.");
                    throw;
                }

                if (!string.IsNullOrEmpty(file))
                {
                    return await _fileImageSourceService.GetImageAsync(ImageSource.FromFile(file), scale, cancellationToken);
                }
            }

            return null;
        }
    }
}