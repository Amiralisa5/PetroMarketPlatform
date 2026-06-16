using Microsoft.EntityFrameworkCore;
using PetroMarketPlatform.API.Common;
using PetroMarketPlatform.API.Models;

namespace PetroMarketPlatform.API.Data;

public class PetroContext : DbContext
{
    public PetroContext(DbContextOptions<PetroContext> options) : base(options) { }

    // Identity & RBAC
    public DbSet<User> Users => Set<User>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<OtpRequest> OtpRequests => Set<OtpRequest>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // KYC
    public DbSet<KycCase> KycCases => Set<KycCase>();
    public DbSet<KycDocument> KycDocuments => Set<KycDocument>();

    // Catalog / marketplace
    public DbSet<Commodity> Commodities => Set<Commodity>();
    public DbSet<Product> Products => Set<Product>();

    // Auction
    public DbSet<Rfq> Rfqs => Set<Rfq>();
    public DbSet<Competition> Competitions => Set<Competition>();
    public DbSet<Bid> Bids => Set<Bid>();
    public DbSet<BidRevision> BidRevisions => Set<BidRevision>();
    public DbSet<Shortlist> Shortlists => Set<Shortlist>();

    // Trade / reputation
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Survey> Surveys => Set<Survey>();
    public DbSet<Rating> Ratings => Set<Rating>();

    // Intelligence
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<PriceQuote> PriceQuotes => Set<PriceQuote>();
    public DbSet<TradeStat> TradeStats => Set<TradeStat>();
    public DbSet<FutureSupply> FutureSupplies => Set<FutureSupply>();
    public DbSet<SupplyAlert> SupplyAlerts => Set<SupplyAlert>();

    // Platform
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void ConfigureConventions(ModelConfigurationBuilder b)
    {
        // Store enums as readable strings; give money/quantity a sane precision.
        b.Properties<KycStatus>().HaveConversion<string>();
        b.Properties<RfqStatus>().HaveConversion<string>();
        b.Properties<CompetitionStatus>().HaveConversion<string>();
        b.Properties<CompetitionVisibility>().HaveConversion<string>();
        b.Properties<TransactionStatus>().HaveConversion<string>();
        b.Properties<ArticleType>().HaveConversion<string>();
        b.Properties<NotificationChannel>().HaveConversion<string>();
        b.Properties<NotificationStatus>().HaveConversion<string>();
        b.Properties<OtpPurpose>().HaveConversion<string>();
        b.Properties<decimal>().HavePrecision(18, 4);
    }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // ---- RBAC join tables ----
        mb.Entity<RolePermission>().HasKey(x => new { x.RoleId, x.PermissionId });
        mb.Entity<RolePermission>()
            .HasOne(x => x.Role).WithMany(r => r.RolePermissions).HasForeignKey(x => x.RoleId);
        mb.Entity<RolePermission>()
            .HasOne(x => x.Permission).WithMany(p => p.RolePermissions).HasForeignKey(x => x.PermissionId);

        mb.Entity<UserRole>().HasKey(x => new { x.UserId, x.RoleId });
        mb.Entity<UserRole>()
            .HasOne(x => x.User).WithMany(u => u.UserRoles).HasForeignKey(x => x.UserId);
        mb.Entity<UserRole>()
            .HasOne(x => x.Role).WithMany(r => r.UserRoles).HasForeignKey(x => x.RoleId);

        // ---- Unique indexes ----
        mb.Entity<User>().HasIndex(u => u.Mobile).IsUnique();
        mb.Entity<Role>().HasIndex(r => r.Name).IsUnique();
        mb.Entity<Permission>().HasIndex(p => p.Name).IsUnique();
        mb.Entity<Competition>().HasIndex(c => c.RfqId).IsUnique();
        mb.Entity<Rating>().HasIndex(r => r.SubjectUserId).IsUnique();
        mb.Entity<Article>().HasIndex(a => a.Slug).IsUnique();
        mb.Entity<BidRevision>().HasIndex(r => new { r.BidId, r.Version }).IsUnique();
        mb.Entity<Survey>().HasIndex(s => new { s.TransactionId, s.RaterId }).IsUnique();
        mb.Entity<OtpRequest>().HasIndex(o => new { o.Mobile, o.Consumed });

        // ---- User 1—1 Rating / 1—* Company ----
        mb.Entity<User>()
            .HasOne(u => u.Rating).WithOne(r => r.SubjectUser)
            .HasForeignKey<Rating>(r => r.SubjectUserId)
            .OnDelete(DeleteBehavior.Cascade);
        mb.Entity<User>()
            .HasOne(u => u.Company).WithMany(c => c.Users)
            .HasForeignKey(u => u.CompanyId).OnDelete(DeleteBehavior.SetNull);

