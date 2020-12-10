using CharlieBackend.Core.DTO.Student;
using CharlieBackend.Core.Entities;
using CharlieBackend.Data.Helpers;
using CharlieBackend.Data.Repositories.Impl.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CharlieBackend.Data.Repositories.Impl
{
    public class StudentGroupRepository : Repository<StudentGroup>, IStudentGroupRepository
    {
        public StudentGroupRepository(ApplicationContext applicationContext) 
            : base(applicationContext) 
        {
        }

        public void AddStudentOfStudentGroups(IEnumerable<StudentOfStudentGroup> items)
        {
            _applicationContext.StudentsOfStudentGroups
                   .AddRange(items);
        }
        public bool DeleteStudentGroup(long StudentGroupModelId)
        {
            var x = SearchStudentGroup(StudentGroupModelId);

            if (x == null)
            {
                return false;
            }    
                
            else
            {
                _applicationContext.StudentGroups.Remove(x);
                _applicationContext.SaveChanges();

                return true;
            }
        }

        public new Task<List<StudentGroup>> GetAllAsync()
        {
            return _applicationContext.StudentGroups
                    .Include(group => group.StudentsOfStudentGroups)
                    .Include(group => group.MentorsOfStudentGroups).ToListAsync();
        }

        public async Task<List<StudentStudyGroupsDto>> GetStudentStudyGroups(long id)
        {
            return await _applicationContext.StudentGroups
                    .Include(group => group.StudentsOfStudentGroups)
                    .Where(x => x.StudentsOfStudentGroups.Any(x => x.StudentId == id))
                    .Select(x => new StudentStudyGroupsDto 
                    { 
                        Id = x.Id,
                        Name = x.Name
                    }).ToListAsync();
        }

        public async Task<bool> IsGroupNameExistAsync(string name)
        {
            return await _applicationContext.StudentGroups.AnyAsync(x => x.Name == name);
        }

        public StudentGroup SearchStudentGroup(long studentGroupId)
        {
            return _applicationContext.StudentGroups
                   .Where(group => group.Id == studentGroupId)
                   .FirstOrDefault();
        }

        public void UpdateManyToMany(IEnumerable<StudentOfStudentGroup> currentStudentsOfStudentGroup, 
                                     IEnumerable<StudentOfStudentGroup> newStudentsOfStudentGroup)
        {
            _applicationContext.StudentsOfStudentGroups.
                    TryUpdateManyToMany(currentStudentsOfStudentGroup, newStudentsOfStudentGroup);
        }

        public new Task<StudentGroup> GetByIdAsync(long id)
        {
            return _applicationContext.StudentGroups
                    .Include(group => group.StudentsOfStudentGroups)
                    .Include(group => group.MentorsOfStudentGroups)
                    .FirstOrDefaultAsync(group => group.Id == id);
        }
    }
}
