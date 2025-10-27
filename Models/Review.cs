using System.ComponentModel.DataAnnotations;

namespace GameReviewsAPI.Models
{
    public class Review
    {
        [Required(ErrorMessage = "Id is required.")]
        public int Id { get; set; }

        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Comment is required.")]
        public string Comment { get; set; }

        [Required(ErrorMessage = "GameId is required.")]
        public int GameId { get; set; }

        [Required(ErrorMessage = "UserId is required.")]
        public int UserId { get; set; }
    }
}