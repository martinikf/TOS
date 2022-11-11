using System.ComponentModel.DataAnnotations;
using TOS.Data;

namespace TOS.Models;

public class Programme
{
    [Key]
    public int FieldOfStudyId { get; set; }

    public string Name { get; set; } = string.Empty;
        
    public bool Active { get; set; }
        
    public ProgramType Type { get; set; }

    public virtual ICollection<TopicRecommendedProgram> TopicRecommendedPrograms { get; } = new HashSet<TopicRecommendedProgram>();
}