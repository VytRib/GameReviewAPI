using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GameReviewsAPI.Models
{
    public class Game
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        public string Title { get; set; }

        public string? Description { get; set; }
        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "GenreId is required.")]
        public int GenreId { get; set; }

        //[JsonIgnore] // Prevent Swagger / JSON from serializing this
        //public List<Review>? Reviews { get; set; }
    }
}