using TOS.Models;

namespace TOS.ViewModels;

public class MyTopicsViewModel
{
    public  IEnumerable<Topic> Topics { get; set; }
    
    public string SearchString { get; set; }
    public bool ShowProposedTopics { get; set; }
    public bool ShowHiddenTopics { get; set; }
}