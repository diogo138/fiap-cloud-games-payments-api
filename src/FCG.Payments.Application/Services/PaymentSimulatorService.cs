using FCG.Payments.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FCG.Payments.Application.Services;

public class PaymentSettings
{
    public int TaxaAprovacaoPercent { get; set; } = 80;
}

public class PaymentSimulatorService : IPaymentSimulatorService
{
    private readonly int _taxaAprovacao;
    private readonly ILogger<PaymentSimulatorService> _logger;

    public PaymentSimulatorService(IOptions<PaymentSettings> options, ILogger<PaymentSimulatorService> logger)
    {
        _taxaAprovacao = options.Value.TaxaAprovacaoPercent;
        _logger = logger;
    }

    public async Task<string> ProcessarAsync(Guid orderId, int userId, int gameId, decimal preco)
    {
        await Task.Delay(500);

        var rng = new Random();
        var aprovado = rng.Next(1, 101) <= _taxaAprovacao;
        var status = aprovado ? "Approved" : "Rejected";

        _logger.LogInformation(
            "[PAYMENTS] Processando OrderId={OrderId} GameId={GameId} Valor={Preco:C} → {Status}",
            orderId, gameId, preco, status);

        return status;
    }
}
