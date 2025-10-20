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

        // GET: api/genres - returns all genres
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Genre>>> GetGenres()
        {
            return Ok(await _context.Genres.ToListAsync());
        }

        // GET: api/genres/{id} - returns single genre
        [HttpGet("{id}")]
        public async Task<ActionResult<Genre>> GetGenre(int id)
        {
            var genre = await _context.Genres.FindAsync(id);
            if (genre == null) return NotFound();
            return Ok(genre);
        }

        // GET: api/genres/{id}/games - returns all games in this genre
        [HttpGet("{id}/games")]
        public async Task<ActionResult<IEnumerable<Game>>> GetGamesByGenre(int id)
        {
            var genreExists = await _context.Genres.AnyAsync(g => g.Id == id);
            if (!genreExists) return NotFound();

            var games = await _context.Games.Where(g => g.GenreId == id).ToListAsync();
            return Ok(games);
        }

        // POST: api/genres - create a genre
        [HttpPost]
        public async Task<ActionResult<Genre>> CreateGenre(Genre genre)
        {
            _context.Genres.Add(genre);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetGenre), new { id = genre.Id }, genre);
        }

        // PUT: api/genres/{id} - update genre
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateGenre(int id, Genre genre)
        {
            if (id != genre.Id) return BadRequest();
            _context.Entry(genre).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/genres/{id} - delete genre
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGenre(int id)
        {
            var genre = await _context.Genres.FindAsync(id);
            if (genre == null) return NotFound();
            _context.Genres.Remove(genre);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}