using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TdpTrans.Models;

namespace TdpTrans.Repositories.Interfaces
{
    public interface ITrucksRepository
    {
        Task<Truck?> GetTruckById(int truckId);
    }
}
