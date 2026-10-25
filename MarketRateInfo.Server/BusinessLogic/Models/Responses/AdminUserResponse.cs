namespace MarketRateInfo.Server.BusinessLogic.Models.Responses;

public sealed record AdminUserResponse(string Id, string Email, string[] Roles);
