using CharlieBackend.Core;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using CharlieBackend.Core.Entities;
using Swashbuckle.AspNetCore.Filters;
using CharlieBackend.Core.DTO.Account;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.Annotations;
using CharlieBackend.Business.Services.Interfaces;
using CharlieBackend.Api.SwaggerExamples.AccountsController;
using CharlieBackend.Business.Helpers;

namespace CharlieBackend.Api.Controllers
{
    /// <summary>
    /// Controller to manupulate with account
    /// </summary>
    [Route("api/accounts")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        #region
        private readonly IAccountService _accountService;
        private readonly IStudentService _studentService;
        private readonly IMentorService _mentorService;
        private readonly ISecretaryService _secretaryService;
        private readonly IJWTGenerator _jWTGenerator;
        #endregion
        /// <summary>
        /// Account controller constructor
        /// </summary>
        public AccountsController(IAccountService accountService,
                IStudentService studentService,
                IMentorService mentorService,
                ISecretaryService secretaryService,
                IJWTGenerator jWTGenerator)
        {
            _accountService = accountService;
            _studentService = studentService;
            _mentorService = mentorService;
            _secretaryService = secretaryService;
            _jWTGenerator = jWTGenerator;
        }

        /// <summary>
        /// Allows user to sign in
        /// </summary>
        /// <returns>JWT</returns>
        /// <response code="200">User successfully logged in</response>
        /// <response code="HTTP: 400, API: 0">Impossible to log in</response>
        /// <response code="HTTP: 401, API: 1">Impossible to log in due to wrong credentials</response>
        /// <response code="HTTP: 401, API: 1">Account is not active</response>
        /// <response code="403">Account not approved</response>
        [SwaggerResponse(200, type: typeof(SignInResponse))]
        [SwaggerResponseHeader(200, "Authorization Bearer", "string", "token")]
        [Route("auth")]
        [HttpPost]
        public async Task<ActionResult> SignIn([FromBody]AuthenticationDto authenticationModel)
        {
            var foundAccount = (await _accountService.GetAccountCredentialsAsync(authenticationModel)).Data;

            if (foundAccount == null)
            {
                return Unauthorized("Incorrect credentials, please try again.");
            }

            if (!foundAccount.IsActive)
            {
                return StatusCode(401, "Account is not active!");
            }

            long entityId = foundAccount.Id;

            if (foundAccount.Role == UserRole.Student)
            {
                var foundStudent = (await _studentService.GetStudentByAccountIdAsync(foundAccount.Id)).Data;

                if (foundStudent == null)
                {
                    return BadRequest();
                }

                entityId = foundStudent.Id;
            }

            if (foundAccount.Role == UserRole.Mentor)
            {
                var foundMentor = (await _mentorService.GetMentorByAccountIdAsync(foundAccount.Id)).Data;

                if (foundMentor == null)
                {
                    return BadRequest();
                }

                entityId = foundMentor.Id;
            }

            if (foundAccount.Role == UserRole.Secretary)
            {
                var foundSecretary = (await _secretaryService.GetSecretaryByAccountIdAsync(foundAccount.Id)).Data;

                if (foundSecretary == null)
                {
                    return BadRequest();
                }

                entityId = foundSecretary.Id;
            }

            if (foundAccount.Role == UserRole.NotAssigned)
            {
                return StatusCode(403, foundAccount.Email + " is registered and waiting assign.");
            }


            var encodedJwt = _jWTGenerator.GenerateEncodedJWT(foundAccount);

            var response = new
            {
                first_name = foundAccount.FirstName,
                last_name = foundAccount.LastName,
                role = foundAccount.Role,
                id = entityId
            };

            Response.Headers.Add("Authorization", "Bearer " + encodedJwt);
            Response.Headers.Add("Access-Control-Expose-Headers",
                    "x-token, Authorization"
                    );

            return Ok(response);
        }

        /// <summary>
        /// Registration of account
        /// </summary>
        /// <response code="200">User successfully registered</response>
        /// <response code="HTTP: 409, API: 5">Email already exists</response>
        [SwaggerResponse(200, type: typeof(AccountDto))]
        [Route("reg")]
        [HttpPost]
        public async Task<ActionResult> PostAccount(CreateAccountDto accountModel)
        {
            var createdAccountModel = await _accountService.CreateAccountAsync(accountModel);

            return createdAccountModel.ToActionResult();
        }

        /// <summary>
        /// Returns all registered accounts
        /// </summary>
        /// <response code="200">Successful return of list of registered accounts</response>
        [SwaggerResponse(200, type: typeof(IList<AccountDto>))]
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult> GetAllAccount()
        {
            return Ok(await _accountService.GetAllAccountsAsync());
        }

        /// <summary>
        /// Returns all not assigned accounts
        /// </summary>
        /// <response code="200">Successful return of list of all accounts which is not assigned to any role entity</response>
        [Route("NotAssigned")]
        [Authorize(Roles = "Admin, Secretary")]
        [HttpGet]
        public async Task<ActionResult<List<AccountDto>>> GetAllNotAssignedAccounts()
        {

            var accountsModels = await _accountService.GetAllNotAssignedAccountsAsync();

            return Ok(accountsModels);
        }

        /// <summary>
        /// Returns an updated account
        /// </summary>
        /// <response code="200">Successful return an updated account entity</response>
        [Route("password")]
        [Authorize(Roles = "Admin, Secretary, Mentor, Student")]
        [HttpPut]
        public async Task<ActionResult> ChangePassword(ChangeCurrentPasswordDto changePassword)
        {
            var updatedAccount = await _accountService.ChangePasswordAsync(changePassword);

            return updatedAccount.ToActionResult();
        }

        /// <summary>
        /// Returns a result of generating a forgot password token
        /// </summary>
        /// <response code="200">Successful returns a forgot password DTO</response>
        [Route("password/forgot")]
        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordDto changeForgotPassword)
        {
            var generatedForgotTokenResult = await _accountService.GenerateForgotPasswordToken(changeForgotPassword);

            return generatedForgotTokenResult.ToActionResult();
        }

        /// <summary>
        /// Returns a result of confirmed password change
        /// </summary>
        /// <response code="200">Successful return an updated account entity</response>
        [Route("password/reset/{guid}")]
        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult> ResetForgotPassword(string guid, ResetPasswordDto resetPassword)
        {
            var updatedAccount = await _accountService.ResetPasswordAsync(guid, resetPassword);

            return updatedAccount.ToActionResult();
        }
    }
}
