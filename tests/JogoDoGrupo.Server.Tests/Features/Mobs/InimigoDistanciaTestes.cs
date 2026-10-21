using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Features.Mobs;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Tests.Features.Mobs;

public class InimigoDistanciaTestes
{
    private const long JogadorId = 42;
    private const long PrimeiroId = ConfigInimigosDoCampo.PrimeiroInimigoId;

    private static readonly DefinicaoDeCriatura Arqueiro = CatalogoCriaturas.Obter(
        CatalogoCriaturas.ArqueiroDoCampo
    )!;

    private sealed class QuedaMock : IEstadoDeQueda
    {
        public bool EstaCaido(long personagemId) => false;
    }

    private sealed class SorteadorMock : ISorteadorDeCriaturas
    {
        public bool Aparece(DefinicaoDeCriatura definicao) => true;

        public double Proximo() => 0.0;

        public CriaturaSorteada Sortear(DefinicaoDeCriatura definicao) =>
            new(
                definicao,
                VarianteDeCriatura.Normal,
                definicao.VidaBase,
                definicao.DanoBase,
                definicao.ExperienciaBase,
                1.0,
                1f,
                definicao.Nivel
            );
    }

    private sealed class DisparoMock : IDisparoDeProjeteis
    {
        public List<(
            long AtacanteId,
            TipoDeProjetil Tipo,
            float PosX,
            float PosZ,
            float DirYGraus,
            int Dano
        )> Disparos { get; } = [];

        public long Disparar(
            long atacanteId,
            string area,
            TipoDeProjetil tipoProjetil,
            TipoDeAtaque tipoAtaque,
            float origemX,
            float origemY,
            float origemZ,
            float direcaoYGraus,
            int dano,
            float elevacaoGraus = 0f,
            int sequencia = 0,
            int passo = 0,
            long instanteVistoMs = 0
        )
        {
            Disparos.Add((atacanteId, tipoProjetil, origemX, origemZ, direcaoYGraus, dano));
            return 1L;
        }
    }

    private sealed class CenarioDistancia
    {
        private const int PassoMs = 100;

        public CenarioDistancia(double x = 0, double z = 0)
        {
            Disparos = new DisparoMock();
            Grupo = new GrupoDeInimigos(
                Arqueiro,
                new SorteadorMock(),
                tamanho: 1,
                () => AgoraMs,
                pontoX: x,
                pontoZ: z,
                queda: new QuedaMock(),
                projeteis: () => Disparos
            );
        }

        public long AgoraMs { get; set; } = 10_000;
        public GrupoDeInimigos Grupo { get; }
        public DisparoMock Disparos { get; }
        public List<JogadorNoCampo> Jogadores { get; } = [];

        public InimigoNoMundo Unico => Assert.Single(Grupo.Vivos());

        public void Por(long id, double x, double z)
        {
            Jogadores.RemoveAll(j => j.PersonagemId == id);
            Jogadores.Add(new JogadorNoCampo(id, x, z));
        }

        public PassoDosInimigos Passo()
        {
            AgoraMs += PassoMs;
            return Grupo.Passo(Jogadores);
        }

        public List<PassoDosInimigos> Avancar(double segundos)
        {
            var passos = new List<PassoDosInimigos>();
            var quantidade = (int)Math.Round(segundos * 1000.0 / PassoMs);
            for (var i = 0; i < quantidade; i++)
            {
                passos.Add(Passo());
            }
            return passos;
        }
    }

    [Fact]
    public void Arqueiro_recua_quando_alvo_se_aproxima_a_menos_de_4_metros()
    {
        var cenario = new CenarioDistancia(x: 0, z: 0);
        // Inimigo nasce em (0, 4). Jogador a 2 metros em (0, 2), distância 2m (< 4m).
        cenario.Por(JogadorId, 0, 2);

        // Desperta do repouso para alerta e entra em perseguição
        cenario.Avancar(RegrasInimigo.AlertaSegundos + 0.2);

        var distInicial = RegrasInimigo.Distancia(cenario.Unico.X, cenario.Unico.Z, 0, 2);
        // Inimigo deve recuar para longe do jogador (aumentando a distância)
        cenario.Avancar(1.0);

        var distFinal = RegrasInimigo.Distancia(cenario.Unico.X, cenario.Unico.Z, 0, 2);
        Assert.True(
            distFinal > distInicial,
            $"Inimigo deveria ter recuado para longe do jogador. Distância inicial: {distInicial}, Distância final: {distFinal}"
        );
    }

    [Fact]
    public void Arqueiro_avanca_quando_alvo_esta_alem_da_faixa_tatica()
    {
        var cenario = new CenarioDistancia(x: 0, z: 0);
        // Inimigo nasce em (0, 4). Jogador em (0, 21): distância 17m (<= 18m de visão do Arqueiro, mas > 16m faixa tática).
        cenario.Por(JogadorId, 0, 21);

        var zInicial = cenario.Unico.Z;
        // Avança até acordar do alerta e perseguir/avançar
        cenario.Avancar(RegrasInimigo.AlertaSegundos + 0.5);

        Assert.True(
            cenario.Unico.Z > zInicial,
            $"Inimigo deveria ter avançado em direção ao jogador (+Z), Z inicial: {zInicial}, Z atual: {cenario.Unico.Z}"
        );
    }

    [Fact]
    public void Arqueiro_mantem_distancia_tatica_e_nao_entra_no_corpo_a_corpo()
    {
        var cenario = new CenarioDistancia(x: 0, z: 0);
        // Jogador na faixa tática ideal de 12m
        cenario.Por(JogadorId, 0, 12);

        cenario.Avancar(RegrasInimigo.AlertaSegundos + 0.2);

        // Deixa a IA agir por 2 segundos
        cenario.Avancar(2.0);

        var dist = RegrasInimigo.Distancia(cenario.Unico.X, cenario.Unico.Z, 0, 12);
        Assert.True(
            dist >= 8.0,
            $"Arqueiro deve manter distância tática e não correr colado no jogador (< 4m). Distância atual: {dist}"
        );
    }

    [Fact]
    public void Arqueiro_telegrafa_e_dispara_projetil_ao_concluir_preparo()
    {
        var cenario = new CenarioDistancia(x: 0, z: 0);
        cenario.Por(JogadorId, 0, 12);

        // Avança até acordar e iniciar ataque
        var passos = cenario.Avancar(RegrasInimigo.AlertaSegundos + 0.5);

        var preparo = passos.SelectMany(p => p.Preparos).FirstOrDefault();
        Assert.NotNull(preparo);
        Assert.Equal(TipoDeAtaqueDeInimigo.Disparo, preparo.Tipo);
        Assert.Equal(1000, preparo.PreparoMs);

        // Avança o tempo de preparo (1,0s) para o disparo acontecer
        cenario.Avancar(1.2);

        Assert.NotEmpty(cenario.Disparos.Disparos);
        var disparo = cenario.Disparos.Disparos[0];
        Assert.Equal(TipoDeProjetil.Flecha, disparo.Tipo);
        Assert.True(disparo.Dano > 0);
    }
}
