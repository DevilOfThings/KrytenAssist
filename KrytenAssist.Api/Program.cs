using KrytenAssist.Application.PromptCards;
using KrytenAssist.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapPost("/api/promptcards", async (
    CreatePromptCardRequest request,
    CreatePromptCard createPromptCard,
    CancellationToken cancellationToken) =>
{
    var response = await createPromptCard.ExecuteAsync(request, cancellationToken);

    return Results.Created($"/api/promptcards/{response.Id}", response);
});

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
