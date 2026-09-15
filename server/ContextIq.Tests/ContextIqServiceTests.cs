using ContextIq.Api.Models;
using ContextIq.Api.Services;
using Xunit;

namespace ContextIq.Tests;

public sealed class ContextIqServiceTests
{
    private readonly ContextIqService service = new(new RequestRepository());

    [Fact]
    public void FindsCurrentUsersRecentRequests()
    {
        var response = service.Respond(new("I want to see my recent requests", "alex"));

        Assert.Equal(3, response.Requests.Count);
        Assert.All(response.Requests, request => Assert.Equal("alex", request.Requester));
    }

    [Fact]
    public void FindsRequestsCreatedByNamedUser()
    {
        var response = service.Respond(new("Show requests created by Jordan"));

        var request = Assert.Single(response.Requests);
        Assert.Equal("jordan", request.Requester);
        Assert.Equal("REQ-1028", request.RequestId);
    }

    [Fact]
    public void CarriesRequestContextToReviewTicket()
    {
        var response = service.Respond(new(
            "List the review ticket for this request",
            SelectedRequestId: "REQ-1042"));

        Assert.Equal("REV-8421", response.SelectedReviewTicketId);
        Assert.Contains("REV-8421", response.Message);
    }

    [Fact]
    public void DoesNotInventMissingReviewTicket()
    {
        var response = service.Respond(new(
            "List the review ticket for this request",
            SelectedRequestId: "REQ-1035"));

        Assert.Null(response.SelectedReviewTicketId);
        Assert.Contains("does not have a review ticket", response.Message);
    }

    [Fact]
    public void RejectsMutationRequests()
    {
        var response = service.Respond(new("Approve request REQ-1042"));

        Assert.Contains("read-only", response.Message);
        Assert.Empty(response.Actions);
    }
}
