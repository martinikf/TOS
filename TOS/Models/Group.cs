﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TOS.Models;

public class Group
{
    [Key]
    public int GroupId { get; set; }
    
    [Required(ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_GroupNameRequired")]
    [StringLength(64, ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_GroupNameMaxLength")]
    [MinLength(3, ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_GroupNameMinLength")]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(64, ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_GroupNameMaxLength")]
    [MinLength(3, ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_GroupNameMinLength")]
    public string NameEng { get; set; } = string.Empty;

    [StringLength(8192, ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_GroupDescriptionMaxLength")]
    public string? Description { get; set; } = string.Empty;
    
    [StringLength(8192, ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_GroupDescriptionMaxLength")]
    public string? DescriptionEng { get; set; } = string.Empty;
    
    public int CreatorId { get; set; }
    [ForeignKey("CreatorId")]
    [InverseProperty("CreatedGroups")]
    public virtual ApplicationUser Creator { get; set; } = null!;

    public bool Selectable { get; set; }
    
    public bool Visible { get; set; }

    [InverseProperty("Group")]
    public virtual ICollection<Topic> Topics { get; } = new HashSet<Topic>();
    
    [NotMapped]
    public bool Highlight { get; set; }
    
    //NameEng is never null, but it is not required in create/edit forms; to skip validation forms uses this property
    [NotMapped]
    [StringLength(64, ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_GroupNameMaxLength")]
    [MinLength(3, ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_GroupNameMinLength")]
    public string? NameEngNotMapped { get; set; } = string.Empty;
}