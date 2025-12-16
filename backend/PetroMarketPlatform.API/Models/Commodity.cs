using System.ComponentModel.DataAnnotations;

namespace PetroMarketPlatform.API.Models
{
    public class Commodity
    {
        [Key]
        public int CommodityId { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        [Required]
        public string Unit { get; set; } = string.Empty;
    }
}
