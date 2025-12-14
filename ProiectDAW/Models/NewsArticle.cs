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
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }

        public string? CoverImagePath { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // AI specific properties
        public decimal? TrustScore { get; set; }
        public string? TrustLabel { get; set; }
        public string? AiJustification { get; set; }

        // Foreign keys
        public string EditorId { get; set; }

        // Navigation properties
        [ForeignKey("EditorId")]
        public virtual ApplicationUser Editor { get; set; }

        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }

    public class Comment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; }

        public DateTime PostedDate { get; set; } = DateTime.Now;

        // Foreign keys
        public int NewsArticleId { get; set; }
        public string UserId { get; set; }

        // Navigation properties
        [ForeignKey("NewsArticleId")]
        public virtual NewsArticle NewsArticle { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
    }
}
