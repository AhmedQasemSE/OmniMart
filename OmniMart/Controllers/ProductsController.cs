using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniMart.Application.Features.Products.Commands;
using OmniMart.Application.Features.Products.Queries;
using OmniMart.Application.Features.Products.Queries.GetAdminProducts;
using OmniMart.Application.Features.Products.Queries.GetProductById;
using OmniMart.Application.Features.Products.Queries.GetProducts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : BaseController
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    #region  Public Endpoints

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] GetProductsQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProductByIdAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetProductByIdQuery(id), cancellationToken);
        return HandleResult(result);
    }

    #endregion

    #region  Vendor Endpoints 

    [Authorize(Roles = "Vendor")]
    [HttpPost]
    public async Task<IActionResult> CreateProductAsync([FromBody] CreateProductCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [Authorize(Roles = "Vendor")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProductAsync([FromRoute] Guid id, [FromBody] UpdateProductCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command with { ProductId = id }, cancellationToken);
        return HandleResult(result);
    }

    [Authorize(Roles = "Vendor")]
    [HttpPatch("{id:guid}/submit")]
    public async Task<IActionResult> SubmitProductForReview([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SubmitProductForReviewCommand(id), cancellationToken);
        return HandleResult(result);
    }

    [Authorize(Roles = "Vendor")]
    [HttpPatch("{id:guid}/publish")]
    public async Task<IActionResult> SetPublishStatus([FromRoute] Guid id, [FromBody] SetPublishStatusCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command with { id = id }, cancellationToken);
        return HandleResult(result);
    }

    [Authorize(Roles = "Vendor")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProduct([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SoftDeleteCommand(id), cancellationToken);
        return HandleResult(result);
    }

    [Authorize(Roles = "Vendor")]
    [HttpPatch("{id:guid}/restore")]
    public async Task<IActionResult> RestoreProduct([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RestoreProductCommand(id), cancellationToken);
        return HandleResult(result);
    }

    [Authorize(Roles = "Vendor")]
    [HttpGet("my-products")]
    public async Task<IActionResult> GetVendorProducts([FromQuery] GetVendorProductsQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    #endregion

    #region  Admin Endpoints

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpGet("admin")]
    public async Task<IActionResult> GetAdminProducts([FromQuery] GetAdminProductsQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPatch("{id:guid}/approve")]
    public async Task<IActionResult> ApproveProduct([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ApproveProductCommand(id), cancellationToken);
        return HandleResult(result);
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPatch("{id:guid}/reject")]
    public async Task<IActionResult> RejectProduct([FromRoute] Guid id, [FromBody] RejectProductCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command with { ProductId = id }, cancellationToken);
        return HandleResult(result);
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPatch("{id:guid}/suspend")]
    public async Task<IActionResult> SuspendProduct([FromRoute] Guid id, [FromBody] SuspendProductCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command with { id = id }, cancellationToken);
        return HandleResult(result);
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPatch("{id:guid}/reactivate")]
    public async Task<IActionResult> ReactivateProduct([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ReactivateProductCommand(id), cancellationToken);
        return HandleResult(result);
    }

    #endregion

    #region  Product Variants & Images 

    [Authorize(Roles = "Vendor")]
    [HttpPost("{id:guid}/variants")]
    public async Task<IActionResult> AddProductVariant([FromRoute] Guid id, [FromBody] AddProductVariantCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command with { ProductId = id }, cancellationToken);
        return HandleResult(result);
    }

    [Authorize(Roles = "Vendor")]
    [HttpPut("{id:guid}/variants/{sku}")]
    public async Task<IActionResult> UpdateProductVariant([FromRoute] Guid id, [FromRoute] string sku, [FromBody] UpdateProductVariantCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command with { ProductId = id, SKU = sku }, cancellationToken);
        return HandleResult(result);
    }

    [Authorize(Roles = "Vendor")]
    [HttpDelete("{id:guid}/variants/{sku}")]
    public async Task<IActionResult> RemoveProductVariant([FromRoute] Guid id, [FromRoute] string sku, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RemoveProductVariantCommand(id, sku), cancellationToken);
        return HandleResult(result);
    }

    [Authorize(Roles = "Vendor")]
    [HttpPut("{id:guid}/variants/{sku}/attributes")]
    public async Task<IActionResult> AssignValuesToVariant([FromRoute] Guid id, [FromRoute] string sku, [FromBody] AssignValuesToVariantCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command with { ProductId = id, SKU = sku }, cancellationToken);
        return HandleResult(result);
    }

    [Authorize(Roles = "Vendor")]
    [HttpPost("{id:guid}/images")]
    public async Task<IActionResult> UploadProductImage([FromRoute] Guid id, Microsoft.AspNetCore.Http.IFormFile file, [FromForm] bool isPrimary, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0) return BadRequest(new { Error = "No image file was provided." });
        using var stream = file.OpenReadStream();
        var result = await _mediator.Send(new UploadProductImageCommand(id, stream, file.FileName, isPrimary), cancellationToken);
        return HandleResult(result);
    }

    [Authorize(Roles = "Vendor")]
    [HttpDelete("{id:guid}/images/{imageId:guid}")]
    public async Task<IActionResult> RemoveProductImage([FromRoute] Guid id, [FromRoute] Guid imageId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RemoveProductImageCommand(id, imageId), cancellationToken);
        return HandleResult(result);
    }

    [Authorize(Roles = "Vendor")]
    [HttpPatch("{id:guid}/images/{imageId:guid}/set-primary")]
    public async Task<IActionResult> SetPrimaryImage([FromRoute] Guid id, [FromRoute] Guid imageId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SetPrimaryProductImageCommand(id, imageId), cancellationToken);
        return HandleResult(result);
    }

    #endregion

    #region  Internal & Shared Endpoints 

    [Authorize(Roles = "Admin,SuperAdmin,Vendor")]
    [HttpGet("{id:guid}/internal")]
    public async Task<IActionResult> GetProductDetailsInternal([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetProductDetailsInternalQuery(id), cancellationToken);
        return HandleResult(result);
    }

    #endregion
}