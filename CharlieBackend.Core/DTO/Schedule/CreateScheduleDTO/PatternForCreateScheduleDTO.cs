using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using CharlieBackend.Core.Entities;

namespace CharlieBackend.Core.DTO.Schedule
{
    public class PatternForCreateScheduleDTO
    {
        [Required]
        public PatternType Type { get; set; }

        [Required]
        public int Interval { get; set; }

        public IList<DayOfWeek> DaysOfWeek { get; set; }

        public MonthIndex? Index { get; set; }

        public IList<int> Dates { get; set; }
    }
}
