using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ProiectDAW.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public string? ProfilePicturePath { get; set; }

        public bool IsProfilePrivate { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ICollection<NewsArticle> NewsArticles { get; set; } = new List<NewsArticle>();
        public virtual ICollection<Group> OwnedGroups { get; set; } = new List<Group>();
        
        // Relationships for Follows

        public virtual ICollection<Follow> Followers { get; set; } = new List<Follow>();
        public virtual ICollection<Follow> Followings { get; set; } = new List<Follow>();

        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<GroupMember> GroupMemberships { get; set; } = new List<GroupMember>();
        public virtual ICollection<GroupMessage> SentGroupMessages { get; set; } = new List<GroupMessage>();
    }
}
