using Importer.Controllers;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()  // Allow requests from any origin (e.g., localhost:3000 for React frontend)
              .AllowAnyHeader()  // Allow any HTTP headers
              .AllowAnyMethod(); // Allow any HTTP methods (GET, POST, PUT, etc.)
    });
});

// Add Controller Services
builder.Services.AddControllers();  // This is required to enable controller endpoints
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddScoped<FileComparisonService>();
builder.Services.AddScoped<RevisedFileComparisonService>();





var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add CORS middleware before endpoints
app.UseCors("AllowAll");





app.MapControllers();
app.Run();