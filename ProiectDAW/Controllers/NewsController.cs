using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProiectDAW.Data;
using ProiectDAW.Models;

namespace ProiectDAW.Controllers
{
    public class NewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public NewsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        // GET: News
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var news = await _context.NewsArticles
                .Include(n => n.Editor)
                .Where(n => !n.Editor.IsProfilePrivate)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();
            return View(news);
        }

        // GET: News/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var article = await _context.NewsArticles
                .Include(n => n.Editor)
                .Include(n => n.Comments).ThenInclude(c => c.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (article == null) return NotFound();

            return View(article);
        }

        // GET: News/Create
        [Authorize(Roles = "Editor,Administrator")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: News/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Editor,Administrator")]
        public async Task<IActionResult> Create(NewsArticle article, IFormFile? coverImage)
        {
            // Remove navigation properties from validation
            ModelState.Remove(nameof(article.Editor));
            ModelState.Remove(nameof(article.EditorId));
            ModelState.Remove(nameof(article.Comments));

            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                article.EditorId = user.Id;
                article.CreatedDate = DateTime.Now;

                // Handle Image Upload
                if (coverImage != null && coverImage.Length > 0)
                {
                    var storagePath = Path.Combine(_env.WebRootPath, "images", "news");
                    if (!Directory.Exists(storagePath)) Directory.CreateDirectory(storagePath);

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(coverImage.FileName);
                    var filePath = Path.Combine(storagePath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await coverImage.CopyToAsync(stream);
                    }

                    article.CoverImagePath = "/images/news/" + fileName;
                }

                _context.Add(article);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(article);
        }

        // POST: News/AddComment
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int articleId, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return RedirectToAction(nameof(Details), new { id = articleId });

            var user = await _userManager.GetUserAsync(User);
            var comment = new Comment
            {
                NewsArticleId = articleId,
                UserId = user.Id,
                Content = content,
                PostedDate = DateTime.Now
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = articleId });
        }
    }
}
