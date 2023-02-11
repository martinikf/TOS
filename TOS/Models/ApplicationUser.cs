using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace TOS.Models;

public class ApplicationUser : IdentityUser<int>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public string? DisplayName
    {
        get
        {
            if (DisplayName is null)
                return FirstName + " " + LastName;
            else return DisplayName;
        }
        set => DisplayName = value;
    }

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
    
}