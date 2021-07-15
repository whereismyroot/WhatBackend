using System.IO;
using AutoMapper;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using CharlieBackend.Core.Entities;
using CharlieBackend.Core.DTO.Attachment;
using CharlieBackend.Core.Models.ResultModel;
using CharlieBackend.Business.Services.Interfaces;
using CharlieBackend.Data.Repositories.Impl.Interfaces;
using System.Linq;


namespace CharlieBackend.Business.Services
{
    public class AttachmentService : IAttachmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IBlobService _blobService;
        private readonly ICurrentUserService _currentUserService;

        public AttachmentService( 
                             IUnitOfWork unitOfWork,
                             IMapper mapper,
                             IBlobService blobService,
                             ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _blobService = blobService;
            _currentUserService = currentUserService;
        }

        public async Task<Result<IList<AttachmentDto>>> AddAttachmentsAsync(IFormFileCollection fileCollection, bool isPublic = false)
        {
            if (!AttachmentsSizeValidation(fileCollection))
            {
                return Result<IList<AttachmentDto>>.GetError(ErrorCode.ValidationError,
                            "Files are too big, max size is 50 MB");
            }

            if (!AttachmentsExtentionValidation(fileCollection))
            {
                return Result<IList<AttachmentDto>>.GetError(ErrorCode.ValidationError,
                           "Some of files have dengerous extention");
            }

            IList<Attachment> attachments = new List<Attachment>();

            foreach (var file in fileCollection)
            {
                var attachment = await AddAttachmentFileAsync(file, isPublic);

                attachments.Add(attachment);
            }

            await _unitOfWork.CommitAsync();

            return Result<IList<AttachmentDto>>.GetSuccess(_mapper.
                        Map<IList<AttachmentDto>>(attachments));
                
        }

        public async Task<Result<AttachmentDto>> AddAttachmentAsync(IFormFile file, bool isPublic = false)
        {
            if (!AttachmentSizeValidation(file))
            {
                return Result<AttachmentDto>.GetError(ErrorCode.ValidationError,
                            "File is too big, max size is 50 MB");
            }

            if (!AttachmentExtentionValidation(file))
            {
                return Result<AttachmentDto>.GetError(ErrorCode.ValidationError,
                           "File has dengerous extention");
            }

            var attachment = await AddAttachmentFileAsync(file, isPublic);

            await _unitOfWork.CommitAsync();

            return Result<AttachmentDto>.GetSuccess(_mapper.Map<AttachmentDto>(attachment));
        }

        public async Task<Result<AttachmentDto>> AddAttachmentAsAvatarAsync(IFormFile file)
        {
            if (!ValidateAvatar(file))
                return Result<AttachmentDto>.GetError(ErrorCode.Conflict, "File has inappropriate extension.");

            var account = await _unitOfWork.AccountRepository.GetByIdAsync(_currentUserService.AccountId);

            if (account != null)
            {
                if(account.AvatarId.HasValue)
                {
                    await DeleteAttachmentAsync(account.AvatarId.Value);
                }

                var attachment = await AddAttachmentAsync(file, true);

                account.AvatarId = attachment.Data.Id;

                await _unitOfWork.CommitAsync();

                return attachment;
            }
            else
                return Result<AttachmentDto>.GetError(ErrorCode.NotFound, "Account not found");
        }

        public async Task<Result<string>> GetAvatarUrl()
        {
            var account = await _unitOfWork.AccountRepository.GetByIdAsync(_currentUserService.AccountId);

            if (account.Avatar != null)
                return Result<string>.GetSuccess(_blobService.GetUrl(account.Avatar));
            else
                return Result<string>.GetError(ErrorCode.Conflict, "Account doesn't have avatar.");
        }

        public async Task<Result<string>> GetAttachmentUrl(long id)
        {
            var attachment = await _unitOfWork.AttachmentRepository.GetByIdAsync(id);

            return attachment == null ?
                 Result<string>.GetError(ErrorCode.NotFound, "Attachment not found") :
                 Result<string>.GetSuccess(_blobService.GetUrl(attachment));
        }

        private async Task<Attachment> AddAttachmentFileAsync(IFormFile file, bool isPublic = false)
        {
            using Stream uploadFileStream = file.OpenReadStream();

            var blob = await _blobService.UploadAsync(file.FileName, uploadFileStream, isPublic);

            Attachment attachment = new Attachment()
            {
                CreatedByAccountId = _currentUserService.AccountId,
                ContainerName = blob.BlobContainerName,
                FileName = file.FileName
            };

            _unitOfWork.AttachmentRepository.Add(attachment);

            return attachment;
        }

