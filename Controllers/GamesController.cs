using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameReviewsAPI.Data;
using GameReviewsAPI.Models;

namespace GameReviewsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GamesController : ControllerBase
    {
        private readonly AppDbContext _context;
        public GamesController(AppDbContext context) => _context = context;

        [HttpGet] // Get all games
        public async Task<ActionResult<IEnumerable<Game>>> GetGames()
        {
            return Ok(await _context.Games.ToListAsync());
        }

        [HttpGet("{id}")] // Get single game
        public async Task<ActionResult<Game>> GetGame(int id)
        {
            var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == id);
            if (game == null) return NotFound();
            return Ok(game);
        }

        [HttpPost] // Create game
        public async Task<ActionResult<Game>> CreateGame(Game game)
        {
            _context.Games.Add(game);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetGame), new { id = game.Id }, game);
        }

        [HttpPut("{id}")] // Update game
        public async Task<IActionResult> UpdateGame(int id, Game game)
        {
            if (id != game.Id) return BadRequest();
            _context.Entry(game).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")] // Delete game
        public async Task<IActionResult> DeleteGame(int id)
        {
            var game = await _context.Games.FindAsync(id);
            if (game == null) return NotFound();
            _context.Games.Remove(game);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}