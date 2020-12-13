using CharlieBackend.Business.Services.Interfaces;
using CharlieBackend.Core;
using CharlieBackend.Core.Entities;
using CharlieBackend.Core.DTO.StudentGroups;
using CharlieBackend.Data.Repositories.Impl.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CharlieBackend.Core.Models.ResultModel;
using Microsoft.Extensions.Logging;

namespace CharlieBackend.Business.Services
{
    public class StudentGroupService : IStudentGroupService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<StudentGroupService> _logger;

        public StudentGroupService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<StudentGroupService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<StudentGroupDto>> CreateStudentGroupAsync(CreateStudentGroupDto studentGroupDto)
        {
            try
            {
                if (studentGroupDto == null)
                {
                    return Result<StudentGroupDto>.GetError(ErrorCode.ValidationError, "StudentGroupDto is null");
                }

                if (await _unitOfWork.StudentGroupRepository.IsGroupNameExistAsync(studentGroupDto.Name))
                {
                    return Result<StudentGroupDto>.GetError(ErrorCode.UnprocessableEntity, "Group name already exists");
                }

                var studentGroup = new StudentGroup
                {
                    Name = studentGroupDto.Name,
                    CourseId = studentGroupDto.CourseId,
                    StartDate = studentGroupDto.StartDate,
                    FinishDate = studentGroupDto.FinishDate,
                };


                _unitOfWork.StudentGroupRepository.Add(studentGroup);

                if (studentGroupDto?.StudentIds.Count != 0)
                {
                    var students = await _unitOfWork.StudentRepository.GetStudentsByIdsAsync(studentGroupDto.StudentIds);
                    studentGroup.StudentsOfStudentGroups = new List<StudentOfStudentGroup>();

                    for (int i = 0; i < students.Count; i++)
                    {
                        studentGroup.StudentsOfStudentGroups.Add(new StudentOfStudentGroup
                        {
                            StudentId = students[i].Id,
                            Student = students[i]
                        });
                    }
                }

                if (studentGroupDto?.MentorIds.Count != 0)
                {
                    var mentors = await _unitOfWork.MentorRepository.GetMentorsByIdsAsync(studentGroupDto.MentorIds);
                    studentGroup.MentorsOfStudentGroups = new List<MentorOfStudentGroup>();

                    for (int i = 0; i < mentors.Count; i++)
                    {
                        studentGroup.MentorsOfStudentGroups.Add(new MentorOfStudentGroup
                        {
                            MentorId = mentors[i].Id,
                            Mentor = mentors[i]
                        }); ;
                    }
                }

                await _unitOfWork.CommitAsync();

                return Result<StudentGroupDto>.GetSuccess(_mapper.Map<StudentGroupDto>(studentGroup));

            }
            catch(Exception ex)
            {
                _unitOfWork.Rollback();

                Console.WriteLine(ex.Message);

                return Result<StudentGroupDto>.GetError(ErrorCode.InternalServerError, "Internal error");
            }
        }

        public async Task<Result<bool>> IsGroupNameExistAsync(string name)
        {
            if (name == null)
            {
                return Result<bool>.GetError(ErrorCode.ValidationError, "Name is null");
            }

            var res = await _unitOfWork.StudentGroupRepository.IsGroupNameExistAsync(name);

            return Result<bool>.GetSuccess(res);
        }

        public async Task<IList<StudentGroupDto>> GetAllStudentGroupsAsync()
        {
            var studentGroup = await _unitOfWork.StudentGroupRepository.GetAllAsync();

            return _mapper.Map<List<StudentGroupDto>>(studentGroup);
        }

        public bool DeleteStudentGrop(long StudentGroupId)
        {
            return _unitOfWork.StudentGroupRepository.DeleteStudentGroup(StudentGroupId);
        }

        // if we set StudentIds or MentorsIds to null, they won't update
        public async Task<Result<StudentGroupDto>> UpdateStudentGroupAsync(long groupId, UpdateStudentGroupDto updatedStudentGroupDto)
        {
            try
            {
                if (updatedStudentGroupDto == null)
                {
                    return Result<StudentGroupDto>.GetError(ErrorCode.ValidationError, "UpdateStudentGroupDto is null");
                }

                var foundStudentGroup = await _unitOfWork.StudentGroupRepository.GetByIdAsync(groupId);

                if (foundStudentGroup == null)
                {
                    return Result<StudentGroupDto>.GetError(ErrorCode.NotFound, "Student Group not found");
                }

                if (await _unitOfWork.StudentGroupRepository.IsGroupNameExistAsync(updatedStudentGroupDto.Name))
                {
                    return Result<StudentGroupDto>.GetError(ErrorCode.UnprocessableEntity, "Group name already exists");
                }

                foundStudentGroup.Name = updatedStudentGroupDto.Name ?? foundStudentGroup.Name;

                if (updatedStudentGroupDto.StartDate != null)
                {
                    foundStudentGroup.StartDate = (DateTime?)(updatedStudentGroupDto.StartDate) ?? foundStudentGroup.StartDate;
                }

                if (updatedStudentGroupDto.FinishDate != null)
                {
                    foundStudentGroup.FinishDate = (DateTime?)(updatedStudentGroupDto.FinishDate) ?? foundStudentGroup.FinishDate;
                }

                if (updatedStudentGroupDto.CourseId != 0)
                {
                    foundStudentGroup.Course = await _unitOfWork.CourseRepository.GetByIdAsync(updatedStudentGroupDto.CourseId);
                }


                if (updatedStudentGroupDto.StudentIds != null)
                {
                    var newStudentsOfStudentGroup = updatedStudentGroupDto.StudentIds.Select(x => new StudentOfStudentGroup
                    {
                        StudentGroupId = foundStudentGroup.Id,
                        StudentId = x
                    }).ToList();

                    _unitOfWork.StudentGroupRepository.UpdateManyToMany(foundStudentGroup.StudentsOfStudentGroups, newStudentsOfStudentGroup);
                }

                if (updatedStudentGroupDto.MentorIds != null)
                {
                    var newMentorsOfStudentGroup = updatedStudentGroupDto.MentorIds.Select(x => new MentorOfStudentGroup
                    {
                        StudentGroupId = foundStudentGroup.Id,
                        MentorId = x
                    }).ToList();

                    _unitOfWork.MentorRepository.UpdateMentorGroups(foundStudentGroup.MentorsOfStudentGroups, newMentorsOfStudentGroup);
                }

                await _unitOfWork.CommitAsync();

                return Result<StudentGroupDto>.GetSuccess(_mapper.Map<StudentGroupDto>(foundStudentGroup));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);

                _unitOfWork.Rollback();

                return Result<StudentGroupDto>.GetError(ErrorCode.InternalServerError, "Internal error");
            }
        }

        public async Task<Result<StudentGroupDto>> GetStudentGroupByIdAsync(long id)
        {
            var foundStudentGroup = await _unitOfWork.StudentGroupRepository.GetByIdAsync(id);

            if (foundStudentGroup == null)
            {
                return Result<StudentGroupDto>.GetError(ErrorCode.NotFound, "Student group not found");
            }

            return Result<StudentGroupDto>.GetSuccess(_mapper.Map<StudentGroupDto>(foundStudentGroup));
        }

        public void AddStudentOfStudentGroups(IEnumerable<StudentOfStudentGroup> items)
        {
            _unitOfWork.StudentGroupRepository.AddStudentOfStudentGroups(items);
        }
    }
}
