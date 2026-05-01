using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TdpTrans.DTOs
{
    public record CreateMissionRequest(
        [Required]
        string MissionType,

        [Required]
        [Range(100000, 999999)]
        int TruckId,

        [Required]
        DateTime Date,

        [Required]
        [Range(0, double.MaxValue)]
        decimal Cost,

        [Required]
        [MinLength(3)]
        string Client,

        [Required]
        [Phone]
        string Phone,

        [Required]
        [MinLength(5)]
        string Address,

        [Required]
        [EmailAddress]
        string Email,

        [Required]
        string MissionStatus)
    {
    }
}
