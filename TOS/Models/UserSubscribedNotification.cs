namespace TOS.Models;

public class UserSubscribedNotification
{
    public int UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;
    
    public int NotificationId { get; set; }
    public virtual Notification Notification { get; set; } = null!;
    
    //Possible to add language field, without it notification is sent in both
    
}