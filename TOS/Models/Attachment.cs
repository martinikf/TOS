﻿using System.ComponentModel.DataAnnotations.Schema;

namespace TOS.Models;

public class Attachment
{
    public int AttachmentId { get; set; }
    
    public string Name { get; set; } = string.Empty;

    public int CreatorId { get; set; }
    [ForeignKey("CreatorId")]
    [InverseProperty("Attachments")]
    public virtual ApplicationUser Creator { get; set; } = null!;
    
    public int TopicId { get; set; }
    [ForeignKey("TopicId")]
    [InverseProperty("Attachments")]
    public virtual Topic Topic { get; set; } = null!;
}