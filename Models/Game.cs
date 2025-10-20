using System.ComponentModel.DataAnnotations;

namespace GameReviewsAPI.Models
{
    public class Game
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public int GenreId { get; set; }
        public List<Review>? Reviews { get; set; }
    }
}