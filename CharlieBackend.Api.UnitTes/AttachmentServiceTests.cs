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
using Microsoft.Extensions.Logging;
using CharlieBackend.Core;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Linq;
using Azure.Storage.Blobs;
using CharlieBackend.Core.DTO.Attachment;
using System.Collections.Generic;
using Azure.Storage.Blobs.Models;

namespace CharlieBackend.Api.UnitTest
{
    public class AttachmentServiceTests : TestBase
    {
        private readonly IMapper _mapper;
        private static Mock<IBlobService> _blobServiceMock;
        private readonly AttachmentService _attachmentService;
        private new readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<IAccountRepository> _accountRepositoryMock;
        private readonly Mock<IAttachmentRepository> _attachmentRepositoryMock;
        private readonly Account _account;
        private readonly Attachment _attachment;
        private readonly string _fakeConnection;
        IFormFile _correctFile;
        IFormFile _tooBigFile;
        IFormFile _dangerousFile;

        public AttachmentServiceTests() 
        {
            _fakeConnection = "DefaultEndpointsProtocol=https;AccountName=123456;AccountKey=3Waz0PXXBe0Lie7HV51jdZsSFCqThFMsqGWdENueI/d6OoV14z6o9HhwlY1TvAtM2g0VIuPQLDDmEruu421NZA==;EndpointSuffix=core.windows.net";
            _mapper = GetMapper(new ModelMappingProfile());
            _blobServiceMock = new Mock<IBlobService>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _currentUserServiceMock.Setup(x => x.AccountId).Returns(1);
            _accountRepositoryMock = new Mock<IAccountRepository>();
            _attachmentRepositoryMock = new Mock<IAttachmentRepository>();
            _attachmentService = new AttachmentService(
                _unitOfWorkMock.Object,
                _mapper,
                _blobServiceMock.Object,
                _currentUserServiceMock.Object
                );
            _account = new Account
            {
                AvatarId = 1,
                IsActive = true,
                Role = UserRole.Admin,
                Avatar = new Attachment { ContainerName = "ff", FileName = "test" },

            };
            _attachment = new Attachment
            {
                Id = 1,
                Account = _account,
                ContainerName = "fff",
                FileName = "fff"
            };

            var fileMock = new Mock<IFormFile>();
            var content = "Hello World from a Fake File";
            var fileName = "TestPhoto.exe";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;
            fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
            fileMock.Setup(_ => _.FileName).Returns(fileName);
            fileMock.Setup(_ => _.Length).Returns(ms.Length);
            fileMock.Setup(_ => _.Name).Returns(fileName);

            _dangerousFile = fileMock.Object;

            fileMock = new Mock<IFormFile>();
            fileName = "TestPhoto.png";
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;
            fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
            fileMock.Setup(_ => _.FileName).Returns(fileName);
            fileMock.Setup(_ => _.Length).Returns(ms.Length);
            fileMock.Setup(_ => _.Name).Returns(fileName);

            _correctFile = fileMock.Object;


            fileMock = new Mock<IFormFile>();
            fileName = "TestPhoto.png";
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;
            fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
            fileMock.Setup(_ => _.FileName).Returns(fileName);
            fileMock.Setup(_ => _.Length).Returns(AttachmentService.FileMaxSize + 1);
            fileMock.Setup(_ => _.Name).Returns(fileName);

            _tooBigFile = fileMock.Object;

        }

        [Fact]
        public async Task AddAttachmentAsync_DengerousExtention_ShouldReturnError()
        {
            //Act
            var res = await _attachmentService.AddAttachmentAsync(_dangerousFile);

            //Assert
            res.Error.Code.Should().Be(ErrorCode.ValidationError);
            res.Error.Message.Should().Be("File has dengerous extention");
        }

        [Fact]
        public async Task AddAttachmentAsync_TooBigFile_ShouldReturnError()
        {
            //Act
            var res = await _attachmentService.AddAttachmentAsync(_tooBigFile);

            //Assert
            res.Error.Code.Should().Be(ErrorCode.ValidationError);
            res.Error.Message.Should().Be("File is too big, max size is 50 MB");
        }

        [Fact]
        public async Task GetAvatar_DontHaveAvatar_ReturnsError()
        {
            //Arrange 
            _accountRepositoryMock.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(new Account{ });

            _unitOfWorkMock.Setup(x => x.AccountRepository).Returns(_accountRepositoryMock.Object);

            //Act
            var res = await _attachmentService.GetAvatarUrl();

            //Assert
            res.Error.Code.Should().Be(ErrorCode.Conflict);
            res.Error.Message.Should().Be("Account doesn't have avatar.");
        }

