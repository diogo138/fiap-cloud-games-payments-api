namespace FCG.Payments.Application.Interfaces;

public interface IPaymentSimulatorService
{
    Task<string> ProcessarAsync(Guid orderId, int userId, int gameId, decimal preco);
}
