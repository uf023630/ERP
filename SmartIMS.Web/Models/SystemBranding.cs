namespace SmartIMS.Web.Models;

public sealed record SystemBranding(
    string CompanyName,
    string? CompanyLogoPath,
    string? LoginHeroImagePath,
    string LoginTitle,
    string LoginSubtitle);
