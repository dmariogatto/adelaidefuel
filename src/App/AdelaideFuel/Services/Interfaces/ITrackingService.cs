using System;
using System.Threading.Tasks;

namespace AdelaideFuel.Services;

public interface ITrackingService
{
    Task<Guid> GetIdfaAsync();
}