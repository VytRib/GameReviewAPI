using System.ComponentModel.DataAnnotations;

namespace GameReviewsAPI.Models
{
    public class Genre
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public List<Game>? Games { get; set; }
    }
}