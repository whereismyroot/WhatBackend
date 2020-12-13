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
using System.Linq;

namespace CharlieBackend.Business.Services
{
    public class ThemeImportService : IThemeImportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ThemeImportService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<List<ThemeFile>>> ImportFileAsync(IFormFile uploadedFile)
        {
            List<ThemeFile> importedThemes = new List<ThemeFile>();
            var worksheetName = "Themes";

            try
            {
                var themesSheet = (await ValidateFile(uploadedFile, worksheetName)).Worksheet(worksheetName);

                int rowCounter = 2;

                while (!IsEndOfFile(rowCounter, themesSheet))
                {
                    ThemeFile fileLine = new ThemeFile
                    {
                        ThemeName = themesSheet.Cell($"A{rowCounter}").Value.ToString(),
                    };

                    List<string> themes = new List<string>();

                    foreach (var theme in await _unitOfWork.ThemeRepository.GetAllAsync())
                    {
                        themes.Add(theme.Name);
                    }

                    var errors = ValidateFileValue(fileLine, rowCounter, themes);

                    if (errors.Any()) 
                    {
                        _unitOfWork.Rollback();

                        return Result<List<ThemeFile>>
                                .GetError(ErrorCode.ValidationError, string.Join("\n", errors));
                    }

                    Theme newTheme = new Theme
                    {
                        Name = fileLine.ThemeName,
                    };

                    importedThemes.Add(fileLine);
                    _unitOfWork.ThemeRepository.Add(newTheme);
                    rowCounter++;
                }
            }
            catch (FormatException ex)
            {
                _unitOfWork.Rollback();

                return Result<List<ThemeFile>>.GetError(ErrorCode.ValidationError, ex.Message);
            }

            await _unitOfWork.CommitAsync();

            return Result<List<ThemeFile>>
                .GetSuccess(_mapper.Map<List<ThemeFile>>(importedThemes));
        }

        private IEnumerable<string> ValidateFileValue(ThemeFile fileLine, 
                                                      int rowCounter, 
                                                      List<string> themes)
        {
            if (themes.Contains(fileLine.ThemeName))
            {
                yield return $"Theme with name {fileLine.ThemeName} already exists. " +
                   $"Problem was occured in col A, row {rowCounter}.";
            }

            if (fileLine.ThemeName.Length > 40) 
            {
                yield return $"Inputed theme it too long. " +
                   $"Problem was occured in col A, row {rowCounter}.";
            }
        }

        private async Task<XLWorkbook> ValidateFile(IFormFile file, string worksheetName)
        {
            using (var stream = new MemoryStream())
            {
                XLWorkbook book = new XLWorkbook();
                string fileExtension = "."
                        + file.FileName.Split('.')[^1];

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
                var themesSheet = book.Worksheet(worksheetName);
                char charPointer = 'A';
                var properties = typeof(ThemeFile).GetProperties();

                foreach (PropertyInfo property in properties)
                {
                    if (property.Name
                            != Convert.ToString(themesSheet.
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

        private bool IsEndOfFile(int rowCounter, IXLWorksheet sheet)
        {
            return (sheet.Cell($"A{rowCounter}").Value.ToString() == "");
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
