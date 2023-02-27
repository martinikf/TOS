using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.Build.Framework;

namespace TOS.Models;

public class ApplicationUser : IdentityUser<int>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    
    public string? DisplayName { get; set; }

        [InverseProperty("Creator")] 
    public virtual ICollection<Topic> CreatedTopics { get; } = new HashSet<Topic>();
    
    [InverseProperty("Supervisor")]
    public virtual ICollection<Topic> SupervisedTopics { get; } = new HashSet<Topic>();
    
    [InverseProperty("AssignedStudent")]
    public virtual ICollection<Topic> AssignedTopics { get; } = new HashSet<Topic>();
    
    [InverseProperty("Creator")]
    public virtual ICollection<Attachment> Attachments { get; } = new HashSet<Attachment>();

    [InverseProperty("Author")] 
    public virtual ICollection<Comment> Comments { get; } = new HashSet<Comment>();
    
    [InverseProperty("Creator")]
    public virtual ICollection<Group> CreatedGroups { get; } = new HashSet<Group>();
    
    public virtual ICollection<UserInterestedTopic> UserInterestedTopics { get; } = new HashSet<UserInterestedTopic>();

    public virtual ICollection<UserSubscribedNotification> UserSubscribedNotifications { get; } = new HashSet<UserSubscribedNotification>();
    
    public string GetDisplayName()
    {
        if (DisplayName == null)
            return FirstName + " " + LastName;
        return DisplayName;
    }
}