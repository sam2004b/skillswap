
using Backend.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Backend.Data;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(
        Microsoft.EntityFrameworkCore.DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
}
