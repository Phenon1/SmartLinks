namespace RedirectService.SmartLinks.Configuration;

public class SmartLinkOptions
{
    public const string SectionName = "SmartLinks";

    public string RedirectPathPrefix { get; set; } = "/s";
}
