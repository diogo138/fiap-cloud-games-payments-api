using FCG.Catalog.Application.Events;
using FCG.Payments.Application.Events;
using FCG.Payments.Application.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FCG.Payments.Application.Consumers;

public class OrderPlacedConsumer : IConsumer<OrderPlacedEvent>
{
    private readonly IPaymentSimulatorService _paymentService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<OrderPlacedConsumer> _logger;

    public OrderPlacedConsumer(
        IPaymentSimulatorService paymentService,
        IPublishEndpoint publishEndpoint,
        ILogger<OrderPlacedConsumer> logger)
    {
        _paymentService = paymentService;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
    {
        var evt = context.Message;

        _logger.LogInformation(
            "[PAYMENTS] Recebido OrderPlacedEvent: OrderId={OrderId} GameId={GameId} UserId={UserId}",
            evt.OrderId, evt.GameId, evt.UserId);

        try
        {
            var status = await _paymentService.ProcessarAsync(
                evt.OrderId, evt.UserId, evt.GameId, evt.Price);

            await _publishEndpoint.Publish(new PaymentProcessedEvent(
                evt.OrderId,
                evt.UserId,
                evt.GameId,
                evt.GameName,
                evt.UserEmail,
                status,
                DateTime.UtcNow));

            _logger.LogInformation(
                "[PAYMENTS] Publicado PaymentProcessedEvent: OrderId={OrderId} Status={Status}",
                evt.OrderId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[PAYMENTS] Erro ao processar OrderId={OrderId}. Mensagem descartada para evitar reprocessamento infinito.",
                evt.OrderId);
        }
    }
}
