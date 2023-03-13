﻿using System.Globalization;
using System.Text;
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

        await SendNotification(users, topic, notification, null, callbackUrl);
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

        await SendNotification(users, topic, notification, comment, callbackUrl);
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

        await SendNotification(new List<ApplicationUser>(){topic.AssignedStudent!}, topic, nStudent, null, callbackUrl);
        await SendNotification(users.ToList(), topic, nOthers, null, callbackUrl);
    }

    public async Task TopicAdopted(Topic topic, string callbackUrl)
    {
        var notification = await GetNotification("TopicAdopted");
        
        //Prepare users to notify
        var users = new HashSet<ApplicationUser> {topic.Creator};

        await SendNotification(users, topic, notification, null, callbackUrl);
    }

    public async Task NewInterest(Topic topic, ApplicationUser user, string callbackUrl)
    {
        var notification = await GetNotification("NewInterest");

        var users = topic.UserInterestedTopics.Select(x => x.User).ToList();
        if(topic.Supervisor != null)
            users.Add(topic.Supervisor);
        users.Add(topic.Creator);

        await SendNotification(users, topic, notification, null, callbackUrl);
    }

    public async Task NewExternalUser(ApplicationUser user)
    {
        var notification = await GetNotification("NewExternalUser");
        
        var users = await _context.UserSubscribedNotifications.Where(x=>x.NotificationId == notification.NotificationId).Select(x=>x.User).ToListAsync();

        await SendNotification(users, null, notification, null, null);
    }

    private async Task<Notification> GetNotification(string name)
    {
        var notification = await _context.Notifications.FirstOrDefaultAsync(x=>x.Name == name);
        if (notification is null) throw new Exception($"Database is not seeded. Notification {name} is missing.");
        
        return notification;
    }

    private async Task SendNotification(IEnumerable<ApplicationUser> users, Topic? topic, Notification notification, Comment? comment, string? callbackUrl)
    {
        var subject = $"{Parameterize(topic, notification.Subject, callbackUrl, comment)} - {Parameterize(topic, notification.SubjectEng, callbackUrl, comment)}";
        var body = $"{Parameterize(topic, notification.Text, callbackUrl, comment)}\n---\n{Parameterize(topic, notification.TextEng, callbackUrl, comment)}";
        
        //Foreach each user that is subscribed to this notification
        foreach (var u in users.Where(x => x.UserSubscribedNotifications.Any(y => y.NotificationId == notification.NotificationId)))
        {
            await _emailSender.SendEmailAsync(u.Email!, subject, body);
        }
    }
    
    private static string Parameterize(Topic? topic, string text, string? callbackUrl, Comment? comment)
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
            sb.Replace("[URL]", callbackUrl);
                
        
        if (comment != null)
        {
            sb.Replace("[COMMENT_AUTHOR]", comment.Author.GetDisplayName())
                .Replace("[COMMENT_AUTHOR_EMAIL]", comment.Author.Email)
                .Replace("[COMMENT_TEXT]", comment.Text)
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

    Task NewExternalUser(ApplicationUser user);
}