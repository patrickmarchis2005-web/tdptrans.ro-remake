using TdpTrans.Models;

namespace TdpTrans.Repositories.Interfaces
{
    public interface IObservationRepository
    {
        Task<UserObservation?> GetActiveByReason(int userId, string reason);
        Task<UserObservation> Add(UserObservation observation);
        Task SaveChanges();
        Task<IReadOnlyList<UserObservation>> GetActive();
    }
}
