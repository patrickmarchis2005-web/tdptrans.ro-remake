using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TdpTrans.DTOs;
using TdpTrans.Models;

namespace TdpTrans.Services
{
    public static class Mapper
    {
        public static MissionResponse FromMissionToDTO(Mission mission)
        {
            return new MissionResponse
            (
                mission.Id,
                mission.Type.ToString(),
                mission.TruckId,
                mission.Date,
                mission.Cost,
                mission.Client.Name,
                mission.Client.Phone,
                mission.Address,
                mission.Client.Email,
                mission.Status.ToString()
            );
        }
    }
}
