namespace TOS.Models;

public class UserInterestedTopic
{
    public int UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;
    
    public int TopicId { get; set; }
    public virtual Topic Topic { get; set; } = null!;
    
    public DateTime DateTime { get; set; }
}