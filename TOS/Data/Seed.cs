﻿using Microsoft.AspNetCore.Identity;
using TOS.Models;
using TOS.Services;

namespace TOS.Data;

public static class Seed
{
    public static async void InitSeed(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetService<ApplicationDbContext>();
        if (ctx is null) return;
        
        CreateRole("Administrator", ctx);
        CreateRole("Teacher", ctx);
        CreateRole("Student", ctx);
        CreateRole("External", ctx);
        CreateRole("Topic", ctx);
        CreateRole("ProposeTopic", ctx);
        CreateRole("AnyTopic", ctx);

        CreateRole("Group", ctx);
        CreateRole("AnyGroup", ctx);
        
        CreateRole("Comment", ctx);
        CreateRole("AnonymousComment", ctx);
        CreateRole("AnyComment", ctx);

        CreateRole("Attachment", ctx);
        CreateRole("AnyAttachment", ctx);

        CreateRole("InterestTopic", ctx);
        CreateRole("AssignedTopic", ctx);
        CreateRole("SuperviseTopic", ctx);

        var adminUser = CreateUser("Admin", "User", "ADMIN", "admin@tos.tos", "admin@tos.tos", true, "password",
            ctx);
        await RoleHelper.AssignRoles(adminUser, Role.Administrator, ctx);
        
        CreateGroup("Nezařazeno", "Unassigned", adminUser, false, false, ctx);
        CreateGroup("Bakalářská", "Bachelor", adminUser, true, true, ctx);
        CreateGroup("Magisterská", "Master", adminUser, true, true, ctx);
        
        CreateNotification("TopicEdit", "Změna tématu", "Topic Change", "Téma bylo změněno.", "Topic was edited.", ctx);
        CreateNotification("TopicAssigned-Student", "Téma bylo přiřazeno Vám", "Topic was assigned to You", "TODO", "TODO", ctx);
        CreateNotification("TopicAssigned-Others", "Téma bylo přiřazeno", "Topic was assigned", "TODO", "TODO", ctx);
        CreateNotification("TopicAdopted", "Téma bylo přijato", "Topic was accepted", "TODO", "TODO", ctx);
        CreateNotification("CommentNew", "Nový komentář", "New comment", "[COMMENT]", "[COMMENT]", ctx);
        CreateNotification("NewInterest", "Někdo projevil zájem", "Someone is interested", "TODO", "TODO", ctx);
        CreateNotification("NewExternalUser", "TODO", "TODO", "TODO", "TODO", ctx);
    }

