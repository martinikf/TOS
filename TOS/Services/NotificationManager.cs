using System.Globalization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging;
using TOS.Data;
using TOS.Models;

namespace TOS.Services;

public class NotificationManager : INotificationManager
{
    private readonly IEmailSender _emailSender;
    private readonly ApplicationDbContext _context;
    
    public NotificationManager(IEmailSender emailSender, ApplicationDbContext context)
    {
        _emailSender = emailSender;
        _context = context;
    }
    
    public async Task TopicEdit(Topic topic, ApplicationUser author, string callbackUrl)
    {
        var notification = await _context.Notifications.FirstOrDefaultAsync(x=>x.Name == "TopicEdit");
        if (notification is null) throw new Exception("Database is not seeded");
        
        //Prepare users to notify
        var users = new HashSet<ApplicationUser>();
        
        users.Add(topic.Creator);
        if (topic.AssignedStudent != null) users.Add(topic.AssignedStudent);
        if (topic.Supervisor != null) users.Add(topic.Supervisor);
        users.AddRange( topic.UserInterestedTopics.Select(x=>x.User));
        
        users.Remove(author);
        users.RemoveWhere(u => u.UserSubscribedNotifications.Any(x => x.NotificationId == notification.NotificationId) == false);

        var subjectF = $"{Parameterize(topic, notification.Subject, callbackUrl, null)} - {Parameterize(topic, notification.SubjectEng, callbackUrl, null)}";
        var bodyF = $"{Parameterize(topic, notification.Text, callbackUrl, null)}\n---\n{Parameterize(topic, notification.TextEng, callbackUrl, null)}";
        
        foreach (var user in users)
        {
            await _emailSender.SendEmailAsync(user.Email!, subjectF, bodyF);
        }
    }

    public async Task NewComment(Comment comment, string callbackUrl)
    {
        var topic = comment.Topic;
        var notification = _context.Notifications.FirstOrDefault(x=>x.Name == "CommentNew");
        if (notification is null) throw new Exception("Database is not seeded");
        
        var users = new HashSet<ApplicationUser>();
        if (topic.AssignedStudent != null) users.Add(topic.AssignedStudent);
        if (topic.Supervisor != null) users.Add(topic.Supervisor);
        users.AddRange( topic.UserInterestedTopics.Select(x=>x.User));
        
        //Notify parent comment author
        if (comment.ParentComment != null && comment.ParentComment.Author != comment.Author)
        {
            users.Add(comment.ParentComment.Author);
        }
        
        users.Remove(comment.Author);
        users.RemoveWhere(u=>u.UserSubscribedNotifications.Any(x=>x.NotificationId == notification.NotificationId) == false);
        
        var subjectF = $"{Parameterize(comment.Topic, notification.Subject, callbackUrl, comment)} - {Parameterize(comment.Topic, notification.SubjectEng, callbackUrl, comment)}";
        var bodyF = $"{Parameterize(comment.Topic, notification.Text, callbackUrl, comment)}\n---\n{Parameterize(comment.Topic, notification.TextEng, callbackUrl, comment)}";
        
        foreach (var user in users)
        {
            await _emailSender.SendEmailAsync(user.Email!, subjectF, bodyF);
        }
    }

    public async Task TopicAssigned(Topic topic, ApplicationUser author, string callbackUrl)
    {
        var nStudent = await _context.Notifications.FirstOrDefaultAsync(x=>x.Name == "TopicAssigned-Student");
        var nOthers = await _context.Notifications.FirstOrDefaultAsync(x=>x.Name == "TopicAssigned-Others");
        if (nStudent is null || nOthers is null) throw new Exception("Database is not seeded");
        
        //Prepare users to notify
        var users = new HashSet<ApplicationUser>();
        
        users.Add(topic.Creator);
        users.Add(topic.AssignedStudent!);
        if (topic.Supervisor != null) users.Add(topic.Supervisor);
        users.AddRange( topic.UserInterestedTopics.Select(x=>x.User));
        
        users.Remove(author);
        users.RemoveWhere(u => u.UserSubscribedNotifications.Any(x => x.NotificationId == nOthers.NotificationId || x.NotificationId == nStudent.NotificationId) == false);

        var subjectStudent = $"{Parameterize(topic, nStudent.Subject, callbackUrl, null)} - {Parameterize(topic, nStudent.SubjectEng, callbackUrl, null)}";
        var bodyStudent = $"{ Parameterize(topic, nStudent.Text, callbackUrl, null)}\n---\n{Parameterize(topic, nStudent.TextEng, callbackUrl, null)}";
        
        var subjectOthers = $"{Parameterize(topic, nOthers.Subject, callbackUrl, null)} - {Parameterize(topic, nOthers.SubjectEng, callbackUrl, null)}";
        var bodyOthers = $"{Parameterize(topic, nOthers.Text, callbackUrl, null)}\n---\n{Parameterize(topic, nOthers.TextEng, callbackUrl, null)}";
        
        foreach (var user in users)
        {
            if (user == topic.AssignedStudent)
            {
                await _emailSender.SendEmailAsync(user.Email!, subjectStudent, bodyStudent);
            }
            else
            {
                await _emailSender.SendEmailAsync(user.Email!, subjectOthers, bodyOthers);
            }
        }
    }

