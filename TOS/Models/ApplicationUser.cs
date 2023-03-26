using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace TOS.Models;

public class ApplicationUser : IdentityUser<int>
{
    [Required(ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_FirstNameRequired")]
    [StringLength(64, ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_FirstNameMaxLength")]
    public string FirstName { get; set; } = string.Empty;
    
    [Required(ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_LastNameRequired")]
    [StringLength(64, ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_LastNameMaxLength")]
    public string LastName { get; set; } = string.Empty;
    
    [StringLength(256, ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_DisplayNameMaxLength")]
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

    public virtual ICollection<ApplicationUserRole> UserRoles { get; } = new HashSet<ApplicationUserRole>();

    public string GetDisplayName()
    {
        if (DisplayName == null)
            return FirstName + " " + LastName;
        return DisplayName;
    }
}