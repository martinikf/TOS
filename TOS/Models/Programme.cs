﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TOS.Data;

namespace TOS.Models;

public class Programme
{
    [Key]
    public int ProgrammeId { get; set; }

    [Required(ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_ProgrammeNameRequired")]
    [StringLength(64, ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_ProgrammeNameMaxLength")]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(64, ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_ProgrammeNameMaxLength")]
    public string? NameEng { get; set; } = string.Empty;
        
    public bool Active { get; set; }
        
    public ProgramType Type { get; set; }

    public virtual ICollection<TopicRecommendedProgramme> TopicRecommendedPrograms { get; } = new HashSet<TopicRecommendedProgramme>();

    [NotMapped] 
    public bool Selected { get; set; }
}