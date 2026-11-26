using System.ComponentModel.DataAnnotations;

namespace EplApi.Services;

// How long cached football-data.org data stays new. Values come from the "Cache" section of appsettings.
public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    [Range(typeof(TimeSpan), "00:01:00", "1.00:00:00")]
    public TimeSpan TableMaxAge { get; set; } = TimeSpan.FromMinutes(10);
}
