using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
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
        var notification = await GetNotification("TopicEdit");
        
        //Prepare users to notify
        var users = new HashSet<ApplicationUser>();
        
        users.Add(topic.Creator);
        if (topic.AssignedStudent != null) users.Add(topic.AssignedStudent);
        if (topic.Supervisor != null) users.Add(topic.Supervisor);
        users.AddRange( topic.UserInterestedTopics.Select(x=>x.User));
        
        users.Remove(author);

        SendNotification(users, topic, notification, null, callbackUrl);
    }

    public async Task NewComment(Comment comment, string callbackUrl)
    {
        var topic = comment.Topic;
        var notification = await GetNotification("CommentNew");
        
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

        SendNotification(users, topic, notification, comment, callbackUrl);
    }

    public async Task TopicAssigned(Topic topic, ApplicationUser author, string callbackUrl)
    {
        var nStudent = await GetNotification("TopicAssigned-Student");
        var nOthers = await GetNotification("TopicAssigned-Others");
        
        //Prepare users to notify
        var users = new HashSet<ApplicationUser>();
        
        users.Add(topic.Creator);
        if (topic.Supervisor != null) users.Add(topic.Supervisor);
        users.AddRange( topic.UserInterestedTopics.Select(x=>x.User));
        
        users.Remove(author);
        users.Remove(topic.AssignedStudent!);

        SendNotification(new List<ApplicationUser>(){topic.AssignedStudent!}, topic, nStudent, null, callbackUrl);
        SendNotification(users.ToList(), topic, nOthers, null, callbackUrl);
    }

    public async Task TopicAdopted(Topic topic, string callbackUrl)
    {
        var notification = await GetNotification("TopicAdopted");
        
        //Prepare users to notify
        var users = new HashSet<ApplicationUser> {topic.Creator};

        SendNotification(users, topic, notification, null, callbackUrl);
    }

    public async Task NewInterest(Topic topic, ApplicationUser user, string callbackUrl)
    {
        var notification = await GetNotification("NewInterest");

        var users = topic.UserInterestedTopics.Select(x => x.User).ToList();
        if(topic.Supervisor != null)
            users.Add(topic.Supervisor);
        users.Add(topic.Creator);
        users.Remove(user);

        SendNotification(users, topic, notification, null, callbackUrl);
    }

    public async Task NewExternalUser(ApplicationUser user, string callbackUrl)
    {
        var notification = await GetNotification("NewExternalUser");
        
        var users = await _context.UserSubscribedNotifications.Where(x=>x.NotificationId == notification.NotificationId)
            .Select(x=>x.User).ToListAsync();

        string subject = $"{notification.Subject} - {notification.SubjectEng}";
        string body = $"{notification.Text}<br/><br/> <a href={callbackUrl}>{callbackUrl.Split("?")[0]}</a> <br/>{user.GetDisplayName()} - {user.Email}<br/><br/>{notification.TextEng}";
        
        await SendNotificationBulk(users, subject, body);
    }

    private async Task<Notification> GetNotification(string name)
    {
        var notification = await _context.Notifications.FirstOrDefaultAsync(x=>x.Name == name);
        if (notification is null) throw new Exception($"Database is not seeded. Notification {name} is missing.");
        
        return notification;
    }

    private void SendNotification(IEnumerable<ApplicationUser> users, Topic? topic, Notification notification, Comment? comment, string? callbackUrl)
    {
        var subject = $"{NotificationSubstitution(topic, notification.Subject, callbackUrl, comment)} - {NotificationSubstitution(topic, notification.SubjectEng, callbackUrl, comment)}";
        var body = $"{NotificationSubstitution(topic, notification.Text, callbackUrl, comment)}<br/>---<br/>{NotificationSubstitution(topic, notification.TextEng, callbackUrl, comment)}";

        users = users
            .Where(x => x.UserSubscribedNotifications.Any(y => y.NotificationId == notification.NotificationId))
            .ToList();
        
        Task.Factory.StartNew(() => SendNotificationBulk(users, subject, body));
    }

    private async Task SendNotificationBulk(IEnumerable<ApplicationUser> users, string subject, string body)
    {
        foreach (var u in users)
        {
            await _emailSender.SendEmailAsync(u.Email!, subject, body);
        }
    }
    
    private static string NotificationSubstitution(Topic? topic, string text, string? callbackUrl, Comment? comment)
    {
        var sb = new StringBuilder(text);
        
        if (topic != null)
        {
            var topicTypeCz = topic.Type switch
            {
                TopicType.Thesis => "Závěrečná práce",
                TopicType.Homework => "Úkol",
                TopicType.Project => "Projekt",
                _ => string.Empty
            };

            sb.Replace("\n", "<br/>");
            
            sb.Replace("[TOPIC_NAME]", topic.Name)
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
        }

        if (callbackUrl != null)
            sb.Replace("[URL]", $"<a href={callbackUrl}>{callbackUrl}</a>");
                
        
        if (comment != null)
        {
            //Danger: Simple regex to prevent xss to email
            sb.Replace("[COMMENT_AUTHOR]", comment.Author.GetDisplayName())
                .Replace("[COMMENT_AUTHOR_EMAIL]", comment.Author.Email)
                .Replace("[COMMENT_TEXT]", Regex.Replace(comment.Text, "<.*?>", string.Empty))
                .Replace("[COMMENT_DATE]", comment.CreatedAt.ToString(CultureInfo.GetCultureInfo("cs-CZ")));
        }

        return sb.ToString();
    }
}

public interface INotificationManager
{
    Task TopicEdit(Topic topic, ApplicationUser author, string callbackUrl);
    
    Task NewComment(Comment comment, string callbackUrl);
    
    Task TopicAssigned(Topic topic, ApplicationUser student, string callbackUrl);

    Task TopicAdopted(Topic topic, string callbackUrl);

    Task NewInterest(Topic topic, ApplicationUser user, string callbackUrl);

    Task NewExternalUser(ApplicationUser user, string callbackUrl);
}