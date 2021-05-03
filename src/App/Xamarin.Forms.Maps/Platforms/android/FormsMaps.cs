using Android.App;
using Android.Gms.Common;
using Android.Gms.Maps;
using Android.OS;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Maps;
using Xamarin.Forms.Maps.Android;

[assembly: ExportRenderer(typeof(Map), typeof(MapRenderer))]

namespace Xamarin
{
    public static class FormsMaps
	{
		public static bool IsInitialized { get; private set; }
        public static IMapCache Cache { get; private set; }

		public static void Init(Activity activity, Bundle bundle, IMapCache mapCache = null)
		{
			if (IsInitialized)
				return;
			IsInitialized = true;
            Cache = mapCache;

            MapRenderer.Bundle = bundle;

#pragma warning disable 618
			if (GooglePlayServicesUtil.IsGooglePlayServicesAvailable(activity) == ConnectionResult.Success)
#pragma warning restore 618
			{
				try
				{
					MapsInitializer.Initialize(activity);
                }
				catch (Exception e)
				{
					Console.WriteLine("Google Play Services Not Found");
					Console.WriteLine("Exception: {0}", e);
				}
			}

			new GeocoderBackend(activity).Register();
		}
	}
}