        // ---- KYC ----
        mb.Entity<KycCase>()
            .HasOne(k => k.User).WithMany().HasForeignKey(k => k.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        mb.Entity<KycCase>()
            .HasOne(k => k.Reviewer).WithMany().HasForeignKey(k => k.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);
        mb.Entity<KycCase>()
            .HasOne(k => k.Company).WithMany().HasForeignKey(k => k.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);
        mb.Entity<KycDocument>()
            .HasOne(d => d.KycCase).WithMany(k => k.Documents).HasForeignKey(d => d.KycCaseId)
            .OnDelete(DeleteBehavior.Cascade);

        // ---- Auction ----
        mb.Entity<Rfq>()
            .HasOne(r => r.Buyer).WithMany().HasForeignKey(r => r.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);
        mb.Entity<Rfq>()
            .HasOne(r => r.Commodity).WithMany().HasForeignKey(r => r.CommodityId)
            .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<Competition>()
            .HasOne(c => c.Rfq).WithOne(r => r.Competition).HasForeignKey<Competition>(c => c.RfqId)
            .OnDelete(DeleteBehavior.Cascade);
        mb.Entity<Competition>()
            .HasOne(c => c.WinningBid).WithMany().HasForeignKey(c => c.WinningBidId)
            .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<Bid>()
            .HasOne(b => b.Competition).WithMany(c => c.Bids).HasForeignKey(b => b.CompetitionId)
            .OnDelete(DeleteBehavior.Cascade);
        mb.Entity<Bid>()
            .HasOne(b => b.Seller).WithMany().HasForeignKey(b => b.SellerId)
            .OnDelete(DeleteBehavior.Restrict);
        mb.Entity<Bid>().HasIndex(b => new { b.CompetitionId, b.SellerId }).IsUnique();

        mb.Entity<BidRevision>()
            .HasOne(r => r.Bid).WithMany(b => b.Revisions).HasForeignKey(r => r.BidId)
            .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<Shortlist>()
            .HasOne(s => s.Competition).WithMany(c => c.Shortlists).HasForeignKey(s => s.CompetitionId)
            .OnDelete(DeleteBehavior.Cascade);
        mb.Entity<Shortlist>()
            .HasOne(s => s.Bid).WithMany().HasForeignKey(s => s.BidId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---- Trade ----
        mb.Entity<Transaction>()
            .HasOne(t => t.Competition).WithOne(c => c.Transaction).HasForeignKey<Transaction>(t => t.CompetitionId)
            .OnDelete(DeleteBehavior.Cascade);
        mb.Entity<Transaction>()
            .HasOne(t => t.Buyer).WithMany().HasForeignKey(t => t.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);
        mb.Entity<Transaction>()
            .HasOne(t => t.Seller).WithMany().HasForeignKey(t => t.SellerId)
            .OnDelete(DeleteBehavior.Restrict);
        mb.Entity<Transaction>()
            .HasOne(t => t.Commodity).WithMany().HasForeignKey(t => t.CommodityId)
            .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<Survey>()
            .HasOne(s => s.Transaction).WithMany(t => t.Surveys).HasForeignKey(s => s.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);
        mb.Entity<Survey>()
            .HasOne(s => s.Rater).WithMany().HasForeignKey(s => s.RaterId)
            .OnDelete(DeleteBehavior.Restrict);
        mb.Entity<Survey>()
            .HasOne(s => s.Ratee).WithMany().HasForeignKey(s => s.RateeId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---- Marketplace ----
        mb.Entity<Product>()
            .HasOne(p => p.Seller).WithMany().HasForeignKey(p => p.SellerId)
            .OnDelete(DeleteBehavior.Cascade);
        mb.Entity<Product>()
            .HasOne(p => p.Commodity).WithMany().HasForeignKey(p => p.CommodityId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---- Intelligence ----
        mb.Entity<PriceQuote>()
            .HasOne(p => p.Commodity).WithMany().HasForeignKey(p => p.CommodityId)
            .OnDelete(DeleteBehavior.Cascade);
        mb.Entity<TradeStat>()
            .HasOne(t => t.Commodity).WithMany().HasForeignKey(t => t.CommodityId)
            .OnDelete(DeleteBehavior.Cascade);
        mb.Entity<FutureSupply>()
            .HasOne(f => f.Commodity).WithMany().HasForeignKey(f => f.CommodityId)
            .OnDelete(DeleteBehavior.Cascade);
        mb.Entity<SupplyAlert>()
            .HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        mb.Entity<SupplyAlert>()
            .HasOne(s => s.Commodity).WithMany().HasForeignKey(s => s.CommodityId)
            .OnDelete(DeleteBehavior.SetNull);

        mb.Entity<PriceQuote>().HasIndex(p => new { p.CommodityId, p.QuotedAt });
        mb.Entity<TradeStat>().HasIndex(t => new { t.CommodityId, t.Period });
        mb.Entity<FutureSupply>().HasIndex(f => f.SupplyDate);
        mb.Entity<Article>().HasIndex(a => new { a.Type, a.PublishedAt });

        // ---- Platform ----
        mb.Entity<Notification>()
            .HasOne(n => n.User).WithMany().HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        mb.Entity<Notification>().HasIndex(n => new { n.UserId, n.Status });
        mb.Entity<AuditLog>().HasIndex(a => new { a.Entity, a.EntityId });
        mb.Entity<RefreshToken>().HasIndex(t => t.TokenHash);
    }
}
