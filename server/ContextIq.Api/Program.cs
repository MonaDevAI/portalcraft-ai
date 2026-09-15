using ContextIq.Api.Models;
using ContextIq.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<RequestRepository>();
builder.Services.AddSingleton<ContextIqService>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();
app.UseCors();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/requests", (
    RequestRepository repository,
    string? requester,
    string? requestId,
    string? reviewTicketId) =>
{
    var requests = repository.Search(requester, requestId, reviewTicketId);
    return Results.Ok(requests);
});

app.MapGet("/api/requests/{requestId}", (RequestRepository repository, string requestId) =>
{
    var request = repository.FindById(requestId);
    return request is null ? Results.NotFound() : Results.Ok(request);
});

app.MapGet("/api/review-queue", (RequestRepository repository, string? reviewTicketId) =>
    Results.Ok(repository.ReviewQueue(reviewTicketId)));

app.MapPost("/api/chat", (ChatRequest request, ContextIqService service) =>
    Results.Ok(service.Respond(request)));

app.Run("http://localhost:5080");

public partial class Program;
