using EplApi.Contracts;
using EplApi.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EplApi.Endpoints;

public static class TableEndpoints
{
    public static void MapTableEndpoints(this WebApplication app)
    {
        var epl = app.MapGroup("/api/epl")
            .WithTags("Table")
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        epl.MapGet("/table", GetTable)
            .WithName("GetTable")
            .WithSummary("The full Premier League table");

        epl.MapGet("/table/top/{n}", GetTop)
            .WithName("GetTopTeams")
            .WithSummary("The first n teams of the table")
            .ProducesProblem(StatusCodes.Status400BadRequest);

        epl.MapGet("/team/{name}", GetTeam)
            .WithName("GetTeam")
            .WithSummary("One team, found by part of its name")
            .ProducesProblem(StatusCodes.Status404NotFound);

        epl.MapGet("/table/bottom/{n}", GetBottom)
            .WithName("GetBottomTeams")
            .WithSummary("The last n teams of the table")
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    static async Task<Ok<IReadOnlyList<TableRowResponse>>> GetTable(
        ITableService tables,
        CancellationToken cancellationToken
    )
    {
        return TypedResults.Ok(await tables.GetTableAsync(cancellationToken));
    }

    static async Task<Ok<List<TableRowResponse>>> GetTop(
        int n,
        ITableService tables,
        CancellationToken cancellationToken
    )
    {
        var table = await tables.GetTableAsync(cancellationToken);

        return TypedResults.Ok(table.Take(n).ToList());
    }

    static async Task<Ok<List<TableRowResponse>>> GetBottom(
        int n,
        ITableService tables,
        CancellationToken cancellationToken
    )
    {
        var table = await tables.GetTableAsync(cancellationToken);

        return TypedResults.Ok(table.TakeLast(n).ToList());
    }

    static async Task<Results<Ok<TableRowResponse>, NotFound>> GetTeam(
        string name,
        ITableService tables,
        CancellationToken cancellationToken
    )
    {
        var table = await tables.GetTableAsync(cancellationToken);
        var row = table.FirstOrDefault(t =>
            t.Team.Contains(name, StringComparison.OrdinalIgnoreCase)
        );
        return row is null ? TypedResults.NotFound() : TypedResults.Ok(row);
    }
}
