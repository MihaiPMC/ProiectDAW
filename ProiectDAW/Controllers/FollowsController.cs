using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProiectDAW.Data;
using ProiectDAW.Models;

namespace ProiectDAW.Controllers
{
    [Authorize]
    public class FollowsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FollowsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> SendRequest(string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return NotFound();

            if (currentUser.Id == userId)
            {
                return BadRequest("You cannot follow yourself.");
            }

            var targetUser = await _userManager.FindByIdAsync(userId);
            if (targetUser == null) return NotFound();

            // Check if request already exists
            var existingFollow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == currentUser.Id && f.EditorId == userId);

            if (existingFollow != null)
            {
                // If it was rejected, maybe allow re-request? For now, just say already requested.
                // Or if we want to toggle, Unfollow. But this is SendRequest.
                if (existingFollow.Status == FollowStatus.Blocked)
                {
                    return BadRequest("You are blocked from following this user.");
                }
                return RedirectToAction("ViewProfile", "Profile", new { id = userId });
            }

            var follow = new Follow
            {
                FollowerId = currentUser.Id,
                EditorId = userId,
                RequestDate = DateTime.Now,
                Status = targetUser.IsProfilePrivate ? FollowStatus.Pending : FollowStatus.Approved
            };

            _context.Follows.Add(follow);
            await _context.SaveChangesAsync();

            return RedirectToAction("ViewProfile", "Profile", new { id = userId });
        }

        [HttpPost]
        public async Task<IActionResult> Unfollow(string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return NotFound();

            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == currentUser.Id && f.EditorId == userId);

            if (follow != null)
            {
                _context.Follows.Remove(follow);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("ViewProfile", "Profile", new { id = userId });
        }

        [HttpPost]
        public async Task<IActionResult> AcceptRequest(int requestId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return NotFound();

            var follow = await _context.Follows.FindAsync(requestId);
            if (follow == null || follow.EditorId != currentUser.Id) return Unauthorized();

            follow.Status = FollowStatus.Approved;
            await _context.SaveChangesAsync();

            return RedirectToAction("Requests");
        }

        [HttpPost]
        public async Task<IActionResult> DeclineRequest(int requestId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return NotFound();

            var follow = await _context.Follows.FindAsync(requestId);
            if (follow == null || follow.EditorId != currentUser.Id) return Unauthorized();

            _context.Follows.Remove(follow); // Or set to Rejected if we want to keep history
            await _context.SaveChangesAsync();

            return RedirectToAction("Requests");
        }

        [HttpGet]
        public async Task<IActionResult> Requests()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return NotFound();

            var requests = await _context.Follows
                .Include(f => f.Follower)
                .Where(f => f.EditorId == currentUser.Id && f.Status == FollowStatus.Pending)
                .ToListAsync();

            return View(requests);
        }

        [HttpGet]
        public async Task<IActionResult> MyFollowers()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return NotFound();

            var followers = await _context.Follows
                .Include(f => f.Follower)
                .Where(f => f.EditorId == currentUser.Id && f.Status == FollowStatus.Approved)
                .ToListAsync();

            var blockedUsers = await _context.Follows
                .Include(f => f.Follower)
                .Where(f => f.EditorId == currentUser.Id && f.Status == FollowStatus.Blocked)
                .ToListAsync();

            ViewBag.BlockedUsers = blockedUsers;

            return View(followers);
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFollower(string followerId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return NotFound();

            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.EditorId == currentUser.Id && f.FollowerId == followerId);

            if (follow != null)
            {
                _context.Follows.Remove(follow);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("MyFollowers");
        }

        [HttpPost]
        public async Task<IActionResult> BlockUser(string followerId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return NotFound();

            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.EditorId == currentUser.Id && f.FollowerId == followerId);

            if (follow != null)
            {
                follow.Status = FollowStatus.Blocked;
                await _context.SaveChangesAsync();
            }
            else 
            {
                // If checking only "followers you have", we might skip this. 
                // But to be robust, we can block someone even if they don't follow us yet?
                // The UI will likely only show this button on existing followers.
                // But if needed we can create a new Blocked record.
                var newBlock = new Follow 
                {
                    EditorId = currentUser.Id,
                    FollowerId = followerId,
                    RequestDate = DateTime.Now,
                    Status = FollowStatus.Blocked
                };
                _context.Follows.Add(newBlock);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("MyFollowers");
        }

        [HttpPost]
        public async Task<IActionResult> UnblockUser(string followerId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return NotFound();

            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.EditorId == currentUser.Id && f.FollowerId == followerId && f.Status == FollowStatus.Blocked);

            if (follow != null)
            {
                _context.Follows.Remove(follow); // Removing block means they are no longer following/blocked. Status resets to nothing.
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("MyFollowers");
        }
    }
}
