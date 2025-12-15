using System;
using System.ComponentModel.DataAnnotations;

namespace PetroMarketPlatform.API.Models
{
    public class FutureSupply
    {
        [Key]
        public int FutureSupplyId { get; set; }

        [Required]
        public int CommodityId { get; set; }
        public Commodity? Commodity { get; set; }

        [Required]
        public decimal Volume { get; set; }

        [Required]
        public string Supplier { get; set; } = string.Empty;

        [Required]
        public DateTime SupplyDate { get; set; }

        public string? Location { get; set; }
    }
}
