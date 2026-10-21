using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Features.Mobs;
using JogoDoGrupo.Shared.Features.Mobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Mobs;

public class ChefeDoCapituloTestes
{
    private sealed class AmbienteDoChefe
    {
        private long _ultimoId = ConfigChefeDoCapitulo.PrimeiroId;

        public AmbienteDoChefe()
        {
            var def = CatalogoCriaturas.Obter(ConfigChefeDoCapitulo.CriaturaId)!;
            var multDano = (double)def.DanoBase / 120.0;
            Boss = new ChefeDaDungeon(
                ConfigChefeDoCapitulo.Area,
                ConfigChefeDoCapitulo.PontoX,
                ConfigChefeDoCapitulo.PontoZ,
                () => ++_ultimoId,
                () => AgoraMs,
                multiplicadorVida: 1.0,
                multiplicadorDano: multDano,
                criaturaId: ConfigChefeDoCapitulo.CriaturaId,
                intervaloRespawnSegundos: ConfigChefeDoCapitulo.IntervaloRespawnSegundos
            );
        }

        public long AgoraMs { get; set; } = 1_000_000;

        public ChefeDaDungeon Boss { get; }

        public PassoDosInimigos Passo(long avancoMs, params JogadorNoCampo[] jogadores)
        {
            AgoraMs += avancoMs;
            return Boss.Passo(jogadores);
        }

        public bool Bater(int dano, long atacanteId, out ResultadoDeDano resultado) =>
            Boss.TentarAplicarDano(Boss.InimigoId, dano, atacanteId, out resultado);
    }

    [Fact]
    public void ChefeDoCapitulo_posicao_e_atributos_iniciais()
    {
        var ambiente = new AmbienteDoChefe();
        var chefe = ambiente.Boss;

        Assert.Equal(AreasDoMundo.Cidade, chefe.Area);
        Assert.False(chefe.Morto);

        var vivos = chefe.Vivos();
        Assert.Single(vivos);
        var retrato = vivos[0];

        Assert.Equal(CatalogoCriaturas.PresaVelha, retrato.CriaturaId);
        var def = CatalogoCriaturas.Obter(CatalogoCriaturas.PresaVelha)!;
        Assert.Equal(def.VidaBase, retrato.VidaMaxima);
        Assert.Equal(def.VidaBase, retrato.Vida);
        Assert.Equal((float)ConfigChefeDoCapitulo.PontoX, retrato.X);
        Assert.Equal((float)ConfigChefeDoCapitulo.PontoZ, retrato.Z);
        Assert.Equal(EstadoDeInimigo.Repouso, retrato.Estado);

        var alvos = chefe.AlvosProximos(
            ConfigChefeDoCapitulo.Area,
            ConfigChefeDoCapitulo.PontoX,
            ConfigChefeDoCapitulo.PontoZ,
            alcanceMetros: 5.0
        );
        Assert.Single(alvos);
        Assert.Equal(chefe.InimigoId, alvos[0].Id);
    }

    [Fact]
    public void ChefeDoCapitulo_credito_vai_para_todos_os_participantes()
    {
        var ambiente = new AmbienteDoChefe();
        var chefe = ambiente.Boss;
        var def = CatalogoCriaturas.Obter(ConfigChefeDoCapitulo.CriaturaId)!;

        // Jogador 1 ataca e causa dano
        var bateu1 = ambiente.Bater(100, atacanteId: 10, out _);
        Assert.True(bateu1);

        // Jogador 2 cura o Jogador 1 durante o combate
        chefe.RegistrarCura(curadorId: 20, curadoId: 10);

        // Jogador 3 desfere o golpe final
        var danoRestante = def.VidaBase - 100;
        var bateu3 = ambiente.Bater(danoRestante, atacanteId: 30, out _);
        Assert.True(bateu3);
        Assert.True(chefe.Morto);

        var passo = ambiente.Passo(100);
        Assert.Single(passo.Derrotas);
        var derrota = passo.Derrotas[0];

        Assert.Equal(CatalogoCriaturas.PresaVelha, derrota.CriaturaId);
        // O crédito vai para todos que participaram (10, 20 e 30), e não só para quem matou (30)
        Assert.Equal([10, 20, 30], derrota.Participantes);
    }

