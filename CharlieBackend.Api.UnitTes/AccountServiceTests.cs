using Moq;
using Xunit;
using System;
using AutoMapper;
using FluentAssertions;
using System.Threading.Tasks;
using CharlieBackend.Core.Mapping;
using CharlieBackend.Core.Entities;
using CharlieBackend.Core.DTO.Account;
using CharlieBackend.Business.Helpers;
using CharlieBackend.Business.Services;
using CharlieBackend.Core.Models.ResultModel;
using CharlieBackend.Business.Services.Interfaces;
using CharlieBackend.Data.Repositories.Impl.Interfaces;


namespace CharlieBackend.Api.UnitTest
{
    public class AccountServiceTests : TestBase
    {
        private readonly IMapper _mapper;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly Mock<IAccountRepository> _accountRepositoryMock;

        public AccountServiceTests()
        {
            _mapper = GetMapper(new ModelMappingProfile());
            _notificationServiceMock = new Mock<INotificationService>();
            _accountRepositoryMock = new Mock<IAccountRepository>();
        }

        [Fact]
        public async Task CreateAccountAsync_ValidData_ShouldReturnAccount()
        {
            //Arrange
            int accountExpectedId = 2;

            var successAccountModel = new CreateAccountDto()
            {
                Email = "test@example.com",
                FirstName = "test",
                LastName = "test",
                Password = "Qqwerty3_",
                ConfirmPassword = "Qqwerty3_"
            };

            _accountRepositoryMock.Setup(x => x.Add(It.IsAny<Account>()))
                .Callback<Account>(x => x.Id = accountExpectedId);

            _unitOfWorkMock.Setup(x => x.AccountRepository).Returns(_accountRepositoryMock.Object);

            var accountService = new AccountService(
                _unitOfWorkMock.Object,
                _mapper,
                _notificationServiceMock.Object);

            //Act
            var successResult = await accountService.CreateAccountAsync(successAccountModel);

            //Assert
            successResult.Error.Should().BeNull();
            successResult.Data.Id.Should().Be(accountExpectedId);
            successResult.Data.Role.Should().Be(UserRole.NotAssigned);
        }

        [Fact]
        public async Task CreateAccountAsync_EmailIstaken_ShouldThrowError()
        {
            //Arrange
            string accountExpectedEmail = "user@example.com";

            var isEmailTakenAccountModel = new CreateAccountDto()
            {
                Email = accountExpectedEmail,
                FirstName = "test",
                LastName = "test",
                Password = "Qqwerty3_",
                ConfirmPassword = "Qqwerty3_"
            };

            _accountRepositoryMock.Setup(x => x.IsEmailTakenAsync(accountExpectedEmail))
                    .ReturnsAsync(true);

            _unitOfWorkMock.Setup(x => x.AccountRepository).Returns(_accountRepositoryMock.Object);

            var accountService = new AccountService(
                _unitOfWorkMock.Object,
                _mapper,
                _notificationServiceMock.Object);

            //Act
            var isEmailTakenResult = await accountService.CreateAccountAsync(isEmailTakenAccountModel);

            //Assert
            isEmailTakenResult.Error.Code.Should().Be(ErrorCode.Conflict);
        }

        [Fact]
        public async Task ChangePasswordAsync_ValidData_ShouldReturnAccount()
        {
            //Arrange
            var salt = PasswordHelper.GenerateSalt();
            var oldPassword = "mypass";
            var newPassword = "changedPass";

            var changePass = new ChangeCurrentPasswordDto
            {
                Email = "user@exmaple.com",
                CurrentPassword = "mypass",
                NewPassword = newPassword,
                ConfirmNewPassword = newPassword
            };

            Account account = new Account
            {
                Id = 5,
                IsActive = true,
                Email = "user@exmaple.com",
                Password = PasswordHelper.HashPassword(oldPassword, salt),
                Salt = salt,
                Role = UserRole.Mentor
            };

            AccountDto updatedAccountDto = new AccountDto
            {
                Id = 5,
                Email = "user@exmaple.com",
                IsActive = true,
                Role = UserRole.Mentor
            };

            _unitOfWorkMock.Setup(x => x.AccountRepository.GetAccountSaltByEmail(account.Email))
                    .ReturnsAsync(salt);

            _unitOfWorkMock.Setup(x => x.AccountRepository.GetAccountCredentialsByEmailAsync(changePass.Email))
                    .ReturnsAsync(account);

            var accountService = new AccountService(
                    _unitOfWorkMock.Object,
                    _mapper,
                    _notificationServiceMock.Object);

            var successResult = await accountService.ChangePasswordAsync(changePass);

            successResult.Data.Should().BeEquivalentTo(updatedAccountDto);
        }

