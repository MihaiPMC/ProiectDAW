using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProiectDAW.Data;
using ProiectDAW.Models;
using System.ComponentModel.DataAnnotations;

namespace ProiectDAW.Controllers
{
    [Authorize]
    public class GroupController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GroupController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Group/Index - Lista toate grupurile
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var groups = await _context.Groups
                .Include(g => g.Owner)
                .Include(g => g.Members)
                .OrderByDescending(g => g.CreatedDate)
                .ToListAsync();

            var viewModel = new GroupIndexViewModel
            {
                Groups = groups.Select(g => new GroupViewModel
                {
                    Id = g.Id,
                    Name = g.Name,
                    Description = g.Description,
                    CreatedDate = g.CreatedDate,
                    OwnerName = g.Owner.FirstName + " " + g.Owner.LastName,
                    OwnerId = g.OwnerId,
                    MembersCount = g.Members.Count(m => m.IsAccepted),
                    IsMember = g.Members.Any(m => m.UserId == currentUser.Id && m.IsAccepted),
                    HasPendingRequest = g.Members.Any(m => m.UserId == currentUser.Id && !m.IsAccepted),
                    IsOwner = g.OwnerId == currentUser.Id
                }).ToList()
            };

            return View(viewModel);
        }

        // GET: Group/Create - Doar editori pot crea grupuri
        [Authorize(Roles = "Editor,Administrator")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Group/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Editor,Administrator")]
        public async Task<IActionResult> Create(CreateGroupViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var currentUser = await _userManager.GetUserAsync(User);

            var group = new Group
            {
                Name = model.Name,
                Description = model.Description,
                OwnerId = currentUser.Id,
                CreatedDate = DateTime.Now
            };

            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            var member = new GroupMember
            {
                GroupId = group.Id,
                UserId = currentUser.Id,
                IsAccepted = true,
                JoinedDate = DateTime.Now
            };
            _context.GroupMembers.Add(member);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Group successfully created!";
            return RedirectToAction(nameof(Details), new { id = group.Id });
        }

        // GET: Group/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var group = await _context.Groups
                .Include(g => g.Owner)
                .Include(g => g.Members.Where(m => m.IsAccepted))
                    .ThenInclude(m => m.User)
                .Include(g => g.Messages)
                    .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
            {
                return NotFound();
            }

            var membership = await _context.GroupMembers
                .FirstOrDefaultAsync(m => m.GroupId == id && m.UserId == currentUser.Id);

            var isMember = membership != null && membership.IsAccepted;
            var hasPendingRequest = membership != null && !membership.IsAccepted;
            var isOwner = group.OwnerId == currentUser.Id;

            var viewModel = new GroupDetailsViewModel
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description,
                CreatedDate = group.CreatedDate,
                OwnerName = group.Owner.FirstName + " " + group.Owner.LastName,
                OwnerId = group.OwnerId,
                IsOwner = isOwner,
                IsMember = isMember,
                HasPendingRequest = hasPendingRequest,
                Members = group.Members.Select(m => new GroupMemberViewModel
                {
                    UserId = m.UserId,
                    UserName = m.User.FirstName + " " + m.User.LastName,
                    JoinedDate = m.JoinedDate
                }).ToList(),
                Messages = isMember || isOwner 
                    ? group.Messages.OrderBy(m => m.Timestamp).Select(m => new GroupMessageViewModel
                    {
                        Id = m.Id,
                        Content = m.Content,
                        Timestamp = m.Timestamp,
                        UpdatedAt = m.UpdatedAt,
                        SenderName = m.Sender.FirstName + " " + m.Sender.LastName,
                        SenderId = m.SenderId,
                        IsOwnMessage = m.SenderId == currentUser.Id
                    }).ToList()
                    : new List<GroupMessageViewModel>()
            };

            return View(viewModel);
        }

        // POST: Group/Join/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var group = await _context.Groups.FindAsync(id);

            if (group == null)
            {
                return NotFound();
            }

            var existingMembership = await _context.GroupMembers
                .FirstOrDefaultAsync(m => m.GroupId == id && m.UserId == currentUser.Id);

            if (existingMembership != null)
            {
                if (existingMembership.IsBanned)
                {
                    TempData["ErrorMessage"] = "You cannot join this group because you are blocked.";
                    return RedirectToAction(nameof(Index));
                }

                TempData["ErrorMessage"] = "You already have a join request for this group.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var membership = new GroupMember
            {
                GroupId = id,
                UserId = currentUser.Id,
                IsAccepted = false,
                JoinedDate = DateTime.Now
            };

            _context.GroupMembers.Add(membership);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your join request has been sent to the moderator.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Group/Leave/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Leave(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == id);
            
            if (group == null)
            {
                return NotFound();
            }

            var membership = group.Members
                .FirstOrDefault(m => m.UserId == currentUser.Id);

            if (membership == null)
            {
                return NotFound();
            }


            if (group.OwnerId == currentUser.Id)
            {

                var successor = group.Members
                    .Where(m => m.IsAccepted && m.UserId != currentUser.Id && !m.IsBanned)
                    .OrderBy(m => m.JoinedDate)
                    .FirstOrDefault();

                if (successor != null)
                {

                    group.OwnerId = successor.UserId;
                    TempData["SuccessMessage"] = "You left the group. The moderator role was automatically transferred.";
                }
                else
                {


                    var messages = await _context.GroupMessages.Where(m => m.GroupId == id).ToListAsync();
                    _context.GroupMessages.RemoveRange(messages);
                    

                    _context.GroupMembers.RemoveRange(group.Members);
                    

                    _context.Groups.Remove(group);
                    
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "The group was deleted because you were the last member (moderator).";
                    return RedirectToAction(nameof(Index));
                }
            }
            else
            {
                TempData["SuccessMessage"] = "You successfully left the group.";
            }

            _context.GroupMembers.Remove(membership);
            await _context.SaveChangesAsync();
            
            return RedirectToAction(nameof(Index));
        }

        // GET: Group/ManageMembers/{id} - Doar pentru moderator
        public async Task<IActionResult> ManageMembers(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var group = await _context.Groups
                .Include(g => g.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
            {
                return NotFound();
            }

            if (group.OwnerId != currentUser.Id)
            {
                return Forbid();
            }

            var viewModel = new ManageMembersViewModel
            {
                GroupId = group.Id,
                GroupName = group.Name,
                PendingMembers = group.Members.Where(m => !m.IsAccepted).Select(m => new MemberRequestViewModel
                {
                    MembershipId = m.Id,
                    UserId = m.UserId,
                    UserName = m.User.FirstName + " " + m.User.LastName,
                    RequestDate = m.JoinedDate
                }).ToList(),
                AcceptedMembers = group.Members.Where(m => m.IsAccepted).Select(m => new MemberRequestViewModel
                {
                    MembershipId = m.Id,
                    UserId = m.UserId,
                    UserName = m.User.FirstName + " " + m.User.LastName,
                    RequestDate = m.JoinedDate
                }).ToList()
            };

            return View(viewModel);
        }

        // POST: Group/AcceptMember
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptMember(int membershipId)
        {
            var membership = await _context.GroupMembers
                .Include(m => m.Group)
                .FirstOrDefaultAsync(m => m.Id == membershipId);

            if (membership == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (membership.Group.OwnerId != currentUser.Id)
            {
                return Forbid();
            }

            membership.IsAccepted = true;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Member accepted into the group.";
            return RedirectToAction(nameof(ManageMembers), new { id = membership.GroupId });
        }

        // POST: Group/RemoveMember
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(int membershipId)
        {
            var membership = await _context.GroupMembers
                .Include(m => m.Group)
                .FirstOrDefaultAsync(m => m.Id == membershipId);

            if (membership == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (membership.Group.OwnerId != currentUser.Id)
            {
                return Forbid();
            }

            _context.GroupMembers.Remove(membership);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Member removed from the group.";
            return RedirectToAction(nameof(ManageMembers), new { id = membership.GroupId });
        }

        // POST: Group/BlockMember
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockMember(int membershipId)
        {
            var membership = await _context.GroupMembers
                .Include(m => m.Group)
                .FirstOrDefaultAsync(m => m.Id == membershipId);

            if (membership == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (membership.Group.OwnerId != currentUser.Id)
            {
                return Forbid();
            }
            
            // Cannot block self (should correspond to leave logic instead)
            if (membership.UserId == currentUser.Id) 
            {
                 TempData["ErrorMessage"] = "You cannot block yourself.";
                 return RedirectToAction(nameof(ManageMembers), new { id = membership.GroupId });
            }

            membership.IsAccepted = false;
            membership.IsBanned = true;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "User has been blocked and will no longer be able to join the group.";
            return RedirectToAction(nameof(ManageMembers), new { id = membership.GroupId });
        }

        // POST: Group/SendMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int groupId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["ErrorMessage"] = "Message cannot be empty.";
                return RedirectToAction(nameof(Details), new { id = groupId });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var membership = await _context.GroupMembers
                .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == currentUser.Id && m.IsAccepted);

            var group = await _context.Groups.FindAsync(groupId);
            if (membership == null && group?.OwnerId != currentUser.Id)
            {
                TempData["ErrorMessage"] = "You are not a member of this group.";
                return RedirectToAction(nameof(Details), new { id = groupId });
            }

            var message = new GroupMessage
            {
                GroupId = groupId,
                SenderId = currentUser.Id,
                Content = content,
                Timestamp = DateTime.Now
            };

            _context.GroupMessages.Add(message);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = groupId });
        }

        // POST: Group/EditMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMessage(int messageId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["ErrorMessage"] = "Message cannot be empty.";
                return RedirectToAction(nameof(Index));
            }

            var message = await _context.GroupMessages.FindAsync(messageId);
            if (message == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (message.SenderId != currentUser.Id)
            {
                return Forbid();
            }

            message.Content = content;
            message.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = message.GroupId });
        }

        // POST: Group/DeleteMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var message = await _context.GroupMessages.FindAsync(messageId);
            if (message == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (message.SenderId != currentUser.Id)
            {
                return Forbid();
            }

            var groupId = message.GroupId;
            _context.GroupMessages.Remove(message);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Message deleted.";
            return RedirectToAction(nameof(Details), new { id = groupId });
        }

        // POST: Group/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var group = await _context.Groups
                .Include(g => g.Members)
                .Include(g => g.Messages)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (group.OwnerId != currentUser.Id)
            {
                return Forbid();
            }


            _context.GroupMessages.RemoveRange(group.Messages);
            _context.GroupMembers.RemoveRange(group.Members);
            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Group successfully deleted.";
            return RedirectToAction(nameof(Index));
        }
    }


    public class GroupIndexViewModel
    {
        public List<GroupViewModel> Groups { get; set; } = new List<GroupViewModel>();
    }

    public class GroupViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerId { get; set; } = string.Empty;
        public int MembersCount { get; set; }
        public bool IsMember { get; set; }
        public bool HasPendingRequest { get; set; }
        public bool IsOwner { get; set; }
    }

    public class CreateGroupViewModel
    {
        [Required(ErrorMessage = "Group name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = string.Empty;
    }

    public class GroupDetailsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerId { get; set; } = string.Empty;
        public bool IsOwner { get; set; }
        public bool IsMember { get; set; }
        public bool HasPendingRequest { get; set; }
        public List<GroupMemberViewModel> Members { get; set; } = new List<GroupMemberViewModel>();
        public List<GroupMessageViewModel> Messages { get; set; } = new List<GroupMessageViewModel>();
    }

    public class GroupMemberViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime JoinedDate { get; set; }
    }

    public class GroupMessageViewModel
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public bool IsOwnMessage { get; set; }
    }

    public class ManageMembersViewModel
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public List<MemberRequestViewModel> PendingMembers { get; set; } = new List<MemberRequestViewModel>();
        public List<MemberRequestViewModel> AcceptedMembers { get; set; } = new List<MemberRequestViewModel>();
    }

    public class MemberRequestViewModel
    {
        public int MembershipId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
    }
}

