using System.ComponentModel.DataAnnotations;

namespace SneCafe.Models
{
    // One row of the Users table
    public class AppUser
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Username { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string Role { get; set; } = "Staff";
        public bool IsActive { get; set; } = true;
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Enter your username")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Enter your password")]
        public string Password { get; set; } = "";

        public string? ReturnUrl { get; set; }
    }

    // Used by the first-time Setup page and by "Add staff"
    public class NewStaffViewModel
    {
        [Required(ErrorMessage = "Enter the full name")]
        [StringLength(100)]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Enter a username")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be 3 to 50 characters")]
        [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "Use letters, numbers, dot, dash or underscore only")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Enter a password")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = "";

        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = "";

        public string Role { get; set; } = "Staff";
    }

    public class StaffPageViewModel
    {
        public List<AppUser> Users { get; set; } = new();
        public NewStaffViewModel Form { get; set; } = new();
    }
}