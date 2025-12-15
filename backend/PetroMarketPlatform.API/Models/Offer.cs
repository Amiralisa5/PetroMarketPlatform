using System;
using System.ComponentModel.DataAnnotations;

namespace PetroMarketPlatform.API.Models
{
    public class Offer
    {
        [Key]
        public int OfferId { get; set; }

        [Required]
        public int PurchaseRequestId { get; set; }
        public PurchaseRequest? PurchaseRequest { get; set; }

        [Required]
        public int SellerId { get; set; }
        public User? Seller { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public decimal Quantity { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
