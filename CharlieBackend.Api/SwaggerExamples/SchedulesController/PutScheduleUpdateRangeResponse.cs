using CharlieBackend.Core.DTO.Schedule;
using CharlieBackend.Core.Entities;
using Swashbuckle.AspNetCore.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CharlieBackend.Api.SwaggerExamples.SchedulesController
{
    internal class PutScheduleUpdateRangeResponse : IExamplesProvider<List<ScheduledEventDTO>>
    {
        public List<ScheduledEventDTO> GetExamples()
        {
            return new List<ScheduledEventDTO>()
            {
                new ScheduledEventDTO
                {
                    Id = 0,
                    EventOccuranceId = 0,
                    StudentGroupId = 0,
                    ThemeId = 0,
                    MentorId = 0,
                    LessonId = null,
                    EventStart = DateTime.Now,
                    EventFinish = DateTime.Now 
                },
                new ScheduledEventDTO
                {
                    Id = 1,
                    EventOccuranceId = 0,
                    StudentGroupId = 0,
                    ThemeId = 0,
                    MentorId = 0,
                    LessonId = null,
                    EventStart = DateTime.Now,
                    EventFinish = DateTime.Now
                }
            };
        }
    }
}
