using AutoMapper;
using Business.Contracts;
using Business.Services;
using DataAccessLayer.Contracts;
using Domains;
using Moq;
using System.Linq.Expressions;

namespace WebApi.Tests.Services;

public class PaymentTransactionServiceWebhookTests
{
    [Fact]
    public async Task RecordWebhookEvent_Adds_New_Event_When_Not_Existing()
    {
        var sut = CreateService(out _, out var webhookEvents, out _, out _, out _);

        await sut.RecordWebhookEvent("Stripe", "evt_001", "payment_intent.succeeded", "pi_001", "{\"id\":\"evt_001\"}");

        Assert.Single(webhookEvents);
        Assert.Equal("Stripe", webhookEvents[0].ProviderName);
        Assert.Equal("evt_001", webhookEvents[0].ProviderEventId);
        Assert.False(webhookEvents[0].IsProcessed);
    }

    [Fact]
    public async Task RecordWebhookEvent_Does_Not_Add_Duplicate_Event()
    {
        var sut = CreateService(out _, out var webhookEvents, out _, out _, out _);

        await sut.RecordWebhookEvent("Stripe", "evt_001", "payment_intent.succeeded", "pi_001", "payload");
        await sut.RecordWebhookEvent("Stripe", "evt_001", "payment_intent.succeeded", "pi_001", "payload");

        Assert.Single(webhookEvents);
    }

    [Fact]
    public async Task MarkWebhookEventProcessed_Updates_Flag_And_Notes()
    {
        var sut = CreateService(out _, out var webhookEvents, out _, out _, out _);

        await sut.RecordWebhookEvent("Stripe", "evt_100", "payment_intent.succeeded", "pi_100", "payload");
        await sut.MarkWebhookEventProcessed("Stripe", "evt_100", true, "Processed in test");

        var updated = webhookEvents.Single(x => x.ProviderEventId == "evt_100");
        Assert.True(updated.IsProcessed);
        Assert.Equal("Processed in test", updated.ProcessingNotes);
    }

    [Fact]
    public async Task IsWebhookEventProcessed_Returns_True_For_Processed_Event()
    {
        var sut = CreateService(out _, out var webhookEvents, out _, out _, out _);

        webhookEvents.Add(new TbPaymentWebhookEvent
        {
            Id = Guid.NewGuid(),
            ProviderName = "PayPal",
            ProviderEventId = "wh_200",
            Payload = "payload",
            IsProcessed = true,
            CreatedBy = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow,
            CurrentState = 1,
            ReceivedAt = DateTime.UtcNow
        });

        var result = await sut.IsWebhookEventProcessed("PayPal", "wh_200");

        Assert.True(result);
    }

    [Fact]
    public async Task ReconcileTransactionFromWebhook_Updates_Transaction_To_Completed_For_Succeeded_Event()
    {
        var sut = CreateService(out var transactions, out _, out _, out _, out _);
        var tx = new TbPaymentTransaction
        {
            Id = Guid.NewGuid(),
            ShipmentId = Guid.NewGuid(),
            PaymentMethodId = Guid.NewGuid(),
            TransactionReference = "pi_777",
            TransactionStatus = (int)PaymentTransactionStatus.Pending,
            TotalAmount = 25,
            ShippingRate = 20,
            CommissionAmount = 5,
            CommissionPercentage = 25,
            CreatedBy = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow,
            CurrentState = 1
        };
        transactions.Add(tx);

        var reconciled = await sut.ReconcileTransactionFromWebhook(
            "Stripe",
            "evt_777",
            "payment_intent.succeeded",
            "pi_777",
            "payload");

        Assert.True(reconciled);
        Assert.Equal((int)PaymentTransactionStatus.Completed, tx.TransactionStatus);
        Assert.Equal("Stripe", tx.ProviderName);
        Assert.Equal("evt_777", tx.ProviderEventId);
        Assert.Contains("Reconciled by webhook", tx.Notes ?? string.Empty);
    }

