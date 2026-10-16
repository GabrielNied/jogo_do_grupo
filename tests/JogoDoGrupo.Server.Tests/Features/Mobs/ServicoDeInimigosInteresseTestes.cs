using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Features.Mobs;
using JogoDoGrupo.Server.Tests.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Mobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace JogoDoGrupo.Server.Tests.Features.Mobs;

public class ServicoDeInimigosInteresseTestes(TempoRealFixture f) : IClassFixture<TempoRealFixture>
{
    [Fact]
    public void Falha_de_um_grupo_nao_impede_o_passo_do_outro()
    {
        var bom = new GrupoSimuladoFalso(deveFalhar: false);
        var ruim = new GrupoSimuladoFalso(deveFalhar: true);
        var servico = new ServicoDeInimigos(
            [ruim, bom],
            f.TempoReal,
            f.Fabrica.Services.GetRequiredService<Contratos.Mundo.IPosicoesDoMundo>(),
            f.Fabrica.Services.GetRequiredService<EventosDoServidor>(),
            NullLogger<ServicoDeInimigos>.Instance,
            f.Fabrica.Services.GetRequiredService<IEstadoDeQueda>()
        );

        servico.Avancar();

        Assert.True(bom.Passou);
    }

    [Fact]
    public void Avancar_reutiliza_buffer_de_jogadores_limpando_entre_grupos_e_passos()
    {
        var posicoes = new PosicoesFalsas();
        var queda = new QuedaFalsa();
        var grupoA = new GrupoComHistorico("area_a");
        var grupoB = new GrupoComHistorico("area_b");

        posicoes.Definir(1, "area_a", 10f, 20f);
        posicoes.Definir(2, "area_a", 30f, 40f);
        posicoes.Definir(3, "area_b", 50f, 60f);
        posicoes.Definir(4, "area_b", 70f, 80f);
        posicoes.Definir(5, "area_a", 90f, 100f);
        queda.Caidos.Add(5);

        var servico = new ServicoDeInimigos(
            [grupoA, grupoB],
            f.TempoReal,
            posicoes,
            f.Fabrica.Services.GetRequiredService<EventosDoServidor>(),
            NullLogger<ServicoDeInimigos>.Instance,
            queda
        );

        // 1º passo
        servico.Avancar();

        Assert.Single(grupoA.PassosRecebidos);
        Assert.Single(grupoB.PassosRecebidos);

        var passoA1 = grupoA.PassosRecebidos[0];
        Assert.Equal(2, passoA1.Length);
        Assert.Contains(passoA1, j => j.PersonagemId == 1 && j.X == 10f && j.Z == 20f);
        Assert.Contains(passoA1, j => j.PersonagemId == 2 && j.X == 30f && j.Z == 40f);

        var passoB1 = grupoB.PassosRecebidos[0];
        Assert.Equal(2, passoB1.Length);
        Assert.Contains(passoB1, j => j.PersonagemId == 3 && j.X == 50f && j.Z == 60f);
        Assert.Contains(passoB1, j => j.PersonagemId == 4 && j.X == 70f && j.Z == 80f);

        // 2º passo: jogador 1 se moveu, jogador 2 caiu, jogador 3 foi para area_a
        posicoes.Definir(1, "area_a", 15f, 25f);
        queda.Caidos.Add(2);
        posicoes.Definir(3, "area_a", 55f, 65f);

        servico.Avancar();

        Assert.Equal(2, grupoA.PassosRecebidos.Count);
        Assert.Equal(2, grupoB.PassosRecebidos.Count);

        var passoA2 = grupoA.PassosRecebidos[1];
        Assert.Equal(2, passoA2.Length);
        Assert.Contains(passoA2, j => j.PersonagemId == 1 && j.X == 15f && j.Z == 25f);
        Assert.Contains(passoA2, j => j.PersonagemId == 3 && j.X == 55f && j.Z == 65f);

        var passoB2 = grupoB.PassosRecebidos[1];
        Assert.Single(passoB2);
        Assert.Equal(4, passoB2[0].PersonagemId);
    }

    [Fact]
    public void AoEntrarNaArea_com_area_sem_inimigos_nao_lanca_nem_envia()
    {
        var posicoes = new PosicoesFalsas();
        var queda = new QuedaFalsa();
        var grupo = new GrupoComHistorico("area_comum");

        var servico = new ServicoDeInimigos(
            [grupo],
            f.TempoReal,
            posicoes,
            f.Fabrica.Services.GetRequiredService<EventosDoServidor>(),
            NullLogger<ServicoDeInimigos>.Instance,
            queda
        );

        servico.AoEntrarNaArea(new JogadorEntrouNaArea(999, "area_vazia"));
    }

    private sealed class GrupoSimuladoFalso(bool deveFalhar) : IInimigosSimulados
    {
        public bool Passou { get; private set; }

        public string Area => ConfigInimigosDoCampo.Area;

        public PassoDosInimigos Passo(IReadOnlyList<JogadorNoCampo> jogadores)
        {
            Passou = true;
            if (deveFalhar)
            {
                throw new InvalidOperationException("falha de teste");
            }

            return new PassoDosInimigos();
        }

        public InimigoNoMundo[] Vivos() => [];
    }

    private sealed class GrupoComHistorico(string area) : IInimigosSimulados
    {
        public string Area => area;
        public List<JogadorNoCampo[]> PassosRecebidos { get; } = new();

        public PassoDosInimigos Passo(IReadOnlyList<JogadorNoCampo> jogadores)
        {
            PassosRecebidos.Add(jogadores.ToArray());
            return new PassoDosInimigos();
        }

        public InimigoNoMundo[] Vivos() => [];
    }

    private sealed class PosicoesFalsas : Contratos.Mundo.IPosicoesDoMundo
    {
        private readonly Dictionary<long, Contratos.Mundo.PosicaoNoMundo> _pos = new();

        public void Definir(long id, string area, float x, float z) =>
            _pos[id] = new Contratos.Mundo.PosicaoNoMundo(area, x, 0f, z, 0f);

        public bool TentarObter(long personagemId, out Contratos.Mundo.PosicaoNoMundo posicao) =>
            _pos.TryGetValue(personagemId, out posicao);

        public IReadOnlyList<long> PersonagensNaArea(string area) =>
            _pos.Where(p => p.Value.Area == area).Select(p => p.Key).ToList();
    }

    private sealed class QuedaFalsa : IEstadoDeQueda
    {
        public HashSet<long> Caidos { get; } = [];

        public bool EstaCaido(long personagemId) => Caidos.Contains(personagemId);
    }
}
