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
                // We load all comments. EF Core fix-up will populate Parent/Replies relationships automatically 
                // because all entities are in the context.
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

        // GET: News/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var article = await _context.NewsArticles.FindAsync(id);
            if (article == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            // Check if user is Admin or the Owner
            if (!User.IsInRole("Administrator") && article.EditorId != user.Id)
            {
                return Forbid();
            }

            return View(article);
        }

        // POST: News/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, NewsArticle article, IFormFile? coverImage)
        {
            if (id != article.Id) return NotFound();

            var existingArticle = await _context.NewsArticles.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            if (existingArticle == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Administrator") && existingArticle.EditorId != user.Id)
            {
                return Forbid();
            }

            ModelState.Remove(nameof(article.Editor));
            ModelState.Remove(nameof(article.EditorId));
            ModelState.Remove(nameof(article.Comments));

            if (ModelState.IsValid)
            {
                try
                {
                    article.CreatedDate = existingArticle.CreatedDate;
                    article.EditorId = existingArticle.EditorId;
                    article.CoverImagePath = existingArticle.CoverImagePath;
                    article.IsEdited = true; // Mark as edited

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

                    _context.Update(article);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.NewsArticles.Any(e => e.Id == article.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Details), new { id = article.Id });
            }
            return View(article);
        }

        // POST: News/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var article = await _context.NewsArticles.FindAsync(id);
            if (article == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Administrator") && article.EditorId != user.Id)
            {
                return Forbid();
            }

            _context.NewsArticles.Remove(article);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: News/AddComment
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int articleId, string content, int? parentCommentId)
        {
            if (string.IsNullOrWhiteSpace(content)) return RedirectToAction(nameof(Details), new { id = articleId });

            var user = await _userManager.GetUserAsync(User);
            var comment = new Comment
            {
                NewsArticleId = articleId,
                UserId = user.Id,
                Content = content,
                PostedDate = DateTime.Now,
                ParentCommentId = parentCommentId
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = articleId });
        }

        // GET: News/EditComment/5
        [Authorize]
        public async Task<IActionResult> EditComment(int? id)
        {
            if (id == null) return NotFound();

            var comment = await _context.Comments.FindAsync(id);
            if (comment == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Administrator") && comment.UserId != user.Id)
            {
                return Forbid();
            }

            return View(comment);
        }

        // POST: News/EditComment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> EditComment(int id, string content)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Administrator") && comment.UserId != user.Id)
            {
                return Forbid();
            }

            if (!string.IsNullOrWhiteSpace(content))
            {
                comment.Content = content;
                comment.IsEdited = true;
                _context.Update(comment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = comment.NewsArticleId });
        }

        // POST: News/DeleteComment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var comment = await _context.Comments
                .Include(c => c.NewsArticle) // Need access to article to check editor
                .FirstOrDefaultAsync(c => c.Id == id);
                
            if (comment == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            // Allow if Admin OR Comment Owner OR Article Editor
            bool isAllowed = User.IsInRole("Administrator") 
                             || comment.UserId == user.Id
                             || comment.NewsArticle.EditorId == user.Id;

            if (!isAllowed)
            {
                return Forbid();
            }

            var articleId = comment.NewsArticleId;

            // Soft Delete Logic:
            // If the comment has replies, we must not remove it from DB or the tree will break.
            // Instead, we mark it as deleted and clear its content effectively.
            if (_context.Comments.Any(c => c.ParentCommentId == id))
            {
                comment.IsDeleted = true;
                comment.Content = "[deleted]"; // Or keep it and filter in view, but clearing it is safer/cleaner.
                // We keep the User association or set to null? 
                // Setting generic content covers privacy. 
                // We can't easily nullify UserId if it's required string, unless we allow nullable or assign a system user.
                // For now, IsDeleted flag will handle the UI display.
                _context.Update(comment);
            }
            else
            {
                // Leaf node - safe to hard delete
                _context.Comments.Remove(comment);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = articleId });
        }
    }
}
