using CharlieBackend.Core.DTO.Schedule;
using CharlieBackend.Core.Entities;
using Swashbuckle.AspNetCore.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CharlieBackend.Api.SwaggerExamples.SchedulesController
{
    public class PutScheduleUpdateSingleResponse : IExamplesProvider<EventOccurrenceDTO>
    {
        public EventOccurrenceDTO GetExamples()
        {
            return new EventOccurrenceDTO()
            {
                Id = 0,
                StudentGroupId= 1,
                EventStart = DateTime.Now,
                EventFinish = DateTime.Now,
                Pattern = PatternType.RelativeMonthly,
                Events = new List<ScheduledEventDTO> 
                {
                   new ScheduledEventDTO
                   {
                        Id = 1,
                        EventOccuranceId = 1,
                        StudentGroupId = 1,
                        ThemeId = 6,
                        MentorId = 5,
                        LessonId = null,
                        EventStart = DateTime.Now,
                        EventFinish = DateTime.Now
                   }
                },
                Storage = 30368744178210
            };
        }
    }
}
