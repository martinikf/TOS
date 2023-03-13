using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace TOS.Models;

public class ApplicationUserRole : IdentityUserRole<int>
{
  
    public virtual ApplicationUser User { get; set; } = null!;
   
    public virtual ApplicationRole Role { get; set; } = null!;
}