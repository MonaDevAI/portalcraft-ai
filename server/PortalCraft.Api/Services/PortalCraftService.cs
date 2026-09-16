using System.Text.RegularExpressions;
using PortalCraft.Api.Models;

namespace PortalCraft.Api.Services;

public sealed partial class PortalCraftService(RequestRepository repository)
{
    public ChatResponse Respond(ChatRequest request)
    {
        var message = request.Message.Trim();

        if (ContainsMutation(message))
        {
            return Response(
                "PortalCraft AI is read-only in this sample. Use the product's governed controls for state-changing actions.");
        }

        var requestId = RequestIdPattern().Match(message);
        if (requestId.Success)
        {
            return RequestDetails(requestId.Value);
        }

        var reviewTicket = ReviewTicketPattern().Match(message);
        if (reviewTicket.Success)
        {
            return ReviewQueue(reviewTicket.Value);
        }

        if (message.Contains("review ticket", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(request.SelectedRequestId))
        {
            return ReviewTicketForRequest(request.SelectedRequestId);
        }

        if (message.Contains("review queue", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(request.SelectedReviewTicketId))
        {
            return ReviewQueue(request.SelectedReviewTicketId);
        }

        if (message.Contains("my recent requests", StringComparison.OrdinalIgnoreCase)
            || message.Contains("my requests", StringComparison.OrdinalIgnoreCase))
        {
            return RequestsBy(request.CurrentUser);
        }

        var requester = RequesterPattern().Match(message);
        if (requester.Success)
        {
            return RequestsBy(requester.Groups["alias"].Value);
        }

        return Response(
            "I couldn't match that read-only request. Try “Show my recent requests”, " +
            "“Show requests created by Alex”, or “Show request REQ-1042”.");
    }

    private ChatResponse RequestsBy(string requester)
    {
        var requests = repository.Search(requester: requester);
        var message = requests.Count == 0
            ? $"No requests were found for {requester}."
            : $"Found {requests.Count} recent request{(requests.Count == 1 ? string.Empty : "s")} created by {requester}.";
        return Response(
            message,
            requests,
            [new("requestSearch", "Request search", requester)],
            requests.Count == 0 ? [] : [new("openView", "requests")]);
    }

    private ChatResponse RequestDetails(string requestId)
    {
        var record = repository.FindById(requestId);
        if (record is null)
        {
            return Response(
                $"Request {requestId} was not found.",
                sources: [new("request", "Request lookup", requestId)]);
        }

        var reviewTicket = record.ReviewTicketId is null
            ? "No review ticket has been generated."
            : $"Review ticket: {record.ReviewTicketId}.";
        return Response(
            $"{record.RequestId} is {record.Status} at stage {record.Stage}. {reviewTicket}",
            [record],
            [new("request", "Request details", record.RequestId)],
            [new("openView", "requests", record.RequestId)],
            record.RequestId,
            record.ReviewTicketId);
    }

    private ChatResponse ReviewTicketForRequest(string requestId)
    {
        var record = repository.FindById(requestId);
        if (record is null)
        {
            return Response($"Request {requestId} was not found.");
        }
        if (record.ReviewTicketId is null)
        {
            return Response(
                $"Request {requestId} does not have a review ticket yet.",
                [record],
                [new("request", "Request details", requestId)],
                selectedRequestId: requestId);
        }
        return Response(
            $"The review ticket for request {requestId} is {record.ReviewTicketId}.",
            [record],
            [new("request", "Request details", requestId)],
            [new("openView", "reviewQueue", requestId, record.ReviewTicketId)],
            requestId,
            record.ReviewTicketId);
    }

    private ChatResponse ReviewQueue(string reviewTicketId)
    {
        var requests = repository.ReviewQueue(reviewTicketId);
        var message = requests.Count == 0
            ? $"Review ticket {reviewTicketId} was not found in the review queue."
            : $"Review ticket {reviewTicketId} is visible in the review queue for request {requests[0].RequestId}.";
        return Response(
            message,
            requests,
            [new("reviewQueue", "Review queue", reviewTicketId)],
            [new("openView", "reviewQueue", requests.FirstOrDefault()?.RequestId, reviewTicketId)],
            requests.FirstOrDefault()?.RequestId,
            reviewTicketId);
    }

    private static ChatResponse Response(
        string message,
        IReadOnlyList<RequestRecord>? requests = null,
        IReadOnlyList<ChatSource>? sources = null,
        IReadOnlyList<ChatAction>? actions = null,
        string? selectedRequestId = null,
        string? selectedReviewTicketId = null) =>
        new(message, requests ?? [], sources ?? [], actions ?? [], selectedRequestId, selectedReviewTicketId);

    private static bool ContainsMutation(string message) =>
        Regex.IsMatch(message, @"\b(approve|reject|submit|delete|discard)\b", RegexOptions.IgnoreCase);

    [GeneratedRegex(@"\bREQ-\d+\b", RegexOptions.IgnoreCase)]
    private static partial Regex RequestIdPattern();

    [GeneratedRegex(@"\bREV-\d+\b", RegexOptions.IgnoreCase)]
    private static partial Regex ReviewTicketPattern();

    [GeneratedRegex(
        @"(?:(?:show|find|list|get)(?:\s+me)?\s+requests?\s+(?:created|submitted|raised)\s+by|show\s+me)\s+(?<alias>[a-z][a-z0-9._-]*)(?:'s)?(?:\s+(?:recent\s+)?requests?)?",
        RegexOptions.IgnoreCase)]
    private static partial Regex RequesterPattern();
}
