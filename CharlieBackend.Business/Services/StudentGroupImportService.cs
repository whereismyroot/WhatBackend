using System;
using System.IO;
using AutoMapper;
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

namespace CharlieBackend.Business.Services
{
    public class StudentGroupImportService : IStudentGroupImportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public StudentGroupImportService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<List<StudentGroupFile>>> ImportFileAsync(IFormFile uploadedFile)
        {
            List<StudentGroupFile> importedGroups = new List<StudentGroupFile>();

            try
            {
                var groupsSheet = (await ValidateFile(uploadedFile)).Worksheet("Groups");

                int rowCounter = 2;

                while (!IsEndOfFile(rowCounter, groupsSheet))
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

                    await ValidateFileValue(fileLine, rowCounter);

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
            await _unitOfWork.CommitAsync();

            Array.ForEach(Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "Upload\\files")), File.Delete);

            return Result<List<StudentGroupFile>>
                .GetSuccess(_mapper.Map<List<StudentGroupFile>>(importedGroups));
        }

        private async Task<XLWorkbook> ValidateFile(IFormFile file)
        {
            string fileExtension = "." + file.FileName.Split('.')[file.FileName.Split('.').Length - 1];
            XLWorkbook book = new XLWorkbook();

            if (fileExtension == ".xlsx")
            {
                string pathToExcel = await CreateFile(file);
                book = new XLWorkbook(pathToExcel);
            }
            else if (fileExtension == ".csv")
            {
                string pathToCsv = await CreateFile(file);
                book = new XLWorkbook(ConvertCsvToExcel(pathToCsv));
            }
            else
            {
                Array.ForEach(Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "Upload\\files")), File.Delete);

                throw new FormatException(
                    "Format of uploaded file is incorrect. " +
                    "It must have .xlsx or .csv extension");
            }

            var themesSheet = book.Worksheet("Groups");
            char charPointer = 'A';

            var properties = typeof(StudentGroupFile).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                if (property.Name != Convert.ToString(themesSheet.Cell($"{charPointer}1").Value))
                {
                    throw new FormatException("Check headers in the file.");
                }
                charPointer++;
            }
            return book;
        }

        private async Task ValidateFileValue(StudentGroupFile fileLine, int rowCounter)
        {
            List<long> existingCourseIds = new List<long>();

            foreach (Course course in await _unitOfWork.CourseRepository.GetAllAsync())
            {
                existingCourseIds.Add(course.Id);
            }

            if (fileLine.CourseId.Replace(" ", "") == "")
            {
                Array.ForEach(Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "Upload\\files")), File.Delete);

                throw new FormatException("CourseId field shouldn't be empty.\n" +
                    $"Problem was occured in col B, row {rowCounter}");
            }

            if (fileLine.Name == "")
            {
                Array.ForEach(Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "Upload\\files")), File.Delete);

                throw new FormatException("Name field shouldn't be empty.\n" +
                    $"Problem was occured in col C, row {rowCounter}");
            }

            if (fileLine.StartDate > fileLine.FinishDate)
            {
                Array.ForEach(Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "Upload\\files")), File.Delete);

                throw new FormatException("StartDate must be less than FinishDate.\n" +
                    $"Problem was occured in col D/E, row {rowCounter}.");
            }

            if (!existingCourseIds.Contains(Convert.ToInt64(fileLine.CourseId)))
            {
                Array.ForEach(Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "Upload\\files")), File.Delete);

                throw new DbUpdateException($"Course with id {fileLine.CourseId} doesn't exist.\n" +
                   $"Problem was occured in col B, row {rowCounter}.");
            }

            if (await _unitOfWork.StudentGroupRepository.IsGroupNameExistAsync(fileLine.Name))
            {
                Array.ForEach(Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "Upload\\files")), File.Delete);

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
        public string ConvertCsvToExcel(string pathToCsv)
        {
            string pathToExcel = pathToCsv.Remove(pathToCsv.Length - 4) + ".xlsx";

            string worksheetsName = "Themes";

            bool firstRowIsHeader = false;

            var format = new ExcelTextFormat();
            format.Delimiter = ',';
            format.EOL = "\r";

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (ExcelPackage package = new ExcelPackage(new FileInfo(pathToExcel)))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add(worksheetsName);
                worksheet.Cells["A1"].LoadFromText(new FileInfo(pathToCsv), format, OfficeOpenXml.Table.TableStyles.Dark1, firstRowIsHeader);
                package.Save();
            }

            return pathToExcel;
        }
    }
}
