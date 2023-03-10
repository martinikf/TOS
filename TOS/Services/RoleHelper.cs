using Microsoft.EntityFrameworkCore;
using TOS.Data;
using TOS.Models;

namespace TOS.Services;

public static class RoleHelper
{
    public static async Task AssignRoles(ApplicationUser user, Role role, ApplicationDbContext ctx)
    {
        var roles = new List<string>();
        switch (role)
        {
            case Role.Administrator:
                roles.AddRange(new [] {"Administrator", "Topic", "AnyTopic", "Comment", "AnyComment", "Attachment","AnyAttachment", "Group", "AnyGroup"});
                break;
            case Role.Teacher:
                roles.AddRange(new[] {"Teacher", "Topic", "Comment", "AnyComment", "Attachment", "Group", "SuperviseTopic"});
                break;
            case Role.Student:
                roles.AddRange(new[] {"Student", "ProposeTopic", "InterestTopic", "Comment", "AnonymousComment", "AssignedTopic"});
                break;
            case Role.External:
                roles.AddRange(new [] {"External", "ProposeTopic", "Comment", "Attachment"});
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(role), role, null);
        }
        
        //Delete users current roles
        var userRoles = ctx.UserRoles.Where(x => x.UserId == user.Id).ToListAsync();
        ctx.UserRoles.RemoveRange(await userRoles);
        await ctx.SaveChangesAsync();

        var roleIds = await ctx.Roles
            .Where(x => roles.Contains(x.Name!))
            .Select(y=>y.Id).ToListAsync();
        
        foreach (var roleId in roleIds)
        {
            var ur = new ApplicationUserRole()
            {
                RoleId = roleId,
                UserId = user.Id
            };
            ctx.UserRoles.Add(ur);
        }
        await ctx.SaveChangesAsync();
    }
}