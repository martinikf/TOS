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
    
    public Task TopicEdit(Topic topic, ApplicationUser user)
    {
        var users = SelectUsers(topic, user);
        if (users.Count == 0) return Task.CompletedTask;
        
        foreach (var u in users)
        {
            _emailSender.SendEmailAsync(u.Email!, "Téma změněno - Topic edited",
                "Téma " + topic.Name + " bylo změněno." + "\n---\n" + "Topic " + topic.NameEng + " was edited");
        }
        
        return Task.CompletedTask;
    }

    public Task NewComment(Comment comment)
    {
        var topic = comment.Topic;
        var users = SelectUsers(topic, comment.Author);
       
        //If comment is a reply, notify the author of the parent comment
        if (comment.ParentComment != null)
        {
            if(comment.Author != comment.ParentComment.Author)
                users.Add(comment.ParentComment.Author);
        }
        
        foreach(var u in users)
        {
            _emailSender.SendEmailAsync(u.Email!, "Nový komentář - New comment", "Byl přidán nový komentář k tématu " + topic.Name + "." + "\n---\n" + "New comment was added to topic " + topic.NameEng + ".");
        }
        
        return Task.CompletedTask;
    }

    public Task TopicAssigned(Topic topic, ApplicationUser student)
    {
        if (topic.AssignedStudent == null)
        {
            topic.AssignedStudent = student;
        }
        
        foreach (var u in topic.UserInterestedTopics.Select(x=>x.User).Union(new[] {topic.AssignedStudent}))
        {
            if(topic.AssignedStudent != u)
                _emailSender.SendEmailAsync(u.Email!, "Téma přiřazeno - Topic assigned", "Téma " + topic.Name + " bylo přiřazeno studentovi." + "\n---\n" + "Topic " + topic.NameEng + " was assigned to student.");
            else
                _emailSender.SendEmailAsync(u.Email!, "Téma přiřazeno - Topic assigned", "Téma " + topic.Name + " bylo přiřazeno vám." + "\n---\n" + "Topic " + topic.NameEng + " was assigned to you.");
        }
        return Task.CompletedTask;
    }

    public Task TopicAdopted(Topic topic)
    {
        var userEmail = topic.Creator.Email!;
        
        _emailSender.SendEmailAsync(userEmail, "Téma přijato - Topic adopted", "Téma " + topic.Name + " bylo přijato." + "\n---\n" + "Topic " + topic.NameEng + " was adopted.");

        return Task.CompletedTask;
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
    Task TopicEdit(Topic topic, ApplicationUser user);
    
    //Notify interested users, supervisor, owner, assigned, parent comment author about new comment. Do not notify the author of the comment.
    Task NewComment(Comment comment);
    
    Task TopicAssigned(Topic topic, ApplicationUser student);

    Task TopicAdopted(Topic topic);
}