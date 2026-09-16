using PortalCraft.Api.Models;

namespace PortalCraft.Api.Services;

public sealed class RequestRepository
{
    private readonly IReadOnlyList<RequestRecord> requests =
    [
        new(
            "REQ-1042",
            "Access",
            "Add analytics workspace access",
            "alex",
            "Submitted",
            "SecurityReview",
            "REV-8421",
            DateTimeOffset.UtcNow.AddHours(-3),
            ["Environment: Production", "Role: Analytics Reader"]),
        new(
            "REQ-1039",
            "Catalog",
            "Publish customer-support dataset",
            "alex",
            "Pending review",
            "DataReview",
            "REV-8398",
            DateTimeOffset.UtcNow.AddDays(-1),
            ["Domain: Customer Support", "Classification: Internal"]),
        new(
            "REQ-1035",
            "Workflow",
            "Update invoice approval threshold",
            "alex",
            "Draft",
            "Requestor",
            null,
            DateTimeOffset.UtcNow.AddDays(-2),
            ["Current threshold: 5,000", "Proposed threshold: 10,000"]),
        new(
            "REQ-1028",
            "Access",
            "Add operations dashboard access",
            "jordan",
            "Pending review",
            "SecurityReview",
            "REV-8320",
            DateTimeOffset.UtcNow.AddDays(-3),
            ["Environment: Test", "Role: Operations Viewer"])
    ];

    public IReadOnlyList<RequestRecord> Search(
        string? requester = null,
        string? requestId = null,
        string? reviewTicketId = null)
    {
        IEnumerable<RequestRecord> query = requests;
        if (!string.IsNullOrWhiteSpace(requester))
        {
            query = query.Where(request =>
                request.Requester.Equals(requester, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(requestId))
        {
            query = query.Where(request =>
                request.RequestId.Equals(requestId, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(reviewTicketId))
        {
            query = query.Where(request =>
                string.Equals(request.ReviewTicketId, reviewTicketId, StringComparison.OrdinalIgnoreCase));
        }
        return query.OrderByDescending(request => request.CreatedAt).ToArray();
    }

    public RequestRecord? FindById(string requestId) =>
        requests.FirstOrDefault(request =>
            request.RequestId.Equals(requestId, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<RequestRecord> ReviewQueue(string? reviewTicketId = null) =>
        Search(reviewTicketId: reviewTicketId)
            .Where(request => request.Status.Contains("review", StringComparison.OrdinalIgnoreCase)
                || request.Stage.Contains("Review", StringComparison.OrdinalIgnoreCase))
            .ToArray();
}
