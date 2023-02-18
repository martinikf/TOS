using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TOS.Data;
using TOS.Models;

namespace TOS.Services;

public static class RoleHelper
{
    public static async Task<bool> AssignRoles(ApplicationUser user, Role role, ApplicationDbContext ctx)
    {
        var roles = new List<string>();
        
        switch (role)
        {
            case Role.Student:
                roles.Add("Student");
                
                roles.Add("ProposeTopic");
                roles.Add("InterestTopic");
                
                roles.Add("Comment");
                roles.Add("AnonymousComment");

                roles.Add("AssignedTopic");
                break;
            case Role.Teacher:
                roles.Add("Teacher");
                
                roles.Add("Topic");

                roles.Add("Comment");
                roles.Add("AnyComment");
                
                roles.Add("Group");

                roles.Add("SuperviseTopic");
                break;
            case Role.External:
                roles.Add("External");
                
                roles.Add("ProposeTopic");
                
                roles.Add("Comment");
                break;
            case Role.Administrator:
                roles.Add("Administrator");

                roles.Add("Topic");
                roles.Add("AnyTopic");

                roles.Add("Comment");
                roles.Add("AnyComment");
                
                roles.Add("Group");
                roles.Add("AnyGroup");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(role), role, null);
        }
        
        //Delete users current roles
        var userRoles = ctx.UserRoles.Where(x => x.UserId == user.Id);
        ctx.UserRoles.RemoveRange(userRoles);
        await ctx.SaveChangesAsync();
        
        //Add new roles
        foreach (var r in roles)
        {
            var rId = await ctx.Roles.FirstAsync(x => x.Name!.ToLower().Equals(r.ToLower()));
            if(ctx.UserRoles.Any(x => x.UserId == user.Id && x.RoleId == rId.Id))
                continue;
            
            var ur = new IdentityUserRole<int>
            {
                RoleId = rId.Id,
                UserId = user.Id
            };
            ctx.UserRoles.Add(ur);
            await ctx.SaveChangesAsync();
        }
        
        return true;
    }
}