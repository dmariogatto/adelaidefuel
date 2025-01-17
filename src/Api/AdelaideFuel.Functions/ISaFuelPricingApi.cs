using Refit;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.Api
{
    [Headers("Content-Type: application/json; charset=UTF-8")]
    public interface ISaFuelPricingApi
    {
        [Get("/Version")]
        Task<string> GetVerionAsync(CancellationToken cancellationToken);

        [Get("/Price/GetSitesPrices?countryId={countryId}&geoRegionLevel={geoRegionLevel}&geoRegionId={geoRegionId}")]
        Task<SitePricesRoot> GetPricesAsync(CancellationToken cancellationToken, int countryId = 21, int geoRegionLevel = 3, int geoRegionId = 4);

        [Get("/Subscriber/GetCountryFuelTypes?countryId={countryId}")]
        Task<FuelsRoot> GetFuelTypesAsync(CancellationToken cancellationToken, int countryId = 21);

        [Get("/Subscriber/GetCountryGeographicRegions?countryId={countryId}")]
        Task<GeographicRegionsRoot> GetCountryGeographicRegionsAsync(CancellationToken cancellationToken, int countryId = 21);

        [Get("/Subscriber/GetCountryBrands?countryId={countryId}")]
        Task<BrandsRoot> GetBrandsAsync(CancellationToken cancellationToken, int countryId = 21);

        [Get("/Subscriber/GetFullSiteDetails?countryId={countryId}&geoRegionLevel={geoRegionLevel}&geoRegionId={geoRegionId}")]
        Task<SitesRoot> GetFullSiteDetailsAsync(CancellationToken cancellationToken, int countryId = 21, int geoRegionLevel = 3, int geoRegionId = 4);
    }
}