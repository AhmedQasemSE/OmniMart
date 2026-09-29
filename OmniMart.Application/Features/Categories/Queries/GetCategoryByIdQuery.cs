using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore; 
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Categories.Queries.GetCategoryById;

public record CategoryDetailDto(
    Guid Id,
    string Name,
    string Description,
    Guid? ParentCategoryId,
    byte[] RowVersion
);

public record GetCategoryByIdQuery(Guid Id) : IRequest<Result<CategoryDetailDto>>;

public class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, Result<CategoryDetailDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetCategoryByIdQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<CategoryDetailDto>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category == null)
        {
            return Result<CategoryDetailDto>.Failure("Category not found.");
        }

        var dto = new CategoryDetailDto(
            category.Id,
            category.Name,
            category.Description,
            category.ParentCategoryId,
            category.RowVersion
        );

        return Result<CategoryDetailDto>.Success(dto);
    }

    public class GetCategoryByIdQueryValidator : AbstractValidator<GetCategoryByIdQuery>
    {
        public GetCategoryByIdQueryValidator() => RuleFor(x => x.Id).NotEmpty().WithMessage("Category ID is required.");
    }
}