using System.Collections.Generic;

namespace TdpTrans.Models
{
    public class Client
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }

        public ICollection<Mission> Missions { get; set; }
    }
}