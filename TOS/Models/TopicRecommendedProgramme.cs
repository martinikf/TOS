namespace TOS.Models;

public class TopicRecommendedProgramme
{
    public int ProgrammeId { get; set; }
    public virtual Programme Programme { get; set; } = null!;

    public int TopicId { get; set; }
    public virtual Topic Topic { get; set; } = null!;
}