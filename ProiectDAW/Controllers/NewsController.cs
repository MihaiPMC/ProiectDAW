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
        private readonly Services.IAiFactCheckService _aiService;

        public NewsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env, Services.IAiFactCheckService aiService)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
            _aiService = aiService;
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

            if (User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);
                var userVotes = await _context.ArticleVotes
                    .Where(v => v.UserId == userId)
                    .ToDictionaryAsync(v => v.NewsArticleId, v => v.Value);
                ViewData["UserArticleVotes"] = userVotes;
            }


            var articleScores = await _context.ArticleVotes
                .GroupBy(v => v.NewsArticleId)
                .Select(g => new { ArticleId = g.Key, Score = g.Sum(v => v.Value) })
                .ToDictionaryAsync(x => x.ArticleId, x => x.Score);
            ViewData["ArticleScores"] = articleScores;

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

            if (article == null) return NotFound();

            if (User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);
                
                // Article Vote
                var articleVote = await _context.ArticleVotes
                    .FirstOrDefaultAsync(v => v.NewsArticleId == id && v.UserId == userId);
                ViewData["UserArticleVote"] = articleVote?.Value ?? 0;

                // Comment Votes
                var commentVotes = await _context.CommentVotes
                    .Where(v => v.UserId == userId && v.Comment.NewsArticleId == id)
                    .ToDictionaryAsync(v => v.CommentId, v => v.Value);
                ViewData["UserCommentVotes"] = commentVotes;
            }

            // Article Score
            ViewData["ArticleScore"] = await _context.ArticleVotes
                .Where(v => v.NewsArticleId == id)
                .SumAsync(v => v.Value);

             // Comments Scores
            var commentScores = await _context.CommentVotes
                .Where(v => v.Comment.NewsArticleId == id)
                .GroupBy(v => v.CommentId)
                .Select(g => new { CommentId = g.Key, Score = g.Sum(v => v.Value) })
                .ToDictionaryAsync(x => x.CommentId, x => x.Score);
            ViewData["CommentScores"] = commentScores;

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

            ModelState.Remove(nameof(article.Editor));
            ModelState.Remove(nameof(article.EditorId));
            ModelState.Remove(nameof(article.Comments));

            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                article.EditorId = user.Id;
                article.CreatedDate = DateTime.Now;

                // AI Fact Check
                var (trustScore, trustLabel, justification) = await _aiService.GetContentTrustScoreAsync(article.Title, article.Content);
                article.TrustScore = trustScore;
                article.TrustLabel = trustLabel;
                article.AiJustification = justification;

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


            if (_context.Comments.Any(c => c.ParentCommentId == id))
            {
                comment.IsDeleted = true;
                comment.Content = "[deleted]"; // Or keep it and filter in view, but clearing it is safer/cleaner.

                _context.Update(comment);
            }
            else
            {

                _context.Comments.Remove(comment);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = articleId });
        }
        // POST: News/VoteArticle
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VoteArticle(int articleId, int voteValue)
        {
            if (voteValue < -1 || voteValue > 1) return BadRequest("Invalid vote value.");

            var user = await _userManager.GetUserAsync(User);
            var existingVote = await _context.ArticleVotes
                .FirstOrDefaultAsync(v => v.NewsArticleId == articleId && v.UserId == user.Id);

            if (existingVote != null)
            {

                if (existingVote.Value == voteValue)
                {
                    _context.ArticleVotes.Remove(existingVote);
                }
                else
                {
                    // Update vote
                    existingVote.Value = voteValue;
                    _context.Update(existingVote);
                }
            }
            else
            {
                // New vote

                if (voteValue != 0)
                {
                    var vote = new ArticleVote
                    {
                        NewsArticleId = articleId,
                        UserId = user.Id,
                        Value = voteValue
                    };
                    _context.ArticleVotes.Add(vote);
                }
            }

            await _context.SaveChangesAsync();

            // Calculate new score
            var score = await _context.ArticleVotes
                .Where(v => v.NewsArticleId == articleId)
                .SumAsync(v => v.Value);

            return Json(new { score = score });
        }

        // POST: News/VoteComment
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VoteComment(int commentId, int voteValue)
        {
            if (voteValue < -1 || voteValue > 1) return BadRequest("Invalid vote value.");

            var user = await _userManager.GetUserAsync(User);
            var existingVote = await _context.CommentVotes
                .FirstOrDefaultAsync(v => v.CommentId == commentId && v.UserId == user.Id);

            if (existingVote != null)
            {
                if (existingVote.Value == voteValue)
                {
                    _context.CommentVotes.Remove(existingVote);
                }
                else
                {
                    existingVote.Value = voteValue;
                    _context.Update(existingVote);
                }
            }
            else
            {
                if (voteValue != 0)
                {
                    var vote = new CommentVote
                    {
                        CommentId = commentId,
                        UserId = user.Id,
                        Value = voteValue
                    };
                    _context.CommentVotes.Add(vote);
                }
            }

            await _context.SaveChangesAsync();

            var score = await _context.CommentVotes
                .Where(v => v.CommentId == commentId)
                .SumAsync(v => v.Value);

            return Json(new { score = score });
        }
    }
}
