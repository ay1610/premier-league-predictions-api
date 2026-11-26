using EplApi.Data;
using EplApi.Endpoints;
using EplApi.Middleware;
using EplApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<EplDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Epl"))
);

builder
    .Services.AddOptions<CacheOptions>()
    .BindConfiguration(CacheOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder
    .Services.AddOptions<FootballDataOptions>()
    .BindConfiguration(FootballDataOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient("football-data", (services, client) =>
{
    var footballData = services.GetRequiredService<IOptions<FootballDataOptions>>().Value;
    client.BaseAddress = new Uri(footballData.BaseUrl);
    client.DefaultRequestHeaders.Add("X-Auth-Token", footballData.Token);
});
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
