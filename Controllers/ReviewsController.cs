using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameReviewsAPI.Data;
using GameReviewsAPI.Models;

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
        public async Task<ActionResult<IEnumerable<Review>>> GetReviews()
        {
            return Ok(await _context.Reviews.ToListAsync());
        }

        // GET 
        [HttpGet("{id}")]
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
        [HttpPut]
        public async Task<IActionResult> UpdateReview([FromBody] Review review)
        {
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            if (review.Id <= 0)
                return BadRequest("A valid 'Id' must be provided");

            if (review.GameId <= 0)
                return BadRequest("A valid 'GameId' must be provided");

            if (review.UserId <= 0)
                return BadRequest("A valid 'UserId' must be provided");

            if (string.IsNullOrWhiteSpace(review.Comment))
                return BadRequest("Comment cannot be empty.");

            if (review.Rating < 1 || review.Rating > 5)
                return BadRequest("Rating must be between 1 and 5.");

            var existing = await _context.Reviews.FindAsync(review.Id);
            if (existing == null)
                return NotFound($"No review found with ID {review.Id}.");

            existing.Rating = review.Rating;
            existing.Comment = review.Comment;
            existing.GameId = review.GameId;
            existing.UserId = review.UserId;

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
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            if (id <= 0)
                return BadRequest("A valid review ID must be provided.");

            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
                return NotFound($"No review found with ID {id}.");

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}