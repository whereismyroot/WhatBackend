using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CharlieBackend.Core;
using CharlieBackend.Core.DTO.Mentor;
using CharlieBackend.Core.Entities;
using CharlieBackend.Core.Models.ResultModel;
using CharlieBackend.Data.Repositories.Impl.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CharlieBackend.Data.Repositories.Impl
{
    public class CourseRepository : Repository<Course>, ICourseRepository
    {
        public CourseRepository(ApplicationContext applicationContext) 
                : base(applicationContext) 
        {
        }

        public async Task<bool> IsCourseNameTakenAsync(string courseName)
        {
            return await _applicationContext.Courses .AnyAsync(course => course.Name == courseName);
        }

        public async Task<List<Course>> GetCoursesByIdsAsync(List<long> courseIds)
        {
            return await _applicationContext.Courses
                    .Where(course => courseIds
                    .Contains(course.Id))
                    .ToListAsync();
        }

        public async Task<bool> IsCourseHasGroupAsync(long id)
        {
            return await _applicationContext.StudentGroups.AnyAsync(s => s.CourseId == id);
        }

        public async Task<IList<Course>> GetCoursesAsync(bool? isActive)
        {
            return await _applicationContext.Courses
                .WhereIf(isActive != null, x => x.IsActive == isActive)
                .ToListAsync();
        }

        public async Task<Result<bool>> DisableCourseByIdAsync(long id)
        {
            var course = await _applicationContext.Courses.FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            if (course == null)
            {
                return Result<bool>.GetError(ErrorCode.NotFound, "Course is not found");
            }
            course.IsActive = false;

            return Result<bool>.GetSuccess(true);
        }

        public async Task<Result<bool>> EnableCourseByIdAsync(long id)
        {
            var course = await _applicationContext.Courses.FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
            {
                return Result<bool>.GetError(ErrorCode.NotFound, "Course is not found");
            }
            
            course.IsActive = true;

            return Result<bool>.GetSuccess(true);
        }

        public async Task<List<MentorCoursesDto>> GetMentorCourses(long id)
        {
            return await _applicationContext.Courses
                    .Where(x =>x.MentorsOfCourses.Any(x => x.MentorId == id))
                    .Select(x => new MentorCoursesDto
                    {
                        Id = x.Id,
                        Name = x.Name
                    }).ToListAsync();
        }

        /// <summary>
        /// This method return true if course is active, return false if course doesn't active or doesn't exist.
        /// </summary>
        public async Task<bool> IsCourseActive(long id)
        {
            var course = await _applicationContext.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

            if (course is null)
            {
                return false;
            }

            return course.IsActive;
        }
    }
}
