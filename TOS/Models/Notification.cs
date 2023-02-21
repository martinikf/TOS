using System.ComponentModel.DataAnnotations.Schema;

namespace TOS.Models;

public class Notification
{
    public int NotificationId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string SubjectEng { get; set; } = string.Empty;
    
    public string Text { get; set; } = string.Empty;
    public string TextEng { get; set; } = string.Empty;

    public virtual ICollection<UserSubscribedNotification> UserSubscribedNotifications { get; } = new HashSet<UserSubscribedNotification>();
    
    [NotMapped]
    public bool Selected { get; set; }
}