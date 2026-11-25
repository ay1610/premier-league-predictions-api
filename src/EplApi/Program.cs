using System.Net;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var token = builder.Configuration["FootballData:Token"];
if (string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException("Set the FootballData:Token user secret first.");

builder.Services.AddHttpClient("football-data", client =>
{
    client.BaseAddress = new Uri("https://api.football-data.org/v4/");
    client.DefaultRequestHeaders.Add("X-Auth-Token", token);
});
builder.Services.AddDbContext<EplDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Epl")));
builder.Services.AddScoped<TableService>();
builder.Services.AddOpenApi();

var app = builder.Build();

// Make epl.db and its tables if they do not exist.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<EplDbContext>().Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "EplApi v1"));
}

var epl = app.MapGroup("/api/epl").WithTags("Table"); 

epl.MapGet("/table", async (TableService tables) =>
{
    try
    {
        return Results.Ok(await tables.GetTableAsync());
    }
    catch (HttpRequestException ex)
    {
        return UpstreamProblem(ex);
    }
}).WithSummary("The full Premier League table")
.Produces<List<TableRowDto>>()
.ProducesProblem(StatusCodes.Status502BadGateway)
.ProducesProblem(StatusCodes.Status503ServiceUnavailable);

epl.MapGet("/table/top/{n}", async (int n, TableService tables) =>
{
    try
    {
        var table = await tables.GetTableAsync();
        return Results.Ok(table.Take(n));
    }
    catch (HttpRequestException ex)
    {
        return UpstreamProblem(ex);
    }
})
.WithSummary("The first n teams of the table")
.Produces<List<TableRowDto>>()
.ProducesProblem(StatusCodes.Status400BadRequest)
.ProducesProblem(StatusCodes.Status502BadGateway)
.ProducesProblem(StatusCodes.Status503ServiceUnavailable);

epl.MapGet("/team/{name}" , async( string name ,TableService tables) =>
{
    try
    {
        var table = await tables.GetTableAsync();
        var teamStanding = table.FirstOrDefault(t => t.Team.Contains(name, StringComparison.OrdinalIgnoreCase));
        return teamStanding is null ? Results.NotFound() : Results.Ok(teamStanding);
    }
    catch (HttpRequestException ex)
    {
         return UpstreamProblem(ex);
    }
    
}).WithSummary("Gets the position of the given team")
.Produces<TableRowDto>()
.Produces(StatusCodes.Status404NotFound)
.ProducesProblem(StatusCodes.Status502BadGateway)
.ProducesProblem(StatusCodes.Status503ServiceUnavailable);

app.Run();

static IResult UpstreamProblem(HttpRequestException ex) =>
    ex.StatusCode == HttpStatusCode.TooManyRequests
        ? Results.Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Football data is busy",
            detail: "The call limit of football-data.org is reached. Try again in one minute.")
        : Results.Problem(
            statusCode: StatusCodes.Status502BadGateway,
            title: "Football data failed",
            detail: "football-data.org did not return the table.");

// What your API returns: your contract, not football-data.org's.
record TableRowDto(int Position, string Team, string Crest ,int Played, int Points, int GoalDifference, string? Form);

// What football-data.org sends (only the fields you need).
record StandingsResponse(List<Standing> Standings);
record Standing(string Type, List<TableRow> Table);
record TableRow(int Position, Team Team, int PlayedGames, int Points, int GoalDifference , string? Form);
record Team(string Name, string ShortName, string Crest);