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

            try
            {
                var themesSheet = (await ValidateFile(uploadedFile)).Worksheet("Themes");

                int rowCounter = 2;

                while (!IsEndOfFile(rowCounter, themesSheet))
                {
                    ThemeFile fileLine = new ThemeFile
                    {
                        ThemeName = themesSheet.Cell($"A{rowCounter}").Value.ToString(),
                    };

                    await IsValueValid(fileLine, rowCounter);

                    Theme theme = new Theme
                    {
                        Name = fileLine.ThemeName,
                    };

                    importedThemes.Add(fileLine);
                    _unitOfWork.ThemeRepository.Add(theme);
                    rowCounter++;
                }
            }
            catch (FormatException ex)
            {
                _unitOfWork.Rollback();

                return Result<List<ThemeFile>>.GetError(ErrorCode.ValidationError, ex.Message);
            }
            catch (DbUpdateException ex)
            {
                _unitOfWork.Rollback();

                return Result<List<ThemeFile>>
                    .GetError(ErrorCode.ValidationError, ex.Message);
            }

            await _unitOfWork.CommitAsync();

            Array.ForEach(Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "Upload\\files")), File.Delete);

            return Result<List<ThemeFile>>
                .GetSuccess(_mapper.Map<List<ThemeFile>>(importedThemes));
        }

        private async Task IsValueValid(ThemeFile fileLine, int rowCounter)
        {
            List<string> themes = new List<string>();

            foreach (var theme in await _unitOfWork.ThemeRepository.GetAllAsync())
            {
                themes.Add(theme.Name);
            }

            if (themes.Contains(fileLine.ThemeName))
            {
                Array.ForEach(Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "Upload\\files")), File.Delete);

                throw new DbUpdateException($"Theme with name {fileLine.ThemeName} already exists. " +
                   $"Problem was occured in col A, row {rowCounter}.");
            }
            if (fileLine.ThemeName.Length > 40) 
            {
                Array.ForEach(Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "Upload\\files")), File.Delete);

                throw new DbUpdateException($"Inputed theme it too long. " +
                   $"Problem was occured in col A, row {rowCounter}.");
            }
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

            var themesSheet = book.Worksheet("Themes");
            char charPointer = 'A';

            var properties = typeof(ThemeFile).GetProperties();
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

        private bool IsEndOfFile(int rowCounter, IXLWorksheet sheet)
        {
            return (sheet.Cell($"A{rowCounter}").Value.ToString() == "");
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
