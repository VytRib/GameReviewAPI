using System.ComponentModel.DataAnnotations;

namespace GameReviewsAPI.Models
{
    public class Genre
    {
        [Required(ErrorMessage = "Id is required.")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; }
    }
}