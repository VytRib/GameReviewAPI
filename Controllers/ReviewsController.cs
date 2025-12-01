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

        // GET all reviews
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Review>>> GetReviews()
        {
            return Ok(await _context.Reviews.ToListAsync());
        }

        // GET 
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<Review>> GetReview(int id)
        {
            if (id <= 0)
                return BadRequest("A valid review ID must be provided.");

            var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == id);
            if (review == null)
                return NotFound($"No review found with ID {id}.");

            return Ok(review);
        }

        // POST 
        [Authorize(Roles = "Admin,User")]
        [HttpPost]
        public async Task<ActionResult<Review>> CreateReview([FromBody] Review review)
        {
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            if (review.Id <= 0)
                return BadRequest("A valid 'Id' must be provided and greater than zero.");

            if (review.Rating < 1 || review.Rating > 5)
                return BadRequest("Rating must be between 1 and 5.");

            if (string.IsNullOrWhiteSpace(review.Comment))
                return BadRequest("Comment cannot be empty.");

            if (review.GameId <= 0)
                return BadRequest("GameId must be specified.");

            if (review.UserId <= 0)
                return BadRequest("UserId must be specified.");

            var existingById = await _context.Reviews.FindAsync(review.Id);
            if (existingById != null)
                return Conflict($"A review with the ID '{review.Id}' already exists.");

            var userReviewForGame = await _context.Reviews
                .FirstOrDefaultAsync(r => r.GameId == review.GameId && r.UserId == review.UserId);
            
            if (userReviewForGame != null)
                return Conflict($"You can only post one review per game. You already have a review for this game.");

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

        // PUT
        [Authorize(Roles = "Admin,User")]
        [HttpPut]
        public async Task<IActionResult> UpdateReview([FromBody] Review review)
        {
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            if (review.Id <= 0)
                return BadRequest("A valid 'Id' must be provided");

            if (review.GameId <= 0)
                return BadRequest("A valid 'GameId' must be provided");

            if (string.IsNullOrWhiteSpace(review.Comment))
                return BadRequest("Comment cannot be empty.");

            if (review.Rating < 1 || review.Rating > 5)
                return BadRequest("Rating must be between 1 and 5.");

            var existing = await _context.Reviews.FindAsync(review.Id);
            if (existing == null)
                return NotFound($"No review found with ID {review.Id}.");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (!int.TryParse(userId, out var parsedUserId))
                return StatusCode(403, "Unable to determine user identity.");

            if (userRole != "Admin" && existing.UserId != parsedUserId)
                return StatusCode(403, "You can only edit your own reviews.");

            existing.Rating = review.Rating;
            existing.Comment = review.Comment;
            existing.GameId = review.GameId;
            if (userRole == "Admin")
            {
                if (review.UserId <= 0)
                    return BadRequest("A valid 'UserId' must be provided when acting as Admin.");

                existing.UserId = review.UserId;
            }

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateException dbEx)
            {
                if (dbEx.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
                    return Conflict("A database constraint prevented updating this review.");

                return StatusCode(500, "An unexpected database error occurred.");
            }
        }

        // DELETE
        [Authorize(Roles = "Admin,User")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            if (id <= 0)
                return BadRequest("A valid review ID must be provided.");

            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
                return NotFound($"No review found with ID {id}.");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (!int.TryParse(userId, out var parsedUserId) || (review.UserId != parsedUserId && userRole != "Admin"))
                return StatusCode(403, "You can only delete your own reviews. Admins can delete any review.");

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}