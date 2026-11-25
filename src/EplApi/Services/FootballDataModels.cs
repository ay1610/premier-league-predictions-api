namespace EplApi.Services;

// What football-data.org sends (only the fields you need).
internal sealed record StandingsResponse(List<Standing> Standings);

internal sealed record Standing(string Type, List<TableRow> Table);

internal sealed record TableRow(
    int Position,
    Team Team,
    int PlayedGames,
    int Points,
    int GoalDifference,
    string? Form
);

internal sealed record Team(string Name, string ShortName, string Crest);
