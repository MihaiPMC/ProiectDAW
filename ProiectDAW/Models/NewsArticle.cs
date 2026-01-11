using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProiectDAW.Models
{
    public class NewsArticle
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public string? CoverImagePath { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // AI specific properties
        public decimal? TrustScore { get; set; }
        public string? TrustLabel { get; set; }
        public string? AiJustification { get; set; }

        // Foreign keys
        public string EditorId { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey("EditorId")]
        public virtual ApplicationUser? Editor { get; set; }

        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

        public bool IsEdited { get; set; } = false;
    }

    public class Comment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; } = string.Empty;

        public DateTime PostedDate { get; set; } = DateTime.Now;

        public bool IsEdited { get; set; } = false;

        public bool IsDeleted { get; set; } = false;

        // Foreign keys
        public int NewsArticleId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int? ParentCommentId { get; set; }

        // Navigation properties
        [ForeignKey("NewsArticleId")]
        public virtual NewsArticle? NewsArticle { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [ForeignKey("ParentCommentId")]
        public virtual Comment? ParentComment { get; set; }

        public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();
    }
}