    public static async void DevSeed(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetService<ApplicationDbContext>();
        if (ctx is null) return;
        
        //Create Roles: Administrator, Teacher, Student, External
        CreateRole("Administrator", ctx);
        CreateRole("Teacher", ctx);
        CreateRole("Student", ctx);
        CreateRole("External", ctx);
        CreateRole("Topic", ctx);
        CreateRole("ProposeTopic", ctx);
        CreateRole("AnyTopic", ctx);

        CreateRole("Group", ctx);
        CreateRole("AnyGroup", ctx);
        
        CreateRole("Comment", ctx);
        CreateRole("AnonymousComment", ctx);
        CreateRole("AnyComment", ctx);

        CreateRole("Attachment", ctx);
        CreateRole("AnyAttachment", ctx);

        CreateRole("InterestTopic", ctx);
        CreateRole("AssignedTopic", ctx);
        CreateRole("SuperviseTopic", ctx);

        var adminUser = CreateUser("Admin", "User", "ADMIN", "admin@tos.tos", "admin@tos.tos", true, "password",
            ctx);
        await RoleHelper.AssignRoles(adminUser, Role.Administrator, ctx);
        await RoleHelper.AssignRoles(
            CreateUser("Eduard", "Bartl", "RNDr. Eduard Bartl, Ph.D.", "eduard.bartl@upol.c", "eduard.bartl@upol.c",
                true, "password", ctx), Role.Teacher, ctx);
        await RoleHelper.AssignRoles(
            CreateUser("Jan", "Outrata", "doc. Mgr. Jan Outrata, Ph.D.", "jan.outrata@upol.c", "jan.outrata@upol.c",
                true, "password", ctx), Role.Teacher, ctx);
        await RoleHelper.AssignRoles(
            CreateUser("Martin", "Trnečka", "RNDr. Martin Trnečka, Ph.D.", "martin.trnecka@upol.c",
                "martin.trnecka@upol.c", true, "password", ctx), Role.Teacher, ctx);
        await RoleHelper.AssignRoles(
            CreateUser("Radim", "Bělohlávek", "prof. RNDr. Radim Bělohlávek, DSc.", "radim.belohlavek@upol.c",
                "radim.belohlavek@upol.c", true, "passowrd", ctx), Role.Teacher, ctx);
        await RoleHelper.AssignRoles(
            CreateUser("Michal", "Krupka", "doc. RNDr. Michal Krupka, Ph.D.", "michal.krupka@upol.c",
                "michal.krupka@upol.c", true, "password", ctx), Role.Teacher, ctx);
        await RoleHelper.AssignRoles(
            CreateUser("Petr", "Jančar", "prof. RNDr. Petr Jančar, CSc.", "petr.jancar@upol.c",
                "petr.jancar@upol.c", true, "password", ctx), Role.Teacher, ctx);
        await RoleHelper.AssignRoles(
            CreateUser("Miroslav", "Kolařík", "doc. RNDr. Miroslav Kolařík, Ph.D.", "miroslova.kolarik@upol.c",
                "miroslova.kolarik@upol.c", true, "password", ctx), Role.Teacher, ctx);

        //create random students
        for (var i = 0; i < 50; i++)
        {
            var student = CreateUser("Student" + i, "Student" + i, null, "student" + i + "@student.tos",
                "student" + i + "@student.tos", true, "password", ctx);
            await RoleHelper.AssignRoles(student, Role.Student, ctx);
        }

        CreateGroup("Nezařazeno", "Unassigned", adminUser, false, false, ctx);
        CreateGroup("Bakalářská", "Bachelor", adminUser, true, true, ctx);
        CreateGroup("Magisterská", "Master", adminUser, true, true, ctx);

        //Create programmees
        //Bachelor
        CreateProgramme("Informační technologie", "Information Technology", true, ProgramType.Bachelor, ctx);
        CreateProgramme("Informatika", "Informatics", true, ProgramType.Bachelor, ctx);
        CreateProgramme("Informatika - Vývoj software", "Informatics - Software Development", true,
            ProgramType.Bachelor, ctx);
        CreateProgramme("Informatika - Obecná informatika", "Informatics - General Informatics", true,
            ProgramType.Bachelor, ctx);
        CreateProgramme("Informatika pro vzdělávání", "Informatics for education", true, ProgramType.Bachelor, ctx);
        //Master
        CreateProgramme("Obecná informatika", "General informatics", true, ProgramType.Master, ctx);
        CreateProgramme("Vývoj software", "Software Development", true, ProgramType.Master, ctx);
        CreateProgramme("Úmělá inteligence", "Artificial intelligence", true, ProgramType.Master, ctx);
        CreateProgramme("Počítačové systémy a technologie", "Computer systems and technologies", true,
            ProgramType.Master, ctx);
        CreateProgramme("Učitelství informatiky pro střední školy", "Teaching informatics for high schools", true,
            ProgramType.Master, ctx);

        
        CreateNotification("TopicEdit", "Změna tématu", "Topic Change", "Téma bylo změněno.", "Topic was edited.", ctx);
        CreateNotification("TopicAssigned-Student", "Téma bylo přiřazeno Vám", "Topic was assigned to You", "TODO", "TODO", ctx);
        CreateNotification("TopicAssigned-Others", "Téma bylo přiřazeno", "Topic was assigned", "TODO", "TODO", ctx);
        CreateNotification("TopicAdopted", "Téma bylo přijato", "Topic was accepted", "TODO", "TODO", ctx);
        CreateNotification("CommentNew", "Nový komentář", "New comment", "[COMMENT]", "[COMMENT]", ctx);
        CreateNotification("NewInterest", "Někdo projevil zájem", "Someone is interested", "TODO", "TODO", ctx);
        CreateNotification("NewExternalUser", "TODO", "TODO", "TODO", "TODO", ctx);
    }

