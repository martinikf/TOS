using Microsoft.AspNetCore.Identity;
using TOS.Models;

namespace TOS.Data;

public static class Seed
{
    public static void InitSeed(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetService<ApplicationDbContext>();
        
        //Create Roles: Administrator, Teacher, Student, External
        var adminRole = CreateRole("Administrator", ctx);
        var teacherRole = CreateRole("Teacher", ctx);
        var studentRole = CreateRole("Student", ctx);
        var externalRole = CreateRole("External", ctx);

        //Create users
        var adminUser = CreateUser("Admin", "User", "admin@tos.tos","admin@tos.tos", true, "password", ctx);
        var teacherUser = CreateUser("Teacher", "User", "teacher@tos.tos","teacher@tos.tos", true, "password", ctx);
        var studentUser = CreateUser("Student", "User", "student@tos.tos", "student@tos.tos",true, "password", ctx);
        var externalUser = CreateUser("External", "User", "external@tos.tos", "external@tos.tos",true, "password", ctx);

        //Add roles
        CreateUserRole(adminUser, adminRole, ctx);
        CreateUserRole(teacherUser, teacherRole, ctx);
        CreateUserRole(studentUser, studentRole, ctx);
        CreateUserRole(externalUser, externalRole, ctx);

        //Create default Groups: Unassigned, Bachelor, Master for topics
        var unassignedGroup = CreateGroup("Nezařazeno", "Unassigned", adminUser, true, false, ctx);
        var bachelorGroup = CreateGroup("Bakalářská","Bachelor", adminUser, true, true, ctx);
        var masterGroup = CreateGroup("Magisterská","Master", adminUser, true, true, ctx);
        //Example for course
        var jj1Group = CreateGroup("KMI/JJ1-2022-A", null, teacherUser, false, true, ctx);
        
        //Create programmees
        //Bachelor
        var bcITProgramme = CreateProgramme("Informační technologie","Information Technology", true, ProgramType.Bachelor, ctx);
        var bcInfProgramme = CreateProgramme("Informatika", "Informatics",  true, ProgramType.Bachelor, ctx);
        var bcSwProgramme = CreateProgramme("Vývoj software","Software Development", true, ProgramType.Bachelor, ctx);
        var bcGeneralInfProgramme = CreateProgramme("Obecná informatika","General Informatics", true, ProgramType.Bachelor, ctx);
        //Master
        var mcInfProgramme = CreateProgramme("Informatika" , "Informatics", true, ProgramType.Master, ctx);
        var mcSwProgramme = CreateProgramme("Vývoj software","Software Development", true, ProgramType.Master, ctx);
        var mcGeneralInfProgramme = CreateProgramme("Obecná informatika","General Informatics", true, ProgramType.Master, ctx);
        var mcAIProgramme = CreateProgramme("Úmělá inteligence","Artificial Intelligence", true, ProgramType.Master, ctx);
        var mcITProgramme = CreateProgramme("Informační technologie", "Information Technlogy", true, ProgramType.Master, ctx);
        
        //Create topics
        var tosTopic = CreateTopic("TOS", null, "Krátký popis", "System for offering topics of diploma theses", "Dlouhý popis","Longer description shown in details.", true, teacherUser,
            teacherUser, studentUser, bachelorGroup, ctx);

        var deskovkyTopic = CreateTopic("Mobilní aplikace pro seznamování hračů deskových her", null,
            "Mobilní aplikace, která bude mít za úkol seskupovat lidi se zájmem o stejné deskové hry. Umožní chat mezi takovými lidmi.", "Eng",
            "Long desc","Eng", true, teacherUser, teacherUser, null, bachelorGroup, ctx);

        var masterTopic = CreateTopic("Překladač pro jazyk LISP", "LISP ENG",
            "Překladač pro jazyk LISP, který bude umožnovat velkou možnost optimalizace kodu","Eng", "Long desc","Eng", true, externalUser,
            teacherUser, null, masterGroup, ctx);

        var master2Topic = CreateTopic("MVC framework pro jednoduchý vývoj webových aplikací", null,
            "Framework pro studentem vybraný programovací jazyk. Framework umožní jednoduchý vývoj web apliackí pomocí architektruy MVC.", null,
            "long Desc", null, true, teacherUser, teacherUser, null, masterGroup, ctx);
        
        
        //Recommended programmes for topics
        CreateTopicRecommendedProgramme(bcSwProgramme, tosTopic, ctx);
        CreateTopicRecommendedProgramme(bcSwProgramme, deskovkyTopic, ctx);
        CreateTopicRecommendedProgramme(bcInfProgramme, deskovkyTopic, ctx);
        CreateTopicRecommendedProgramme(mcInfProgramme, masterTopic, ctx);
        CreateTopicRecommendedProgramme(mcInfProgramme, master2Topic, ctx);
        CreateTopicRecommendedProgramme(mcSwProgramme, master2Topic, ctx);
        CreateTopicRecommendedProgramme(mcGeneralInfProgramme, masterTopic, ctx);


    }



    public static ApplicationUser CreateUser(string firstname, string lastname, string email, string username, bool emailConfirmed, string? password, ApplicationDbContext ctx)
    {
        //Create usef if not exists
        var user = ctx.Users.FirstOrDefault(u => u.Email == email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                FirstName = firstname,
                LastName = lastname,
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
        var role = ctx.Roles.FirstOrDefault(r => r.Name.Equals(name));
        if (role == null)
        {
            role = new IdentityRole<int>
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
                userRole = new IdentityUserRole<int>
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
            a.Name.Equals(name) && a.Path.Equals(path) && a.Creator.Id.Equals(creator.Id) && a.Topic.TopicId.Equals(topic.TopicId));
        
        if (attachment == null)
        {
            
            if(nameEng == null)
            {
                nameEng = name;
            }
            
            attachment = new Attachment
            {
                Name = name,
                Path = path,
                CreatedAt = createdAt,
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