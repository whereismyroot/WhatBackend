using CharlieBackend.Core.Entities;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace CharlieBackend.Core.DTO.Account
{
    public class AccountDto
    {
        [Required]
        public long Id { get; set; }

        [Required]
        [StringLength(30)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(30)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(50)]
        public string Email { get; set; }

        [EnumDataType(typeof(UserRole))]
        public UserRole Role { get; set; }

        [Required]
        public bool IsActive { get; set; }

        public int AttachmentId { get; set; }
    }
}
