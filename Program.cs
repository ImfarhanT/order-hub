using Microsoft.EntityFrameworkCore;
using HubApi.Data;
using HubApi.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/order-hub-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();

// Add Entity Framework
builder.Services.AddDbContext<OrderHubDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Add services
builder.Services.AddScoped<IHmacService, HmacService>();
builder.Services.AddScoped<ICryptoService, CryptoService>();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");

app.UseRouting();

app.MapControllers();

// Simple health check endpoint
app.MapGet("/", () => "Order Hub API is running!");

// Ensure database is created and migrated (non-blocking)
_ = Task.Run(async () =>
{
    try
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderHubDbContext>();
        await context.Database.EnsureCreatedAsync();
        Log.Information("Database connection established successfully");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Database connection failed during startup, will retry later");
    }
});

try
{
    Log.Information("Starting Order Hub API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Order Hub API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
