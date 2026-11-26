using EplApi.Contracts;
using EplApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;

namespace EplApi.Services;

public sealed class TableService(
    EplDbContext db,
    IHttpClientFactory factory,
    ILogger<TableService> logger,
    IOptions<CacheOptions> options
) : ITableService
{
    public async Task<IReadOnlyList<TableRowResponse>> GetTableAsync(
        CancellationToken cancellationToken
    )
    {
        var rows = await db.StandingRows.OrderBy(r => r.Position).ToListAsync(cancellationToken);

        var maxAge = options.Value.TableMaxAge;

        var isOld = rows.Count == 0 || DateTime.UtcNow - rows[0].UpdatedAtUtc > maxAge;
        if (isOld)
        {
            try
            {
                rows = await RefreshAsync(cancellationToken);
            }
            catch (HttpRequestException ex) when (rows.Count > 0)
            {
                logger.LogWarning(
                    ex,
                    "football-data.org failed. Returning cached table from {UpdatedAt}.",
                    rows[0].UpdatedAtUtc
                );
            }
        }

        return rows.Select(r => new TableRowResponse(
                r.Position,
                r.Team,
                r.Crest,
                r.Played,
                r.Points,
                r.GoalDifference,
                r.Form
            ))
            .ToList();
    }

    async Task<List<StandingRow>> RefreshAsync(CancellationToken cancellationToken)
    {
        var http = factory.CreateClient("football-data");
        var response = await http.GetFromJsonAsync<StandingsResponse>(
            "competitions/PL/standings",
            cancellationToken
        );
        var table = response!.Standings.First(s => s.Type == "TOTAL").Table;
        var now = DateTime.UtcNow;

        var rows = table
            .Select(row => new StandingRow
            {
                Position = row.Position,
                Team = row.Team.Name,
                Crest = row.Team.Crest,
                Played = row.PlayedGames,
                Points = row.Points,
                GoalDifference = row.GoalDifference,
                Form = row.Form,
                UpdatedAtUtc = now,
            })
            .ToList();

        db.StandingRows.RemoveRange(db.StandingRows);
        db.StandingRows.AddRange(rows);
        await db.SaveChangesAsync(cancellationToken);

        return rows;
    }
}
