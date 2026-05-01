using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TdpTrans.DTOs
{
    public record UpdateMissionRequest(
        string? MissionType,

        [Range(100000, 999999)]
        int? TruckId,

        DateTime? Date,

        [Range(0, double.MaxValue)]
        decimal? Cost,

        [MinLength(3)]
        string? Client,

        [Phone]
        string? Phone,

        [MinLength(5)]
        string? Address,

        [EmailAddress]
        string? Email,

        string? MissionStatus)
    {
    }
}
