﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TOS.Data;

namespace TOS.Models;

public class Topic
{
        [Key]
        public int TopicId { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_TopicNameRequired")]
        [StringLength(128, ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_TopicNameLength")]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(128, ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_TopicNameLength")]
        public string? NameEng { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_TopicDescriptionShortRequired")]
        [StringLength(512, ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_TopicDescriptionShortLength")]
        public string DescriptionShort { get; set; } = string.Empty;
        
        [StringLength(512, ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_TopicDescriptionShortLength")]
        public string? DescriptionShortEng { get; set; } = string.Empty;
        
        [StringLength(16384, ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_TopicDescriptionLongLength")]
        public string? DescriptionLong { get; set; }
        
        [StringLength(16384, ErrorMessageResourceType = typeof(Resources.ValidationErrorResource), ErrorMessageResourceName = "ERROR_TopicDescriptionLongLength")]
        public string? DescriptionLongEng { get; set; }

        public bool Visible { get; set; }

        public bool Proposed { get; set; }
        
        public TopicType Type { get; set; }

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
        
        public int GroupId { get; set; }

        [ForeignKey("GroupId")]
        [InverseProperty("Topics")]
        public virtual Group Group { get; set; } = null!;

        public virtual ICollection<TopicRecommendedProgramme> TopicRecommendedPrograms { get; } = new HashSet<TopicRecommendedProgramme>();

        [InverseProperty("Topic")]
        public virtual ICollection<Comment> Comments { get; } = new HashSet<Comment>();
        
        [InverseProperty("Topic")]
        public virtual ICollection<Attachment> Attachments { get; } = new HashSet<Attachment>();
}