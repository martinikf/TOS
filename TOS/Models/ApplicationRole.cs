using Microsoft.AspNetCore.Identity;

namespace TOS.Models;

public class ApplicationRole : IdentityRole<int>
{
    public virtual ICollection<ApplicationUserRole> UserRoles { get; set; }
}