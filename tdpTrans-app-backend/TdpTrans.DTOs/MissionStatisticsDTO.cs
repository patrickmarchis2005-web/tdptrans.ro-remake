using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TdpTrans.DTOs
{
    public class MonthlyStatisticDTO
    {
        public string Name { get; set; } = string.Empty;
        public int Towing { get; set; }
        public int Transport { get; set; }
    }

    public class MissionStatisticsDTO
    {
        public int TotalComenzi { get; set; }
        public int TotalTransportMarfa { get; set; }
        public int TotalTractari { get; set; }
        public List<MonthlyStatisticDTO> MonthlyData { get; set; } = new List<MonthlyStatisticDTO>();
    }
}
