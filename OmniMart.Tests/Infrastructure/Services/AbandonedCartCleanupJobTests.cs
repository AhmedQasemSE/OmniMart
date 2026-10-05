using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;
using OmniMart.Infrastructure.Data;
using OmniMart.Infrastructure.Services.BackgroundJobs;
using OmniMart.Tests.Builders;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Infrastructure.BackgroundJobs;

public class AbandonedCartCleanupJobTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ILogger<AbandonedCartCleanupJob>> _loggerMock;
    private readonly AbandonedCartCleanupJob _job;

    public AbandonedCartCleanupJobTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _loggerMock = new Mock<ILogger<AbandonedCartCleanupJob>>();

        _job = new AbandonedCartCleanupJob(_dbContext, _loggerMock.Object);
    }
    [Fact]
    public async Task ExecuteAsync_ShouldDoNothing_WhenNoAbandonedCartsExist()
    {
        Func<Task> act = async () => await _job.ExecuteAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldIgnorePaymentGroup_WhenNotExpired()
    {
        var product = new ProductBuilder()
            .WithVariant("SKU-123", 100m, 10) 
            .Build();

        var variant = product.Variants.First();
        variant.ReserveStock(5);

        var customer = new CustomerProfileBuilder().Build();

        var paymentGroup = new PaymentGroupBuilder()
            .WithCustomerId(customer.Id)
            .WithCreatedAt(DateTimeOffset.UtcNow.AddMinutes(-5)) 
            .Build();
        var order = new OrderBuilder().WithPaymentGroupId(paymentGroup.Id).Build();
        order.AddOrderItem(variant.Id, 100m, 5);
        paymentGroup.AddOrder(order);

        await _dbContext.ProductVariants.AddAsync(variant);
        await _dbContext.PaymentGroups.AddAsync(paymentGroup);
        await _dbContext.SaveChangesAsync();

        await _job.ExecuteAsync();

        var updatedVariant = await _dbContext.ProductVariants.FirstAsync();
        var updatedPaymentGroup = await _dbContext.PaymentGroups.FirstAsync();
        var updatedOrder = updatedPaymentGroup.Orders.First();

        updatedOrder.Status.Should().NotBe(OrderStatus.Cancelled); 
        updatedVariant.ReservedStock.Should().Be(5); 
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCancelExpiredPaymentGroup_AndReleaseStock()
    {
        var product = new ProductBuilder()
            .WithVariant("SKU-123", 100m, 10)
            .Build();

        var variant = product.Variants.First();
        variant.ReserveStock(5);


        var customer = new CustomerProfileBuilder().Build();

        var paymentGroup = new PaymentGroupBuilder()
            .WithCustomerId(customer.Id)
            .WithCreatedAt(DateTimeOffset.UtcNow.AddMinutes(-20)) 
            .Build();

        var order = new OrderBuilder().WithPaymentGroupId(paymentGroup.Id).Build();
        order.AddOrderItem(variant.Id, 100m, 5); 
        paymentGroup.AddOrder(order);

        await _dbContext.ProductVariants.AddAsync(variant);
        await _dbContext.PaymentGroups.AddAsync(paymentGroup);
        await _dbContext.SaveChangesAsync();

        await _job.ExecuteAsync();

        var updatedVariant = await _dbContext.ProductVariants.FirstAsync();
        var updatedPaymentGroup = await _dbContext.PaymentGroups.FirstAsync();
        var updatedOrder = updatedPaymentGroup.Orders.First();

        updatedPaymentGroup.IsPaid.Should().BeFalse();
        updatedOrder.Status.Should().Be(OrderStatus.Cancelled); 
        updatedVariant.ReservedStock.Should().Be(0); 
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}