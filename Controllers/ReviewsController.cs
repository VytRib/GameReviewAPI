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

        [HttpGet] // Get all reviews
        public async Task<ActionResult<IEnumerable<Review>>> GetReviews()
        {
            return Ok(await _context.Reviews.ToListAsync());
        }

        [HttpGet("{id}")] // Get single review
        public async Task<ActionResult<Review>> GetReview(int id)
        {
            var review = await _context.Reviews
                                    .FirstOrDefaultAsync(r => r.Id == id);
            if (review == null) return NotFound();
            return Ok(review);
        }

        [HttpPost] // Create review
        public async Task<ActionResult<Review>> CreateReview(Review review)
        {
            if (review.Rating < 1 || review.Rating > 5) return BadRequest("Rating must be 1–5.");
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetReview), new { id = review.Id }, review);
        }

        [HttpPut("{id}")] // Update review
        public async Task<IActionResult> UpdateReview(int id, Review review)
        {
            if (id != review.Id) return BadRequest();
            if (review.Rating < 1 || review.Rating > 5) return BadRequest("Rating must be 1–5.");
            _context.Entry(review).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")] // Delete review
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();
            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}