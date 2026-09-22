using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace RecruitmentProject.Data
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(50)]
        public string? MemberClass { get; set; }

        [StringLength(20)]
        public string? Classification { get; set; }

        [StringLength(100)]
        public string? Major { get; set; }

        [StringLength(100)]
        public string? Hometown { get; set; }

        [StringLength(260)]
        public string? ProfilePicturePath { get; set; }
    }
}