        [Fact]
        public async Task GetAvatar_HaveAvatar_ReturnsSuccess()
        {
            //Arrange 
            _accountRepositoryMock.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(_account);
            _attachmentRepositoryMock.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(_attachment);
            _blobServiceMock.Setup(x => x.GetUrl(_account.Avatar)).Returns("fff");

            _unitOfWorkMock.Setup(x => x.AccountRepository).Returns(_accountRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.AttachmentRepository).Returns(_attachmentRepositoryMock.Object);

            //Act
            var res = await _attachmentService.GetAvatarUrl();

            //Assert
            res.Error.Should().BeNull();
            res.Data.Should().BeEquivalentTo("fff");
        }

        [Fact]
        public async Task AddAttachment_ValidData_ReturnSuccess()
        {
            _blobServiceMock.Setup(x => x.UploadAsync(_correctFile.FileName, It.IsAny<Stream>(), It.IsAny<bool>()))
                .ReturnsAsync(new BlobClient(_fakeConnection,
                "vv", "vv"));
            _unitOfWorkMock.Setup(x => x.AttachmentRepository).Returns(_attachmentRepositoryMock.Object);

            var res = await _attachmentService.AddAttachmentAsync(_correctFile);

            //res.Should().Be(1);
            res.Error.Should().BeNull();
            res.Data.Should().BeEquivalentTo(
                new AttachmentDto {
                    ContainerName = "vv",
                    CreatedByAccountId = 1,
                    FileName = "TestPhoto.png",
                    Id = 0,
                    CreatedOn = res.Data.CreatedOn
                });
        }

        [Fact]
        public async Task AddAttachmentAsAvatarAsync_NoAccount_ReturnError() 
        {
            var at = _attachment;
            at.Account = null;

            //Arrange 
            _attachmentRepositoryMock.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(at);

            _unitOfWorkMock.Setup(x => x.AccountRepository).Returns(_accountRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.AttachmentRepository).Returns(_attachmentRepositoryMock.Object);

            var res = await _attachmentService.AddAttachmentAsAvatarAsync(_correctFile);

            //Assert 
            res.Error.Code.Should().Be(ErrorCode.NotFound);
            res.Error.Message.Should().Be("Account not found");
        }

        [Fact]
        public async Task AddAttachmentAsAvatarAsync_validData_ReturnAttachment()
        {
            var fileMock = new Mock<IFormFile>();
            var content = "Hello World from a Fake File";
            var fileName = "TestPhoto.png";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;
            fileMock.Setup(_ => _.FileName).Returns(fileName);
            fileMock.Setup(_ => _.Length).Returns(ms.Length);
            fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
            fileMock.Setup(_ => _.ContentDisposition).Returns(string.Format("inline; filename={0}", fileName));
            var file = fileMock.Object;

            _attachmentRepositoryMock.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(_attachment);
            _accountRepositoryMock.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(_account);
            _blobServiceMock.Setup(x => x.UploadAsync(fileName, It.IsAny<Stream>(), It.IsAny<bool>()))
                .ReturnsAsync(new BlobClient(_fakeConnection,
                "vv", "vv"));

            _unitOfWorkMock.Setup(x => x.AttachmentRepository).Returns(_attachmentRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.AccountRepository).Returns(_accountRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.AttachmentRepository).Returns(_attachmentRepositoryMock.Object);

            //Act
            var res = await _attachmentService.AddAttachmentAsAvatarAsync(file);

            //Assert 
            res.Error.Should().BeNull();
            res.Data.Should().BeEquivalentTo(
                new AttachmentDto
                {
                    ContainerName = "vv",
                    CreatedByAccountId = 1,
                    FileName = "TestPhoto.png",
                    Id = 0,
                    CreatedOn = res.Data.CreatedOn
                }
            );
        }

        [Fact]
        public async Task AddAttachmentsAsync_validData_ReturnSuccess()
        {
            //Arrange
            var formFiles = new FormFileCollection();
            formFiles.Add(_correctFile);

            _attachmentRepositoryMock.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(_attachment);
            _accountRepositoryMock.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(_account);
            _blobServiceMock.Setup(x => x.UploadAsync(_correctFile.FileName, It.IsAny<Stream>(), It.IsAny<bool>()))
                .ReturnsAsync(new BlobClient(_fakeConnection,
                "vv", "vv"));

            _unitOfWorkMock.Setup(x => x.AccountRepository).Returns(_accountRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.AttachmentRepository).Returns(_attachmentRepositoryMock.Object);
            
            //Act
            var res = await _attachmentService.AddAttachmentsAsync(formFiles);

            //Assert
            res.Data[0].Should().BeEquivalentTo(new AttachmentDto
            {
                ContainerName = "vv",
                CreatedByAccountId = 1,
                FileName = "TestPhoto.png",
                Id = 0,
                CreatedOn = res.Data[0].CreatedOn
            });
        }

