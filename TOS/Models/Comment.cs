using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TOS.Models;

public class Comment
{
    [Key]
    public int CommentId { get; set; }

    [Required(ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_CommentTextRequired")]
    [MinLength(2, ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_CommentTextMinLength")]
    public string Text { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    public bool Anonymous { get; set; }

    public int AuthorId { get; set; }
    [ForeignKey("Comments")] 
    public virtual ApplicationUser Author { get; set; } = null!;
    
    public int? ParentCommentId { get; set; }
    [ForeignKey("ParentCommentId")]
    [InverseProperty("Replies")]
    public virtual Comment? ParentComment { get; set; }

    public int TopicId { get; set; }
    [ForeignKey("TopicId")]
    [InverseProperty("Comments")]
    public virtual Topic Topic { get; set; } = null!;

    public virtual ICollection<Comment> Replies { get; } = new HashSet<Comment>();

    [NotMapped] 
    public int Depth { get; set; }

}