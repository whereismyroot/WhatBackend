using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace CharlieBackend.Core.DTO.Schedule.CreateScheduleDTO
{
    public class CreateScheduledEventDto
    {
        [Required]
        public OccurenceRange Range { get; set; }

        [Required]
        public ContextForCreateScheduleDTO Context { get; set; }
    }
}
