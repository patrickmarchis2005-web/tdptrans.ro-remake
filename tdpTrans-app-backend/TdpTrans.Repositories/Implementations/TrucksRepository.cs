using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TdpTrans.Models;
using TdpTrans.Repositories.Interfaces;

namespace TdpTrans.Repositories.Implementations
{
    public class TrucksRepository : ITrucksRepository
    {
        private readonly ApplicationDbContext _context;

        public TrucksRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Truck?> GetTruckById(int id)
        {
            return await _context.Trucks.FirstOrDefaultAsync(t => t.Id == id);
        }
    }
}