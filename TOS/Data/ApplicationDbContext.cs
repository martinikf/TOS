using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TOS.Models;

namespace TOS.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int, IdentityUserClaim<int>, ApplicationUserRole, IdentityUserLogin<int>, IdentityRoleClaim<int>, IdentityUserToken<int>>
    {
        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Group> Groups { get; set; }
        
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Programme> Programmes { get; set; }
        public DbSet<Topic> Topics { get; set; }
        public DbSet<TopicRecommendedProgramme> TopicRecommendedProgrammes { get; set; }
        public DbSet<UserInterestedTopic> UserInterestedTopics { get; set; }
        
        public DbSet<UserSubscribedNotification> UserSubscribedNotifications { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);


            builder.Entity<ApplicationUser>(b =>
            {
                b.HasMany(e => e.UserRoles)
                    .WithOne(e => e.User)
                    .HasForeignKey(ur => ur.UserId)
                    .IsRequired();
            });
            
            builder.Entity<ApplicationRole>(b =>
            {
                // Each Role can have many entries in the UserRole join table
                b.HasMany(e => e.UserRoles)
                    .WithOne(e => e.Role)
                    .HasForeignKey(ur => ur.RoleId)
                    .IsRequired();
            });

            builder.Entity<ApplicationUserRole>(b =>
            {
                b.HasKey(p => new {p.UserId, p.RoleId});
            });
            
            //Ignore some default User columns
            builder.Entity<ApplicationUser>()
                .Ignore(c => c.PhoneNumber)
                .Ignore(c => c.PhoneNumberConfirmed)
                .Ignore(c => c.TwoFactorEnabled)
                .Ignore(c => c.LockoutEnabled)
                .Ignore(c => c.LockoutEnd)
                .Ignore(c => c.AccessFailedCount);

            builder.Entity<ApplicationRole>().Ignore(x => x.ConcurrencyStamp);

            //Rename identity tables
            builder.Entity<ApplicationUser>(b =>
            {
                b.ToTable("User");
            });
            builder.Entity<ApplicationUserRole>(b =>
            {
                b.ToTable("UserRole");
            });
            builder.Entity<ApplicationRole>(b =>
            {
                b.ToTable("Role");
            });
            builder.Entity<Attachment>(b =>
            {
                b.ToTable("Attachment");
            });
            builder.Entity<Comment>(b =>
            {
                b.ToTable("Comment");
            });
            builder.Entity<Group>(b =>
            {
                b.ToTable("Group");
            });
            builder.Entity<Notification>(b =>
            {
                b.ToTable("Notification");
            });
            builder.Entity<Programme>(b =>
            {
                b.ToTable("Programme");
            });
            builder.Entity<Topic>(b =>
            {
                b.ToTable("Topic");
            });
            builder.Entity<TopicRecommendedProgramme>(b =>
            {
                b.ToTable("TopicRecommendedProgram");
            });
            builder.Entity<UserInterestedTopic>(b =>
            {
                b.ToTable("UserInterestedTopic");
            });
            builder.Entity<UserSubscribedNotification>(b =>
            {
                b.ToTable("UserSubscribedNotification");
            });

            //M:N relation
            builder.Entity<UserInterestedTopic>().HasKey(ut => new { ut.UserId, ut.TopicId });

            builder.Entity<UserInterestedTopic>()
                .HasOne(ut => ut.User)
                .WithMany(t => t.UserInterestedTopics)
                .HasForeignKey(uk => uk.UserId);

            builder.Entity<UserInterestedTopic>()
                .HasOne(uk => uk.Topic)
                .WithMany(u => u.UserInterestedTopics)
                .HasForeignKey(uk => uk.TopicId);

            //M:N relation
            builder.Entity<TopicRecommendedProgramme>().HasKey(tf => new { tf.TopicId, tf.ProgramId });

            builder.Entity<TopicRecommendedProgramme>()
                .HasOne(tf => tf.Topic)
                .WithMany(x => x.TopicRecommendedPrograms)
                .HasForeignKey(tf => tf.TopicId);

            builder.Entity<TopicRecommendedProgramme>()
                .HasOne(tf => tf.Programme)
                .WithMany(x => x.TopicRecommendedPrograms)
                .HasForeignKey(tf => tf.ProgramId);
            
            //M:N relation for notifications
            builder.Entity<UserSubscribedNotification>().HasKey(s => new { s.UserId, s.NotificationId });

            builder.Entity<UserSubscribedNotification>()
                .HasOne(tf => tf.Notification)
                .WithMany(x => x.UserSubscribedNotifications)
                .HasForeignKey(tf => tf.NotificationId);

            builder.Entity<UserSubscribedNotification>()
                .HasOne(tf => tf.User)
                .WithMany(x => x.UserSubscribedNotifications)
                .HasForeignKey(tf => tf.UserId);
            
            

            //Conversion of enum ProgramType for Programme
            builder.Entity<Programme>()
                .Property(x => x.Type)
                .HasConversion(
                    v => v.ToString(),
                    v => (ProgramType)Enum.Parse(typeof(ProgramType), v));

            builder.Entity<Topic>()
                .Property(x => x.Type)
                .HasConversion(
                    v => v.ToString(),
                    v => (TopicType)Enum.Parse(typeof(TopicType), v));

        }
    }
}