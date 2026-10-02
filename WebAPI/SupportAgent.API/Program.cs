using Npgsql;
using Pgvector.Npgsql;
using SupportAgent.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Register pgvector
var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(
    builder.Configuration.GetConnectionString("DefaultConnection"));
dataSourceBuilder.UseVector();

builder.Services.AddSingleton(dataSourceBuilder.Build());

// Services
builder.Services.AddHttpClient<EmbeddingService>();
builder.Services.AddScoped<DocumentIngestionService>();
builder.Services.AddScoped<DocumentRetrieverService>();
builder.Services.AddScoped<CustomerDataService>();
builder.Services.AddHttpClient<ChatOrchestratorService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngular");
app.MapControllers();

app.Run();