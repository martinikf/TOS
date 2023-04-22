using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TOS.Models;

public class Notification
{
    public int NotificationId { get; set; }
    
    [StringLength(64, ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_NotificationNameMaxLength")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_NotificationSubjectRequired")]
    [StringLength(64, ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_SubjectMaxLength")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_NotificationTextRequired")]
    [StringLength(4096, ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_TextMaxLength")]
    public string Text { get; set; } = string.Empty;

    public virtual ICollection<UserSubscribedNotification> UserSubscribedNotifications { get; } = new HashSet<UserSubscribedNotification>();
    
    [NotMapped]
    public bool Selected { get; set; }
}