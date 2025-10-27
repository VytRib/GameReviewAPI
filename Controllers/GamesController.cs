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

        // GET all games
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Game>>> GetGames()
        {
            return Ok(await _context.Games.ToListAsync());
        }

        // GET
        [HttpGet("{id}")]
        public async Task<ActionResult<Game>> GetGame(int id)
        {
            if (id <= 0)
                return BadRequest("A valid game ID must be provided.");

            var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == id);
            if (game == null)
                return NotFound($"No game found with ID {id}.");

            return Ok(game);
        }

        // POST
        [HttpPost]
        public async Task<ActionResult<Game>> CreateGame([FromBody] Game game)
        {
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            if (game.Id <= 0)
                return BadRequest("A valid 'Id' field must be provided.");

            if (string.IsNullOrWhiteSpace(game.Title))
                return BadRequest("Title cannot be empty.");

            if (game.GenreId <= 0)
                return BadRequest("GenreId must be specified.");

            var existingById = await _context.Games.FindAsync(game.Id);
            if (existingById != null)
                return Conflict($"A game with the ID '{game.Id}' already exists.");

            try
            {
                _context.Games.Add(game);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetGame), new { id = game.Id }, game);
            }
            catch (DbUpdateException dbEx)
            {
                if (dbEx.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
                    return Conflict("A game with this ID already exists in the database.");

                return StatusCode(500, "An unexpected database error occurred.");
            }
        }

        // PUT
        [HttpPut]
        public async Task<IActionResult> UpdateGame([FromBody] Game game)
        {
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            if (game.Id <= 0)
                return BadRequest("Game Id is required.");

            var existing = await _context.Games.FindAsync(game.Id);
            if (existing == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(game.Title))
                return BadRequest("Title cannot be empty.");

            if (game.GenreId <= 0)
                return BadRequest("GenreId must be specified.");

            existing.Title = game.Title;
            existing.Description = game.Description;
            existing.ImageUrl = game.ImageUrl;
            existing.GenreId = game.GenreId;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGame(int id)
        {
            if (id <= 0)
                return BadRequest("Invalid ID.");

            var game = await _context.Games.FindAsync(id);
            if (game == null)
                return NotFound($"No game found with ID {id}.");

            _context.Games.Remove(game);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}