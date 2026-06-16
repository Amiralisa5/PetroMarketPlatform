namespace PetroMarketPlatform.API.Auth;

/// <summary>
/// Named, fine-grained permissions (RBAC, §3). Roles are bundles of these and are
/// editable at runtime by an Admin, so Operator scope can change without code edits.
/// </summary>
public static class Permissions
{
    // KYC / identity
    public const string KycComplete = "kyc.complete";
    public const string KycReview = "kyc.review";

    // RFQ / competition
    public const string RfqManage = "rfq.manage";
    public const string BidSubmit = "bid.submit";
    public const string CompetitionShortlist = "competition.shortlist";
    public const string CompetitionMonitor = "competition.monitor";
    public const string CompetitionIntervene = "competition.intervene";

    // Marketplace
    public const string ProductManage = "product.manage";

    // Feedback
    public const string SurveySubmit = "survey.submit";

    // Admin / operator
    public const string UsersManage = "users.manage";
    public const string ContentManage = "content.manage";
    public const string ReportsView = "reports.view";
    public const string SettingsManage = "settings.manage";
    public const string AuditView = "audit.view";

    public static readonly string[] All =
    {
        KycComplete, KycReview, RfqManage, BidSubmit, CompetitionShortlist,
        CompetitionMonitor, CompetitionIntervene, ProductManage, SurveySubmit,
        UsersManage, ContentManage, ReportsView, SettingsManage, AuditView
    };
}

/// <summary>System role names and their default permission bundles (the §3 matrix). Seeded, then editable.</summary>
public static class Roles
{
    public const string Buyer = "Buyer";
    public const string Seller = "Seller";
    public const string Operator = "Operator";
    public const string Admin = "Admin";

    public static readonly Dictionary<string, string[]> DefaultPermissions = new()
    {
        [Buyer] = new[]
        {
            Permissions.KycComplete, Permissions.RfqManage,
            Permissions.CompetitionShortlist, Permissions.SurveySubmit
        },
        [Seller] = new[]
        {
            Permissions.KycComplete, Permissions.ProductManage,
            Permissions.BidSubmit, Permissions.SurveySubmit
        },
        [Operator] = new[]
        {
            Permissions.KycReview, Permissions.ContentManage,
            Permissions.CompetitionMonitor, Permissions.ReportsView, Permissions.AuditView
        },
        [Admin] = Permissions.All // superuser
    };
}
