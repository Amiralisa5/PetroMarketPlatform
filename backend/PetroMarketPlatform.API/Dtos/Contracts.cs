using PetroMarketPlatform.API.Common;

namespace PetroMarketPlatform.API.Dtos;

// ---- Auth ----
public record OtpRequestDto(string Mobile);
public record VerifyDto(string Mobile, string Code, string? FullName, List<string>? Roles);
public record RefreshDto(string RefreshToken);
public record AuthResponse(string AccessToken, string RefreshToken, DateTime AccessExpiresAt,
    int UserId, string[] Roles, string[] Permissions);
public record MeDto(int Id, string Mobile, string? FullName, string KycStatus,
    string[] Roles, string[] Permissions, int? CompanyId);

// ---- KYC ----
public record CompanyDto(string LegalName, string? RegistrationNumber, string? NationalId, string? Address);
public record ReviewDto(string Decision, string? Notes);
public record KycDocumentDto(int Id, string Type, string? FileName, DateTime UploadedAt);
public record KycCaseDto(int Id, int UserId, string? UserName, string Status, int? CompanyId,
    string? CompanyName, string? Notes, DateTime UpdatedAt, List<KycDocumentDto> Documents);

// ---- Marketplace ----
public record CreateProductDto(int CommodityId, string? Specs, string? Availability, decimal? PriceIndication);
public record ProductDto(int Id, int SellerId, string SellerName, int CommodityId, string CommodityName,
    string? Specs, string? Availability, decimal? PriceIndication, bool IsActive);
public record SellerProfileDto(int SellerId, string? Name, string? CompanyName, double StarScore,
    string MetricsJson, List<ProductDto> Products);

// ---- RFQ / Competition ----
public record CreateRfqDto(int CommodityId, decimal Quantity, string? DeliveryTerms, string? PaymentTerms,
    string? Notes, DateTime Deadline, CompetitionVisibility Visibility = CompetitionVisibility.Confidential);
public record RfqDto(int Id, int BuyerId, int CommodityId, string CommodityName, decimal Quantity,
    string? DeliveryTerms, string? PaymentTerms, string? Notes, DateTime Deadline, string Status, DateTime CreatedAt);
public record CompetitionDto(int Id, int RfqId, string CommodityName, decimal Quantity, DateTime BidWindowStart,
    DateTime BidWindowEnd, string Status, string Visibility, bool IdentitiesRevealed, int BidCount,
    int? WinningBidId, RfqDto Rfq);

public record SubmitBidDto(decimal Price, decimal Quantity);
/// <summary>Anonymous projection shown during bidding (BR-3): no seller identity.</summary>
public record BidPublicDto(int Id, int Rank, decimal Price, decimal Quantity, int Version,
    DateTime SubmittedAt, bool IsMine);
/// <summary>Revealed projection shown to the buyer after shortlist (BR-6) and to admins.</summary>
public record BidRevealedDto(int Id, int Rank, decimal Price, decimal Quantity, int Version,
    DateTime SubmittedAt, int SellerId, string? SellerName, string? CompanyName, double StarScore, bool Shortlisted);

public record ShortlistDto(List<int> BidIds);
public record SelectWinnerDto(int BidId);

// ---- Transactions & surveys ----
public record TransactionDto(int Id, int CompetitionId, int BuyerId, int SellerId, int CommodityId,
    string CommodityName, decimal Quantity, decimal Price, string Status, DateTime? CompletedAt);
public record SubmitSurveyDto(int Score, int OnTimeScore, int InfoAccuracyScore, string? Comments);
public record SurveyDto(int Id, int TransactionId, int RaterId, int RateeId, int Score,
    int OnTimeScore, int InfoAccuracyScore, string? Comments, bool Completed);

// ---- Admin / RBAC ----
public record AssignRolesDto(List<string> Roles);
public record SetRolePermissionsDto(List<string> Permissions);
public record CreateArticleDto(string Type, string Title, string Slug, string? Summary, string? Body,
    string? Category, string? Tags, bool IsPublished);
public record CreatePriceQuoteDto(int CommodityId, string? Market, decimal Price, string Currency, DateTime QuotedAt);
public record CreateTradeStatDto(int CommodityId, DateTime Period, decimal Volume, decimal BasePrice, decimal FinalPrice);
public record CreateFutureSupplyDto(int CommodityId, string Supplier, decimal Volume, DateTime SupplyDate, string? Location);
public record CommodityDto(int Id, string Name, string? Category, string? Description, string Unit);