    [Fact]
    public void ChefeDoCapitulo_respawn_automatico_no_tempo_da_config()
    {
        var ambiente = new AmbienteDoChefe();
        var chefe = ambiente.Boss;
        var def = CatalogoCriaturas.Obter(ConfigChefeDoCapitulo.CriaturaId)!;
        var idOriginal = chefe.InimigoId;

        // Mata o chefe
        ambiente.Bater(def.VidaBase, atacanteId: 1, out _);
        Assert.True(chefe.Morto);
        var passoMorte = ambiente.Passo(100);
        Assert.Single(passoMorte.Mortes);

        // 59 segundos depois: ainda morto, sem aparição
        var passoAntes = ambiente.Passo(
            (long)(ConfigChefeDoCapitulo.IntervaloRespawnSegundos * 1000) - 200
        );
        Assert.True(chefe.Morto);
        Assert.Empty(passoAntes.Aparicoes);

        // 60 segundos após a morte: revive automaticamente
        var passoRespawn = ambiente.Passo(200);
        Assert.False(chefe.Morto);
        Assert.Single(passoRespawn.Aparicoes);

        var novaAparicao = passoRespawn.Aparicoes[0].Inimigo;
        Assert.NotEqual(idOriginal, novaAparicao.InimigoId);
        Assert.Equal(CatalogoCriaturas.PresaVelha, novaAparicao.CriaturaId);
        Assert.Equal(def.VidaBase, novaAparicao.Vida);
        Assert.Equal(EstadoDeInimigo.Repouso, novaAparicao.Estado);
    }

    [Fact]
    public void ChefeDoCapitulo_registrado_no_conteiner_di()
    {
        var servicos = new ServiceCollection();
        servicos.AddLogging();
        servicos.AddSingleton<IEstadoDeQueda, EstadoDeQuedaFake>();
        servicos.AddSingleton<IPosicoesDoMundo, PosicoesDoMundoFake>();
        servicos.AddSingleton<IHistoricoDePosicoesDoMundo, HistoricoFake>();

        var modulo = new MobsModule();
        var config = new ConfigurationBuilder().Build();
        modulo.ConfigurarServicos(servicos, config);

        using var provedor = servicos.BuildServiceProvider();

        var chefeDoCapitulo = provedor.GetKeyedService<ChefeDaDungeon>(
            MobsModule.ChaveDoChefeDoCapitulo
        );
        Assert.NotNull(chefeDoCapitulo);
        Assert.Equal(ConfigChefeDoCapitulo.Area, chefeDoCapitulo.Area);

        var alvos = provedor.GetServices<IAlvosDeCombate>().ToArray();
        Assert.Contains(chefeDoCapitulo, alvos);
    }

    private sealed class EstadoDeQuedaFake : IEstadoDeQueda
    {
        public bool EstaCaido(long personagemId) => false;

        public void Derrubar(long personagemId) { }

        public void Levantar(long personagemId) { }
    }

    private sealed class PosicoesDoMundoFake : IPosicoesDoMundo
    {
        public bool TentarObter(long personagemId, out PosicaoNoMundo posicao)
        {
            posicao = default;
            return false;
        }

        public IReadOnlyList<long> PersonagensNaArea(string area) => [];

        public IReadOnlyList<PosicaoNoMundo> ObterTodas() => [];

        public void Atualizar(long personagemId, PosicaoNoMundo posicao) { }

        public void Remover(long personagemId) { }
    }

    private sealed class HistoricoFake : IHistoricoDePosicoesDoMundo
    {
        public void Registrar(long personagemId, PosicaoNoMundo posicao, long instanteMs) { }

        public bool TentarObterEm(long personagemId, long instanteMs, out PosicaoNoMundo posicao)
        {
            posicao = default;
            return false;
        }

        public int QuantidadeDeAmostras(long entidadeId) => 0;
    }
}
