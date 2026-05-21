using TdpTrans.DTOs;

namespace TdpTrans.Services
{
    public interface ISuspiciousActivityService
    {
        Task Evaluate(int userId);
        Task<IReadOnlyList<ObservationResponse>> GetActiveObservations();
    }
}
