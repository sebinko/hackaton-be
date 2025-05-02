using API.Data;
using API.LLM;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpClient();
builder.Services.AddScoped<ILlmService, LocalLlmService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer(); // Required for Swagger
builder.Services.AddSwaggerGen(); // Add Swagger generator
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


// Add services to the container.
builder.Services.AddSingleton<MongoDbService>(sp =>
    new MongoDbService(
        builder.Configuration.GetConnectionString("MongoDb"),
        "ThoughtsDb"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // Enable Swagger middleware
    app.UseSwaggerUI(); // Enable Swagger UI
}


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();