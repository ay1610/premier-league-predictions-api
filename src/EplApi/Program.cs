using EplApi.Data;
using EplApi.Endpoints;
using EplApi.Middleware;
using EplApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var token = builder.Configuration["FootballData:Token"];
if (string.IsNullOrWhiteSpace(token))
    throw new InvalidOperationException("Set the FootballData:Token user secret first.");

builder.Services.AddHttpClient("football-data", client =>
{
    client.BaseAddress = new Uri("https://api.football-data.org/v4/");
    client.DefaultRequestHeaders.Add("X-Auth-Token", token);
});
builder.Services.AddDbContext<EplDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Epl"))
);
builder.Services.AddScoped<ITableService, TableService>();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<UpstreamExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler(
    new ExceptionHandlerOptions
    {
        // A binding error (for example /top/abc) keeps its 400; anything unhandled is a 500.
        StatusCodeSelector = ex =>
            ex is BadHttpRequestException bad
                ? bad.StatusCode
                : StatusCodes.Status500InternalServerError,
    }
);
app.UseStatusCodePages();

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

app.MapTableEndpoints();

app.Run();
