using System.Threading.Tasks;

namespace AdelaideFuel.Services;

/// <summary>
/// Interface for StoreReview
/// </summary>
public interface IStoreReview
{
    /// <summary>
    /// Opens the store listing.
    /// </summary>
    /// <param name="appId">App identifier.</param>
    Task<bool> OpenStoreListingAsync(string appId);

    /// <summary>
    /// Opens the store review page.
    /// </summary>
    /// <param name="appId">App identifier.</param>
    Task<bool> OpenStoreReviewPageAsync(string appId);

    /// <summary>
    /// Requests an app review.
    /// </summary>
    Task RequestReviewAsync(bool testMode);
}