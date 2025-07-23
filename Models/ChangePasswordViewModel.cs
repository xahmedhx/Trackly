using System.ComponentModel.DataAnnotations;

namespace Trackly.Models;
public class ChangePasswordViewModel
{
    [Required]
    public string CurrentPassword { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; }

    [Required]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    [DataType(DataType.Password)]
    public string ConfirmNewPassword { get; set; }
}
