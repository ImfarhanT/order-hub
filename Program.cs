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
builder.Services.AddRazorPages();

// Add Entity Framework
builder.Services.AddDbContext<OrderHubDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Add services
builder.Services.AddScoped<IHmacService, HmacService>();
builder.Services.AddScoped<ICryptoService, CryptoService>();
builder.Services.AddScoped<IOrderProcessingService, OrderProcessingServiceV2>();
builder.Services.AddScoped<OrderProcessingServiceV2>();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add authentication
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/admin/login";
        options.LogoutPath = "/admin/logout";
        options.AccessDeniedPath = "/admin/access-denied";
    });

// Add authorization
builder.Services.AddAuthorization();

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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable Swagger in production for now
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors("AllowAll");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

// Simple health check endpoint
app.MapGet("/", () => "Order Hub API is running!");

// Ensure database is created and migrations are applied
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrderHubDbContext>();
    await context.Database.EnsureCreatedAsync();
    
    // Manually create RawOrderData table if it doesn't exist
    try
    {
        var tableExists = await context.Database.SqlQueryRaw<bool>(
            "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'RawOrderData')").FirstOrDefaultAsync();
        
        if (!tableExists)
        {
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE ""RawOrderData"" (
                    ""Id"" uuid NOT NULL,
                    ""SiteId"" uuid NOT NULL,
                    ""SiteName"" character varying(255) NOT NULL,
                    ""RawJson"" text NOT NULL,
                    ""ReceivedAt"" timestamp with time zone NOT NULL,
                    ""Processed"" boolean NOT NULL DEFAULT false,
                    ""ProcessedAt"" timestamp with time zone NULL,
                    CONSTRAINT ""PK_RawOrderData"" PRIMARY KEY (""Id"")
                );
                
                ALTER TABLE ""RawOrderData"" 
                ADD CONSTRAINT ""FK_RawOrderData_Sites_SiteId"" 
                FOREIGN KEY (""SiteId"") REFERENCES ""sites""(""Id"") ON DELETE CASCADE;
                
                CREATE INDEX ""IX_RawOrderData_SiteId"" ON ""RawOrderData"" (""SiteId"");
                CREATE INDEX ""IX_RawOrderData_ReceivedAt"" ON ""RawOrderData"" (""ReceivedAt"");
            ");
            
            Log.Information("RawOrderData table created successfully");
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Could not create RawOrderData table - it may already exist");
    }
}

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
