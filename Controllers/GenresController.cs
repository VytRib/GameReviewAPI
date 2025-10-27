using Microsoft.AspNetCore.Mvc;
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
        public async Task<ActionResult<IEnumerable<Genre>>> GetGenres()
        {
            return Ok(await _context.Genres.ToListAsync());
        }

        // GET
        [HttpGet("{id}")]
        public async Task<ActionResult<Genre>> GetGenre(int id)
        {
            if (id <= 0)
                return BadRequest("A valid 'Id' must be provided.");

            var genre = await _context.Genres.FindAsync(id);
            if (genre == null)
                return NotFound($"No genre found with ID {id}.");

            return Ok(genre);
        }

        // GET
        [HttpGet("{id}/games")]
        public async Task<ActionResult<IEnumerable<Game>>> GetGamesByGenre(int id)
        {
            var genreExists = await _context.Genres.AnyAsync(g => g.Id == id);
            if (!genreExists) return NotFound("Genre not found.");

            var games = await _context.Games.Where(g => g.GenreId == id).ToListAsync();
            return Ok(games);
        }

        // POST 
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