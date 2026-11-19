using System.ComponentModel.DataAnnotations;
using itoscon.Services;

namespace itoscon.Models;

public class LoginViewModel
{
    [Display(Name = "field.email")]
    [LRequired(ErrorMessage = "err.emailRequired")]
    [LEmail(ErrorMessage = "err.emailFormat")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "field.password")]
    [LRequired(ErrorMessage = "err.passwordRequired")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}