using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TOS.Models;

public class Comment
{
    [Key]
    public int CommentId { get; set; }

    public string Text { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }

    public int AuthorId { get; set; }
    [ForeignKey("Comments")] 
    public virtual ApplicationUser Author { get; set; } = null!;
    
    public int? ParentCommentId { get; set; }
    [ForeignKey("ParentCommentId")]
    [InverseProperty("Replies")]
    public virtual Comment? ParentComment { get; set; }

    public virtual ICollection<Comment> Replies { get; } = new HashSet<Comment>();
    
    public int TopicId { get; set; }
    [ForeignKey("TopicId")]
    [InverseProperty("Comments")]
    public virtual Topic Topic { get; set; } = null!;
    
    public bool Anonymous { get; set; }

    [NotMapped] 
    public int Depth { get; set; } = 0;

}