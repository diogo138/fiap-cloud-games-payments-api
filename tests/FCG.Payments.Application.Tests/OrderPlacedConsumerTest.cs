using FCG.Catalog.Application.Events;
using FCG.Payments.Application.Consumers;
using FCG.Payments.Application.Events;
using FCG.Payments.Application.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace FCG.Payments.Application.Tests;

[TestFixture]
public class OrderPlacedConsumerTest
{
    private Mock<IPaymentSimulatorService> _paymentServiceMock = null!;
    private Mock<IPublishEndpoint> _publishEndpointMock = null!;
    private OrderPlacedConsumer _consumer = null!;

    [SetUp]
    public void Setup()
    {
        _paymentServiceMock = new Mock<IPaymentSimulatorService>();
        _publishEndpointMock = new Mock<IPublishEndpoint>();
        _consumer = new OrderPlacedConsumer(
            _paymentServiceMock.Object,
            _publishEndpointMock.Object,
            NullLogger<OrderPlacedConsumer>.Instance);
    }

    [Test]
    public async Task Consume_DevePublicarPaymentProcessedEvent_QuandoApproved()
    {
        var orderId = Guid.NewGuid();
        var evt = new OrderPlacedEvent(orderId, 1, 10, "Game X", 49.99m, "user@test.com");

        _paymentServiceMock
            .Setup(s => s.ProcessarAsync(orderId, 1, 10, 49.99m))
            .ReturnsAsync("Approved");

        var contextMock = new Mock<ConsumeContext<OrderPlacedEvent>>();
        contextMock.Setup(c => c.Message).Returns(evt);

        await _consumer.Consume(contextMock.Object);

        _publishEndpointMock.Verify(
            p => p.Publish(
                It.Is<PaymentProcessedEvent>(e =>
                    e.OrderId == orderId &&
                    e.Status == "Approved" &&
                    e.UserEmail == "user@test.com"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Consume_DevePublicarPaymentProcessedEvent_QuandoRejected()
    {
        var orderId = Guid.NewGuid();
        var evt = new OrderPlacedEvent(orderId, 2, 20, "Game Y", 99.99m, "outro@test.com");

        _paymentServiceMock
            .Setup(s => s.ProcessarAsync(orderId, 2, 20, 99.99m))
            .ReturnsAsync("Rejected");

        var contextMock = new Mock<ConsumeContext<OrderPlacedEvent>>();
        contextMock.Setup(c => c.Message).Returns(evt);

        await _consumer.Consume(contextMock.Object);

        _publishEndpointMock.Verify(
            p => p.Publish(
                It.Is<PaymentProcessedEvent>(e =>
                    e.OrderId == orderId &&
                    e.Status == "Rejected"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Consume_NaoDeveLancarException_QuandoServicoFalha()
    {
        var orderId = Guid.NewGuid();
        var evt = new OrderPlacedEvent(orderId, 3, 30, "Game Z", 19.99m, "fail@test.com");

        _paymentServiceMock
            .Setup(s => s.ProcessarAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>()))
            .ThrowsAsync(new Exception("Erro simulado no pagamento"));

        var contextMock = new Mock<ConsumeContext<OrderPlacedEvent>>();
        contextMock.Setup(c => c.Message).Returns(evt);

        Assert.DoesNotThrowAsync(() => _consumer.Consume(contextMock.Object));
    }

    [Test]
    public async Task Consume_NaoDevePublicar_QuandoServicoFalha()
    {
        var orderId = Guid.NewGuid();
        var evt = new OrderPlacedEvent(orderId, 4, 40, "Game W", 9.99m, "error@test.com");

        _paymentServiceMock
            .Setup(s => s.ProcessarAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>()))
            .ThrowsAsync(new Exception("Falha crítica"));

        var contextMock = new Mock<ConsumeContext<OrderPlacedEvent>>();
        contextMock.Setup(c => c.Message).Returns(evt);

        await _consumer.Consume(contextMock.Object);

        _publishEndpointMock.Verify(
            p => p.Publish(It.IsAny<PaymentProcessedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