        [Fact]
        public async Task AddAttachmentsAsync_dangerousExtension_ReturnError()
        {
            var formFiles = new FormFileCollection();
            formFiles.Add(_dangerousFile);

            _attachmentRepositoryMock.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(_attachment);

            _unitOfWorkMock.Setup(x => x.AttachmentRepository).Returns(_attachmentRepositoryMock.Object);
            
            var res = await _attachmentService.AddAttachmentsAsync(formFiles);

            res.Error.Code.Should().Be(ErrorCode.ValidationError);
            res.Error.Message.Should().Be("Some of files have dengerous extention");
        }

        [Fact]
        public async Task AddAttachmentsAsync_maxSize_ReturnError()
        {
            var formFiles = new FormFileCollection();
            formFiles.Add(_tooBigFile);

            _attachmentRepositoryMock.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(_attachment);

            _unitOfWorkMock.Setup(x => x.AttachmentRepository).Returns(_attachmentRepositoryMock.Object);

            var res = await _attachmentService.AddAttachmentsAsync(formFiles);

            res.Error.Code.Should().Be(ErrorCode.ValidationError);
            res.Error.Message.Should().Be("Files are too big, max size is 50 MB");
        }

        [Fact]
        public async Task GetAttachmentUrl_ValidData_ReturnSuccess()
        {

            _accountRepositoryMock.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(_account);
            _attachmentRepositoryMock.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(_attachment);
            _blobServiceMock.Setup(x => x.GetUrl(_attachment)).Returns("fff");

            _unitOfWorkMock.Setup(x => x.AccountRepository).Returns(_accountRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.AttachmentRepository).Returns(_attachmentRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.AttachmentRepository)
                .Returns(_attachmentRepositoryMock.Object);

            var res = await _attachmentService.GetAttachmentUrl(1);

            res.Should().NotBeNull();
            res.Data.Should().Be("fff");
        }

        [Fact]
        public async Task GetAttachmentUrl_NotValidData_ReturnError()
        {

            _accountRepositoryMock.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(_account);
            _blobServiceMock.Setup(x => x.GetUrl(_attachment)).Returns("fff");

            _unitOfWorkMock.Setup(x => x.AccountRepository).Returns(_accountRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.AttachmentRepository).Returns(_attachmentRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.AttachmentRepository)
                .Returns(_attachmentRepositoryMock.Object);

            var res = await _attachmentService.GetAttachmentUrl(1);

            res.Error.Code.Should().Be(ErrorCode.NotFound);
            res.Error.Message.Should().Be("Attachment not found");
        }

        [Fact]
        public async Task GetAttachmentsListAsync_ValidData_Success() 
        {
            //Arrange
            var test = new AttachmentRequestDto { };
            _attachmentRepositoryMock.Setup(x => x.GetAttachmentListFiltered(test))
                .ReturnsAsync(new List<Attachment>()
                {
                    new Attachment { Id = 1 },
                    new Attachment { Id = 2 }
                });

            _currentUserServiceMock.Setup(x => x.Role).Returns(UserRole.Admin);
            _unitOfWorkMock.Setup(x => x.AttachmentRepository).Returns(_attachmentRepositoryMock.Object);

            //Act
            var res = await _attachmentService.GetAttachmentsListAsync(test);

            var responce = new List<Attachment>() { 
            new Attachment { 
                ContainerName = null,
                FileName = null ,
                Id = 1,
                CreatedOn = res.Data[0].CreatedOn
            },
            new Attachment {
                ContainerName = null,
                FileName = null ,
                Id = 2,
                CreatedOn = res.Data[1].CreatedOn
            }
            };
            res.Error.Should().BeNull();
            res.Data.Should().BeEquivalentTo(_mapper.Map<IList<AttachmentDto>>(responce));
        }

        [Fact]
        public async Task DownloadAttachmentAsync_ValidData_GetSuccess() 
        {
            //Arrange
            _accountRepositoryMock.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(_account);
            _attachmentRepositoryMock.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(_attachment);
            _blobServiceMock.Setup(x => x.GetUrl(_account.Avatar)).Returns("fff");

            _unitOfWorkMock.Setup(x => x.AccountRepository).Returns(_accountRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.AttachmentRepository).Returns(_attachmentRepositoryMock.Object);

            //Act
            var res = await _attachmentService.DownloadAttachmentAsync(1);

            res.Error.Should().BeNull();
            res.Data.FileName.Should().BeEquivalentTo("fff");
        }

