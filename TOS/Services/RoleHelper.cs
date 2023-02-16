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
                roles.Add("CreateComment");
                roles.Add("CreateAnonymousComment");
                roles.Add("DeleteComment");
                roles.Add("AssignedToTopic");
                roles.Add("AssignedToGroup");
                roles.Add("SeeComments");
                break;
            case Role.Teacher:
                roles.Add("Teacher");
                roles.Add("CreateTopic");
                roles.Add("EditTopic");
                roles.Add("EditProposedTopic");
                roles.Add("DeleteTopic");
                roles.Add("DeleteProposedTopic");
                roles.Add("CreateComment");
                roles.Add("DeleteComment");
                roles.Add("DeleteAnyComment");
                roles.Add("CreateGroup");
                roles.Add("EditGroup");
                roles.Add("DeleteGroup");
                roles.Add("SupervisorToTopic");
                roles.Add("ShowHiddenGroups");
                roles.Add("SeeHiddenTopics");
                roles.Add("SeeProposedTopics");
                roles.Add("UploadAttachments");
                roles.Add("SeeComments");
                break;
            case Role.External:
                roles.Add("External");
                roles.Add("ProposeTopic");
                roles.Add("EditTopic");
                roles.Add("DeleteTopic");
                roles.Add("CreateComment");
                roles.Add("DeleteComment");
                roles.Add("UploadAttachments");
                roles.Add("SeeComments");
                break;
            case Role.Administrator:
                roles.Add("Administrator");
                roles.Add("CreateTopic");
                roles.Add("EditTopic");
                roles.Add("EditProposedTopic");
                roles.Add("EditAnyTopic");
                roles.Add("DeleteTopic");
                roles.Add("DeleteProposedTopic");
                roles.Add("DeleteAnyTopic");
                roles.Add("CreateComment");
                roles.Add("DeleteComment");
                roles.Add("DeleteAnyComment");
                roles.Add("CreateGroup");
                roles.Add("EditGroup");
                roles.Add("EditAnyGroup");
                roles.Add("DeleteGroup");
                roles.Add("DeleteAnyGroup");
                roles.Add("AssignRoles");
                roles.Add("SeeComments");
                roles.Add("CreateProgramme");
                roles.Add("EditProgramme");
                roles.Add("DeleteProgramme");
                roles.Add("ShowHiddenGroups");
                roles.Add("UploadAttachments");
                roles.Add("SeeProposedTopics");
                roles.Add("SeeHiddenTopics");

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