        [Fact]
        public async Task ChangePasswordAsync_notExistAccount_ShouldReturnError()
        {
            //Arrange
            var salt = PasswordHelper.GenerateSalt();
            var oldPassword = "mypass";

            Account account = new Account
            {
                Id = 5,
                IsActive = true,
                Email = "user@exmaple.com",
                Password = PasswordHelper.HashPassword(oldPassword, salt),
                Salt = salt,
                Role = UserRole.Mentor
            };

            var notExistDto = new ChangeCurrentPasswordDto
            {
                Email = "notExist@exmaple.com"
            };

            _unitOfWorkMock.Setup(x => x.AccountRepository.GetAccountSaltByEmail(account.Email))
                    .ReturnsAsync(salt);

            var accountService = new AccountService(
                    _unitOfWorkMock.Object,
                    _mapper,
                    _notificationServiceMock.Object);

            var notExistAccount = await accountService.ChangePasswordAsync(notExistDto);

            notExistAccount.Error.Code.Should().Be(ErrorCode.NotFound);
        }

        [Fact]
        public async Task ChangePasswordAsync_WrongPassword_ShouldReturnError()
        {
            var salt = PasswordHelper.GenerateSalt();
            var oldPassword = "mypass";
            var newPassword = "changedPass";

            Account account = new Account
            {
                Id = 5,
                IsActive = true,
                Email = "user@exmaple.com",
                Password = PasswordHelper.HashPassword(oldPassword, salt),
                Salt = salt,
                Role = UserRole.Mentor
            };

            var wrongPasswordDto = new ChangeCurrentPasswordDto
            {
                Email = "user@exmaple.com",
                CurrentPassword = "wrongPassword",
                NewPassword = newPassword,
                ConfirmNewPassword = newPassword
            };

            var changePass = new ChangeCurrentPasswordDto
            {
                Email = "user@exmaple.com",
                CurrentPassword = "mypass",
                NewPassword = newPassword,
                ConfirmNewPassword = newPassword
            };

            var accountService = new AccountService(
                    _unitOfWorkMock.Object,
                    _mapper,
                    _notificationServiceMock.Object);

            _unitOfWorkMock.Setup(x => x.AccountRepository.GetAccountSaltByEmail(account.Email))
                   .ReturnsAsync(salt);

            _unitOfWorkMock.Setup(x => x.AccountRepository.GetAccountCredentialsByEmailAsync(changePass.Email))
                    .ReturnsAsync(account);

            var wrongPassword = await accountService.ChangePasswordAsync(wrongPasswordDto);

            wrongPassword.Error.Code.Should().Be(ErrorCode.Conflict);
        }

        [Fact]
        public async Task ChangePasswordAsync_AccWithoutSalt_ShouldReturnError()
        {
            //Arrange
            var salt = PasswordHelper.GenerateSalt();
            var oldPassword = "mypass";

            Account account = new Account
            {
                Id = 5,
                IsActive = true,
                Email = "user@exmaple.com",
                Password = PasswordHelper.HashPassword(oldPassword, salt),
                Salt = salt,
                Role = UserRole.Mentor
            };

            var withoutSaltDto = new ChangeCurrentPasswordDto
            {
                Email = "withoutSalt@exmaple.com"
            };

            Account accountWithoutSalt = new Account
            {
                Id = 5,
                IsActive = true,
                Email = "withoutSalt@exmaple.com",
                Password = PasswordHelper.HashPassword(oldPassword, salt),
                Salt = null,
                Role = UserRole.Mentor
            };

            _unitOfWorkMock.Setup(x => x.AccountRepository.GetAccountSaltByEmail(account.Email))
                    .ReturnsAsync(salt);

            _unitOfWorkMock.Setup(x => x.AccountRepository.GetAccountCredentialsByEmailAsync(accountWithoutSalt.Email))
                    .ReturnsAsync(accountWithoutSalt);

            var accountService = new AccountService(
                    _unitOfWorkMock.Object,
                    _mapper,
                    _notificationServiceMock.Object);

            //Act
            var accWithoutSalt = await accountService.ChangePasswordAsync(withoutSaltDto);

            //Assert
            Assert.Equal(ErrorCode.InternalServerError, accWithoutSalt.Error.Code);
        }

