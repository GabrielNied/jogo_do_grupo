using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Economia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JogoDoGrupo.Server.Features.Economia;

/// <summary>
/// Conecta as mensagens UDP em tempo real ao <see cref="ServicoDeBancoGuilda"/> (#338).
/// </summary>
public sealed class BancoGuildaDoServidor(
    IServiceScopeFactory escopos,
    ILogger<BancoGuildaDoServidor> log
)
{
    private ServidorTempoReal? _tempoReal;

    public void Ligar(ServidorTempoReal tempoReal) => _tempoReal = tempoReal;

    public void AoConsultar(SessaoTempoReal sessao, ConsultarBancoGuilda _) =>
        Executar(
            sessao,
            async servico =>
            {
                var res = await servico.ConsultarAsync(sessao.PersonagemId, CancellationToken.None);
                if (res.Sucesso && res.Estado != null)
                {
                    Enviar(sessao, EconomiaMessageIds.EstadoDoBancoGuilda, res.Estado);
                }
                else
                {
                    EnviarRecusa(sessao, res.CodigoErro ?? CodigosErroEconomia.SemGuilda);
                }
            }
        );

    public void AoDepositarItem(SessaoTempoReal sessao, DepositarItemBancoGuilda pedido) =>
        Executar(
            sessao,
            async servico =>
            {
                var res = await servico.DepositarItemAsync(
                    sessao.PersonagemId,
                    pedido.InstanciaId,
                    pedido.Aba,
                    pedido.Casa,
                    CancellationToken.None
                );
                if (res.Sucesso && res.Estado != null)
                {
                    Enviar(sessao, EconomiaMessageIds.EstadoDoBancoGuilda, res.Estado);
                    Enviar(
                        sessao,
                        EconomiaMessageIds.AvisoBancoGuilda,
                        new AvisoBancoGuilda { Mensagem = "Item depositado no cofre da guilda." }
                    );
                }
                else
                {
                    EnviarRecusa(sessao, res.CodigoErro ?? CodigosErroEconomia.ItemNaoNegociavel);
                }
            }
        );

    public void AoRetirarItem(SessaoTempoReal sessao, RetirarItemBancoGuilda pedido) =>
        Executar(
            sessao,
            async servico =>
            {
                var res = await servico.RetirarItemAsync(
                    sessao.PersonagemId,
                    pedido.ItemBancoId,
                    CancellationToken.None
                );
                if (res.Sucesso && res.Estado != null)
                {
                    Enviar(sessao, EconomiaMessageIds.EstadoDoBancoGuilda, res.Estado);
                    Enviar(
                        sessao,
                        EconomiaMessageIds.AvisoBancoGuilda,
                        new AvisoBancoGuilda { Mensagem = "Item retirado do cofre da guilda." }
                    );
                }
                else
                {
                    EnviarRecusa(
                        sessao,
                        res.CodigoErro ?? CodigosErroEconomia.ItemNaoEncontradoNoBanco
                    );
                }
            }
        );

    public void AoDepositarCoroas(SessaoTempoReal sessao, DepositarCoroasBancoGuilda pedido) =>
        Executar(
            sessao,
            async servico =>
            {
                var res = await servico.DepositarCoroasAsync(
                    sessao.PersonagemId,
                    pedido.Quantidade,
                    CancellationToken.None
                );
                if (res.Sucesso && res.Estado != null)
                {
                    Enviar(sessao, EconomiaMessageIds.EstadoDoBancoGuilda, res.Estado);
                    Enviar(
                        sessao,
                        EconomiaMessageIds.AvisoBancoGuilda,
                        new AvisoBancoGuilda
                        {
                            Mensagem =
                                $"Depositadas {pedido.Quantidade} coroas no cofre da guilda.",
                        }
                    );
                }
                else
                {
                    EnviarRecusa(sessao, res.CodigoErro ?? CodigosErroEconomia.SaldoInsuficiente);
                }
            }
        );

    public void AoRetirarCoroas(SessaoTempoReal sessao, RetirarCoroasBancoGuilda pedido) =>
        Executar(
            sessao,
            async servico =>
            {
                var res = await servico.RetirarCoroasAsync(
                    sessao.PersonagemId,
                    pedido.Quantidade,
                    CancellationToken.None
                );
                if (res.Sucesso && res.Estado != null)
                {
                    Enviar(sessao, EconomiaMessageIds.EstadoDoBancoGuilda, res.Estado);
                    Enviar(
                        sessao,
                        EconomiaMessageIds.AvisoBancoGuilda,
                        new AvisoBancoGuilda
                        {
                            Mensagem = $"Retiradas {pedido.Quantidade} coroas do cofre da guilda.",
                        }
                    );
                }
                else
                {
                    EnviarRecusa(
                        sessao,
                        res.CodigoErro ?? CodigosErroEconomia.CoroasInsuficientesNoBanco
                    );
                }
            }
        );

    private void Executar(SessaoTempoReal sessao, Func<ServicoDeBancoGuilda, Task> acao) =>
        _ = Task.Run(async () =>
        {
            try
            {
                await using var escopo = escopos.CreateAsyncScope();
                var servico = escopo.ServiceProvider.GetRequiredService<ServicoDeBancoGuilda>();
                await acao(servico);
            }
            catch (Exception ex)
            {
                log.LogWarning(
                    ex,
                    "Falha na operação de banco de guilda para personagem {PersonagemId}",
                    sessao.PersonagemId
                );
                EnviarRecusa(sessao, CodigosErroEconomia.SemPermissaoBancoGuilda);
            }
        });

    private void Enviar<T>(SessaoTempoReal sessao, ushort id, T mensagem) =>
        _tempoReal?.Enviar(sessao, id, mensagem);

    private void EnviarRecusa(SessaoTempoReal sessao, string motivo) =>
        Enviar(sessao, EconomiaMessageIds.EconomiaRecusada, new EconomiaRecusada(motivo));
}
