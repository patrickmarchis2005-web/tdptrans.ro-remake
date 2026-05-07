using System.Collections.Generic;

namespace TdpTrans.Models
{
    public class Truck
    {
        public int Id { get; set; }
        public string LicensePlate { get; set; }

        public ICollection<Mission> Missions { get; set; }
    }
}