using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RecruitmentProject.Data;

namespace RecruitmentProject.Pages
{
    public class ProfileModel : PageModel
    {
        private static readonly string[] AllowedPictureExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProfileModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _webHostEnvironment = webHostEnvironment;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ProfilePictureUrl { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        public static readonly string[] ClassificationOptions =
        {
            "Freshman", "Sophomore", "Junior", "Senior", "Graduate"
        };

        public class InputModel
        {
            [Required]
            [StringLength(50, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
            [Display(Name = "Username")]
            public string Username { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Phone]
            [Display(Name = "Phone Number")]
            public string? PhoneNumber { get; set; }

            [StringLength(50)]
            [Display(Name = "Member Class")]
            public string? MemberClass { get; set; }

            [Display(Name = "Classification")]
            public string? Classification { get; set; }

            [StringLength(100)]
            [Display(Name = "Major")]
            public string? Major { get; set; }

            [StringLength(100)]
            [Display(Name = "Hometown")]
            public string? Hometown { get; set; }

            [Display(Name = "Profile Picture")]
            public IFormFile? ProfilePicture { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return NotFound();
            }

            Input = new InputModel
            {
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                MemberClass = user.MemberClass,
                Classification = user.Classification,
                Major = user.Major,
                Hometown = user.Hometown,
            };

            ProfilePictureUrl = user.ProfilePicturePath;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ProfilePictureUrl = user.ProfilePicturePath;
                return Page();
            }

            if (!string.Equals(user.UserName, Input.Username, StringComparison.Ordinal))
            {
                var setUsernameResult = await _userManager.SetUserNameAsync(user, Input.Username);
                if (!setUsernameResult.Succeeded)
                {
                    foreach (var error in setUsernameResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    ProfilePictureUrl = user.ProfilePicturePath;
                    return Page();
                }
            }

            if (!string.Equals(user.Email, Input.Email, StringComparison.OrdinalIgnoreCase))
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, Input.Email);
                if (!setEmailResult.Succeeded)
                {
                    foreach (var error in setEmailResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    ProfilePictureUrl = user.ProfilePicturePath;
                    return Page();
                }
            }

            if (!string.Equals(user.PhoneNumber, Input.PhoneNumber, StringComparison.Ordinal))
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    foreach (var error in setPhoneResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    ProfilePictureUrl = user.ProfilePicturePath;
                    return Page();
                }
            }

            user.MemberClass = Input.MemberClass;
            user.Classification = Input.Classification;
            user.Major = Input.Major;
            user.Hometown = Input.Hometown;

            if (Input.ProfilePicture is { Length: > 0 } picture)
            {
                var extension = Path.GetExtension(picture.FileName).ToLowerInvariant();
                if (!AllowedPictureExtensions.Contains(extension))
                {
                    ModelState.AddModelError(nameof(Input.ProfilePicture), "Please upload a valid image file (jpg, png, gif, or webp).");
                    ProfilePictureUrl = user.ProfilePicturePath;
                    return Page();
                }

                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "profile-pictures");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{user.Id}{extension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await picture.CopyToAsync(stream);
                }

                user.ProfilePicturePath = $"/uploads/profile-pictures/{fileName}";
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                ProfilePictureUrl = user.ProfilePicturePath;
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated.";
            return RedirectToPage();
        }
    }
}
