using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPlatformAPI.DTO
{
    public class PublishEventRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string Agenda { get; set; } = string.Empty;
        public DateTime DateTime { get; set; }
        public decimal DurationInHours { get; set; }
        public decimal Price { get; set; }
        public int TypeId { get; set; }
        public int LocationId { get; set; }
        public List<int> LecturerIds { get; set; } = [];
        public string OrganizerEmail { get; set; } = string.Empty;
    }
}
