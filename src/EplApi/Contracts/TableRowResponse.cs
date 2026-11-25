namespace EplApi.Contracts;

/// <summary>One row of the Premier League table, as this API returns it.</summary>
/// <param name="Position">League position, from 1 to 20.</param>
/// <param name="Team">Full team name, for example "Chelsea FC".</param>
/// <param name="Crest">URL of the team crest image.</param>
/// <param name="Played">Matches played.</param>
/// <param name="Points">League points.</param>
/// <param name="GoalDifference">Goals scored minus goals conceded.</param>
/// <param name="Form">Last results, for example "W,W,D,L,W". Null until a later lesson computes it.</param>
public sealed record TableRowResponse(
    int Position,
    string Team,
    string Crest,
    int Played,
    int Points,
    int GoalDifference,
    string? Form);