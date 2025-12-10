using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using GameReviewsAPI.Data;
using GameReviewsAPI.Models;
using System.Security.Claims;

namespace GameReviewsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ReviewsController(AppDbContext context) => _context = context;

        // Map GUID-like string to a stable int that matches client JS hashing
        private int MapUserGuidToInt(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            int hash = 0;
            unchecked
            {
                foreach (var ch in s)
                {
                    hash = ((hash << 5) - hash) + ch;
                }
            }
            if (hash == int.MinValue) return int.MaxValue;
            return Math.Abs(hash);
        }

        // GET reviews (optionally filter by gameId)
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<object>>> GetReviews([FromQuery] int? gameId)
        {
            // determine caller identity (if any)
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int? callerId = null;
            if (!string.IsNullOrWhiteSpace(userIdClaim))
                callerId = MapUserGuidToInt(userIdClaim);

            IQueryable<Review> q = _context.Reviews;
            if (gameId.HasValue && gameId > 0)
                q = q.Where(r => r.GameId == gameId.Value);

            var list = await q.ToListAsync();

            // project to include ownership flag for the client
            var projected = list.Select(r => new {
                r.Id,
                r.Rating,
                r.Comment,
                r.GameId,
                r.UserId,
                IsOwner = callerId.HasValue && r.UserId == callerId.Value
            });

            return Ok(projected);
        }

        // GET by id
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<object>> GetReview(int id)
        {
            if (id <= 0)
                return BadRequest("A valid review ID must be provided.");

            var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == id);
            if (review == null)
                return NotFound($"No review found with ID {id}.");

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int? callerId = null;
            if (!string.IsNullOrWhiteSpace(userIdClaim))
                callerId = MapUserGuidToInt(userIdClaim);

            var projected = new {
                review.Id,
                review.Rating,
                review.Comment,
                review.GameId,
                review.UserId,
                IsOwner = callerId.HasValue && review.UserId == callerId.Value
            };

            return Ok(projected);
        }

        // POST - Allow Admin and User roles; server sets UserId from JWT
        [HttpPost]
        [Authorize(Roles = "Admin,User")]
        public async Task<ActionResult<Review>> CreateReview([FromBody] Review review)
        {
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            if (review.Rating < 1 || review.Rating > 5)
                return BadRequest("Rating must be between 1 and 5.");

            if (string.IsNullOrWhiteSpace(review.Comment))
                return BadRequest("Comment cannot be empty.");

            if (review.GameId <= 0)
                return BadRequest("GameId must be specified.");

            // Determine user ID from JWT claim (NameIdentifier is a GUID string)
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim))
                return StatusCode(403, "Unable to determine user identity from token.");

            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            bool isAdmin = userRole == "Admin";

            // For User role, always set their own ID; for Admin, allow override if provided
            if (!isAdmin)
            {
                var userId = MapUserGuidToInt(userIdClaim);
                review.UserId = userId;
                
                // Prevent review bombing: users can only have 1 review total
                // no global per-token limit here; per-game duplicate check is enforced below
                
                // For regular users, auto-generate next ID
                if (review.Id <= 0)
                {
                    var maxId = await _context.Reviews.MaxAsync(r => (int?)r.Id) ?? 0;
                    review.Id = maxId + 1;
                }
            }
            else
            {
                // Admin path: allow admin to omit Id (DB or server will assign next Id)
                if (review.Id <= 0)
                {
                    var maxId = await _context.Reviews.MaxAsync(r => (int?)r.Id) ?? 0;
                    review.Id = maxId + 1;
                }
                if (review.UserId <= 0)
                {
                    // For admin, if no UserId provided, use hash of their GUID
                    var userId = MapUserGuidToInt(userIdClaim);
                    review.UserId = userId;
                }
            }

            // Check for duplicate review by user for same game
            var existingUserReview = await _context.Reviews.FirstOrDefaultAsync(
                r => r.GameId == review.GameId && r.UserId == review.UserId);
            if (existingUserReview != null)
                return Conflict("You have already posted a review for this game.");

            // Check if review with same ID already exists (for Admin)
            if (isAdmin)
            {
                var existing = await _context.Reviews.FindAsync(review.Id);
                if (existing != null)
                    return Conflict($"A review with the ID '{review.Id}' already exists.");
            }

            try
            {
                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetReview), new { id = review.Id }, review);
            }
            catch (DbUpdateException dbEx)
            {
                if (dbEx.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
                    return Conflict("A review with this ID already exists in the database.");

                return StatusCode(500, "An unexpected database error occurred.");
            }
        }

        // PUT - Admin and User can edit their own; Admin can edit any
        [HttpPut]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> UpdateReview([FromBody] Review review)
        {
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            if (review.Id <= 0)
                return BadRequest("Review Id is required.");

            var existing = await _context.Reviews.FindAsync(review.Id);
            if (existing == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(review.Comment))
                return BadRequest("Comment cannot be empty.");

            if (review.Rating < 1 || review.Rating > 5)
                return BadRequest("Rating must be between 1 and 5.");

            // Check authorization - get user ID from JWT claim (GUID string)
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            
            if (string.IsNullOrWhiteSpace(userIdClaim))
                return StatusCode(403, "Invalid user identity.");

            var userId = MapUserGuidToInt(userIdClaim);
            bool isAdmin = userRole == "Admin";
            
            if (!isAdmin && existing.UserId != userId)
                return StatusCode(403, "You can only edit your own reviews.");

            existing.Rating = review.Rating;
            existing.Comment = review.Comment;
            existing.GameId = review.GameId;
            if (isAdmin && review.UserId > 0)
                existing.UserId = review.UserId;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE - Admin and User can delete their own; Admin can delete any
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            if (id <= 0)
                return BadRequest("Invalid ID.");

            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
                return NotFound($"No review found with ID {id}.");

            // Check authorization - get user ID from JWT claim (GUID string)
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            
            if (string.IsNullOrWhiteSpace(userIdClaim))
                return StatusCode(403, "Invalid user identity.");

            var userId = MapUserGuidToInt(userIdClaim);
            bool isAdmin = userRole == "Admin";
            
            if (!isAdmin && review.UserId != userId)
                return StatusCode(403, "You can only delete your own reviews.");

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}