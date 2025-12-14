using Microsoft.EntityFrameworkCore;
using PetroMarketPlatform.API.Models;

namespace PetroMarketPlatform.API.Data
{
    public class PetroContext : DbContext
    {
        public PetroContext(DbContextOptions<PetroContext> options) : base(options) { }

        public DbSet<Commodity> Commodities { get; set; } = default!;
        public DbSet<User> Users { get; set; } = default!;
        public DbSet<PurchaseRequest> PurchaseRequests { get; set; } = default!;
        public DbSet<Offer> Offers { get; set; } = default!;
public DbSet<FutureSupply> FutureSupplies { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Additional configuration can be added here
        }
    }
}
