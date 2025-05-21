using AdelaideFuel.Services;

#if __IOS__
using StoreKit;
using UIKit;
#endif

#if __ANDROID__
using Android.Views;
using AndroidX.Room.Util;
using Xamarin.Google.Android.Play.Core.Review;
using Xamarin.Google.Android.Play.Core.Review.Testing;
#endif

namespace AdelaideFuel.Maui.Services;

public class StoreReview :
#if __ANDROID__
    Java.Lang.Object, IStoreReview, Android.Gms.Tasks.IOnCompleteListener
#else
    IStoreReview
#endif
{
    /// <summary>
    /// Opens the store listing.
    /// </summary>
    /// <param name="appId">App identifier.</param>
    public async Task<bool> OpenStoreListingAsync(string appId)
    {
        try
        {
            var url = default(string);
#if __IOS__
            url = $"itms-apps://itunes.apple.com/app/id{appId}";
#elif __ANDROID__
            url = await GetAndroidUrlAsync(appId);
#else
            throw new NotImplementedException();
#endif
            return await Launcher.Default.OpenAsync(url);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Unable to launch app store: " + ex.Message);
        }

        return false;
    }

    /// <summary>
    /// Opens the store review page.
    /// </summary>
    /// <param name="appId">App identifier.</param>
    public async Task<bool> OpenStoreReviewPageAsync(string appId)
    {
        try
        {
            var url = default(string);
#if __IOS__
            url = $"itms-apps://itunes.apple.com/app/id{appId}?action=write-review";
#elif __ANDROID__
            url = await GetAndroidUrlAsync(appId);
#else
            throw new NotImplementedException();
#endif
            return await Launcher.Default.OpenAsync(url);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Unable to launch app store: " + ex.Message);
        }

        return false;
    }

    /// <summary>
    /// Requests an app review.
    /// </summary>
    public async Task RequestReviewAsync(bool testMode)
    {
#if __IOS__
        if (UIDevice.CurrentDevice.CheckSystemVersion(14, 0))
        {
#pragma warning disable CA1416,CA1422  // Validate platform compatibility
            if (UIApplication.SharedApplication?.ConnectedScenes?.ToArray<UIScene>()?.FirstOrDefault(x => x.ActivationState == UISceneActivationState.ForegroundActive) is UIWindowScene windowScene)
            {
                SKStoreReviewController.RequestReview(windowScene);
            }
#pragma warning restore CA1416,CA1422 // Validate platform compatibility
        }
        else
        {
            SKStoreReviewController.RequestReview();
        }
        await Task.CompletedTask;
#elif __ANDROID__
        _tcs?.TrySetCanceled();
        _tcs = new TaskCompletionSource<bool>();

        if (testMode)
            _manager = new FakeReviewManager(Platform.AppContext);
        else
            _manager = ReviewManagerFactory.Create(Platform.AppContext);

        _forceReturn = false;
        var request = _manager.RequestReviewFlow();
        request.AddOnCompleteListener(this);
        var status = await _tcs.Task;
        _manager.Dispose();
        request.Dispose();
        _manager = null;
#else
        throw new NotImplementedException();
#endif
    }

#if __ANDROID__
    private IReviewManager _manager;
    private TaskCompletionSource<bool> _tcs;
    private bool _forceReturn;
    private Android.Gms.Tasks.Task _launchTask;
    public void OnComplete(Android.Gms.Tasks.Task task)
    {
        if (!task.IsSuccessful || _forceReturn)
        {
            _tcs.TrySetResult(_forceReturn);
            _launchTask?.Dispose();
            _launchTask = null;
            return;
        }

        try
        {
            var reviewInfo = (ReviewInfo)task.GetResult(Java.Lang.Class.FromType(typeof(ReviewInfo)));
            _forceReturn = true;
            _launchTask = _manager.LaunchReviewFlow(Platform.CurrentActivity, reviewInfo);
            _launchTask.AddOnCompleteListener(this);
        }
        catch (Exception ex)
        {
            _tcs.TrySetResult(false);
            System.Diagnostics.Debug.WriteLine(ex.Message);
        }
    }

    private static async Task<string> GetAndroidUrlAsync(string appId)
    {
        var url = $"market://details?id={appId}";
        if (!await Launcher.Default.CanOpenAsync(url))
            url = $"https://play.google.com/store/apps/details?id={appId}";
        return url;
    }
#endif
}