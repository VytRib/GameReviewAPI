using System.ComponentModel.DataAnnotations;

namespace GameReviewsAPI.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [Required]
        public string Comment { get; set; }

        public int GameId { get; set; }
        public int UserId { get; set; } 
    }
}