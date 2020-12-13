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
using CharlieBackend.Core.DTO.Account;
using CharlieBackend.Core.Models.ResultModel;
using CharlieBackend.Business.Services.Interfaces;
using CharlieBackend.Data.Repositories.Impl.Interfaces;

namespace CharlieBackend.Business.Services
{
    public class StudentImportService : IStudentImportService
    {
        #region private
        private readonly IStudentService _studentService;
        private readonly IStudentGroupService _studentGroupService;
        private readonly IAccountService _accountService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        #endregion

        public StudentImportService(IMapper mapper,
                                    IUnitOfWork unitOfWork,
                                    IAccountService accountService,
                                    IStudentService studentService,
                                    IStudentGroupService studentGroupService)
        {
            _studentGroupService = studentGroupService;
            _studentService = studentService;
            _accountService = accountService;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<List<StudentFile>>> ImportFileAsync(long groupId, IFormFile uploadedFile)
        {
            List<StudentFile> importedAccounts = new List<StudentFile>();
            var worksheetName = "Students";

            try
            {
                var group = _unitOfWork.StudentGroupRepository.SearchStudentGroup(groupId);

                if (group == null)
                {
                    return Result<List<StudentFile>>.GetError(ErrorCode.NotFound, $"Group with id {groupId} doesn't exist.");
                }

                var studentsSheet = (await ValidateFile(uploadedFile, worksheetName)).Worksheet(worksheetName);

                int rowCounter = 2;

                List<string> existingEmails = new List<string>();

                foreach (Account account in await _unitOfWork.AccountRepository.GetAllAsync())
                {
                    existingEmails.Add(account.Email);
                }

                while (!IsEndOfFile(rowCounter, studentsSheet))
                {

                    StudentFile fileLine = new StudentFile
                    {
                        Email = studentsSheet.Cell($"A{rowCounter}").Value.ToString(),
                        FirstName = studentsSheet.Cell($"B{rowCounter}").Value.ToString(),
                        LastName = studentsSheet.Cell($"C{rowCounter}").Value.ToString()
                    };

                    var errors = ValidateFileValue(fileLine, rowCounter, existingEmails);

                    if (errors.Any())
                    {
                        _unitOfWork.Rollback();

                        return Result<List<StudentFile>>.GetError(ErrorCode.ValidationError, string.Join("\n", errors));
                    }

                    CreateAccountDto studentAccount = new CreateAccountDto
                    {
                        Email = fileLine.Email,
                        FirstName = fileLine.FirstName,
                        LastName = fileLine.LastName,
                        Password = "changeYourPassword",
                        ConfirmPassword = "changeYourPassword"
                    };

                    await _accountService.CreateAccountAsync(studentAccount);

                    importedAccounts.Add(fileLine);
                    rowCounter++;
                }
            }
            catch (FormatException ex)
            {
                _unitOfWork.Rollback();

                return Result<List<StudentFile>>.GetError(ErrorCode.ValidationError, ex.Message);
            }

            await _unitOfWork.CommitAsync();

            await BoundStudentsToTheGroupAsync(importedAccounts, groupId);

            return Result<List<StudentFile>>
                .GetSuccess(_mapper.Map<List<StudentFile>>(importedAccounts));
        }

        private async Task BoundStudentsToTheGroupAsync(List<StudentFile> importedAccounts, long groupId)
        {
            List<string> studentEmails = new List<string>();
            List<long> accountsIds = new List<long>();
            List<long> studentsIds = new List<long>();
            var newStudentStudentGroup = new List<StudentOfStudentGroup>();

            foreach (var account in importedAccounts)
            {
                studentEmails.Add(account.Email);
            }

            foreach (var account in await _accountService.GetAllNotAssignedAccountsAsync())
            {
                if (studentEmails.Contains(account.Email))
                {
                    accountsIds.Add(account.Id);
                }
            }

            foreach (var id in accountsIds)
            {
                await _studentService.CreateStudentAsync(id);
            }

            foreach (var student in await _studentService.GetAllActiveStudentsAsync())
            {
                if (studentEmails.Contains(student.Email))
                {
                    studentsIds.Add(student.Id);
                }
            }

            foreach (var studentId in studentsIds)
            {
                newStudentStudentGroup.Add(new StudentOfStudentGroup
                {
                    StudentGroupId = groupId,
                    StudentId = studentId
                });
            }

            _studentGroupService.AddStudentOfStudentGroups(newStudentStudentGroup);
            await _unitOfWork.CommitAsync();
        }

        private async Task<XLWorkbook> ValidateFile(IFormFile file, string WorksheetName)
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
                    book = new XLWorkbook(ConvertCsvToExcel(stream, WorksheetName));
                }
                else
                {
                    throw new FormatException(
                        "Format of uploaded file is incorrect. " +
                        "It must have .xlsx or .csv extension");
                }

                var studentsSheet = book.Worksheet("Students");
                char charPointer = 'A';

                var properties = typeof(StudentFile).GetProperties();
                foreach (PropertyInfo property in properties)
                {
                    if (property.Name != Convert.ToString(studentsSheet.Cell($"{charPointer}1").Value))
                    {
                        throw new FormatException("Check headers in the file.");
                    }
                    charPointer++;
                }
                return book;
            }
        }

        private IEnumerable<string> ValidateFileValue(StudentFile fileLine, int rowCounter, List<string> existingEmails)
        {
            if (fileLine.FirstName == "")
            {
                yield return "Name field shouldn't be empty.\n" +
                    $"Problem was occured in col B, row {rowCounter}";
            }

            if (fileLine.LastName == "")
            {
                yield return "Name field shouldn't be empty.\n" +
                    $"Problem was occured in col C, row {rowCounter}";
            }

            if (existingEmails.Contains(fileLine.Email))
            {
                yield return $"Account with email {fileLine.Email} already exists.\n" +
                   $"Problem was occured in col A, row {rowCounter}.";
            }
        }

        private bool IsEndOfFile(int rowCounter, IXLWorksheet studentsSheet)
        {
            return (studentsSheet.Cell($"A{rowCounter}").Value.ToString() == "")
               && (studentsSheet.Cell($"B{rowCounter}").Value.ToString() == "")
               && (studentsSheet.Cell($"C{rowCounter}").Value.ToString() == "");
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
