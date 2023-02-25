using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;

namespace TOS.Models;

public class Group
{
    [Key]
    public int GroupId { get; set; }
    
    [Required(ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_GroupNameRequired")]
    public string Name { get; set; } = string.Empty;
    
    public string NameEng { get; set; } = string.Empty;
    
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
}