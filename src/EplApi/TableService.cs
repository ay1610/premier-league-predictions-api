using Microsoft.EntityFrameworkCore;

class TableService(EplDbContext db, IHttpClientFactory factory, ILogger<TableService> logger)
{
    static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(1);

    public async Task<List<TableRowDto>> GetTableAsync()
    {
        var rows = await db.StandingRows.OrderBy(r => r.Position).ToListAsync();

        var isOld = rows.Count == 0 || DateTime.UtcNow - rows[0].UpdatedAtUtc > MaxAge;
        if (isOld)
        {
            try
            {
                rows = await RefreshAsync();
            }
            catch (HttpRequestException ex) when (rows.Count > 0)
            {
              logger.LogWarning(ex, "football-data.org failed. Returning cached table from {UpdatedAt}.", rows[0].UpdatedAtUtc);
            }
        }

        return rows.Select(r => new TableRowDto(r.Position, r.Team, r.Crest ,r.Played, r.Points, r.GoalDifference, r.Form)).ToList();
    }

    async Task<List<StandingRow>> RefreshAsync()
    {
        var http = factory.CreateClient("football-data");
        var response = await http.GetFromJsonAsync<StandingsResponse>("competitions/PL/standings");
        var table = response!.Standings.First(s => s.Type == "TOTAL").Table;
        var now = DateTime.UtcNow;

        var rows = table.Select(row => new StandingRow
        {
            Position = row.Position,
            Team = row.Team.Name,
            Crest = row.Team.Crest,
            Played = row.PlayedGames,
            Points = row.Points,
            GoalDifference = row.GoalDifference,
            Form = row.Form,
            UpdatedAtUtc = now
        }).ToList();

        db.StandingRows.RemoveRange(db.StandingRows);
        db.StandingRows.AddRange(rows);
        await db.SaveChangesAsync();

        return rows;
    }
}