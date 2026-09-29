
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Attributes.Commands;

public record CreateProductAttributeCommand(string Name) : IRequest<Result<Guid>>;

public class CreateProductAttributeCommandHandler : IRequestHandler<CreateProductAttributeCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateProductAttributeCommandHandler> _logger;

    public CreateProductAttributeCommandHandler(IUnitOfWork unitOfWork, ILogger<CreateProductAttributeCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateProductAttributeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling CreateProductAttributeCommand for attribute: '{AttributeName}'", request.Name);

        string cleanName = request.Name.Trim();

        bool isExists = await _unitOfWork.ProductAttributes.IsNameExistsAsync(cleanName, cancellationToken);
        if (isExists)
        {
            _logger.LogWarning("Failed to create attribute. The name '{AttributeName}' already exists.", cleanName);
            return Result<Guid>.Failure($"The attribute '{cleanName}' already exists in the system.", ErrorType.Conflict);
        }

        try
        {
            var attribute = new ProductAttribute(cleanName);

            await _unitOfWork.ProductAttributes.AddAsync(attribute, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully created Product Attribute '{AttributeName}' with ID: {AttributeId}", attribute.Name, attribute.Id);

            return Result<Guid>.Success(attribute.Id);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Domain validation failed while creating attribute '{AttributeName}'.", cleanName);
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}

public class CreateProductAttributeCommandValidator : AbstractValidator<CreateProductAttributeCommand>
{
    public CreateProductAttributeCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Attribute name cannot be empty.")
            .MinimumLength(2).WithMessage("Attribute name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Attribute name cannot exceed 100 characters.");
    }
}