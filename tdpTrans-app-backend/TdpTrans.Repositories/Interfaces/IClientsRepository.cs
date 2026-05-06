using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TdpTrans.Models;

namespace TdpTrans.Repositories.Interfaces
{
    public interface IClientsRepository
    {
        Task<Client> AddClient(Client client);
        Task<Client?> GetClientByEmail(string email);
    }
}
