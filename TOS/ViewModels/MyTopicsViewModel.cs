using TOS.Models;

namespace TOS.ViewModels;

public class MyTopicsViewModel
{
    public  IEnumerable<Topic> Topics { get; set; } = null!;
    
    public string SearchString { get; set; } = null!;
    public bool ShowHiddenTopics { get; set; }
}