        [Fact]
        public async Task GenerateForgotPasswordToken_ValidData()
        {
            //Arrange
            var successForgotPasswordDto = new ForgotPasswordDto
            {
                Email = "example@example.com",
                FormUrl = "https://frontenddomain/account/resetPassword"
            };

            var successUserAccount = new Account
            {
                Email = "example@example.com"
            };

            var accountService = new AccountService(
                        _unitOfWorkMock.Object,
                        _mapper,
                        _notificationServiceMock.Object);

            _unitOfWorkMock.Setup(x => x.AccountRepository.GetAccountCredentialsByEmailAsync(successForgotPasswordDto.Email))
                    .ReturnsAsync(successUserAccount);

            //Act
            var successResult = await accountService.GenerateForgotPasswordToken(successForgotPasswordDto);

            //Assert
            successResult.Data.Should().BeEquivalentTo(successForgotPasswordDto);
        }

        [Fact]
        public async Task GenerateForgotPasswordToken_userDoesntExist_ShouldReturnError()
        {
            //Arrange
            var successForgotPasswordDto = new ForgotPasswordDto
            {
                Email = "example@example.com",
                FormUrl = "https://frontenddomain/account/resetPassword"
            };

            var successUserAccount = new Account
            {
                Email = "example@example.com"
            };

            var userDoesntExistDto = new ForgotPasswordDto
            {
                Email = "doesntexist@example.com",
                FormUrl = "https://frontenddomain/account/resetPassword"
            };

            _unitOfWorkMock.Setup(x => x.AccountRepository.GetAccountCredentialsByEmailAsync(successForgotPasswordDto.Email))
                    .ReturnsAsync(successUserAccount);

            var accountService = new AccountService(
                    _unitOfWorkMock.Object,
                    _mapper,
                    _notificationServiceMock.Object);

            //Act
            var userDoesntExistResult = await accountService.GenerateForgotPasswordToken(userDoesntExistDto);

            //Assert
            Assert.Equal(ErrorCode.NotFound, userDoesntExistResult.Error.Code);
        }


        [Fact]
        public async Task ResetPasswordAsync_ValidData_ShouldReturnAccount() 
        {
            //Arrange
            var userGuid = Guid.NewGuid().ToString();
            var forgotPasswordGenDate = DateTime.Now;

            var validUser = new Account
            {
                Id = 5,
                Email = "example@example.com",
                IsActive = true,
                ForgotPasswordToken = userGuid,
                ForgotTokenGenDate = forgotPasswordGenDate,
                Role = UserRole.Mentor
            };

            var successResetPasswordDto = new ResetPasswordDto
            {
                Email = "example@example.com",
                NewPassword = "bob228",
                ConfirmNewPassword = "bob228"
            };

            var successAccountDto = new AccountDto
            {
                Id = 5,
                Email = "example@example.com",
                IsActive = true,
                Role = UserRole.Mentor
            };

            _unitOfWorkMock.Setup(x => x.AccountRepository.GetAccountCredentialsByEmailAsync(successResetPasswordDto.Email))
                    .ReturnsAsync(validUser);

            var accountService = new AccountService(
                    _unitOfWorkMock.Object,
                    _mapper,
                    _notificationServiceMock.Object);

            //Act
            var successResult = await accountService.ResetPasswordAsync(userGuid.ToString(), successResetPasswordDto);

            //Assert
            successResult.Data.Should().BeEquivalentTo(successAccountDto);
        }

