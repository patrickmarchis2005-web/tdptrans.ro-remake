using Microsoft.EntityFrameworkCore;
using TdpTrans.Models;
using TdpTrans.Repositories.Interfaces;

namespace TdpTrans.Repositories.Implementations
{
    public class ObservationRepository : IObservationRepository
    {
        private readonly ApplicationDbContext _context;

        public ObservationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserObservation> Add(UserObservation observation)
        {
            _context.UserObservations.Add(observation);
            await _context.SaveChangesAsync();
            return observation;
        }

        public async Task<IReadOnlyList<UserObservation>> GetActive()
        {
            return await _context.UserObservations
                .Include(observation => observation.User)
                    .ThenInclude(user => user.UserRoles)
                        .ThenInclude(userRole => userRole.Role)
                .Where(observation => observation.IsActive)
                .OrderByDescending(observation => observation.RiskScore)
                .ThenByDescending(observation => observation.LastDetectedAtUtc)
                .ToListAsync();
        }

        public async Task<UserObservation?> GetActiveByReason(int userId, string reason)
        {
            return await _context.UserObservations
                .FirstOrDefaultAsync(observation =>
                    observation.UserId == userId &&
                    observation.Reason == reason &&
                    observation.IsActive);
        }

        public async Task SaveChanges()
        {
            await _context.SaveChangesAsync();
        }
    }
}
