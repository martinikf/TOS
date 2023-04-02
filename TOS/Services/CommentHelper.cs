using Microsoft.EntityFrameworkCore;
using TOS.Data;
using TOS.Models;

namespace TOS.Services;

public static class CommentHelper
{
    public static async Task<bool> DeleteComment(Comment comment, ApplicationUser newAuthor, ApplicationDbContext context)
    {
        var topicId = comment.TopicId;
        var parent = comment.ParentComment;
            
        if (comment.Replies.Count > 0)
        {
            comment.Text = "Deleted comment";
            comment.Anonymous = true;
            comment.Author = newAuthor;
            comment.AuthorId = newAuthor.Id;
            context.Comments.Update(comment);
        }
        else
        {
            context.Comments.Remove(comment);
        }
            
        await context.SaveChangesAsync();
            
        if (parent != null && parent.Text == "Deleted comment")
        {
            return await DeleteComment(parent, newAuthor, context);
        }

        return true;
    }
}