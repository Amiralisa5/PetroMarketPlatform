using System;
using System.ComponentModel.DataAnnotations;

namespace PetroMarketPlatform.API.Models
{
    public class PurchaseRequest
    {
        [Key]
        public int PurchaseRequestId { get; set; }

        [Required]
        public int CommodityId { get; set; }
        public Commodity? Commodity { get; set; }

        [Required]
        public int BuyerId { get; set; }
        public User? Buyer { get; set; }

        [Required]
        public decimal Quantity { get; set; }

        [Required]
        public DateTime DeliveryDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Offer>? Offers { get; set; }
    }
}
