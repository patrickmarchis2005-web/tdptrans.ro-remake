using System;
using System.Collections.Generic;

namespace TdpTrans.Models
{
    public class Mission
    {
        public int Id { get; set; }
        public MissionType Type { get; set; }
        public MissionStatus Status { get; set; }
        public DateTime Date { get; set; }
        public decimal Cost { get; set; }
        public string Address { get; set; }

        // --- RELATIA CU CLIENTUL ---
        public int ClientId { get; set; }
        public Client Client { get; set; }

        // --- RELATIA CU CAMIONUL ---
        public int TruckId { get; set; }
        public Truck Truck { get; set; }
    }
}