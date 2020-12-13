using System;
using System.IO;
using AutoMapper;
using ClosedXML.Excel;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using CharlieBackend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using CharlieBackend.Core.FileModels;
using CharlieBackend.Core.Models.ResultModel;
using CharlieBackend.Business.Services.Interfaces;
using CharlieBackend.Data.Repositories.Impl.Interfaces;

namespace CharlieBackend.Business.Services
{
    public class StudentGroupImportService : IStudentGroupImportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;

        public StudentGroupImportService(IUnitOfWork unitOfWork, IMapper mapper, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _notificationService = notificationService;
        }

        public async Task<Result<List<StudentGroupFile>>> ImportFileAsync(IFormFile uploadedFile)
        {
            string path = "";
            List<StudentGroupFile> importedGroups = new List<StudentGroupFile>();

            if (uploadedFile != null)
            {
                path = await CreateFile(uploadedFile);
                var book = new XLWorkbook(path);
                var groupsSheet = book.Worksheet("Groups");

                Type studentGroupType = typeof(StudentGroupFile);
                char charPointer = 'A';
                int rowCounter = 2;

                var properties = studentGroupType.GetProperties();
                foreach (PropertyInfo property in properties)
                {
                    if (property.Name != Convert.ToString(groupsSheet.Cell($"{charPointer}1").Value))
                    {
                        return Result<List<StudentGroupFile>>.GetError(ErrorCode.ValidationError,
                                    "The format of the downloaded file is not suitable."
                                         + "Check headers in the file.");
                    }
                    charPointer++;
                }

                while (!IsEndOfFile(rowCounter, groupsSheet))
                {
                    try
                    {
                        StudentGroupFile fileLine = new StudentGroupFile
                        {
                            CourseId = groupsSheet.Cell($"B{rowCounter}").Value.ToString(),
                            Name = groupsSheet.Cell($"C{rowCounter}").Value.ToString(),
                            StartDate = Convert
                            .ToDateTime(groupsSheet.Cell($"D{rowCounter}").Value),
                            FinishDate = Convert
                            .ToDateTime(groupsSheet.Cell($"E{rowCounter}").Value)
                        };

                        await IsValueValid(fileLine, rowCounter);

                        StudentGroup group = new StudentGroup
                        {
                            CourseId = Convert.ToInt32(fileLine.CourseId),
                            Name = fileLine.Name,
                            StartDate = fileLine.StartDate,
                            FinishDate = fileLine.FinishDate,
                        };

                        importedGroups.Add(fileLine);
                        _unitOfWork.StudentGroupRepository.Add(group);
                        rowCounter++;
                    }
                    catch (FormatException ex)
                    {
                        _unitOfWork.Rollback();

                        return Result<List<StudentGroupFile>>.GetError(ErrorCode.ValidationError,
                            "The format of the inputed data is incorrect.\n" + ex.Message);
                    }
                    catch (DbUpdateException ex)
                    {
                        _unitOfWork.Rollback();

                        return Result<List<StudentGroupFile>>
                            .GetError(ErrorCode.ValidationError,
                                "Inputed data is incorrect.\n" + ex.Message);
                    }
                }
            }
            await _unitOfWork.CommitAsync();

            Array.ForEach(Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "Upload\\files")), File.Delete);

            return Result<List<StudentGroupFile>>
                .GetSuccess(_mapper.Map<List<StudentGroupFile>>(importedGroups));
        }

        private async Task IsValueValid(StudentGroupFile fileLine, int rowCounter)
        {
            List<long> existingCourseIds = new List<long>();

            foreach (Course course in await _unitOfWork.CourseRepository.GetAllAsync())
            {
                existingCourseIds.Add(course.Id);
            }

            if (fileLine.CourseId.Replace(" ", "") == "")
            {
                throw new FormatException("CourseId field shouldn't be empty.\n" +
                    $"Problem was occured in col B, row {rowCounter}");
            }

            if (fileLine.Name == "")
            {
                throw new FormatException("Name field shouldn't be empty.\n" +
                    $"Problem was occured in col C, row {rowCounter}");
            }

            if (fileLine.StartDate > fileLine.FinishDate)
            {
                throw new FormatException("StartDate must be less than FinishDate.\n" +
                    $"Problem was occured in col D/E, row {rowCounter}.");
            }

            if (!existingCourseIds.Contains(Convert.ToInt64(fileLine.CourseId)))
            {
                throw new DbUpdateException($"Course with id {fileLine.CourseId} doesn't exist.\n" +
                   $"Problem was occured in col B, row {rowCounter}.");
            }

            if (await _unitOfWork.StudentGroupRepository.IsGroupNameExistAsync(fileLine.Name))
            {
                throw new DbUpdateException($"Group with name {fileLine.Name} already exists.\n" +
                   $"Problem was occured in col C, row {rowCounter}.");
            }
        }

        private bool IsEndOfFile(int rowCounter, IXLWorksheet sheet)
        {
            return (sheet.Cell($"B{rowCounter}").Value.ToString() == "")
               && (sheet.Cell($"C{rowCounter}").Value.ToString() == "")
               && (sheet.Cell($"D{rowCounter}").Value.ToString() == "")
               && (sheet.Cell($"E{rowCounter}").Value.ToString() == "");
        }

        private async Task<string> CreateFile(IFormFile file)
        {
            string path = "";
            string fileName;
            var extension = "." + file.FileName.Split('.')[file.FileName.Split('.').Length - 1];
            fileName = DateTime.Now.Ticks + extension; //Create a new Name for the file due to security reasons.

            var pathBuilt = Path.Combine(Directory.GetCurrentDirectory(), "Upload\\files");

            if (!Directory.Exists(pathBuilt))
            {
                Directory.CreateDirectory(pathBuilt);
            }

            path = Path.Combine(Directory.GetCurrentDirectory(), "Upload\\files", fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return path;
        }

        public bool CheckIfExcelFile(IFormFile file)
        {
            var extension = "." + file.FileName.Split('.')[file.FileName.Split('.').Length - 1];

            return (extension == ".xlsx" || extension == ".xls");
        }
    }
}
