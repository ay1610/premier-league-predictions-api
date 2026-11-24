using Microsoft.EntityFrameworkCore;

class EplDbContext(DbContextOptions<EplDbContext> options) : DbContext(options)
{
    public DbSet<StandingRow> StandingRows => Set<StandingRow>();
}

// One row of the league table, as you keep it in your own database.
class StandingRow
{
    public int Id { get; set; }
    public int Position { get; set; }
    public string Team { get; set; } = "";
    public string Crest {get; set;} = "";
    public int Played { get; set; }
    public int Points { get; set; }
    public int GoalDifference { get; set; }
    public string? Form {get; set;} = "";
    public DateTime UpdatedAtUtc { get; set; }
}