        [Fact]
        public async Task ResetPasswordAsync_UserDoesntExist_ShouldReturnError() 
        {
            var userGuid = Guid.NewGuid().ToString();
            var forgotPasswordGenDate = DateTime.Now;

            var validUser = new Account
            {
                Id = 5,
                Email = "example@example.com",
                IsActive = true,
                ForgotPasswordToken = userGuid,
                ForgotTokenGenDate = forgotPasswordGenDate,
                Role = UserRole.Mentor
            };

            var userDoesntExist = new ResetPasswordDto
            {
                Email = "dontexist@example.com"
            };

            var successResetPasswordDto = new ResetPasswordDto
            {
                Email = "example@example.com",
                NewPassword = "bob228",
                ConfirmNewPassword = "bob228"
            };

            _unitOfWorkMock.Setup(x => x.AccountRepository.GetAccountCredentialsByEmailAsync(successResetPasswordDto.Email))
                    .ReturnsAsync(validUser);

            var accountService = new AccountService(
                    _unitOfWorkMock.Object,
                    _mapper,
                    _notificationServiceMock.Object);

            //Act
            var userDoesntExistResult = await accountService.ResetPasswordAsync(userGuid, userDoesntExist);

            //Assert
            Assert.Equal(ErrorCode.NotFound, userDoesntExistResult.Error.Code);
        }

        [Fact]
        public async Task ResetPasswordAsync_InvalidFormToken() 
        {
            //Arrange
            var userGuid = Guid.NewGuid().ToString();
            var forgotPasswordGenDate = DateTime.Now;

            var validUser = new Account
            {
                Id = 5,
                Email = "example@example.com",
                IsActive = true,
                ForgotPasswordToken = userGuid,
                ForgotTokenGenDate = forgotPasswordGenDate,
                Role = UserRole.Mentor
            };

            var successResetPasswordDto = new ResetPasswordDto
            {
                Email = "example@example.com",
                NewPassword = "bob228",
                ConfirmNewPassword = "bob228"
            };

            _unitOfWorkMock.Setup(x => x.AccountRepository.GetAccountCredentialsByEmailAsync(successResetPasswordDto.Email))
                    .ReturnsAsync(validUser);

            var accountService = new AccountService(
                    _unitOfWorkMock.Object,
                    _mapper,
                    _notificationServiceMock.Object);

            //Act
            var invalidFormToken = await accountService.ResetPasswordAsync(Guid.NewGuid().ToString(), successResetPasswordDto);

            //Assert
            invalidFormToken.Error.Code.Should().Be(ErrorCode.ValidationError);
        }

        [Fact]
        public async Task ResetPasswordAsync_expiredTokenDate_ShouldReturnError()
        {
            //Arrange
            var userGuid = Guid.NewGuid().ToString();
            var forgotPasswordGenDate = DateTime.Now;
            var expiredDate = DateTime.Now.AddDays(-1);

            var validUser = new Account
            {
                Id = 5,
                Email = "example@example.com",
                IsActive = true,
                ForgotPasswordToken = userGuid,
                ForgotTokenGenDate = forgotPasswordGenDate,
                Role = UserRole.Mentor
            };

            var userWithTokenDateExpired = new Account
            {
                Email = "expiredDate@example.com",
                ForgotPasswordToken = userGuid,
                ForgotTokenGenDate = expiredDate
            };

            var userWithTokenDateExpiredDto = new ResetPasswordDto
            {
                Email = "expiredDate@example.com"
            };

            _unitOfWorkMock.Setup(x => x.AccountRepository.GetAccountCredentialsByEmailAsync(userWithTokenDateExpiredDto.Email))
                    .ReturnsAsync(userWithTokenDateExpired);

            var accountService = new AccountService(
                    _unitOfWorkMock.Object,
                    _mapper,
                    _notificationServiceMock.Object);

            //Act
            var expiredTokenDate = await accountService.ResetPasswordAsync(userGuid, userWithTokenDateExpiredDto);

            //Assert
            expiredTokenDate.Error.Code.Should().Be(ErrorCode.ForgotPasswordExpired);
        }
    }
}
