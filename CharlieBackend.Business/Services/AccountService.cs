using System;
using AutoMapper;
using System.Threading.Tasks;
using System.Collections.Generic;
using CharlieBackend.Core.Entities;
using CharlieBackend.Core.DTO.Account;
using CharlieBackend.Business.Helpers;
using CharlieBackend.Core.Models.ResultModel;
using CharlieBackend.Business.Services.Interfaces;
using CharlieBackend.Data.Repositories.Impl.Interfaces;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace CharlieBackend.Business.Services
{
    public class AccountService : IAccountService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly INotificationService _notification;

        public AccountService(IUnitOfWork unitOfWork,
                              IMapper mapper,
                              INotificationService notification)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _notification = notification;
        }

        public async Task<Result<AccountDto>> CreateAccountAsync(CreateAccountDto accountModel)
        {
            var isEmailTaken = await IsEmailTakenAsync(accountModel.Email);

            if (isEmailTaken)
            {
                return Result<AccountDto>.GetError(ErrorCode.Conflict,
                    "Account already exists!");
            }

            try
            {
                var account = new Account
                {
                    Email = accountModel.Email,
                    FirstName = accountModel.FirstName,
                    LastName = accountModel.LastName
                };
                string answerFromPasswordValidation = PasswordHelper.PasswordValidation(accountModel.Password);

                if (!string.IsNullOrEmpty(answerFromPasswordValidation))
                {
                    return Result<AccountDto>.GetError(ErrorCode.ValidationError, answerFromPasswordValidation);
                }

                account.Salt = PasswordHelper.GenerateSalt();
                account.Password = PasswordHelper.HashPassword(accountModel.ConfirmPassword, account.Salt);

                _unitOfWork.AccountRepository.Add(account);

                await _unitOfWork.CommitAsync();

                await _notification.RegistrationSuccess(account);

                return Result<AccountDto>.GetSuccess(_mapper.Map<AccountDto>(account));
            }
            catch
            {
                _unitOfWork.Rollback();

                return Result<AccountDto>.GetError(ErrorCode.InternalServerError, "Cannot create account.");
            }
        }

        public async Task<Result<AccountDto>> GetAccountCredentialsAsync(AuthenticationDto authenticationModel)
        {
            var account = await _unitOfWork.AccountRepository.GetAccountCredentialsByEmailAsync(authenticationModel.Email);

            if (account != null)
            {
                string password = PasswordHelper.HashPassword(authenticationModel.Password, account.Salt);

                if(password != account.Password)
                {
                    return Result<AccountDto>.GetError(ErrorCode.Unauthorized, "Email or password is incorrect.");
                }
                else
                {
                    var foundAccount = _mapper.Map<AccountDto>(account);

                    return Result<AccountDto>.GetSuccess(foundAccount);
                }
            }

            return Result<AccountDto>.GetError(ErrorCode.NotFound,"User Not Found");

        }

        public async Task<IList<AccountDto>> GetAllAccountsAsync()
        {
            var foundAccount = _mapper.Map<IList<AccountDto>>(await _unitOfWork.AccountRepository.GetAllAsync());

            return foundAccount;
        }

        public async Task<Account> GetAccountCredentialsByIdAsync(long id)
        {
            var account = await _unitOfWork.AccountRepository.GetAccountCredentialsById(id);

            if (account != null)
            {
                return account;
            }

            return null;
        }

        public async Task<IList<AccountDto>> GetAllNotAssignedAccountsAsync()
        {
            var accounts = _mapper.Map<List<AccountDto>>(await _unitOfWork.AccountRepository.GetAllNotAssignedAsync());

            return accounts;
        }

        public Task<bool> IsEmailTakenAsync(string email)
        {
            return _unitOfWork.AccountRepository.IsEmailTakenAsync(email);
        }

        public Task<bool> IsEmailChangableToAsync(long id, string newEmail)
        {
            return _unitOfWork.AccountRepository.IsEmailChangableToAsync(id, newEmail);
        }

        public Task<bool?> IsAccountActiveAsync(string email)
        {
            return _unitOfWork.AccountRepository.IsAccountActiveAsync(email);
        }

        public async Task<bool> DisableAccountAsync(long id)
        {
            try
            {
                var isSucceeded = await _unitOfWork.AccountRepository.DisableAccountAsync(id);

                await _unitOfWork.CommitAsync();

                return isSucceeded;
            }
            catch
            {
                _unitOfWork.Rollback();

                return false;
            }
        }

        public async Task<Result<AccountDto>> ChangePasswordAsync(ChangeCurrentPasswordDto changePassword)
        {
            var user = await _unitOfWork.AccountRepository.GetAccountCredentialsByEmailAsync(changePassword.Email);
            
            if (user == null)
            {
                return Result<AccountDto>.GetError(ErrorCode.NotFound, "Account does not exist.");
            }

            var salt = user.Salt;

            if (!string.IsNullOrEmpty(salt))
            {
                string checkPassword = PasswordHelper.HashPassword(changePassword.CurrentPassword, salt);

                if (user.Password == checkPassword)
                {
                    user.Salt = PasswordHelper.GenerateSalt();
                    user.Password = PasswordHelper.HashPassword(changePassword.NewPassword, user.Salt);

                    await _unitOfWork.CommitAsync();

                    return Result<AccountDto>.GetSuccess(_mapper.Map<AccountDto>(user));
                }
                else
                {
                    return Result<AccountDto>.GetError(ErrorCode.Conflict, "Wrong current password.");
                }
            }

            return Result<AccountDto>.GetError(ErrorCode.InternalServerError, "Salt for this account does not exist.");
        }

        public async Task<Result<ForgotPasswordDto>> GenerateForgotPasswordToken(ForgotPasswordDto forgotPassword)
        {
            var user = await _unitOfWork.AccountRepository.GetAccountCredentialsByEmailAsync(forgotPassword.Email);

            if (user == null)
            {
                return Result<ForgotPasswordDto>.GetError(ErrorCode.NotFound,
                        $"Account with email {forgotPassword.Email} does not exist!");
            }

            user.ForgotPasswordToken = Guid.NewGuid().ToString();
            user.ForgotTokenGenDate = DateTime.Now;

            await _unitOfWork.CommitAsync();

            string callbackUrl = forgotPassword.FormUrl + "?token=" + user.ForgotPasswordToken;

            await _notification.ForgotPasswordNotify(forgotPassword.Email, callbackUrl);

            return Result<ForgotPasswordDto>.GetSuccess(forgotPassword);
        }

        public async Task<Result<AccountDto>> ResetPasswordAsync(string guid, ResetPasswordDto resetPassword)
        {
            var user = await _unitOfWork.AccountRepository.GetAccountCredentialsByEmailAsync(resetPassword.Email);

            if (user == null)
            {
                return Result<AccountDto>.GetError(ErrorCode.NotFound, "Account does not exist.");
            }

            if (user.ForgotPasswordToken != guid)
            {
                return Result<AccountDto>.GetError(ErrorCode.ValidationError, "Invalid forgot password token");
            }

            DateTime tokenGenDate = (DateTime)user.ForgotTokenGenDate;

            if (DateTime.Now > tokenGenDate.AddDays(1) || !user.ForgotTokenGenDate.HasValue)
            {
                return Result<AccountDto>.GetError(ErrorCode.ForgotPasswordExpired,
                                                   $"Forgot password token for {user.Email} is expired");
            }

            user.Salt = PasswordHelper.GenerateSalt();
            user.Password = PasswordHelper.HashPassword(resetPassword.NewPassword, user.Salt);
            user.ForgotPasswordToken = null;
            user.ForgotTokenGenDate = null;

            await _unitOfWork.CommitAsync();

            return Result<AccountDto>.GetSuccess(_mapper.Map<AccountDto>(user));
        }

        public async Task<bool> AddAvatar(long id, IFormFile avatar)
        {
            var account = await _unitOfWork.AccountRepository.GetAccountCredentialsById(id);

            if(account.AttachmentId.HasValue)
            {
                return false;
            }
            else
            {
                using (var stream = new FileStream(avatar.FileName, FileMode.Create))
                {
                    await avatar.CopyToAsync(stream);
                }

                Attachment attachment = new Attachment
                {
                    FileName = avatar.FileName,
                    ContainerName = "none",
                    CreatedByAccountId = account.Id;
                };

                _unitOfWork.AttachmentRepository.Add(attachment);

                await _unitOfWork.CommitAsync();

                account.AttachmentId = attachment.Id;

                await _unitOfWork.CommitAsync();

                return true;
            }
        }
    }
}
