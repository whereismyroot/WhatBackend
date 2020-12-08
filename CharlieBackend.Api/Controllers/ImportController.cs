using CharlieBackend.Core;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using CharlieBackend.Core.FileModels;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.Annotations;
using CharlieBackend.Core.Models.ResultModel;
using CharlieBackend.Business.Services.Interfaces;

namespace CharlieBackend.Api.Controllers
{
    /// <summary>
    /// Controller to manage import from external sources
    /// </summary>
    [Route("api/imports")]
    [ApiController]
    public class ImportController : ControllerBase
    {
        private readonly IStudentImportService _studentImportService;
        private readonly IThemeImportService _themeImportService;
        private readonly IStudentGroupImportService _groupImportService;

        /// <summary>
        /// Import controller constructor
        /// </summary>
        public ImportController(IStudentImportService studentImportService,
                                IStudentGroupImportService groupImportService,
                                IThemeImportService themeImportService)
        {
            _studentImportService = studentImportService;
            _groupImportService = groupImportService;
            _themeImportService = themeImportService;
        }

        /// <summary>
        /// Imports group data from .xlsx file to database
        /// </summary>
        /// <response code="200">Successful import of data from file</response>
        /// <response code="HTTP: 400, API: 4">File validation error</response>
        [SwaggerResponse(200, type: typeof(List<StudentGroupFile>))]
        [Authorize(Roles = "Mentor, Secretary, Admin")]
        [Route("groups")]
        [HttpPost]
        public async Task<ActionResult> ImportGroupDataFromFile(IFormFile file)
        {
            var listOfImportedGroups = new Result<List<StudentGroupFile>>();

            if (_groupImportService.CheckIfExcelFile(file))
            {
                listOfImportedGroups = await _groupImportService.ImportFileAsync(file);
            }
            else
            {
                return BadRequest(new { message = "Invalid file extension" });
            }

            return listOfImportedGroups.ToActionResult();
        }

        /// <summary>
        /// Imports student data from .xlsx file to database
        /// </summary>
        /// <response code="200">Successful import of data from file</response>
        /// <response code="HTTP: 400, API: 4">File validation error</response>
        [SwaggerResponse(200, type: typeof(List<StudentFile>))]
        [Authorize(Roles = "Mentor, Secretary, Admin")]
        [Route("students/{groupId}")]
        [HttpPost]
        public async Task<ActionResult> ImportStudentDataFromFile(long groupId, IFormFile file)
        {
            var listOfImportedStudents = new Result<List<StudentFile>>();

            if (_groupImportService.CheckIfExcelFile(file))
            {
                listOfImportedStudents = await _studentImportService.ImportFileAsync(groupId, file);
            }
            else
            {
                return BadRequest(new { message = "Invalid file extension" });
            }

            return listOfImportedStudents.ToActionResult();
        }

        /// <summary>
        /// Imports student data from .xlsx file to database
        /// </summary>
        /// <response code="200">Successful import of data from file</response>
        /// <response code="HTTP: 400, API: 4">File validation error</response>
        [SwaggerResponse(200, type: typeof(List<ThemeFile>))]
        [Authorize(Roles = "Mentor, Secretary, Admin")]
        [Route("themes")]
        [HttpPost]
        public async Task<ActionResult> ImportThemeDataFromFile(IFormFile file)
        {
            var listOfImportedThemes = await _themeImportService.ImportFileAsync(file);

            return listOfImportedThemes.ToActionResult();
        }
    }
}
