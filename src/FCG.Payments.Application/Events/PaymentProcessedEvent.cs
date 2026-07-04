namespace FCG.Payments.Application.Events;

public record PaymentProcessedEvent(
    Guid OrderId,
    int UserId,
    int GameId,
    string GameName,
    string UserEmail,
    string Status,
    DateTime ProcessedAt);
