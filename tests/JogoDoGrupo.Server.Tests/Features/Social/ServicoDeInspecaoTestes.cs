using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Contratos.Social;
using JogoDoGrupo.Server.Contratos.Talentos;
using JogoDoGrupo.Server.Features.Social;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Social;
using JogoDoGrupo.Shared.Features.Talentos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace JogoDoGrupo.Server.Tests.Features.Social;

public sealed class ServicoDeInspecaoTestes
{
    private sealed class SaidaInspecaoGravada : ISaidaDeInspecao
    {
        public readonly List<(long Para, RetratoInspecaoResponse Retrato)> Retratos = [];
        public readonly List<(long Para, InspecaoRecusada Recusa)> Recusas = [];

        public void Retrato(long paraPersonagemId, RetratoInspecaoResponse retrato) =>
            Retratos.Add((paraPersonagemId, retrato));

        public void Recusada(long paraPersonagemId, InspecaoRecusada recusa) =>
            Recusas.Add((paraPersonagemId, recusa));
    }

    private sealed class ConsultaEquipamentosFalsa : IConsultaInspecaoEquipamento
    {
        public Task<(
            IReadOnlyList<EntradaInventario> Equipados,
            int ConjuntoAtivo,
            AtributosDosEquipamentos Atributos
        )> ObterAsync(long personagemId, CancellationToken cancelamento)
        {
            IReadOnlyList<EntradaInventario> equipados =
            [
                new EntradaInventario
                {
                    InstanciaId = 1,
                    Posicao = PosicaoEquipamento.Peito,
                    Nome = "Peitoral de Ferro",
                    Equipamento = new Equipamento
                    {
                        ModeloId = 10,
                        Nome = "Peitoral de Ferro",
                        Tipo = TipoEquipamento.Peito,
                        TierOrigem = TierItem.S,
                        Raridade = RaridadeItem.Epico,
                        Nivel = 15,
                    },
                },
            ];

            return Task.FromResult(
                (equipados, 1, new AtributosDosEquipamentos { DefesaFisica = 30, VidaMaxima = 150 })
            );
        }
    }

    private sealed class ConsultaGrupoFalsa : IConsultaDeGrupo
    {
        public GrupoAtual? Grupo;

        public GrupoAtual? Consultar(long personagemId) => Grupo;
    }

    private sealed class PosicoesFalsa : IPosicoesDoMundo
    {
        public readonly Dictionary<long, string> Areas = [];

        public bool TentarObter(long personagemId, out PosicaoNoMundo posicao)
        {
            if (Areas.TryGetValue(personagemId, out var area))
            {
                posicao = new PosicaoNoMundo(area, 0, 0, 0, 0);
                return true;
            }

            posicao = default;
            return false;
        }

        public IReadOnlyList<long> PersonagensNaArea(string area) =>
            Areas.Where(kv => kv.Value == area).Select(kv => kv.Key).ToList();
    }

    private sealed class AtributosArvoreFalsa : IAtributosDaArvore
    {
        public Task<AtributosDaArvore> ObterAsync(
            long personagemId,
            CancellationToken cancelamento
        ) =>
            Task.FromResult(
                new AtributosDaArvore
                {
                    Forca = 10,
                    Vigor = 12,
                    Agilidade = 8,
                    Intelecto = 5,
                    Espirito = 7,
                }
            );
    }

    private readonly DiretorioFalso _diretorio = new();
    private readonly ConsultaEquipamentosFalsa _equipamentos = new();
    private readonly SaidaInspecaoGravada _saida = new();
    private readonly HashSet<long> _online = [];
    private readonly ConsultaGrupoFalsa _grupo = new();
    private readonly PosicoesFalsa _posicoes = new();
    private readonly AtributosArvoreFalsa _arvore = new();
    private readonly IServiceScopeFactory _escopos = new ServiceCollection()
        .BuildServiceProvider()
        .GetRequiredService<IServiceScopeFactory>();

    private ServicoDeInspecao CriarServico() =>
        new(
            _diretorio,
            _equipamentos,
            _escopos,
            _saida,
            id => _online.Contains(id),
            _grupo,
            () => _posicoes,
            () => _arvore,
            NullLogger<ServicoDeInspecao>.Instance
        );

