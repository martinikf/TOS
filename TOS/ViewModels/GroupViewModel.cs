namespace TOS.ViewModels;

public class GroupViewModel
{
    public  IEnumerable<Models.Topic> Topics { get; set; }
    
    public Models.Group Group { get; set; }
}