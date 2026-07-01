using KrytenAssist.Application.PromptCards;
using KrytenAssist.Infrastructure;
using KrytenAssist.Application.Abstractions.Persistence;
using KrytenAssist.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapPromptCardEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