    [Fact]
    public async Task Inspecao_de_alvo_inexistente_recusa_AlvoNaoEncontrado()
    {
        var servico = CriarServico();
        await servico.ProcessarPedidoAsync(
            1,
            new PedirInspecao { PersonagemId = 999 },
            CancellationToken.None
        );

        Assert.Single(_saida.Recusas);
        Assert.Equal(CodigoErroInspecao.AlvoNaoEncontrado, _saida.Recusas[0].Recusa.Motivo);
    }

    [Fact]
    public async Task Inspecao_de_alvo_offline_recusa_AlvoOffline()
    {
        _diretorio.Adicionar(2, "Bia");
        var servico = CriarServico();
        await servico.ProcessarPedidoAsync(
            1,
            new PedirInspecao { PersonagemId = 2 },
            CancellationToken.None
        );

        Assert.Single(_saida.Recusas);
        Assert.Equal(CodigoErroInspecao.AlvoOffline, _saida.Recusas[0].Recusa.Motivo);
    }

    [Fact]
    public async Task Inspecao_de_alvo_fora_do_alcance_recusa_AlvoForaDoAlcance()
    {
        _diretorio.Adicionar(1, "Ana");
        _diretorio.Adicionar(2, "Bia");
        _online.Add(1);
        _online.Add(2);
        _posicoes.Areas[1] = "Cidade";
        _posicoes.Areas[2] = "Dungeon";

        var servico = CriarServico();
        await servico.ProcessarPedidoAsync(
            1,
            new PedirInspecao { PersonagemId = 2 },
            CancellationToken.None
        );

        Assert.Single(_saida.Recusas);
        Assert.Equal(CodigoErroInspecao.AlvoForaDoAlcance, _saida.Recusas[0].Recusa.Motivo);
    }

    [Fact]
    public async Task Inspecao_de_si_mesmo_retorna_retrato()
    {
        _diretorio.Adicionar(1, "Ana");
        _online.Add(1);

        var servico = CriarServico();
        await servico.ProcessarPedidoAsync(
            1,
            new PedirInspecao { PersonagemId = 1 },
            CancellationToken.None
        );

        Assert.Empty(_saida.Recusas);
        Assert.Single(_saida.Retratos);
        var retrato = _saida.Retratos[0].Retrato;
        Assert.Equal(1, retrato.PersonagemId);
        Assert.Equal("Ana", retrato.Nome);
        Assert.Equal(10, retrato.Forca);
        Assert.Equal(30, retrato.Atributos.DefesaFisica);
        Assert.Single(retrato.Equipados);
        Assert.Equal(TierItem.S, retrato.Equipados[0].Equipamento.TierOrigem);
    }

    [Fact]
    public async Task Inspecao_de_aliado_no_grupo_mesmo_em_area_diferente_retorna_retrato()
    {
        _diretorio.Adicionar(1, "Ana");
        _diretorio.Adicionar(2, "Bia");
        _online.Add(1);
        _online.Add(2);
        _posicoes.Areas[1] = "Cidade";
        _posicoes.Areas[2] = "Dungeon";
        _grupo.Grupo = new GrupoAtual(10, 1, [1, 2]);

        var servico = CriarServico();
        await servico.ProcessarPedidoAsync(
            1,
            new PedirInspecao { PersonagemId = 2 },
            CancellationToken.None
        );

        Assert.Empty(_saida.Recusas);
        Assert.Single(_saida.Retratos);
        Assert.Equal("Bia", _saida.Retratos[0].Retrato.Nome);
    }

    [Fact]
    public async Task Inspecao_por_nome_localiza_jogador_na_mesma_area()
    {
        _diretorio.Adicionar(1, "Ana");
        _diretorio.Adicionar(2, "Bia");
        _online.Add(1);
        _online.Add(2);
        _posicoes.Areas[1] = "Cidade";
        _posicoes.Areas[2] = "Cidade";

        var servico = CriarServico();
        await servico.ProcessarPedidoAsync(
            1,
            new PedirInspecao { Nome = "bia" },
            CancellationToken.None
        );

        Assert.Empty(_saida.Recusas);
        Assert.Single(_saida.Retratos);
        Assert.Equal(2, _saida.Retratos[0].Retrato.PersonagemId);
        Assert.Equal("Bia", _saida.Retratos[0].Retrato.Nome);
    }
}
