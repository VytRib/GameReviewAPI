using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using GameReviewsAPI.Models;
using GameReviewsAPI.Data;

namespace GameReviewsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GenresController : ControllerBase
    {
        private readonly AppDbContext _context;
        public GenresController(AppDbContext context) => _context = context;

        // GET ALL
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Genre>>> GetGenres()
        {
            return Ok(await _context.Genres.ToListAsync());
        }

        // GET
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<Genre>> GetGenre(int id)
        {
            if (id <= 0)
                return BadRequest("A valid 'Id' must be provided.");

            var genre = await _context.Genres.FindAsync(id);
            if (genre == null)
                return NotFound($"No genre found with ID {id}.");

            return Ok(genre);
        }

        /*// GET all games in a genre
        [HttpGet("{genreId}/games")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Game>>> GetGamesByGenre(int genreId)
        {
            if (genreId <= 0)
                return BadRequest("A valid 'genreId' must be provided.");

            var genreExists = await _context.Genres.AnyAsync(g => g.Id == genreId);
            if (!genreExists) return NotFound("Genre not found.");

            var games = await _context.Games.Where(g => g.GenreId == genreId).ToListAsync();
            return Ok(games);
        }

        // GET a single game in a genre
        [HttpGet("{genreId}/games/{gameId}")]
        [AllowAnonymous]
        public async Task<ActionResult<Game>> GetGameByGenre(int genreId, int gameId)
        {
            if (genreId <= 0 || gameId <= 0)
                return BadRequest("Valid 'genreId' and 'gameId' must be provided.");

            var genreExists = await _context.Genres.AnyAsync(g => g.Id == genreId);
            if (!genreExists) return NotFound("Genre not found.");

            var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == gameId && g.GenreId == genreId);
            if (game == null)
                return NotFound("Game not found in this genre.");

            return Ok(game);
        }

        // GET reviews for a game in a genre
        [HttpGet("{genreId}/games/{gameId}/reviews")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Review>>> GetReviewsByGameInGenre(int genreId, int gameId)
        {
            if (genreId <= 0 || gameId <= 0)
                return BadRequest("Valid 'genreId' and 'gameId' must be provided.");

            var genreExists = await _context.Genres.AnyAsync(g => g.Id == genreId);
            if (!genreExists) return NotFound("Genre not found.");

            var game = await _context.Games.FindAsync(gameId);
            if (game == null || game.GenreId != genreId)
                return NotFound("Game not found in this genre.");

            var reviews = await _context.Reviews.Where(r => r.GameId == gameId).ToListAsync();
            return Ok(reviews);
        }*/

        // GET
        [HttpGet("{genreId}/games/{gameId}/reviews/{reviewId}")]
        [AllowAnonymous]
        public async Task<ActionResult<Review>> GetReviewByGameInGenre(int genreId, int gameId, int reviewId)
        {
            if (genreId <= 0 || gameId <= 0 || reviewId <= 0)
                return BadRequest("Valid 'genreId', 'gameId', and 'reviewId' must be provided.");

            var genreExists = await _context.Genres.AnyAsync(g => g.Id == genreId);
            if (!genreExists) return NotFound("Genre not found.");

            var game = await _context.Games.FindAsync(gameId);
            if (game == null || game.GenreId != genreId)
                return NotFound("Game not found in this genre.");

            var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId && r.GameId == gameId);
            if (review == null)
                return NotFound("Review not found for this game.");

            return Ok(review);
        }

        // POST 
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<Genre>> CreateGenre([FromBody] Genre genre)
        {
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            if (genre.Id <= 0)
                return BadRequest("A valid 'Id' must be provided.");

            if (string.IsNullOrWhiteSpace(genre.Name))
                return BadRequest("Genre 'Name' cannot be empty.");

            var existingById = await _context.Genres.FindAsync(genre.Id);
            if (existingById != null)
                return Conflict($"A genre with the ID '{genre.Id}' already exists.");

            var existingByName = await _context.Genres
                .FirstOrDefaultAsync(g => g.Name.ToLower() == genre.Name.ToLower());

            if (existingByName != null)
                return Conflict($"A genre with the name '{genre.Name}' already exists.");

            try
            {
                _context.Genres.Add(genre);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetGenre), new { id = genre.Id }, genre);
            }
            catch (DbUpdateException dbEx)
            {
                if (dbEx.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
                    return Conflict("A genre with this ID already exists in the database.");

                return StatusCode(500, "An unexpected database error occurred.");
            }
        }

        // PUT
        [Authorize(Roles = "Admin")]
        [HttpPut]
        public async Task<IActionResult> UpdateGenre([FromBody] Genre genre)
        {
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            if (genre.Id <= 0)
                return BadRequest("A valid 'Id' must be provided.");

            var existing = await _context.Genres.FindAsync(genre.Id);
            if (existing == null)
                return NotFound("Genre not found.");

            if (string.IsNullOrWhiteSpace(genre.Name))
                return BadRequest("Genre 'Name' cannot be empty.");

            existing.Name = genre.Name;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGenre(int id)
        {
            var genre = await _context.Genres.FindAsync(id);
            if (genre == null)
                return NotFound("Genre not found.");

            _context.Genres.Remove(genre);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}