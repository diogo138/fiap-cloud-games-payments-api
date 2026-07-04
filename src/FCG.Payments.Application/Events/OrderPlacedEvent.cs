namespace FCG.Catalog.Application.Events;

public record OrderPlacedEvent(
    Guid OrderId,
    int UserId,
    int GameId,
    string GameName,
    decimal Price,
    string UserEmail);
