using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TOS.Models;

namespace TOS.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
    {

        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Programme> Programmes { get; set; }
        public DbSet<Topic> Topics { get; set; }
        public DbSet<TopicRecommendedProgramme> TopicRecommendedProgrammes { get; set; }
        public DbSet<UserInterestedTopic> UserInterestedTopics { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            //Ignore some default User columns
            builder.Entity<ApplicationUser>()
                .Ignore(c => c.PhoneNumber)
                .Ignore(c => c.PhoneNumberConfirmed)
                .Ignore(c => c.TwoFactorEnabled);

            //Rename identity tables
            builder.Entity<ApplicationUser>(b =>
            {
                b.ToTable("User");
            });
            builder.Entity<IdentityUserRole<int>>(b =>
            {
                b.ToTable("UserRole");
            });
            builder.Entity<IdentityRole<int>>(b =>
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

            //Conversion of enum ProgramType for Programme
            builder.Entity<Programme>()
                .Property(x => x.Type)
                .HasConversion(
                    v => v.ToString(),
                    v => (ProgramType)Enum.Parse(typeof(ProgramType), v));


        }
    }
}