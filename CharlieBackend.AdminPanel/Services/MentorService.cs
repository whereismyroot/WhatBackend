using CharlieBackend.AdminPanel.Models.Course;
using CharlieBackend.AdminPanel.Models.Mentor;
using CharlieBackend.AdminPanel.Models.StudentGroups;
using CharlieBackend.AdminPanel.Services.Interfaces;
using CharlieBackend.AdminPanel.Utils.Interfaces;
using CharlieBackend.Core.DTO.Mentor;
using CharlieBackend.Core.Models.ResultModel;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CharlieBackend.AdminPanel.Services
{
    public class MentorService : IMentorService
    {
        private readonly IApiUtil _apiUtil;
        private readonly MentorsApiEndpoints _mentorsApiEndpoints;
        private readonly CoursesApiEndpoints _coursesApiEndpoints;
        private readonly StudentGroupsApiEndpoints _studentGroupsApiEndpoints;

        public MentorService(IApiUtil apiUtil, IOptions<ApplicationSettings> options)
        {
            _apiUtil = apiUtil;

            _mentorsApiEndpoints = options.Value.Urls.ApiEndpoints.Mentors;
            _coursesApiEndpoints = options.Value.Urls.ApiEndpoints.Courses;
            _studentGroupsApiEndpoints = options.Value.Urls.ApiEndpoints.StudentGroups;
        }

        public async Task<MentorDto> AddMentorAsync(long id)
        {
            var addMentorEndpoint = string
                .Format(_mentorsApiEndpoints.AddMentorEndpoint, id);

            return await
                _apiUtil.CreateAsync<MentorDto>(addMentorEndpoint, null);
        }

        public async Task<bool> DisableMentorAsync(long id)
        {
            var disableMentorEndpoint = string
                .Format(_mentorsApiEndpoints.DisableMentorEndpoint, id);

            return await _apiUtil.DeleteAsync<bool>(disableMentorEndpoint);
        }

        public async Task<bool> EnableMentorAsync(long id)
        {
            var enableMentorEndpoint = string
                .Format(_mentorsApiEndpoints.EnableMentorEndpoint, id);

            return await _apiUtil.EnableAsync<bool>(enableMentorEndpoint);
        }

        public async Task<IList<MentorViewModel>> GetAllMentorsAsync()
        {
            var getAllMentorsEndpoint = _mentorsApiEndpoints.GetAllMentorsEndpoint;
            var activeMentorEndpoint = _mentorsApiEndpoints.ActiveMentorEndpoint;

            var allMentors = await 
                _apiUtil.GetAsync<IList<MentorViewModel>>(getAllMentorsEndpoint);
            var activeMentors = await 
                _apiUtil.GetAsync<IList<MentorViewModel>>(activeMentorEndpoint);

            foreach (var mentor in allMentors)
            {
                mentor.IsActive = activeMentors.Any(x => x.Id == mentor.Id);
            }

            return allMentors;
        }

        public async Task<MentorEditViewModel> GetMentorByIdAsync(long id)
        {
            var getAllCoursesEndpoint = _coursesApiEndpoints.GetAllCoursesEndpoint;
            var getAllStudentGroupsEndpoint = _studentGroupsApiEndpoints.GetAllStudentGroupsEndpoint;
            var getMentorEndpoint = string
                .Format(_mentorsApiEndpoints.GetMentorEndpoint, id);

            var coursesTask = _apiUtil.GetAsync<IList<CourseViewModel>>(getAllCoursesEndpoint);
            var studentGroupTask = _apiUtil.GetAsync<IList<StudentGroupViewModel>>(getAllStudentGroupsEndpoint);
            var mentor = await _apiUtil.GetAsync<MentorEditViewModel>(getMentorEndpoint);

            mentor.AllGroups = await studentGroupTask;
            mentor.AllCourses = await coursesTask;

            return mentor;
        }

        public async Task<UpdateMentorDto> UpdateMentorAsync(long id, UpdateMentorDto UpdateDto)
        {
            var updateMentorEndpoint = string
               .Format(_mentorsApiEndpoints.UpdateMentorEndpoint, id);

            return await 
                _apiUtil.PutAsync(updateMentorEndpoint, UpdateDto);
        }
    }
}