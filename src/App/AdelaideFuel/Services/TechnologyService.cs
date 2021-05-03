using AdelaideFuel.Models;
using System.Collections.Generic;

namespace AdelaideFuel.Services
{
    public class TechnologyService : BaseService, ITechnologyService
    {
        private readonly IList<Technology> _technologies = new []
        {
            new Technology("AiForms.SettingsView",
                "1.3.18",
                "https://github.com/muak/AiForms.SettingsView"),
            new Technology("LiteDB",
                "5.0.10",
                "https://github.com/mbdavid/LiteDB"),
            new Technology("Polly",
                "7.2.1",
                "https://github.com/App-vNext/Polly"),
            new Technology("Refit",
                "5.2.4",
                "https://github.com/reactiveui/refit"),
            new Technology("SimpleInjector",
                "5.3.0",
                "https://github.com/simpleinjector/SimpleInjector"),
            new Technology("Xamarin.Forms",
                "5.0.0.2012",
                "https://github.com/xamarin/Xamarin.Forms"),
            new Technology("Xamarin.Forms.PancakeView",
                "2.3.0.759",
                "https://github.com/sthewissen/Xamarin.Forms.PancakeView"),
        };

        public TechnologyService(
            ICacheService cacheService,
            ILogger logger) : base(cacheService, logger)
        { }

        public IList<Technology> GetTechnologies() => _technologies;
    }
}