using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TdpTrans.DTOs
{
    public record MissionResponse(
        int Id,
        string MissionType,
        int TruckId,
        DateTime Date,
        decimal Cost,
        string Client,
        string Phone,
        string Address,
        string Email,
        string MissionStatus
    )
    {
    }
}
