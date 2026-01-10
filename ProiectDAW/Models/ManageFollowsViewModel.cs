using System.Collections.Generic;

namespace ProiectDAW.Models
{
    public class ManageFollowsViewModel
    {
        public List<Follow> Followers { get; set; } = new List<Follow>();
        public List<Follow> Requests { get; set; } = new List<Follow>();
        public List<Follow> BlockedUsers { get; set; } = new List<Follow>();
    }
}
