using System.Collections.Generic;

namespace CharlieBackend.AdminPanel
{
    public class ApplicationSettings
    {
        public CorsSettings Cors { get; set; }

        public UrlsSettings Urls { get; set; }

        public CookiesSettings Cookies { get; set; }
    }

    public class UrlsSettings
    {
        public ApiSettings Api { get; set; }

        public ApiEndpoints ApiEndpoints { get; set; }
    }

    public class ApiSettings
    {
        public string Http { get; set; }

        public string Https { get; set; }
    }

    #region ApiEndpoints

    public class ApiEndpoints
    {
        public CoursesApiEndpoints Courses { get; set; }
        public StudentsApiEndpoints Students { get; set; }
        public MentorsApiEndpoints Mentors { get; set; }
        public ThemesApiEndpoints Themes { get; set; }
        public ScheduleApiEndpoints Schedule { get; set; }
        public StudentGroupsApiEndpoints StudentGroups { get; set; }
    }

    public class CoursesApiEndpoints
    {
        public string GetAllCoursesEndpoint { get; set; }
        public string AddCourseEndpoint { get; set; }
        public string UpdateCourseEndpoint { get; set; }
        public string DisableCourseEndpoint { get; set; }
        public string EnableCourseEndpoint { get; set; }
    }

    public class StudentsApiEndpoints
    {
        public string ActiveStudentEndpoint { get; set; }
        public string GetAllStudentsEndpoint { get; set; }
        public string AddStudentEndpoint { get; set; }
        public string GetStudentEndpoint { get; set; }
        public string UpdateStudentEndpoint { get; set; }
        public string DisableStudentEndpoint { get; set; }
        public string EnableStudentEndpoint { get; set; }
    }

    public class MentorsApiEndpoints
    {
        public string ActiveMentorEndpoint { get; set; }
        public string GetAllMentorsEndpoint { get; set; }
        public string AddMentorEndpoint { get; set; }
        public string GetMentorEndpoint { get; set; }
        public string UpdateMentorEndpoint { get; set; }
        public string DisableMentorEndpoint { get; set; }
        public string EnableMentorEndpoint { get; set; }
    }

    public class ThemesApiEndpoints
    {
        public string ActiveThemeEndpoint { get; set; }
        public string GetAllThemesEndpoint { get; set; }
        public string AddThemeEndpoint { get; set; }
        public string GetThemeEndpoint { get; set; }
        public string UpdateThemeEndpoint { get; set; }
        public string DeleteThemeEndpoint { get; set; }
    }

    public class ScheduleApiEndpoints
    {
        public string EventOccurrencesEndpoint { get; set; }
        public string EventsEndpoint { get; set; }
        public string EventOccurrenceById { get; set; }
        public string AddEventOccurrence { get; set; }
        public string DeleteScheduleEndpoint { get; set; }
        public string UpdateScheduleEndpoint { get; set; }
    }

    public class StudentGroupsApiEndpoints
    {
        public string GetAllStudentGroupsEndpoint { get; set; }
        public string AddStudentGroupEndpoint { get; set; }
        public string UpdateStudentGroupEndpoint { get; set; }
        public string GetStudentGroupEndpoint { get; set; }
    }

    #endregion 

    public class CookiesSettings
    {
        public string SecureKey { get; set; }
    }

    public class CorsSettings
    {
        public CorsSettings()
        {
            AllowedOrigins = new List<string>();
        }

        public List<string> AllowedOrigins { get; set; }
    }

}
