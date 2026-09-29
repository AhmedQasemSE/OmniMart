using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniMart.Application.Features.Reviews.Commands;
using OmniMart.Application.Features.Reviews.Queries;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Controllers;

[ApiController]
[Route("api/reviews")]
public class ReviewsController : BaseController
{
    private readonly IMediator _mediator;

    public ReviewsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    #region  (Product Reviews)

    [HttpGet("product/{productId:guid}")]
    public async Task<IActionResult> GetProductReviews([FromRoute] Guid productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetProductReviewsQuery(productId, page, pageSize), cancellationToken);
        return HandleResult(result);
    }

    [Authorize(Roles = "Customer")]
    [HttpPost]
    public async Task<IActionResult> AddReview([FromBody] AddReviewCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [Authorize(Roles = "Customer")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateReview([FromRoute] Guid id, [FromBody] UpdateReviewCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command with { ReviewId = id }, cancellationToken);
        return HandleResult(result);
    }

    [Authorize(Roles = "Customer,Admin,SuperAdmin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteReview([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteReviewCommand(id), cancellationToken);
        return HandleResult(result);
    }

    #endregion
}