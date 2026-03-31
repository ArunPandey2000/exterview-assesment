using ExterView.Api.Data;
using ExterView.Api.Services;
using ExterView.Api.Workers;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() {
        Title = "ExterView Assessment API",
        Version = "v1",
        Description = "Microsoft Teams Meeting Bot Assessment - Local Implementation"
    });
});

// Configure PostgreSQL with Entity Framework Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register application services
builder.Services.AddSingleton<IBackgroundQueue, BackgroundQueue>();
builder.Services.AddScoped<IMockGraphService, MockGraphService>();
builder.Services.AddScoped<ILocalFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();
builder.Services.AddScoped<ITranscriptRepository, TranscriptRepository>();

// Register background worker
builder.Services.AddHostedService<TranscriptProcessorWorker>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ExterView Assessment API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseSerilogRequestLogging();

app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.Migrate();
        Log.Information("Database migration completed successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while migrating the database");
        throw;
    }
}

Log.Information("ExterView Assessment API starting...");
Log.Information("Swagger UI available at: http://localhost:5000");
Log.Information("Test endpoint: POST http://localhost:5000/api/meetings/simulate");

app.Run();
