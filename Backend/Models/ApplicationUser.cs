using Microsoft.AspNetCore.Identity;
namespace Backend.Models;
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    // Individual or Business
    public string AccountType { get; set; } = "Individual";

    public string Country { get; set; } = string.Empty;

    public bool IsEmailVerified { get; set; } = false;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
