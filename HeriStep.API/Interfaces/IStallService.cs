using System.Collections.Generic;
using System.Threading.Tasks;
using HeriStep.Shared.Models;
using HeriStep.Shared.Models.DTOs.Responses;

namespace HeriStep.API.Interfaces
{
    public interface IStallService
    {
        Task<IEnumerable<PointOfInterest>> GetAllStallsAsync();
        Task<bool> ExtendSubscriptionAsync(int stallId);
    }
}