        [Fact]
        public async Task DownloadAttachmentAsync_NoId_ReturnError()
        {
            //Arrange
            _accountRepositoryMock.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(_account);
            _blobServiceMock.Setup(x => x.GetUrl(_account.Avatar)).Returns("fff");

            _unitOfWorkMock.Setup(x => x.AccountRepository).Returns(_accountRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.AttachmentRepository).Returns(_attachmentRepositoryMock.Object);

            //Act
            var res = await _attachmentService.DownloadAttachmentAsync(1);

            res.Error.Code.Should().Be(ErrorCode.ValidationError);
            res.Error.Message.Should().Be("Attachement with id: 1 is not found");
        }

        [Fact]
        public async Task DeleteAttachmentAsync_ValidData_ReturnSuccess() 
        {
            _attachmentRepositoryMock.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(_attachment);
            _currentUserServiceMock.Setup(x => x.Role).Returns(UserRole.Admin);

            _unitOfWorkMock.Setup(x => x.AttachmentRepository).Returns(_attachmentRepositoryMock.Object);

            var res = await _attachmentService.DeleteAttachmentAsync(1);

            res.Error.Should().BeNull();
            res.Data.Should()
                .BeEquivalentTo(new AttachmentDto {
                    CreatedOn = res.Data.CreatedOn,
                    Id = 1,
                    ContainerName ="fff",
                    FileName = "fff"
                });
        }

        [Fact]
        public async Task DeleteAttachmentAsync_InvalidId_ReturnError()
        {
            _currentUserServiceMock.Setup(x => x.Role).Returns(UserRole.Admin);

            _unitOfWorkMock.Setup(x => x.AttachmentRepository).Returns(_attachmentRepositoryMock.Object);

            var res = await _attachmentService.DeleteAttachmentAsync(1);

            res.Error.Code.Should().Be(ErrorCode.ValidationError);
        }

        [Fact]
        public async Task DeleteAttachmentAsync_InvalidUser_ReturnError()
        {
            _attachmentRepositoryMock.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(_attachment);
            _currentUserServiceMock.Setup(x => x.Role).Returns(UserRole.Student);

            _unitOfWorkMock.Setup(x => x.AttachmentRepository).Returns(_attachmentRepositoryMock.Object);

            var res = await _attachmentService.DeleteAttachmentAsync(1);

            res.Error.Code.Should().Be(ErrorCode.NotFound);
            res.Error.Message.Should().Be("You cannot delete another user's data");
        }

        [Fact]
        public async Task AttachmentExtentionValidation_DangerousExtension_ReturnFalse() 
        {
            //Act & Assert
            _attachmentService.AttachmentExtentionValidation(_dangerousFile).Should().BeFalse();
        }

        [Fact]
        public async Task AttachmentsExtentionValidation_DangerousExtention_ReturnFalse() 
        {
            var formFiles = new FormFileCollection();
            formFiles.Add(_dangerousFile);

            var res = _attachmentService.AttachmentsExtentionValidation(formFiles);

            res.Should().Be(false);
        }

        [Fact]
        public async Task AttachmentSizeValidation_TooBigFile_ReturnFalse() 
        {

            var res = _attachmentService.AttachmentSizeValidation(_tooBigFile);

            res.Should().BeFalse();
        }

        [Fact]
        public async Task AttachmentsExtentionValidation_TooBigFile_ReturnFalse()
        {
            var formFiles = new FormFileCollection();
            formFiles.Add(_tooBigFile);

            var res = _attachmentService.AttachmentsSizeValidation(formFiles);

            res.Should().Be(false);
        }

        [Fact]
        public async Task ValidateAvatar_ValidData_ReturnTrue()
        {
            var res = _attachmentService.ValidateAvatar(_correctFile);

            res.Should().Be(true);
        }

        [Fact]
        public async Task ValidateAvatar_InValidData_ReturnFalse()
        {
            var fileMock = new Mock<IFormFile>();
            var content = "Hello World from a Fake File";
            var fileName = "test.pdf";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;
            fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
            fileMock.Setup(_ => _.FileName).Returns(fileName);
            fileMock.Setup(_ => _.Length).Returns(1);
            fileMock.Setup(_ => _.Name).Returns(fileName);
            var file = fileMock.Object;

            var res = _attachmentService.ValidateAvatar(file);

            res.Should().Be(false);
        }
    }
}
