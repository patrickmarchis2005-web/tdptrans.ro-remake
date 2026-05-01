using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TdpTrans.DTOs
{
    public class PaginatedResult
    {
        public List<MissionResponse> Items { get; set; } = new List<MissionResponse>();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int) Math.Ceiling((double) TotalCount / PageSize);
    }
}
