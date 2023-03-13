﻿using TOS.Models;

namespace TOS.ViewModels;

public class GroupViewModel
{
    public  IEnumerable<Topic> Topics { get; set; } = null!;

    public Group Group { get; set; } = null!;

    public string SearchString { get; set; } = null!;

    public bool ShowTakenTopics { get; set; }
    public bool ShowProposedTopics { get; set; }
    public bool ShowHiddenTopics { get; set; }

}