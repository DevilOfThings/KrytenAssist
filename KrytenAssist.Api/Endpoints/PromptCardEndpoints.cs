using FluentValidation;
using KrytenAssist.Application.Abstractions.Persistence;
using KrytenAssist.Application.PromptCards;

namespace KrytenAssist.Api.Endpoints;

public static class PromptCardEndpoints
{
    public static IEndpointRouteBuilder MapPromptCardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/promptcards")
            .WithTags("PromptCards");
        

        group.MapPost("/", async (
                CreatePromptCardRequest request,
                IValidator<CreatePromptCardRequest> validator,
                CreatePromptCard createPromptCard,
                CancellationToken cancellationToken
                ) =>
            {
                var validationResult = await validator.ValidateAsync(request, cancellationToken);
                
                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }

                var response = await createPromptCard.ExecuteAsync(request, cancellationToken);

                return Results.Created($"/api/promptcards/{response.Id}", response);
            })
            .WithName("CreatePromptCard")
            .WithSummary("Creates a new prompt card.");

        group.MapGet("/", async (
                IPromptCardRepository repository,
                CancellationToken cancellationToken) =>
            {
                var promptCards = await repository.GetAllAsync(cancellationToken);

                return Results.Ok(promptCards);
            })
            .WithName("GetPromptCards")
            .WithSummary("Gets all prompt cards.");

        group.MapGet("/{id:guid}", async (
                Guid id,
                IPromptCardRepository repository,
                CancellationToken cancellationToken) =>
            {
                var promptCard = await repository.GetByIdAsync(id, cancellationToken);

                return promptCard is null
                    ? Results.NotFound()
                    : Results.Ok(promptCard);
            })
            .WithName("GetPromptCardById")
            .WithSummary("Gets a prompt card by id.");
        
        group.MapPut("/{id:guid}", async (
                Guid id,
                UpdatePromptCardRequest request,
                IValidator<UpdatePromptCardRequest> validator,
                UpdatePromptCard updatePromptCard,
                CancellationToken cancellationToken) =>
            {
                var validationResult = await validator.ValidateAsync(request, cancellationToken);

                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }
                
                var response = await updatePromptCard.ExecuteAsync(id, request, cancellationToken);

                return response is null
                    ? Results.NotFound()
                    : Results.Ok(response);
            })
            .WithName("UpdatePromptCard")
            .WithSummary("Updates an existing prompt card.");

        group.MapDelete("/{id:guid}", async (
                Guid id,
                DeletePromptCard deletePromptCard,
                CancellationToken cancellationToken) =>
            {
                var response = await deletePromptCard.ExecuteAsync(id, cancellationToken);

                return response.Deleted
                    ? Results.NoContent()
                    : Results.NotFound();
            })
            .WithName("DeletePromptCard")
            .WithSummary("Deletes an existing prompt card.");
        
        return app;
    }
}