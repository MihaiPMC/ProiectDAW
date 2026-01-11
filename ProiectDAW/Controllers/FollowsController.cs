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


            var existingFollow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == currentUser.Id && f.EditorId == userId);

            if (existingFollow != null)
            {

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

            return RedirectToAction("MyFollowers");
        }

        [HttpPost]
        public async Task<IActionResult> DeclineRequest(int requestId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return NotFound();

            var follow = await _context.Follows.FindAsync(requestId);
            if (follow == null || follow.EditorId != currentUser.Id) return Unauthorized();

            _context.Follows.Remove(follow);
            await _context.SaveChangesAsync();

            return RedirectToAction("MyFollowers");
        }

        [HttpGet]
        public async Task<IActionResult> Requests()
        {
            return RedirectToAction("MyFollowers");
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

            var requests = await _context.Follows
                .Include(f => f.Follower)
                .Where(f => f.EditorId == currentUser.Id && f.Status == FollowStatus.Pending)
                .ToListAsync();

            var blockedUsers = await _context.Follows
                .Include(f => f.Follower)
                .Where(f => f.EditorId == currentUser.Id && f.Status == FollowStatus.Blocked)
                .ToListAsync();

            var viewModel = new ManageFollowsViewModel
            {
                Followers = followers,
                Requests = requests,
                BlockedUsers = blockedUsers
            };

            return View(viewModel);
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
                _context.Follows.Remove(follow);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("MyFollowers");
        }
    }
}
