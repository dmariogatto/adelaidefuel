using AdelaideFuel.Maui.ImageSources;
using Android.Content;
using Android.Graphics.Drawables;
using Android.Widget;
using Microsoft.Extensions.Logging;

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

        public override async Task<IImageSourceServiceResult<Drawable>> GetDrawableAsync(IImageSource imageSource, Context context, CancellationToken cancellationToken = default)
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
                    return await _fileImageSourceService.GetDrawableAsync(ImageSource.FromFile(file), context, cancellationToken);
                }
            }

            return null;
        }

        public override async Task<IImageSourceServiceResult> LoadDrawableAsync(IImageSource imageSource, ImageView imageView, CancellationToken cancellationToken = default)
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
                    return await _fileImageSourceService.LoadDrawableAsync(ImageSource.FromFile(file), imageView, cancellationToken);
                }
            }

            return null;
        }
    }
}