using CharlieBackend.Core.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CharlieBackend.AdminPanel.Models.Schedules
{
    public class SchedulesViewModel
    {
        public long StudentGroupId { get; set; }

        public DateTime EventStart { get; set; }

        public DateTime EventFinish { get; set; }

        [DataType(DataType.Time)]
        [EnumDataType(typeof(PatternType))]
        public PatternType Pattern { get; set; }
    }
}
