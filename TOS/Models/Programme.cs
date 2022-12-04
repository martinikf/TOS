using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TOS.Data;

namespace TOS.Models;

public class Programme
{
    [Key]
    public int ProgrammeId { get; set; }

    public string Name { get; set; } = string.Empty;
        
    public bool Active { get; set; }
        
    public ProgramType Type { get; set; }

    public virtual ICollection<TopicRecommendedProgramme> TopicRecommendedPrograms { get; } = new HashSet<TopicRecommendedProgramme>();

    [NotMapped] 
    public bool Selected { get; set; }
}