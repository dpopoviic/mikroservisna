using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPlatformAPI.DTO
{
    public class PublishEventResultDto
    {
        public Guid CorrelationId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
