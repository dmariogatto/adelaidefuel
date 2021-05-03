using AdelaideFuel.Models;
using System.Collections.Generic;

namespace AdelaideFuel.Services
{
    public interface ITechnologyService
    {
        IList<Technology> GetTechnologies();
    }
}