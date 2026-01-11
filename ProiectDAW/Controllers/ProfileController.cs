using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProiectDAW.Data;
using ProiectDAW.Models;
using System.ComponentModel.DataAnnotations;

namespace ProiectDAW.Controllers
{
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProfileController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Profile/Search
        [AllowAnonymous]
        public async Task<IActionResult> Search(string searchTerm)
        {
            var model = new EditorSearchViewModel
            {
                SearchTerm = searchTerm
            };


            return View(model);
        }

        // GET: Profile/SearchEditors (AJAX)
        [AllowAnonymous]
        public async Task<IActionResult> SearchEditors(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return PartialView("_EditorListPartial", new List<EditorProfileViewModel>());
            }


            searchTerm = searchTerm.ToLower();
            

            var blockedByids = new List<string>();
            if (User.Identity.IsAuthenticated)
            {
                var currentUserId = _userManager.GetUserId(User);
                blockedByids = await _context.Follows
                    .Where(f => f.FollowerId == currentUserId && f.Status == FollowStatus.Blocked)
                    .Select(f => f.EditorId)
                    .ToListAsync();
            }

            var editors = await _userManager.Users
                .ToListAsync();
                                
            var filteredEditors = new List<EditorProfileViewModel>();
            
            foreach (var user in editors)
            {

                if (blockedByids.Contains(user.Id))
                {
                    continue;
                }


                string fullName = (user.FirstName + " " + user.LastName).ToLower();
                if (user.FirstName.ToLower().Contains(searchTerm) || 
                    user.LastName.ToLower().Contains(searchTerm) || 
                    fullName.Contains(searchTerm))
                {

                    if (await _userManager.IsInRoleAsync(user, "Editor") || await _userManager.IsInRoleAsync(user, "Administrator"))
                    {
                        filteredEditors.Add(new EditorProfileViewModel
                        {
                            UserId = user.Id,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            Description = user.Description,
                            ProfilePicturePath = user.ProfilePicturePath,
                            IsProfilePrivate = user.IsProfilePrivate
                        });
                    }
                }
            }

            return PartialView("_EditorListPartial", filteredEditors); 
        }

        // GET: Profile/ViewProfile/{id}
        [AllowAnonymous]
        public async Task<IActionResult> ViewProfile(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }


            var roles = await _userManager.GetRolesAsync(user);
            var isEditor = roles.Contains("Editor") || roles.Contains("Administrator");


            FollowStatus? followStatus = null;
            bool isFollowing = false;
            
            if (User.Identity.IsAuthenticated)
            {
                var currentUserId = _userManager.GetUserId(User);
                if (currentUserId != user.Id)
                {
                    var follow = await _context.Follows
                        .FirstOrDefaultAsync(f => f.FollowerId == currentUserId && f.EditorId == user.Id);
                        
                    if (follow != null)
                    {
                        followStatus = follow.Status;
                        isFollowing = follow.Status == FollowStatus.Approved;
                    }
                }
                else
                {
                    isFollowing = true; 
                }
            }

            var model = new ViewProfileViewModel
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Description = user.Description,
                ProfilePicturePath = user.ProfilePicturePath,
                IsProfilePrivate = user.IsProfilePrivate,
                IsEditor = isEditor,
                CreatedAt = user.CreatedAt,
                CurrentUserFollowStatus = followStatus,
                IsFollowing = isFollowing
            };

            bool showFullProfile = !user.IsProfilePrivate || (user.IsProfilePrivate && isFollowing);
            

            
            if (!showFullProfile)
            {
                model.IsLimitedView = true;
            }
            else
            {
                model.IsLimitedView = false;
                

                model.ArticlesCount = await _context.NewsArticles
                    .Where(a => a.EditorId == user.Id)
                    .CountAsync();
                
                model.Articles = await _context.NewsArticles
                    .Where(a => a.EditorId == user.Id)
                    .OrderByDescending(a => a.CreatedDate)
                    .ToListAsync();
                
                model.FollowersCount = await _context.Follows
                    .Where(f => f.EditorId == user.Id && f.Status == FollowStatus.Approved)
                    .CountAsync();
            }

            return View(model);
        }

        // GET: Profile/Edit
        [Authorize]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var model = new EditProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Description = user.Description ?? "",
                CurrentProfilePicturePath = user.ProfilePicturePath,
                IsProfilePrivate = user.IsProfilePrivate
            };

            return View(model);
        }

        // POST: Profile/Edit
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }


            if (model.ProfilePicture == null && string.IsNullOrEmpty(user.ProfilePicturePath))
            {
                ModelState.AddModelError("ProfilePicture", "Profile picture is required");
            }

            if (!ModelState.IsValid)
            {
                model.CurrentProfilePicturePath = user.ProfilePicturePath;
                return View(model);
            }

            // Update basic information
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Description = model.Description;
            user.IsProfilePrivate = model.IsProfilePrivate;


            if (model.ProfilePicture != null)
            {

                if (!string.IsNullOrEmpty(user.ProfilePicturePath))
                {
                    var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, user.ProfilePicturePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }


                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "profiles");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ProfilePicture.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProfilePicture.CopyToAsync(fileStream);
                }

                user.ProfilePicturePath = "/uploads/profiles/" + uniqueFileName;
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction(nameof(ViewProfile), new { id = user.Id });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            model.CurrentProfilePicturePath = user.ProfilePicturePath;
            return View(model);
        }

        // GET: Profile/MyProfile
        [Authorize]
        public async Task<IActionResult> MyProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(ViewProfile), new { id = user.Id });
        }
    }


    public class EditorSearchViewModel
    {
        public string? SearchTerm { get; set; }
        public List<EditorProfileViewModel> Editors { get; set; } = new List<EditorProfileViewModel>();
    }

    public class EditorProfileViewModel
    {
        public string UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Description { get; set; }
        public string? ProfilePicturePath { get; set; }
        public bool IsProfilePrivate { get; set; }
    }

    public class ViewProfileViewModel
    {
        public string UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Description { get; set; }
        public string? ProfilePicturePath { get; set; }
        public bool IsProfilePrivate { get; set; }
        public bool IsEditor { get; set; }
        public bool IsLimitedView { get; set; }
        public DateTime CreatedAt { get; set; }
        

        public int ArticlesCount { get; set; }
        public int FollowersCount { get; set; }
        public List<NewsArticle> Articles { get; set; } = new List<NewsArticle>();
        

        public FollowStatus? CurrentUserFollowStatus { get; set; }
        public bool IsFollowing { get; set; }
    }

    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "First Name is required")]
        [StringLength(100, ErrorMessage = "First Name cannot exceed 100 characters")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last Name is required")]
        [StringLength(100, ErrorMessage = "Last Name cannot exceed 100 characters")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; }

        public IFormFile? ProfilePicture { get; set; }

        public string? CurrentProfilePicturePath { get; set; }

        public bool IsProfilePrivate { get; set; }
    }
}

