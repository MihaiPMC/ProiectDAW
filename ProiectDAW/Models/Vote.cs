using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProiectDAW.Models
{
    public class ArticleVote
    {
        [Key]
        public int Id { get; set; }

        public int NewsArticleId { get; set; }

        public string UserId { get; set; } = string.Empty;

        // 1 for Upvote, -1 for Downvote
        [Range(-1, 1)]
        public int Value { get; set; }

        // Navigation
        [ForeignKey("NewsArticleId")]
        public virtual NewsArticle? NewsArticle { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }
    }

    public class CommentVote
    {
        [Key]
        public int Id { get; set; }

        public int CommentId { get; set; }

        public string UserId { get; set; } = string.Empty;

        // 1 for Upvote, -1 for Downvote
        [Range(-1, 1)]
        public int Value { get; set; }

        // Navigation
        [ForeignKey("CommentId")]
        public virtual Comment? Comment { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }
    }
}
