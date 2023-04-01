using Microsoft.EntityFrameworkCore;
using TOS.Data;
using TOS.Models;

namespace TOS.Services;

public class CommentHelper
{
    private readonly ApplicationDbContext _context;
    
    public CommentHelper(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> DeleteComment(Comment comment, ApplicationUser newAuthor)
    {
        var topicId = comment.TopicId;
        var parent = comment.ParentComment;
            
        if (comment.Replies.Count > 0)
        {
            comment.Text = "Deleted comment";
            comment.Anonymous = true;
            comment.Author = newAuthor;
            comment.AuthorId = newAuthor.Id;
            _context.Comments.Update(comment);
        }
        else
        {
            _context.Comments.Remove(comment);
        }
            
        await _context.SaveChangesAsync();
            
        if (parent != null && parent.Text == "Deleted comment")
        {
            return await DeleteComment(parent, newAuthor);
        }

        return true;
    }
}