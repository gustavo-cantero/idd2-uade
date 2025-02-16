using IDD2.Model;

var builder = WebApplication.CreateBuilder(args);

//Configuraciones
ConfigurationModel config = new();
builder.Configuration.GetSection("App").Bind(config);
builder.Services.AddSingleton(config);

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
