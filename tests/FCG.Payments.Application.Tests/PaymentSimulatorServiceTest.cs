using FCG.Payments.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace FCG.Payments.Application.Tests;

[TestFixture]
public class PaymentSimulatorServiceTest
{
    private static PaymentSimulatorService CriarService(int taxaAprovacao)
    {
        var options = Options.Create(new PaymentSettings { TaxaAprovacaoPercent = taxaAprovacao });
        var logger = NullLogger<PaymentSimulatorService>.Instance;
        return new PaymentSimulatorService(options, logger);
    }

    [Test]
    public async Task ProcessarAsync_DeveRetornarApprovedOuRejected()
    {
        var service = CriarService(80);

        var resultado = await service.ProcessarAsync(Guid.NewGuid(), 1, 1, 29.99m);

        Assert.That(resultado, Is.EqualTo("Approved").Or.EqualTo("Rejected"));
    }

    [Test]
    public async Task ProcessarAsync_ComTaxa100_DeveRetornarSempreApproved()
    {
        var service = CriarService(100);

        var resultado = await service.ProcessarAsync(Guid.NewGuid(), 1, 1, 29.99m);

        Assert.That(resultado, Is.EqualTo("Approved"));
    }

    [Test]
    public async Task ProcessarAsync_ComTaxa0_DeveRetornarSempreRejected()
    {
        var service = CriarService(0);

        var resultado = await service.ProcessarAsync(Guid.NewGuid(), 1, 1, 29.99m);

        Assert.That(resultado, Is.EqualTo("Rejected"));
    }

    [Test]
    public async Task ProcessarAsync_DeveAceitarDadosValidos()
    {
        var service = CriarService(80);
        var orderId = Guid.NewGuid();

        var resultado = await service.ProcessarAsync(orderId, userId: 42, gameId: 7, preco: 59.90m);

        Assert.That(resultado, Is.EqualTo("Approved").Or.EqualTo("Rejected"));
    }

    [Test]
    [Repeat(10)]
    public async Task ProcessarAsync_ComTaxa50_DeveRetornarApprovedOuRejected()
    {
        var service = CriarService(50);

        var resultado = await service.ProcessarAsync(Guid.NewGuid(), 1, 1, 10.00m);

        Assert.That(resultado, Is.EqualTo("Approved").Or.EqualTo("Rejected"));
    }
}