    private static PaymentTransactionService CreateService(
        out List<TbPaymentTransaction> transactions,
        out List<TbPaymentWebhookEvent> webhookEvents,
        out Mock<IGenericRepository<TbPaymentTransaction>> transactionRepoMock,
        out Mock<IGenericRepository<TbPaymentWebhookEvent>> webhookRepoMock,
        out Mock<IUserService> userServiceMock)
    {
        transactions = new List<TbPaymentTransaction>();
        webhookEvents = new List<TbPaymentWebhookEvent>();
        var transactionStore = transactions;
        var webhookEventStore = webhookEvents;

        transactionRepoMock = new Mock<IGenericRepository<TbPaymentTransaction>>();
        var paymentMethodRepoMock = new Mock<IGenericRepository<TbPaymentMethod>>();
        var shipmentRepoMock = new Mock<IGenericRepository<TbShippment>>();
        webhookRepoMock = new Mock<IGenericRepository<TbPaymentWebhookEvent>>();
        var mapperMock = new Mock<IMapper>();
        userServiceMock = new Mock<IUserService>();
        var paymentGatewayFactoryMock = new Mock<IPaymentGatewayFactory>();

        var testUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        userServiceMock.Setup(x => x.GetLoggedInUser()).Returns(testUserId);

        transactionRepoMock
            .Setup(x => x.GetList(
                It.IsAny<Expression<Func<TbPaymentTransaction, bool>>>(),
                It.IsAny<Expression<Func<TbPaymentTransaction, TbPaymentTransaction>>>(),
                It.IsAny<Expression<Func<TbPaymentTransaction, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<TbPaymentTransaction, object>>[]>()))
            .ReturnsAsync((Expression<Func<TbPaymentTransaction, bool>> filter,
                           Expression<Func<TbPaymentTransaction, TbPaymentTransaction>> selector,
                           Expression<Func<TbPaymentTransaction, object>> orderBy,
                           bool isDescending,
                           Expression<Func<TbPaymentTransaction, object>>[] includes) =>
            {
                var compiled = filter.Compile();
                return transactionStore.Where(compiled).ToList();
            });

        transactionRepoMock
            .Setup(x => x.Update(It.IsAny<TbPaymentTransaction>()))
            .ReturnsAsync((TbPaymentTransaction entity) =>
            {
                var existing = transactionStore.FirstOrDefault(t => t.Id == entity.Id);
                if (existing == null)
                {
                    transactionStore.Add(entity);
                    return true;
                }

                var index = transactionStore.IndexOf(existing);
                transactionStore[index] = entity;
                return true;
            });

        webhookRepoMock
            .Setup(x => x.GetFirstOrDefault(It.IsAny<Expression<Func<TbPaymentWebhookEvent, bool>>>() ))
            .ReturnsAsync((Expression<Func<TbPaymentWebhookEvent, bool>> filter) =>
                webhookEventStore.FirstOrDefault(filter.Compile()));

        webhookRepoMock
            .Setup(x => x.Add(It.IsAny<TbPaymentWebhookEvent>()))
            .ReturnsAsync((TbPaymentWebhookEvent entity) =>
            {
                if (entity.Id == Guid.Empty)
                {
                    entity.Id = Guid.NewGuid();
                }

                webhookEventStore.Add(entity);
                return (true, entity.Id);
            });

        webhookRepoMock
            .Setup(x => x.Update(It.IsAny<TbPaymentWebhookEvent>()))
            .ReturnsAsync((TbPaymentWebhookEvent entity) =>
            {
                var existing = webhookEventStore.FirstOrDefault(e => e.ProviderName == entity.ProviderName && e.ProviderEventId == entity.ProviderEventId);
                if (existing == null)
                {
                    webhookEventStore.Add(entity);
                    return true;
                }

                var index = webhookEventStore.IndexOf(existing);
                webhookEventStore[index] = entity;
                return true;
            });

        return new PaymentTransactionService(
            transactionRepoMock.Object,
            paymentMethodRepoMock.Object,
            shipmentRepoMock.Object,
            webhookRepoMock.Object,
            mapperMock.Object,
            userServiceMock.Object,
            paymentGatewayFactoryMock.Object);
    }
}