    public static Notification CreateNotification(string name, string subject, string subjectEng, string text, string textEng, ApplicationDbContext ctx)
    {
        if (!ctx.Notifications.Any(x => x.Name.Equals(name)))
        {
            ctx.Notifications.Add(new Notification()
            {
                Name = name, 
                Subject = subject, 
                SubjectEng = subjectEng,
                Text = text,
                TextEng = textEng
            });
        }

        ctx.SaveChanges();
        return ctx.Notifications.First(x => x.Name.Equals(name));
    }

    public static ApplicationUser CreateUser(string firstname, string lastname, string? displayname, string email, string username, bool emailConfirmed, string? password, ApplicationDbContext ctx)
    {
        //Create usef if not exists
        var user = ctx.Users.FirstOrDefault(u => u.Email == email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                FirstName = firstname,
                LastName = lastname,
                DisplayName = displayname,
                Email = email,
                EmailConfirmed = emailConfirmed,
                UserName = username,
                NormalizedEmail = email.ToUpper(),
                NormalizedUserName = username.ToUpper()
            };
            if (password != null)
            {
                user.PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(user, password);
            }
            else
            {
                user.PasswordHash = null;
            }
            user.SecurityStamp = Guid.NewGuid().ToString("D");
            
            ctx.Users.Add(user);
            ctx.SaveChanges();
        }
        
