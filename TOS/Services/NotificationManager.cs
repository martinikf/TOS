using Microsoft.AspNetCore.Identity.UI.Services;
using NuGet.Packaging;
using TOS.Models;

namespace TOS.Services;

public class NotificationManager : INotificationManager
{
    IEmailSender _emailSender;

    public NotificationManager(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }
    
    public void TopicEdit(Topic topic, ApplicationUser user)
    {
        foreach (var u in SelectUsers(topic, user))
        {
            _emailSender.SendEmailAsync(u.Email, "Téma změněno - Topic edited",
                "Téma " + topic.Name + " bylo změněno." + "\n---\n" + "Topic " + topic.NameEng + " was edited");
        }
    }

    public void NewComment(Topic topic, Comment comment)
    {
        var users = SelectUsers(topic, comment.Author);
        //If comment is a reply, notify the author of the parent comment
        if (comment.ParentComment != null)
        {
            if(comment.Author != comment.ParentComment.Author)
                users.Add(comment.ParentComment.Author);
        }
        
        foreach(var u in users)
        {
            _emailSender.SendEmailAsync(u.Email, "Nový komentář - New comment", "Byl přidán nový komentář k tématu " + topic.Name + "." + "\n---\n" + "New comment was added to topic " + topic.NameEng + ".");
        }
    }

    public void TopicAssigned(Topic topic, ApplicationUser author)
    {
        foreach (var u in SelectUsers(topic, author))
        {
            if(topic.AssignedStudent != u)
                _emailSender.SendEmailAsync(u.Email, "Téma přiřazeno - Topic assigned", "Téma " + topic.Name + " bylo přiřazeno studentovi." + "\n---\n" + "Topic " + topic.NameEng + " was assigned to student.");
            else
                _emailSender.SendEmailAsync(u.Email, "Téma přiřazeno - Topic assigned", "Téma " + topic.Name + " bylo přiřazeno vám." + "\n---\n" + "Topic " + topic.NameEng + " was assigned to you.");
        }
    }

    private ICollection<ApplicationUser> SelectUsers(Topic topic, ApplicationUser author)
    {
        var users = new HashSet<ApplicationUser>();
        
        users.Add(topic.Creator);
        if(topic.Supervisor != null)
            users.Add(topic.Supervisor);
        if(topic.AssignedStudent != null)
            users.Add(topic.AssignedStudent);
        users.AddRange(topic.UserInterestedTopics.Select(x=> x.User));

        //Remove author of change
        users.Remove(author);
        
        return users;
    }
}

public interface INotificationManager
{
    void TopicEdit(Topic topic, ApplicationUser user);
    
    //Notify interested users, supervisor, owner, assigned, parent comment author about new comment. Do not notify the author of the comment.
    void NewComment(Topic topic, Comment comment);
    
    void TopicAssigned(Topic topic, ApplicationUser author);
}