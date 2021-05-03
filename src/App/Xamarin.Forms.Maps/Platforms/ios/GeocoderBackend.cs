using AddressBookUI;
using CoreLocation;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CCLGeocoder = CoreLocation.CLGeocoder;

namespace Xamarin.Forms.Maps.iOS
{
    internal class GeocoderBackend
	{
		public static void Register()
		{
			Geocoder.GetPositionsForAddressAsyncFunc = GetPositionsForAddressAsync;
			Geocoder.GetAddressesForPositionFuncAsync = GetAddressesForPositionAsync;
		}

		static Task<IEnumerable<string>> GetAddressesForPositionAsync(Position position)
		{
			var location = new CLLocation(position.Latitude, position.Longitude);
			var geocoder = new CCLGeocoder();
			var source = new TaskCompletionSource<IEnumerable<string>>();
			geocoder.ReverseGeocodeLocation(location, (placemarks, error) =>
			{
				if (placemarks == null)
					placemarks = new CLPlacemark[0];
				List<string> addresses = new List<string>();
				addresses = placemarks.Select(p => ABAddressFormatting.ToString(p.AddressDictionary, false)).ToList();

				source.SetResult(addresses);

			});
			return source.Task;
		}

		static Task<IEnumerable<Position>> GetPositionsForAddressAsync(string address)
		{
			var geocoder = new CCLGeocoder();
			var source = new TaskCompletionSource<IEnumerable<Position>>();
			geocoder.GeocodeAddress(address, (placemarks, error) =>
			{
				if (placemarks == null)
					placemarks = new CLPlacemark[0];
				IEnumerable<Position> positions = placemarks.Select(p => new Position(p.Location.Coordinate.Latitude, p.Location.Coordinate.Longitude));
				source.SetResult(positions);
			});
			return source.Task;
		}
	}
}