        return user;
    }

    public static IdentityRole<int> CreateRole(string name, ApplicationDbContext ctx)
    {
        //Create new IdentityRole if not exists
        var role = ctx.Roles.FirstOrDefault(r => r.Name!.Equals(name));
        if (role == null)
        {
            role = new ApplicationRole()
            {
                Name = name,
                NormalizedName = name.ToUpper()
            };
            ctx.Roles.Add(role);
            ctx.SaveChanges();
        }
        
        return role;
    }
    
    public static IdentityUserRole<int> CreateUserRole(ApplicationUser user, IdentityRole<int> role, ApplicationDbContext ctx)
    {
       //Create new IdentityUserRole if not exists
            var userRole = ctx.UserRoles.FirstOrDefault(r => r.UserId.Equals(user.Id) && r.RoleId.Equals(role.Id));
            if (userRole == null)
            {
                userRole = new ApplicationUserRole()
                {
                    UserId = user.Id,
                    RoleId = role.Id
                };
                ctx.UserRoles.Add(userRole);
                ctx.SaveChanges();
            }
            
            return userRole;
    }

    public static Attachment CreateAttachment(string name, string? nameEng, string path, DateTime createdAt, ApplicationUser creator, Topic topic, ApplicationDbContext ctx)
    {
        //Create new Attachment if not exists
        var attachment = ctx.Attachments.FirstOrDefault(a =>
            a.Name.Equals(name) && a.Creator.Id.Equals(creator.Id) && a.Topic.TopicId.Equals(topic.TopicId));
        
        if (attachment == null)
        {
            
            if(nameEng == null)
            {
                nameEng = name;
            }
            
            attachment = new Attachment
            {
                Name = name,
                Creator = creator,
                Topic = topic
            };
            ctx.Attachments.Add(attachment);
            ctx.SaveChanges();
        }
        
        return attachment;
    }

    public static Comment CreateComment(string text, DateTime createAt, ApplicationUser author, Comment? parentComment, Topic topic, bool anonymous, ApplicationDbContext ctx)
    {
        //Create new Comment if not exists
        var comment = ctx.Comments.FirstOrDefault(c =>
            c.Text.Equals(text) && c.Author.Id.Equals(author.Id) && 
            (c.ParentComment == null && parentComment == null || (c.ParentComment != null && parentComment != null && c.ParentComment.CommentId.Equals(parentComment.CommentId)))
            && c.Topic.TopicId.Equals(topic.TopicId) && c.Anonymous.Equals(anonymous));

        if (comment == null)
        {
            comment = new Comment()
            {
                Text = text,
                CreatedAt = createAt,
                Author = author,
                ParentComment = parentComment,
                Topic = topic,
                Anonymous = anonymous
            };
            ctx.Comments.Add(comment);
            ctx.SaveChanges();
        }
        
        return comment;
    }

    public static Group CreateGroup(string name, string? nameEng, ApplicationUser creator, bool selectable, bool visible, ApplicationDbContext ctx)
    {
        //Create group if not exists
        var group = ctx.Groups.FirstOrDefault(g => g.Name.Equals(name) /*&& g.Creator.Id.Equals(creator.Id) && g.Selectable.Equals(selectable) && g.Visible.Equals(visible)*/);
        if (group == null)
        {
            if(nameEng == null)
            {
                nameEng = name;
            }
            
            group = new Group()
            {
                Name = name,
                NameEng = nameEng,
                Creator = creator,
                Selectable = selectable,
                Visible = visible
            };
            ctx.Groups.Add(group);
            ctx.SaveChanges();
        }

        return group;
    }

    public static Programme CreateProgramme(string name, string? nameEng, bool active, ProgramType type, ApplicationDbContext ctx)
    {
        //Create programme if not exists
        var programme = ctx.Programmes.FirstOrDefault(p => p.Name.Equals(name) && p.Type.Equals(type));
        if (programme == null)
        {
            if(nameEng == null)
            {
                nameEng = name;
            }
            
            programme = new Programme()
            {
                Name = name,
                NameEng = nameEng,
                Active = active,
                Type = type
            };
            ctx.Programmes.Add(programme);
            ctx.SaveChanges();
        }
        
        return programme;
    }

    public static Topic CreateTopic(string name, string? nameEng, string descriptionShort, string? descriptionShortEng, string? descriptionLong, string? descriptionLongEng, bool visible, ApplicationUser creator,
        ApplicationUser? supervisor, ApplicationUser? assignedStudent, Group group, ApplicationDbContext ctx)
    {
        //Crate new topic if not exists
        var topic = ctx.Topics.FirstOrDefault(t => t.Name.Equals(name));

        if (topic == null)
        {
            if(nameEng == null)
            {
                nameEng = name;
            }
            
            if(descriptionShortEng == null)
            {
                descriptionShortEng = descriptionShort;
            }
            
            if(descriptionLongEng == null)
            {
                descriptionLongEng = descriptionLong;
            }
            
            topic = new Topic()
            {
                Name = name,
                NameEng = nameEng,
                DescriptionShort = descriptionShort,
                DescriptionShortEng = descriptionShortEng,
                DescriptionLong = descriptionLong,
                DescriptionLongEng = descriptionLongEng,
                Visible = visible,
                Creator = creator,
                Supervisor = supervisor,
                AssignedStudent = assignedStudent,
                Group = group
            };
            ctx.Topics.Add(topic);
            ctx.SaveChanges();
        }
        
        return topic;
    }

    public static TopicRecommendedProgramme CreateTopicRecommendedProgramme(Programme program, Topic topic, ApplicationDbContext ctx)
    {
        //Create topic recommended programme if not exists
        var topicRecommendedProgramme = ctx.TopicRecommendedProgrammes.FirstOrDefault(t =>
            t.Programme.ProgrammeId.Equals(program.ProgrammeId) && t.Topic.TopicId.Equals(topic.TopicId));

        if (topicRecommendedProgramme == null)
        {
            topicRecommendedProgramme = new TopicRecommendedProgramme()
            {
                Programme = program,
                Topic = topic
            };
            ctx.TopicRecommendedProgrammes.Add(topicRecommendedProgramme);
            ctx.SaveChanges();
        }
        
        return topicRecommendedProgramme;
    }
    
    public static UserInterestedTopic CreateUserInterestedTopic(ApplicationUser user, Topic topic, DateTime dateTime, ApplicationDbContext ctx)
    {
        //Create user interested topic if not exists
        var userInterestedTopic = ctx.UserInterestedTopics.FirstOrDefault(u =>
            u.User.Id.Equals(user.Id) && u.Topic.TopicId.Equals(topic.TopicId));

        if (userInterestedTopic == null)
        {
            userInterestedTopic = new UserInterestedTopic()
            {
                User = user,
                Topic = topic,
                DateTime = dateTime
            };
            ctx.UserInterestedTopics.Add(userInterestedTopic);
            ctx.SaveChanges();
        }
        
        return userInterestedTopic;
    }

}