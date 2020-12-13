using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using CharlieBackend.Core.DTO.Student;

namespace CharlieBackend.Core.IntegrationEvents.Events
{
    //immutable
    public class CourseOpenedEvent
    {

        [JsonConstructor]
        public CourseOpenedEvent(DateTime startDate,
                                    string courseName,
                                    List<StudentDto> students)
        {
            StartDate = startDate;
            CourseName = courseName;
            Students = students;
        }
        
        public DateTime StartDate { get; private set; }

        public string CourseName { get; private set; }

        public List<StudentDto> Students { get; private set; }
    }
}