        public async Task<Result<IList<AttachmentDto>>> GetAttachmentsListAsync(AttachmentRequestDto request)
        {
            string error = ValidateAttachmentRequest(request);

            if (error != null)
            {
                return Result<IList<AttachmentDto>>.GetError(ErrorCode.ValidationError, error);
            }

            switch (_currentUserService.Role)
            {
                case UserRole.Student:
                    request.StudentAccountID = _currentUserService.EntityId;
                    break;
                case UserRole.Mentor:
                    request.MentorID = _currentUserService.EntityId;
                    break;
                case UserRole.Secretary:
                case UserRole.Admin:
                    break;
                case UserRole.NotAssigned:
                default:
                    throw new InvalidDataException($"Provided role {_currentUserService.Role} is not supported");
            }

            var result = await _unitOfWork.AttachmentRepository.GetAttachmentListFiltered(request);

            return Result<IList<AttachmentDto>>.GetSuccess(_mapper.Map<IList<AttachmentDto>>(result));
        }

        public async Task<Result<DownloadAttachmentDto>> DownloadAttachmentAsync(long attachmentId)
        {
            var attachment = await _unitOfWork.AttachmentRepository.GetByIdAsync(attachmentId);

            if (attachment == null)
            {
                return Result<DownloadAttachmentDto>.GetError(ErrorCode.ValidationError,
                     "Attachement with id: " + attachmentId + " is not found");
            }

            var download = await _blobService.DownloadAsync(attachment.ContainerName, attachment.FileName);

            DownloadAttachmentDto downloadedAttachment = new DownloadAttachmentDto()
                        { 
                           DownloadInfo = download,
                           FileName = attachment.FileName
                        };

            return Result<DownloadAttachmentDto>.GetSuccess(downloadedAttachment);
        }

        public async Task<Result<AttachmentDto>> DeleteAttachmentAsync(long attachmentId)
        {
            var attachment = await _unitOfWork.AttachmentRepository.GetByIdAsync(attachmentId);

            if (attachment == null)
            {
                return Result<AttachmentDto>.GetError(ErrorCode.ValidationError,
                     "Attachement with id: " + attachmentId + " is not found");
            }
            var role = _currentUserService.Role;

            if ( (attachment.CreatedByAccountId == _currentUserService.AccountId) || (role == UserRole.Admin))
            {
                await _blobService.DeleteAsync(attachment.ContainerName);
                await _unitOfWork.AttachmentRepository.DeleteAsync(attachmentId);
                await _unitOfWork.CommitAsync();
            }
            else
            {
                return Result<AttachmentDto>.GetError(ErrorCode.NotFound, "You cannot delete another user's data");
            }

            return Result<AttachmentDto>.GetSuccess(_mapper.Map<AttachmentDto>(attachment));
        }

        public bool AttachmentExtentionValidation(IFormFile file)
        {
            foreach (var extention in DangerousExtentions)
            {
                if (file.FileName.ToLower().EndsWith(extention))
                {
                    return false;
                }
            }

            return true;
        }

        public bool AttachmentsExtentionValidation(IFormFileCollection fileCollection)
        {
            foreach (var file in fileCollection)
            {
                return AttachmentExtentionValidation(file);
            }

            return true;
        }

        public bool AttachmentSizeValidation(IFormFile file)
        {
            long currentSize = 0;

            currentSize += file.Length;

            if (currentSize <= FileMaxSize)
            {
                return true;
            }

            return false;
        }

        public bool AttachmentsSizeValidation(IFormFileCollection fileCollection)
        {
            long currentSize = 0;

            foreach (var file in fileCollection)
            {
                currentSize += file.Length;
            }

            if (currentSize <= FileMaxSize)
            {
                return true;
            }

            return false;
        }

        private string ValidateAttachmentRequest(AttachmentRequestDto request)
        {

            if (request == default)
            {
                return "RequestDto is null";
            }

            return null;
        }

        public bool ValidateAvatar(IFormFile file)
        {
            foreach (var extention in AvatarExtentions)
            {
                if (file.FileName.ToLower().EndsWith(extention))
                {
                    return true;
                }
            }

            return false;
        }

        public readonly string[] AvatarExtentions =
            {
                ".png", ".jpg", ".jped", ".gif", ".svg", ".bmp"
            };

        public readonly string[] DangerousExtentions =
            {
                ".exe",".pif",".application",".gadget",".msi",".msp",".com",
                ".scr",".hta",".cpl",".msc",".jar",".bat",".cmd",".vb",".vbs",
                ".vbe",".js",".jse",".ws",".wsf",".wsc",".wsh",".ps1",".ps1xml",
                ".ps2",".ps2xml",".psc1",".psc2",".msh",".msh1",".msh2",".mshxml",
                ".msh1xml",".msh2xml",".scf",".lnk",".inf",".reg",".doc",".xls",
                ".ppt",".docm",".dotm",".xlsm",".xltm",".xlam",".pptm",".potm",
                ".ppam",".ppsm",".sldm",".dll"
            };

        public const int FileMaxSize = 52428800;
    }
}
