using System;
using System.IO;
using AutoMapper;
using System.Linq;
using OfficeOpenXml;
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
using System.Data;

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

        public async Task<Result<List<StudentGroupFile>>> ImportFileAsync(long courseId, IFormFile uploadedFile)
        {
            List<StudentGroupFile> importedGroups = new List<StudentGroupFile>();
            var worksheetName = "Groups";

            try
            {
                var groupsSheet = (await ValidateFile(uploadedFile, worksheetName))
                        .Worksheet(worksheetName);

                int rowCounter = 2;

                while (!IsEndOfFile(rowCounter, groupsSheet))
                {
                    StudentGroupFile fileLine = new StudentGroupFile
                    {
                        Name = groupsSheet.Cell($"A{rowCounter}").Value.ToString(),
                        StartDate = Convert
                        .ToDateTime(groupsSheet.Cell($"B{rowCounter}").Value),
                        FinishDate = Convert
                        .ToDateTime(groupsSheet.Cell($"C{rowCounter}").Value)
                    };

                    List<long> existingCourseIds = new List<long>();

                    foreach (Course course in await _unitOfWork.CourseRepository.GetAllAsync())
                    {
                        existingCourseIds.Add(course.Id);
                    }

                    var errors = ValidateFileValue(fileLine, rowCounter, existingCourseIds, 
                            await _unitOfWork.StudentGroupRepository
                                    .IsGroupNameExistAsync(fileLine.Name),
                                    courseId);

                    if (errors.Any()) 
                    {
                        _unitOfWork.Rollback();

                        return Result<List<StudentGroupFile>>
                                .GetError(ErrorCode.ValidationError, string.Join("\n", errors));
                    }

                    StudentGroup group = new StudentGroup
                    {
                        CourseId = courseId,
                        Name = fileLine.Name,
                        StartDate = fileLine.StartDate,
                        FinishDate = fileLine.FinishDate,
                    };

                    importedGroups.Add(fileLine);
                    _unitOfWork.StudentGroupRepository.Add(group);
                    rowCounter++;
                }
            }
            catch (FormatException ex)
            {
                _unitOfWork.Rollback();

                return Result<List<StudentGroupFile>>
                    .GetError(ErrorCode.ValidationError, ex.Message);
            }

            await _unitOfWork.CommitAsync();

            return Result<List<StudentGroupFile>>
                .GetSuccess(_mapper.Map<List<StudentGroupFile>>(importedGroups));
        }

        private async Task<XLWorkbook> ValidateFile(IFormFile file, string worksheetName)
        {
            using (var stream = new MemoryStream())
            {
                string fileExtension = "." 
                        + file.FileName.Split('.')[^1];
                XLWorkbook book = new XLWorkbook();

                await file.CopyToAsync(stream);

                if (fileExtension == ".xlsx")
                {
                    book = new XLWorkbook(stream);
                }
                else if (fileExtension == ".csv")
                {
                    book = new XLWorkbook(ConvertCsvToExcel(stream, worksheetName));
                }
                else
                {
                    throw new FormatException(
                        "Format of uploaded file is incorrect. " +
                        "It must have .xlsx or .csv extension");
                }
                var groupsSheet = book.Worksheet("Groups");
                char charPointer = 'A';
                var properties = typeof(StudentGroupFile).GetProperties();

                foreach (PropertyInfo property in properties)
                {
                    if (property.Name 
                            != Convert.ToString(groupsSheet.
                                Cell($"{charPointer}1").Value))
                    {
                        throw new FormatException("Format of uploaded file is incorrect. " 
                                                + "Check headers in the file.");
                    }
                    charPointer++;
                }
                return book;
            }
        }

        private IEnumerable<string> ValidateFileValue(StudentGroupFile fileLine,
                                                      int rowCounter, 
                                                      List<long> existingCourseIds,
                                                      bool IsGroupNameExists,
                                                      long courseId)
        {
            if (fileLine.Name == "")
            {
                yield return "Name field shouldn't be empty.\n" +
                    $"Problem was occured in col C, row {rowCounter}";
            }

            if (fileLine.StartDate > fileLine.FinishDate)
            {
                yield return  "StartDate must be less than FinishDate.\n" +
                    $"Problem was occured in col D/E, row {rowCounter}.";
            }

            if (!existingCourseIds.Contains(courseId))
            {
                yield return $"Course with id {courseId} doesn't exist.\n" +
                   $"Problem was occured in col B, row {rowCounter}.";
            }

            if (IsGroupNameExists)
            {
                yield return $"Group with name {fileLine.Name} already exists.\n" +
                   $"Problem was occured in col C, row {rowCounter}.";
            }
        }

        private bool IsEndOfFile(int rowCounter, IXLWorksheet sheet)
        {
            return (sheet.Cell($"A{rowCounter}").Value.ToString() == "")
                && (sheet.Cell($"B{rowCounter}").Value.ToString() == "")
                && (sheet.Cell($"C{rowCounter}").Value.ToString() == "");
        }

        public Stream ConvertCsvToExcel(MemoryStream stream, string worksheetName)
        {
            string fileContent;
            stream.Position = 0;
            using (var reader = new StreamReader(stream))
            {
                fileContent = reader.ReadToEnd();
            }

            var format = new ExcelTextFormat();
            format.Delimiter = ',';
            format.EOL = "\r";

            var result = new MemoryStream();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (ExcelPackage package = new ExcelPackage())
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add(worksheetName);
                worksheet.Cells["A1"].LoadFromText(fileContent, format, OfficeOpenXml.Table.TableStyles.Dark1, false);
                package.SaveAs(result);
                result.Position = 0;
            }

            return result;
        }
    }
}