    public async Task TopicAdopted(Topic topic, string callbackUrl)
    {
        var notification = await _context.Notifications.FirstOrDefaultAsync(x=>x.Name == "TopicAdopted");
        if (notification is null) throw new Exception("Database is not seeded");
        
        //Prepare users to notify
        var users = new HashSet<ApplicationUser>();
        
        users.Add(topic.Creator);
        if (topic.AssignedStudent != null) users.Add(topic.AssignedStudent);
        
        var subjectF = $"{Parameterize(topic, notification.Subject, callbackUrl, null)} - { Parameterize(topic, notification.SubjectEng, callbackUrl, null)}";
        var bodyF = $"{Parameterize(topic, notification.Text, callbackUrl, null)}\n---\n{Parameterize(topic, notification.TextEng, callbackUrl, null)}";
        
        foreach (var user in users)
        {
            await _emailSender.SendEmailAsync(user.Email!, subjectF, bodyF);
        }
    }

    public async Task NewInterest(Topic topic, ApplicationUser user, string callbackUrl)
    {
        var notification = await _context.Notifications.FirstOrDefaultAsync(x=>x.Name == "TopicInterest");
        if (notification is null) throw new Exception("Database is not seeded");

        var users = topic.UserInterestedTopics.Select(x => x.User).ToList();
        if(topic.Supervisor != null)
            users.Add(topic.Supervisor);
        users.Add(topic.Creator);
        
        var usersToRemove = await _context.Users.Where(x =>
            x.UserSubscribedNotifications.Any(y =>
                y.UserId == x.Id && y.NotificationId == notification.NotificationId) == false).ToListAsync();
        usersToRemove.ForEach(x=> users.Remove(x));
        
        var subject = $"{Parameterize(topic, notification.Subject, callbackUrl, null)} - {Parameterize(topic, notification.SubjectEng, callbackUrl, null)}";
        var body = $"{Parameterize(topic, notification.Text, callbackUrl, null)}\n---\n{Parameterize(topic, notification.TextEng, callbackUrl, null)}";
        
        foreach (var u in users)
        {
            await _emailSender.SendEmailAsync(u.Email!, subject, body);
        }
    }

    public async Task NewExternalUser(ApplicationUser user)
    {
        var notification = await _context.Notifications.FirstOrDefaultAsync(x=>x.Name == "NewExternalUser");
        if (notification is null) throw new Exception("Database is not seeded");
        
        var users = await _context.UserSubscribedNotifications.Where(x=>x.NotificationId == notification.NotificationId).Select(x=>x.User).ToListAsync();
        
        var subject = $"{Parameterize(null, notification.Subject, null, null)} - {Parameterize(null, notification.SubjectEng, null, null)}";
        var body = $"{Parameterize(null, notification.Text, null, null)}\n---\n{Parameterize(null, notification.TextEng, null, null)}";
        
        foreach (var u in users)
        {
            await _emailSender.SendEmailAsync(u.Email!, subject, body);
        }
    }
    
    private static string Parameterize(Topic topic, string text, string callbackUrl, Comment? comment)
    {
        var topicTypeCz = string.Empty;
        switch (topic.Type)
        {
            case TopicType.Thesis:
                topicTypeCz = "Závěrečná práce";
                break;
            case TopicType.Homework:
                topicTypeCz = "Domácí úkol";
                break;
            case TopicType.Project:
                topicTypeCz = "Projekt";
                break;
        }
        
        var returnString =
            text.Replace("[URL]", callbackUrl)
                .Replace("[TOPIC_NAME]", topic.Name)
                .Replace("[TOPIC_NAME_ENG]", topic.NameEng)
                .Replace("[TOPIC_SHORT_DESCRIPTION]", topic.DescriptionShort)
                .Replace("[TOPIC_SHORT_DESCRIPTION_ENG]", topic.DescriptionShortEng)
                .Replace("[TOPIC_LONG_DESCRIPTION]", topic.DescriptionLong)
                .Replace("[TOPIC_LONG_DESCRIPTION_ENG]", topic.DescriptionLongEng)
                .Replace("[TOPIC_CREATOR]", topic.Creator.GetDisplayName())
                .Replace("[TOPIC_CREATOR_EMAIL]", topic.Creator.Email)
                .Replace("[TOPIC_SUPERVISOR]", topic.Supervisor?.GetDisplayName() ?? "-")
                .Replace("[TOPIC_SUPERVISOR_EMAIL]", topic.Supervisor?.Email ?? "-")
                .Replace("[TOPIC_ASSIGNED]", topic.AssignedStudent?.GetDisplayName() ?? "-")
                .Replace("[TOPIC_ASSIGNED_EMAIL]", topic.AssignedStudent?.Email ?? "-")
                .Replace("[TOPIC_TYPE]", topicTypeCz)
                .Replace("[TOPIC_TYPE_ENG]", topic.Type.ToString());
        
        if (comment != null)
        {
            returnString = returnString
                .Replace("[COMMENT_AUTHOR]", comment.Author.GetDisplayName())
                .Replace("[COMMENT_AUTHOR_EMAIL]", comment.Author.Email)
                .Replace("[COMMENT_TEXT]", comment.Text)
                .Replace("[COMMENT_DATE]", comment.CreatedAt.ToString(CultureInfo.GetCultureInfo("cs-CZ")));
        }

        return returnString;
    }
    
}

public interface INotificationManager
{
    Task TopicEdit(Topic topic, ApplicationUser author, string callbackUrl);
    
    Task NewComment(Comment comment, string callbackUrl);
    
    Task TopicAssigned(Topic topic, ApplicationUser student, string callbackUrl);

    Task TopicAdopted(Topic topic, string callbackUrl);

    Task NewInterest(Topic topic, ApplicationUser user, string callbackUrl);

    Task NewExternalUser(ApplicationUser user);
}