using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HostingEnvironmentExtensions = Microsoft.AspNetCore.Hosting.HostingEnvironmentExtensions;

namespace TOS.Models;

public class Topic
{
        [Key]
        public int TopicId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string DescriptionShort { get; set; }

        public string? DescriptionLong { get; set; }

        public bool Visible { get; set; }
        
        public int CreatorId { get; set; }
        [ForeignKey("CreatorId")]
        [InverseProperty("CreatedTopics")]
        public virtual ApplicationUser Creator { get; set; } = null!;
        
        public int? SupervisorId { get; set; }
        [ForeignKey("SupervisorId")]
        [InverseProperty("SupervisedTopics")]
        public virtual ApplicationUser? Supervisor { get; set; }
        
        public int? AssignedId { get; set; }
        [ForeignKey("AssignedId")]
        [InverseProperty("AssignedTopics")]
        public virtual ApplicationUser? AssignedStudent { get; set; }

        public virtual ICollection<UserInterestedTopic> UserInterestedTopics { get; } = new HashSet<UserInterestedTopic>();
        
        public int? GroupId { get; set; }
        [ForeignKey("GroupId")]
        [InverseProperty("Topics")]
        public virtual Group? Group { get; set; }
        
        public virtual ICollection<TopicRecommendedProgramme> TopicRecommendedPrograms { get; } = new HashSet<TopicRecommendedProgramme>();

        [InverseProperty("Topic")]
        public virtual ICollection<Comment> Comments { get; } = new HashSet<Comment>();
        
        [InverseProperty("Topic")]
        public virtual ICollection<Attachment> Attachments { get; } = new HashSet<Attachment>();
}