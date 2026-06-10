using Business.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using WebApi.Controllers;

namespace WebApi.Tests.Controllers;

public class PaymentWebhooksControllerIntegrationTests
{
    [Fact]
    public async Task Stripe_InvalidSignature_ReturnsBadRequest()
    {
        var stripeGateway = new Mock<IPaymentGateway>();
        stripeGateway.Setup(x => x.ValidateWebhook(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var gatewayFactory = CreateGatewayFactory(stripeGateway.Object, CreateDefaultGateway());
        var paymentTxService = CreatePaymentTransactionService();

        using var server = CreateServer(gatewayFactory.Object, paymentTxService.Object);
        using var client = server.CreateClient();

        var payload = "{\"id\":\"evt_001\",\"type\":\"payment_intent.succeeded\",\"data\":{\"object\":{\"id\":\"pi_001\"}}}";
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/PaymentWebhooks/stripe")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "bad-signature");

        var response = await client.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Invalid Stripe webhook", ReadMessage(json));

        paymentTxService.Verify(x => x.RecordWebhookEvent(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Stripe_AlreadyProcessed_ReturnsOk_Without_Reconcile()
    {
        var stripeGateway = new Mock<IPaymentGateway>();
        stripeGateway.Setup(x => x.ValidateWebhook(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var gatewayFactory = CreateGatewayFactory(stripeGateway.Object, CreateDefaultGateway());
        var paymentTxService = CreatePaymentTransactionService();
        paymentTxService.Setup(x => x.IsWebhookEventProcessed("Stripe", "evt_002")).ReturnsAsync(true);

        using var server = CreateServer(gatewayFactory.Object, paymentTxService.Object);
        using var client = server.CreateClient();

        var payload = "{\"id\":\"evt_002\",\"type\":\"payment_intent.succeeded\",\"data\":{\"object\":{\"id\":\"pi_002\"}}}";
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/PaymentWebhooks/stripe")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "valid-signature");

        var response = await client.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Stripe webhook already processed", ReadMessage(json));

        paymentTxService.Verify(x => x.RecordWebhookEvent(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        paymentTxService.Verify(x => x.ReconcileTransactionFromWebhook(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Stripe_ValidWebhook_RecordsAndReconciles()
    {
        var stripeGateway = new Mock<IPaymentGateway>();
        stripeGateway.Setup(x => x.ValidateWebhook(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var gatewayFactory = CreateGatewayFactory(stripeGateway.Object, CreateDefaultGateway());
        var paymentTxService = CreatePaymentTransactionService();
        paymentTxService.Setup(x => x.IsWebhookEventProcessed("Stripe", "evt_003")).ReturnsAsync(false);
        paymentTxService.Setup(x => x.ReconcileTransactionFromWebhook("Stripe", "evt_003", "payment_intent.succeeded", "pi_003", It.IsAny<string>()))
            .ReturnsAsync(true);

        using var server = CreateServer(gatewayFactory.Object, paymentTxService.Object);
        using var client = server.CreateClient();

        var payload = "{\"id\":\"evt_003\",\"type\":\"payment_intent.succeeded\",\"data\":{\"object\":{\"id\":\"pi_003\"}}}";
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/PaymentWebhooks/stripe")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "valid-signature");

        var response = await client.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Stripe webhook processed", ReadMessage(json));

        paymentTxService.Verify(x => x.RecordWebhookEvent("Stripe", "evt_003", "payment_intent.succeeded", "pi_003", It.IsAny<string>()), Times.Once);
        paymentTxService.Verify(x => x.ReconcileTransactionFromWebhook("Stripe", "evt_003", "payment_intent.succeeded", "pi_003", It.IsAny<string>()), Times.Once);
        paymentTxService.Verify(x => x.MarkWebhookEventProcessed("Stripe", "evt_003", true, "Reconciled successfully"), Times.Once);
    }

    [Fact]
    public async Task PayPal_MissingEventId_ReturnsBadRequest()
    {
        var payPalGateway = new Mock<IPaymentGateway>();
        payPalGateway.Setup(x => x.ValidateWebhook(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var gatewayFactory = CreateGatewayFactory(CreateDefaultGateway(), payPalGateway.Object);
        var paymentTxService = CreatePaymentTransactionService();

        using var server = CreateServer(gatewayFactory.Object, paymentTxService.Object);
        using var client = server.CreateClient();

        var payload = "{\"event_type\":\"PAYMENT.CAPTURE.COMPLETED\",\"resource\":{\"id\":\"CAPTURE_001\"}}";
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/PaymentWebhooks/paypal")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Paypal-Transmission-Sig", "valid-signature");

        var response = await client.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Invalid PayPal webhook payload", ReadMessage(json));
    }

    private static TestServer CreateServer(IPaymentGatewayFactory gatewayFactory, IPaymentTransactionService paymentTransactionService)
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddSingleton(gatewayFactory);
                services.AddSingleton(paymentTransactionService);
                services.AddControllers().AddApplicationPart(typeof(PaymentWebhooksController).Assembly);
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => endpoints.MapControllers());
            });

        return new TestServer(builder);
    }

    private static Mock<IPaymentGatewayFactory> CreateGatewayFactory(IPaymentGateway stripeGateway, IPaymentGateway payPalGateway)
    {
        var factory = new Mock<IPaymentGatewayFactory>();
        factory.Setup(x => x.GetGateway("Stripe")).Returns(stripeGateway);
        factory.Setup(x => x.GetGateway("PayPal")).Returns(payPalGateway);
        return factory;
    }

    private static Mock<IPaymentTransactionService> CreatePaymentTransactionService()
    {
        var paymentTxService = new Mock<IPaymentTransactionService>();
        paymentTxService.Setup(x => x.IsWebhookEventProcessed(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
        paymentTxService.Setup(x => x.RecordWebhookEvent(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        paymentTxService.Setup(x => x.ReconcileTransactionFromWebhook(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
        paymentTxService.Setup(x => x.MarkWebhookEventProcessed(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        return paymentTxService;
    }

    private static IPaymentGateway CreateDefaultGateway()
    {
        var gateway = new Mock<IPaymentGateway>();
        gateway.Setup(x => x.ValidateWebhook(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        return gateway.Object;
    }

    private static string ReadMessage(string json)
    {
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("Message", out var pascalCaseMessage))
            return pascalCaseMessage.GetString() ?? string.Empty;

        if (doc.RootElement.TryGetProperty("message", out var camelCaseMessage))
            return camelCaseMessage.GetString() ?? string.Empty;

        return string.Empty;
    }
}
