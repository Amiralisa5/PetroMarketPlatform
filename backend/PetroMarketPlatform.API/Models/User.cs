using System.ComponentModel.DataAnnotations;

namespace PetroMarketPlatform.API.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;

        public int Rank { get; set; } = 0;

        public ICollection<PurchaseRequest>? PurchaseRequests { get; set; }
        public ICollection<Offer>? Offers { get; set; }
    }
}
