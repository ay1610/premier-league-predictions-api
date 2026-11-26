using System.ComponentModel.DataAnnotations;

namespace EplApi.Services;

public sealed class FootballDataOptions
{
    public const string SectionName = "FootballData";

    [Required, Url]
    public string BaseUrl { get; set; } = "https://api.football-data.org/v4/";

    [Required]
    public string Token { get; set; } = "";
}
