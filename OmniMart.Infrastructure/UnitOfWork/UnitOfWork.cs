using Microsoft.EntityFrameworkCore.Storage;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Domain.Entities;
using OmniMart.Infrastructure.Data;
using OmniMart.Infrastructure.Data.Configurations;

namespace OmniMart.Infrastructure.Data.Configurations;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _currentTransaction;
    public IUserRepository Users { get; private set; }
    public ICustomerProfileRepository CustomerProfiles { get; private set; }
    public IVendorProfileRepository VendorProfiles { get; private set; }
    public IProductRepository Products { get; private set; }    
    public ICategoryRepository Categories { get; private set; }    
    public IStaffProfileRepository StaffProfiles { get; private set; }
    public ICartRepository Carts { get; private set; }
    public IOrderRepository Orders { get; private set; }
    public IProductReviewRepository ProductReviews { get; private set; }
    public IPaymentGroupRepository PaymentGroups { get; private set; }
    public IProductAttributeRepository ProductAttributes { get; private set; }
    public UnitOfWork(AppDbContext context , IUserRepository users,
        ICustomerProfileRepository customerProfiles, 
        IVendorProfileRepository vendorProfiles, 
        IProductRepository products, 
        ICategoryRepository categories,
        IStaffProfileRepository staffProfiles,
        ICartRepository carts,
        IOrderRepository orders,
        IProductReviewRepository productReviews,
        IPaymentGroupRepository paymentGroups,
        IProductAttributeRepository productAttributes)

    {
        _context = context;
        Users =  users;
        CustomerProfiles =  customerProfiles; 
        VendorProfiles =  vendorProfiles;
        Products =  products;
        Categories =  categories;
        StaffProfiles =  staffProfiles;
        Carts =  carts;
        Orders =  orders;
        ProductReviews =  productReviews;
        PaymentGroups = paymentGroups;
        ProductAttributes = productAttributes;
    }
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => await _context.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            throw new InvalidOperationException("A transaction is already in progress.");
        }
        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);

            if (_currentTransaction != null)
            {
                await _currentTransaction.CommitAsync(cancellationToken);
            }
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.RollbackAsync(cancellationToken);
            }
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

    public void Dispose() => _context.Dispose();






}
