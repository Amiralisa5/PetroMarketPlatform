using Microsoft.EntityFrameworkCore;
using PetroMarketPlatform.API.Auth;
using PetroMarketPlatform.API.Common;
using PetroMarketPlatform.API.Models;

namespace PetroMarketPlatform.API.Data;

/// <summary>Seeds RBAC, reference data, demo users and Phase-1 intelligence content for local runs.</summary>
public static class DbSeeder
{
    public static async Task SeedAsync(PetroContext db)
    {
        await SeedPermissionsAndRolesAsync(db);
        await SeedCommoditiesAsync(db);
        await SeedUsersAsync(db);
        await SeedIntelligenceAsync(db);
    }

    private static async Task SeedPermissionsAndRolesAsync(PetroContext db)
    {
        foreach (var name in Permissions.All)
            if (!await db.Permissions.AnyAsync(p => p.Name == name))
                db.Permissions.Add(new Permission { Name = name, Description = name });
        await db.SaveChangesAsync();

        foreach (var (roleName, perms) in Roles.DefaultPermissions)
        {
            var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role is null)
            {
                role = new Role { Name = roleName, IsSystem = true, Description = $"{roleName} role" };
                db.Roles.Add(role);
                await db.SaveChangesAsync();
            }
            foreach (var permName in perms)
            {
                var perm = await db.Permissions.FirstAsync(p => p.Name == permName);
                if (!await db.RolePermissions.AnyAsync(rp => rp.RoleId == role.Id && rp.PermissionId == perm.Id))
                    db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = perm.Id });
            }
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedCommoditiesAsync(PetroContext db)
    {
        if (await db.Commodities.AnyAsync()) return;
        db.Commodities.AddRange(
            new Commodity { Name = "Polyethylene (HDPE)", Category = "Polymers", Unit = "ton", Description = "High-density polyethylene" },
            new Commodity { Name = "Polypropylene (PP)", Category = "Polymers", Unit = "ton", Description = "Polypropylene granules" },
            new Commodity { Name = "Methanol", Category = "Chemicals", Unit = "ton", Description = "Industrial methanol" },
            new Commodity { Name = "Urea", Category = "Fertilizers", Unit = "ton", Description = "Granular urea fertilizer" },
            new Commodity { Name = "Sulfur", Category = "Chemicals", Unit = "ton", Description = "Granular sulfur" }
        );
        await db.SaveChangesAsync();
    }

    private static async Task SeedUsersAsync(PetroContext db)
    {
        if (await db.Users.AnyAsync()) return;

        var roleByName = await db.Roles.ToDictionaryAsync(r => r.Name, r => r.Id);

        async Task<User> Add(string mobile, string name, KycStatus kyc, string? company, params string[] roles)
        {
            Company? co = null;
            if (company is not null)
            {
                co = new Company { LegalName = company, RegistrationNumber = "REG-" + mobile[^4..], NationalId = "NID-" + mobile[^4..], VerifiedAt = kyc == KycStatus.Approved ? DateTime.UtcNow : null };
                db.Companies.Add(co);
                await db.SaveChangesAsync();
            }
            var u = new User { Mobile = mobile, FullName = name, KycStatus = kyc, CompanyId = co?.Id };
            db.Users.Add(u);
            await db.SaveChangesAsync();
            foreach (var r in roles)
                db.UserRoles.Add(new UserRole { UserId = u.Id, RoleId = roleByName[r] });
            db.Ratings.Add(new Rating { SubjectUserId = u.Id });
            await db.SaveChangesAsync();
            return u;
        }

        await Add("09120000001", "Platform Admin", KycStatus.None, null, Roles.Admin);
        await Add("09120000002", "Compliance Operator", KycStatus.None, null, Roles.Operator);
        await Add("09120000003", "Bita Trading Co (Buyer)", KycStatus.Approved, "Bita Trading Co", Roles.Buyer);
        var seller1 = await Add("09120000004", "Pars Petrochem (Seller)", KycStatus.Approved, "Pars Petrochem", Roles.Seller);
        var seller2 = await Add("09120000005", "Aria Polymer (Seller)", KycStatus.Approved, "Aria Polymer", Roles.Seller, Roles.Buyer);

        // Sample marketplace products so qualified-seller matching (BR-4) has data.
        var hdpe = await db.Commodities.FirstAsync(c => c.Name.StartsWith("Polyethylene"));
        var pp = await db.Commodities.FirstAsync(c => c.Name.StartsWith("Polypropylene"));
        db.Products.AddRange(
            new Product { SellerId = seller1.Id, CommodityId = hdpe.Id, Specs = "HDPE film grade", Availability = "2000 ton / month", PriceIndication = 520_000_000 },
            new Product { SellerId = seller1.Id, CommodityId = pp.Id, Specs = "PP injection grade", Availability = "1500 ton / month", PriceIndication = 540_000_000 },
            new Product { SellerId = seller2.Id, CommodityId = hdpe.Id, Specs = "HDPE blow molding", Availability = "1000 ton / month", PriceIndication = 515_000_000 }
        );
        await db.SaveChangesAsync();
    }

    private static async Task SeedIntelligenceAsync(PetroContext db)
    {
        if (!await db.Articles.AnyAsync())
        {
            var op = await db.Users.FirstOrDefaultAsync(u => u.FullName!.Contains("Operator"));
            db.Articles.AddRange(
                new Article { Type = ArticleType.News, Title = "رشد صادرات محصولات پتروشیمی", Slug = "petchem-exports-growth", Summary = "صادرات پتروشیمی در ماه گذشته رشد داشت.", Body = "متن کامل خبر…", Category = "Market", Tags = "export,petchem", AuthorId = op?.Id, PublishedAt = DateTime.UtcNow.AddDays(-2) },
                new Article { Type = ArticleType.News, Title = "افتتاح واحد جدید متانول", Slug = "new-methanol-unit", Summary = "یک واحد جدید متانول وارد مدار تولید شد.", Body = "متن کامل خبر…", Category = "Industry", Tags = "methanol", AuthorId = op?.Id, PublishedAt = DateTime.UtcNow.AddDays(-1) },
                new Article { Type = ArticleType.Analysis, Title = "تحلیل روند قیمت پلی‌اتیلن", Slug = "hdpe-price-analysis", Summary = "بررسی عوامل مؤثر بر قیمت پلی‌اتیلن.", Body = "متن کامل تحلیل…", Category = "Analysis", Tags = "hdpe,price", AuthorId = op?.Id, PublishedAt = DateTime.UtcNow.AddDays(-3) }
            );
            await db.SaveChangesAsync();
        }

        var commodities = await db.Commodities.ToListAsync();
        if (!await db.PriceQuotes.AnyAsync())
        {
            var rnd = new Random(42);
            foreach (var c in commodities)
            {
                decimal basePrice = 400_000_000 + rnd.Next(0, 200) * 1_000_000;
                for (var d = 30; d >= 0; d -= 2)
                {
                    var drift = (decimal)(rnd.NextDouble() - 0.5) * 10_000_000;
                    basePrice = Math.Max(100_000_000, basePrice + drift);
                    db.PriceQuotes.Add(new PriceQuote { CommodityId = c.Id, Market = "Domestic", Price = Math.Round(basePrice), Currency = "IRR", QuotedAt = DateTime.UtcNow.AddDays(-d) });
                }
            }
            await db.SaveChangesAsync();
        }

        if (!await db.TradeStats.AnyAsync())
        {
            var rnd = new Random(7);
            foreach (var c in commodities)
                for (var m = 5; m >= 0; m--)
                {
                    var bp = 380_000_000 + rnd.Next(0, 100) * 1_000_000;
                    db.TradeStats.Add(new TradeStat
                    {
                        CommodityId = c.Id,
                        Period = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-m),
                        Volume = rnd.Next(5_000, 40_000),
                        BasePrice = bp,
                        FinalPrice = bp + rnd.Next(0, 30) * 1_000_000
                    });
                }
            await db.SaveChangesAsync();
        }

        if (!await db.FutureSupplies.AnyAsync())
        {
            var rnd = new Random(11);
            foreach (var c in commodities.Take(4))
                db.FutureSupplies.Add(new FutureSupply
                {
                    CommodityId = c.Id,
                    Supplier = "NPC Supply Co",
                    Volume = rnd.Next(1_000, 10_000),
                    SupplyDate = DateTime.UtcNow.AddDays(rnd.Next(3, 30)),
                    Location = "Assaluyeh"
                });
            await db.SaveChangesAsync();
        }
    }
}
