using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Contratos.Social;
using JogoDoGrupo.Server.Contratos.Talentos;
using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Server.Features.Social;

/// <summary>
/// Trata os pedidos de inspeção de equipamento e build entre jogadores.
/// Garante que o alvo existe, está online e acessível (mesma área ou mesmo grupo),
/// e devolve o retrato completo somente leitura.
/// </summary>
public sealed class ServicoDeInspecao(
    IDiretorioDePersonagens diretorio,
    IConsultaInspecaoEquipamento consultaEquipamentos,
    IServiceScopeFactory escopos,
    ISaidaDeInspecao saida,
    Func<long, bool> estaOnline,
    IConsultaDeGrupo consultaGrupo,
    Func<IPosicoesDoMundo?> obterPosicoes,
    Func<IAtributosDaArvore?> obterAtributosArvore,
    ILogger<ServicoDeInspecao> log
)
{
    public async Task ProcessarPedidoAsync(
        long solicitanteId,
        PedirInspecao pedido,
        CancellationToken cancelamento
    )
    {
        try
        {
            var alvoId = pedido.PersonagemId;
            if (alvoId <= 0 && !string.IsNullOrWhiteSpace(pedido.Nome))
            {
                if (!diretorio.TentarObterPorNome(pedido.Nome.Trim(), out alvoId))
                {
                    saida.Recusada(
                        solicitanteId,
                        new InspecaoRecusada
                        {
                            PersonagemId = 0,
                            Motivo = CodigoErroInspecao.AlvoNaoEncontrado,
                        }
                    );
                    return;
                }
            }

            if (alvoId <= 0 || !diretorio.TentarObterNome(alvoId, out var nomeAlvo))
            {
                saida.Recusada(
                    solicitanteId,
                    new InspecaoRecusada
                    {
                        PersonagemId = alvoId,
                        Motivo = CodigoErroInspecao.AlvoNaoEncontrado,
                    }
                );
                return;
            }

            // Alvo precisa estar online
            if (!estaOnline(alvoId))
            {
                saida.Recusada(
                    solicitanteId,
                    new InspecaoRecusada
                    {
                        PersonagemId = alvoId,
                        Motivo = CodigoErroInspecao.AlvoOffline,
                    }
                );
                return;
            }

            // Alcance: inspecionar a si mesmo, aliado no mesmo grupo ou jogador na mesma área
            var alcanceValido = alvoId == solicitanteId;
            if (!alcanceValido)
            {
                var grupo = consultaGrupo.Consultar(solicitanteId);
                if (grupo != null && grupo.Membros.Contains(alvoId))
                {
                    alcanceValido = true;
                }
            }

            if (!alcanceValido)
            {
                var posicoes = obterPosicoes();
                if (
                    posicoes != null
                    && posicoes.TentarObter(solicitanteId, out var posSolicitante)
                    && posicoes.TentarObter(alvoId, out var posAlvo)
                    && string.Equals(posSolicitante.Area, posAlvo.Area, StringComparison.Ordinal)
                )
                {
                    alcanceValido = true;
                }
            }

            if (!alcanceValido)
            {
                saida.Recusada(
                    solicitanteId,
                    new InspecaoRecusada
                    {
                        PersonagemId = alvoId,
                        Motivo = CodigoErroInspecao.AlvoForaDoAlcance,
                    }
                );
                return;
            }

            diretorio.TentarObterRaca(alvoId, out var raca);

            var nivel = 1;
            try
            {
                await using var escopo = escopos.CreateAsyncScope();
                var pontosDeNivel = escopo.ServiceProvider.GetService<IPontosDeNivel>();
                if (pontosDeNivel != null)
                {
                    nivel = await pontosDeNivel.ObterNivelAsync(alvoId, cancelamento);
                }
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Não foi possível obter nível de {AlvoId}", alvoId);
            }

            var (equipados, conjuntoAtivo, atributosEquip) = await consultaEquipamentos.ObterAsync(
                alvoId,
                cancelamento
            );

            var atributosArvore = obterAtributosArvore();
            var atributosBase =
                atributosArvore != null
                    ? await atributosArvore.ObterAsync(alvoId, cancelamento)
                    : default;

            var resposta = new RetratoInspecaoResponse
            {
                PersonagemId = alvoId,
                Nome = nomeAlvo,
                Raca = raca,
                Nivel = Math.Max(1, nivel),
                ConjuntoAtivo = conjuntoAtivo,
                Equipados = equipados.ToArray(),
                Atributos = atributosEquip,
                Forca = atributosBase?.Forca ?? 0,
                Vigor = atributosBase?.Vigor ?? 0,
                Agilidade = atributosBase?.Agilidade ?? 0,
                Intelecto = atributosBase?.Intelecto ?? 0,
                Espirito = atributosBase?.Espirito ?? 0,
            };

            saida.Retrato(solicitanteId, resposta);
        }
        catch (Exception erro)
        {
            log.LogError(erro, "Erro ao processar inspeção de {AlvoId}", pedido.PersonagemId);
        }
    }
}
