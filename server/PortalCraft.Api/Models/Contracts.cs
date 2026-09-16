namespace PortalCraft.Api.Models;

public sealed record RequestRecord(
    string RequestId,
    string Type,
    string Title,
    string Requester,
    string Status,
    string Stage,
    string? ReviewTicketId,
    DateTimeOffset CreatedAt,
    IReadOnlyList<string> Details);

public sealed record ChatRequest(
    string Message,
    string CurrentUser = "alex",
    string? SelectedRequestId = null,
    string? SelectedReviewTicketId = null);

public sealed record ChatSource(string Type, string Label, string Identifier);

public sealed record ChatAction(string Type, string View, string? RequestId = null, string? ReviewTicketId = null);

public sealed record ChatResponse(
    string Message,
    IReadOnlyList<RequestRecord> Requests,
    IReadOnlyList<ChatSource> Sources,
    IReadOnlyList<ChatAction> Actions,
    string? SelectedRequestId = null,
    string? SelectedReviewTicketId = null);
