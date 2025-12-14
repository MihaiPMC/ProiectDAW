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

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                // Caută editori după nume (FirstName sau LastName)
                var editors = await _userManager.Users
                    .Where(u => 
                        (u.FirstName.Contains(searchTerm) || u.LastName.Contains(searchTerm)) ||
                        (u.FirstName + " " + u.LastName).Contains(searchTerm))
                    .ToListAsync();

                // Filtrează doar utilizatorii care au rolul de Editor sau Administrator
                var editorUsers = new List<ApplicationUser>();
                foreach (var user in editors)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    if (roles.Contains("Editor") || roles.Contains("Administrator"))
                    {
                        editorUsers.Add(user);
                    }
                }

                model.Editors = editorUsers.Select(e => new EditorProfileViewModel
                {
                    UserId = e.Id,
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    Description = e.Description,
                    ProfilePicturePath = e.ProfilePicturePath,
                    IsProfilePrivate = e.IsProfilePrivate
                }).ToList();
            }

            return View(model);
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

            // Verifică dacă utilizatorul este Editor sau Administrator
            var roles = await _userManager.GetRolesAsync(user);
            var isEditor = roles.Contains("Editor") || roles.Contains("Administrator");

            var model = new ViewProfileViewModel
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Description = user.Description,
                ProfilePicturePath = user.ProfilePicturePath,
                IsProfilePrivate = user.IsProfilePrivate,
                IsEditor = isEditor,
                CreatedAt = user.CreatedAt
            };

            // Dacă profilul este privat, returnează doar informațiile de bază
            if (user.IsProfilePrivate)
            {
                model.IsLimitedView = true;
            }
            else
            {
                // Profilul este public - afișează toate informațiile
                model.IsLimitedView = false;
                
                // Adaugă statistici suplimentare pentru profilul public
                model.ArticlesCount = await _context.NewsArticles
                    .Where(a => a.EditorId == user.Id)
                    .CountAsync();
                
                model.FollowersCount = await _context.Follows
                    .Where(f => f.EditorId == user.Id)
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

            // Validare custom: imaginea este obligatorie doar dacă nu există deja una
            if (model.ProfilePicture == null && string.IsNullOrEmpty(user.ProfilePicturePath))
            {
                ModelState.AddModelError("ProfilePicture", "Imaginea de profil este obligatorie");
            }

            if (!ModelState.IsValid)
            {
                model.CurrentProfilePicturePath = user.ProfilePicturePath;
                return View(model);
            }

            // Actualizează informațiile de bază
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Description = model.Description;
            user.IsProfilePrivate = model.IsProfilePrivate;

            // Procesează imaginea de profil dacă a fost încărcată
            if (model.ProfilePicture != null)
            {
                // Șterge imaginea veche dacă există
                if (!string.IsNullOrEmpty(user.ProfilePicturePath))
                {
                    var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, user.ProfilePicturePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // Salvează imaginea nouă
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
                TempData["SuccessMessage"] = "Profilul a fost actualizat cu succes!";
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

    // ViewModels
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
        
        // Informații suplimentare pentru profilul public
        public int ArticlesCount { get; set; }
        public int FollowersCount { get; set; }
    }

    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Prenumele este obligatoriu")]
        [StringLength(100, ErrorMessage = "Prenumele nu poate depăși 100 de caractere")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Numele este obligatoriu")]
        [StringLength(100, ErrorMessage = "Numele nu poate depăși 100 de caractere")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Descrierea este obligatorie")]
        [StringLength(500, ErrorMessage = "Descrierea nu poate depăși 500 de caractere")]
        public string Description { get; set; }

        public IFormFile? ProfilePicture { get; set; }

        public string? CurrentProfilePicturePath { get; set; }

        public bool IsProfilePrivate { get; set; }
    }
}

