using TOS.Models;

namespace TOS.ViewModels;

public class GroupViewModel
{
    public  IEnumerable<Topic> Topics { get; set; }
    
    public Group Group { get; set; }
    
    public IEnumerable<Programme> Programmes { get; set; }

    public string SelectedProgramme { get; set; }
    
    public string SearchString { get; set; }
    
    public string OrderBy { get; set; }
    
    public bool ShowTakenTopics { get; set; }
    public bool ShowProposedTopics { get; set; }
    public bool ShowHiddenTopics { get; set; }

}