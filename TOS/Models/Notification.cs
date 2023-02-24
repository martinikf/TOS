using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TOS.Models;

public class Notification
{
    public int NotificationId { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_NotificationSubjectRequired")]
    public string Subject { get; set; } = string.Empty;
    public string SubjectEng { get; set; } = string.Empty;
    
    [Required(ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_NotificationTextRequired")]
    public string Text { get; set; } = string.Empty;
    public string TextEng { get; set; } = string.Empty;

    public virtual ICollection<UserSubscribedNotification> UserSubscribedNotifications { get; } = new HashSet<UserSubscribedNotification>();
    
    [NotMapped]
    public bool Selected { get; set; }
}