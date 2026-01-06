using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProiectDAW.Models
{
    public enum FollowStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        Blocked = 3
    }

    public class Follow
    {
        [Key]
        public int Id { get; set; }

        public FollowStatus Status { get; set; }

        public DateTime RequestDate { get; set; }

        // Foreign keys
        public string FollowerId { get; set; }
        public string EditorId { get; set; }

        // Navigation properties
        [ForeignKey("FollowerId")]
        public virtual ApplicationUser Follower { get; set; }

        [ForeignKey("EditorId")]
        public virtual ApplicationUser Editor { get; set; }
    }
}
