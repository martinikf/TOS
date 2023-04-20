using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

        await ctx.Database.MigrateAsync();
        
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

        CreateRole("InterestTopic", ctx);
        CreateRole("AssignedTopic", ctx);
        CreateRole("SuperviseTopic", ctx);

        var adminUser = CreateUser("Admin", "User", "ADMIN", "admin@upol.c", "admin@upol.c", true, "password",
            ctx);
        await RoleHelper.AssignRoles(adminUser, Role.Administrator, ctx);
        
        CreateGroup("Nezařazeno", "Unassigned", adminUser, false, false, ctx);
        CreateGroup("Bakalářská", "Bachelor", adminUser, true, true, ctx);
        CreateGroup("Diplomová", "Master", adminUser, true, true, ctx);
        
        CreateNotification("TopicEdit", "Změna tématu", "Topic Change", "Téma, [TOPIC_NAME], bylo změněno.", "Topic, [TOPIC_NAME_ENG], was edited.", ctx);
        CreateNotification("TopicAssigned-Student", "Téma bylo přiřazeno Vám", "Topic was assigned to You", "Téma, [TOPIC_NAME], bylo přiřazeno Vám.", "Topic, [TOPIC_NAME_ENG], was assigned to You.", ctx);
        CreateNotification("TopicAssigned-Others", "Téma bylo přiřazeno", "Topic was assigned", "Téma, [TOPIC_NAME], bylo přiřazeno někomu jinému.", "Topic, [TOPIC_NAME_ENG], was assigned to someone else.", ctx);
        CreateNotification("TopicAdopted", "Téma bylo přijato", "Topic was accepted", "Vaše navrhnuté téma, [TOPIC_NAME], bylo přijato.", "Your proposed topic, [TOPIC_NAME_ENG], was accepted.", ctx);
        CreateNotification("CommentNew", "Nový komentář", "New comment", "Nový komentář u tématu, [TOPIC_NAME], - [COMMENT_TEXT]", "New comment, [TOPIC_NAME_ENG], - [COMMENT_TEXT]", ctx);
        CreateNotification("NewInterest", "Někdo projevil zájem", "Someone is interested", "Někdo projevil zájem o téma, [TOPIC_NAME].", "Some is interested in topic, [TOPIC_NAME_ENG].", ctx);
        CreateNotification("NewExternalUser", "Nová registrace", "New registration", "Právě se někdo zaregistroval.", "Someone has just registered.", ctx);
    }

    public static async void DevSeed(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetService<ApplicationDbContext>();
        if (ctx is null) return;

        InitSeed(app);

        List<ApplicationUser> teachers = new();
        teachers.Add(CreateUser("Eduard", "Bartl", "RNDr. Eduard Bartl, Ph.D.", "eduard.bartl@upol.c", "bartl", true, "password", ctx));
        //teachers.Add(CreateUser("Jan", "Outrata", "doc. Mgr. Jan Outrata, Ph.D.", "jan.outrata@upol.c", "outrata", true, "password", ctx));
        teachers.Add(CreateUser("Martin", "Trnečka", "RNDr. Martin Trnečka, Ph.D.", "martin.trnecka@upol.c", "trnecka", true, "password", ctx));
        teachers.Add(CreateUser("Radim", "Bělohlávek", "prof. RNDr. Radim Bělohlávek, DSc.", "radim.belohlavek@upol.c", "belohlavek", true, "password", ctx));
        teachers.Add(CreateUser("Michal", "Krupka", "doc. RNDr. Michal Krupka, Ph.D.", "michal.krupka@upol.c", "krupka", true, "password", ctx));
        teachers.Add(CreateUser("Petr", "Jančar", "prof. RNDr. Petr Jančar, CSc.", "petr.jancar@upol.c", "jancar", true, "password", ctx));
        teachers.Add(CreateUser("Miroslav", "Kolařík", "doc. RNDr. Miroslav Kolařík, Ph.D.", "miroslav.kolarik@upol.c", "kolarik", true, "password", ctx));
        teachers.Add(CreateUser("Jan", "Konečný", "doc. RNDr. Jan Konečný, Ph.D.", "jan.konecny@upol.c", "konecny", true, "password", ctx));
        teachers.Add(CreateUser("Tomáš", "Masopust", "doc. RNDr. Tomáš Masopust, Ph.D., DSc.", "tomas.masopust@upol.c", "masopust", true, "password", ctx));
        teachers.Add(CreateUser("Radek", "Janoštík", "Mgr. Radek Janoštík, Ph.D.", "radek.janostik@upol.c", "janostik", true, "password", ctx));
        teachers.Add(CreateUser("Petr", "Krajča", "Mgr. Petr Krajča, Ph.D.", "petr.krajca@upol.c", "krajca", true, "password", ctx));
        teachers.Add(CreateUser("Petr", "Osička", "Mgr. Petr Osička, Ph.D.", "petr.osicka@upol.c", "osicka", true, "password", ctx));
        teachers.Add(CreateUser("Jan", "Laštovička", "Mgr. Jan Laštovička, Ph.D.", "jan.lastovicka@upol.c", "lastovicka", true, "password", ctx));
        teachers.Add(CreateUser("Arnošt", "Večerka", "RNDr. Arnošt Večerka", "arnost.vecerka@upol.c", "vecerka", true, "password", ctx));
        teachers.Add(CreateUser("Markéta", "Trnečková", "Mgr. Markéta Trnečková, Ph.D.", "markt.trneckova@upol.c", "trneckova", true, "password", ctx));
        teachers.Add(CreateUser("Jiří", "Zacpal", "Mgr. Jiří Zacpal, Ph.D.", "jiri.zacpal@upol.c", "zacpal", true, "password", ctx));
        teachers.Add(CreateUser("Jiří", "Balun", "Mgr. Jiří Balun", "jiri.balun@upol.c", "balun", true, "password", ctx));
        teachers.Add(CreateUser("Tomáš", "Urbanec", "Mgr. Tomáš Urbanec", "tomas.urbanec@upol.c", "urbanec", true, "password", ctx));
        teachers.Add(CreateUser("Jakub", "Juračka", "Mgr. Jakub Juračka", "jakub.juracka@upol.c", "juracka", true, "password", ctx));
        teachers.Add(CreateUser("Tomáš", "Mikula", "Mgr. Tomáš Mikula", "tomas.mikula@upol.c", "mikula", true, "password", ctx));
        teachers.Add(CreateUser("Roman", "Vyjídáček", "Mgr. Roman Vyjídáček", "roman.vyjidacek@upol.c", "vyjidacek", true, "password", ctx));
        foreach (var tea in teachers)
        {
            await RoleHelper.AssignRoles(tea, Role.Teacher, ctx);
        }
        
        List<ApplicationUser> students = new();
        //create random students
        for (var i = 0; i < 512; i++)
        {
            var student = CreateUser("Student" + i, "Student" + i, null, "student" + i + "@upol.c",
                "student" + i + "@upol.c", true, "password", ctx);
            await RoleHelper.AssignRoles(student, Role.Student, ctx);
            students.Add(student);
        }
/*
        var notification = ctx.Notifications.First(x => x.Name == "CommentNew");
        var notification2 = ctx.Notifications.First(x => x.Name == "TopicEdit");

        foreach (var st in students)
        {
            CreateSubscribe(st, notification, ctx);
            CreateSubscribe(st, notification2, ctx);
        }
        */
        //Create programmes
        List<Programme> programmesBc = new();
        List<Programme> programmesMs = new();
        //Bachelor
        programmesBc.Add(CreateProgramme("Informační technologie", "Information Technology", true, ProgramType.Bachelor, ctx));
        programmesBc.Add(CreateProgramme("Informatika", "Informatics", true, ProgramType.Bachelor, ctx));
        programmesBc.Add(CreateProgramme("Informatika - Vývoj software", "Informatics - Software Development", true, ProgramType.Bachelor, ctx));
        programmesBc.Add(CreateProgramme("Informatika - Obecná informatika", "Informatics - General Informatics", true, ProgramType.Bachelor, ctx));
        programmesBc.Add(CreateProgramme("Informatika pro vzdělávání", "Informatics for education", true, ProgramType.Bachelor, ctx));
        programmesBc.Add(CreateProgramme("Bioinformatika", "Bioinformatics", true, ProgramType.Bachelor, ctx));
        //Master
        programmesMs.Add(CreateProgramme("Obecná informatika", "General informatics", true, ProgramType.Master, ctx));
        programmesMs.Add(CreateProgramme("Vývoj software", "Software Development", true, ProgramType.Master, ctx));
        programmesMs.Add(CreateProgramme("Úmělá inteligence", "Artificial intelligence", true, ProgramType.Master, ctx));
        programmesMs.Add(CreateProgramme("Počítačové systémy a technologie", "Computer systems and technologies", true, ProgramType.Master, ctx));
        programmesMs.Add(CreateProgramme("Učitelství informatiky pro střední školy", "Teaching informatics for high schools", true, ProgramType.Master, ctx));

        var groups = new List<Group>()
        {
            ctx.Groups.First(x=>x.NameEng.Equals("Bachelor")),
            ctx.Groups.First(x=>x.NameEng.Equals("Master"))
        };
        
        List<Topic> theses = new();
        for(int i = 0; i < 50; i++)
        {
            theses.Add(CreateTopic("Téma " + i, "Topic " + i,
                "CZ: lorem ipsum dolor sit  amet lorem lorem ipsum dolor sit  amet lorem lorem ipsum dolor sit  amet lorem lorem ipsum dolor sit  amet lorem",
                "EN: lorem ipsum dolor sit  amet lorem lorem ipsum dolor sit  amet lorem lorem ipsum dolor sit  amet lorem lorem ipsum dolor sit  amet lorem",
                "CZ: lorem ipsum dolor sit  amet lorem lorem ipsum dolor sit  amet lorem lorem ipsum dolor sit  amet lorem lorem ipsum dolor sit  amet lorem CZ: lorem ipsum dolor sit  amet lorem lorem ipsum dolor sit  amet lorem lorem ipsum dolor sit  amet lorem lorem ipsum dolor sit  amet lorem CZ: lorem ipsum dolor sit  amet lorem lorem ipsum dolor sit  amet lorem lorem ipsum dolor sit  amet lorem lorem ipsum dolor sit  amet lorem",
                "EN: lorem ipsum dolor sit  amet lorem lorem ipsum dolor sit  amet lorem lorem ipsum dolor sit  amet lorem lorem ipsum dolor sit  amet lorem EN: lorem ipsum dolor sit  amet lorem lorem ipsum dolor sit  amet lorem lorem ipsum dolor sit  amet lorem lorem ipsum dolor sit  amet lorem EN: lorem ipsum dolor sit  amet lorem lorem ipsum dolor sit  amet lorem lorem ipsum dolor sit  amet lorem lorem ipsum dolor sit  amet lorem",
                true,
                teachers[Random.Shared.Next(0, teachers.Count)],
                teachers[Random.Shared.Next(0, teachers.Count)],
                null,
                groups[Random.Shared.Next(0, groups.Count)],
                ctx));
        }

        foreach (var t in theses)
        {
            int rProgrammes = Random.Shared.Next(0, 3);
            int rInterest = Random.Shared.Next(0, 3);
            int rTaken = Random.Shared.Next(0, 10);
            var g = programmesBc;
            if (t.Group.NameEng == "Master")
            {
                g = programmesMs;
            }

            if (rTaken > 7)
            {
                t.AssignedId = students[Random.Shared.Next(0, students.Count - 1)].Id;
            }
            
            for(int p = 0; p < rProgrammes; p++)
            {
                CreateTopicRecommendedProgramme(g[Random.Shared.Next(0, g.Count - 1)], t, ctx);
            }
            
            for(int interest = 0; interest < rInterest; interest++)
            {
                CreateUserInterestedTopic(students[Random.Shared.Next(0, students.Count - 1)], t, DateTime.Now, ctx);
            }
            
        }
        await ctx.SaveChangesAsync();
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

    public static Attachment CreateAttachment(string name, string path, DateTime createdAt, ApplicationUser creator, Topic topic, ApplicationDbContext ctx)
    {
        //Create new Attachment if not exists
        var attachment = ctx.Attachments.FirstOrDefault(a =>
            a.Name.Equals(name) && a.Creator.Id.Equals(creator.Id) && a.Topic.TopicId.Equals(topic.TopicId));
        
        if (attachment == null)
        {
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

    public static void CreateSubscribe(ApplicationUser user, Notification notification,
        ApplicationDbContext ctx)
    {
        if (!ctx.UserSubscribedNotifications.Any(x =>
                x.UserId == user.Id && x.NotificationId == notification.NotificationId))
        {
            ctx.UserSubscribedNotifications.Add(new()
            {
                UserId = user.Id,
                NotificationId = notification.NotificationId
            });
            ctx.SaveChanges();
        }
    }
}