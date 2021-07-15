using CharlieBackend.AdminPanel.Models.StudentGroups;
using CharlieBackend.AdminPanel.Models.Students;
using CharlieBackend.AdminPanel.Services.Interfaces;
using CharlieBackend.AdminPanel.Utils.Interfaces;
using CharlieBackend.Core.DTO.Student;
using CharlieBackend.Core.Models.ResultModel;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CharlieBackend.AdminPanel.Services
{
    public class StudentService: IStudentService
    {
        private readonly IApiUtil _apiUtil;
        private readonly StudentsApiEndpoints _studentsApiEndpoints;
        private readonly StudentGroupsApiEndpoints _studentGroupsApiEndpoints;

        public StudentService(IApiUtil apiUtil, IOptions<ApplicationSettings> options)
        {
            _apiUtil = apiUtil;
            _studentsApiEndpoints = options.Value.Urls.ApiEndpoints.Students;
            _studentGroupsApiEndpoints = options.Value.Urls.ApiEndpoints.StudentGroups;
        }

        public async Task<IList<StudentViewModel>> GetAllStudentsAsync()
        {
            var getAllStudentsEndpoints = _studentsApiEndpoints.GetAllStudentsEndpoint;
            var activeStudentsEndpoints = _studentsApiEndpoints.ActiveStudentEndpoint;

            var allStudentsTask =  _apiUtil.GetAsync<IList<StudentViewModel>>(getAllStudentsEndpoints);
            var activeStudentsTask = _apiUtil.GetAsync<IList<StudentViewModel>>(activeStudentsEndpoints);

            var allStudents = await allStudentsTask;
            var activeStudents = await activeStudentsTask;

            foreach (var student in allStudents)
            {
                student.IsActive = activeStudents.Any(x => x.Id == student.Id);
            }

            return allStudents;
        }

        public async Task<StudentEditViewModel> GetStudentByIdAsync(long id)
        {
            var getStudentEndpoint = string
                .Format(_studentsApiEndpoints.GetStudentEndpoint, id);
            var getAllStudentGroupsEndpoint = _studentGroupsApiEndpoints.GetAllStudentGroupsEndpoint;

            var studentTask =  _apiUtil.GetAsync<StudentEditViewModel>(getStudentEndpoint);
            var studentGroupsTask = _apiUtil.GetAsync<IList<StudentGroupViewModel>>(getAllStudentGroupsEndpoint);

            var student = await studentTask;
            var studentGroup = await studentGroupsTask;

            student.AllGroups = studentGroup;

            return student;
        }

        public async Task<UpdateStudentDto> UpdateStudentAsync(long id, UpdateStudentDto UpdateDto)
        {
            var updateStudentEndpoint = string
                .Format(_studentsApiEndpoints.UpdateStudentEndpoint, id);

            var updatedStudent = await _apiUtil.PutAsync(updateStudentEndpoint, UpdateDto);
            
            return updatedStudent;
        }

        public async Task<StudentDto> AddStudentAsync(long id)
        {
            var addStudentEndpoint = string
                .Format(_studentsApiEndpoints.AddStudentEndpoint, id);

            var addedStudentTask = await _apiUtil.CreateAsync<StudentDto>(addStudentEndpoint, null);

            return addedStudentTask;
        }

        public async Task<bool> DisableStudentAsync(long id)
        {
            var disableStudentsEndpoints = string
                .Format(_studentsApiEndpoints.DisableStudentEndpoint, id);

            return await _apiUtil.DeleteAsync<bool>(disableStudentsEndpoints);
        }

        public async Task<bool> EnableStudentAsync(long id)
        {
            var enableStudentsEndpoints = string
                .Format(_studentsApiEndpoints.EnableStudentEndpoint, id);

            return await _apiUtil.EnableAsync<bool>(enableStudentsEndpoints);
        }
    }

}
