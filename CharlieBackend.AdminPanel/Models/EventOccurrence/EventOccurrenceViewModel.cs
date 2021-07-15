using System;

namespace CharlieBackend.AdminPanel.Models.EventOccurrence
{
    public class EventOccurrenceViewModel
    {
        public long? Id { get; set; }

        public long StudentGroupId { get; set; }

        public DateTime? EventStart { get; set; }

        public DateTime? EventFinish { get; set; }

        public long Storage { get; set; }
    }
}
