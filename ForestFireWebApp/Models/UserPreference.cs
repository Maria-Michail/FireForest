using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ForestFireWebApp.Data;

namespace ForestFireWebApp.Models
{
    public class UserPreference
    {
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; } = default!;
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = default!;
        [Required]
        public string State { get; set; } = default!;
    }
}
