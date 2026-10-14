using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Features.Mobs;
using JogoDoGrupo.Shared.Features.Mobs;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Features.Mobs;

public class AmeacaDeInimigosTestes
{
    private static readonly DefinicaoDeCriatura Javali = CatalogoCriaturas.Obter(
        CatalogoCriaturas.JavaliDoCampo
    );

    private sealed class QuedaFalsa : IEstadoDeQueda
    {
        public HashSet<long> Caidos { get; } = [];

        public bool EstaCaido(long personagemId) => Caidos.Contains(personagemId);
    }

    private sealed class SorteadorFixo : ISorteadorDeCriaturas
    {
        public CriaturaSorteada Sortear(DefinicaoDeCriatura definicao) =>
            new(
                definicao,
                VarianteDeCriatura.Normal,
                definicao.VidaBase,
                definicao.DanoBase,
                definicao.ExperienciaBase,
                1.0,
                1.0f
            );

        public bool Aparece(DefinicaoDeCriatura definicao) => true;

        public double Proximo() => 0.0;
    }

    private sealed class CampoDeTeste
    {
        public long AgoraMs { get; set; } = 10_000;
        public QuedaFalsa Queda { get; } = new();
        public GrupoDeInimigos Grupo { get; }
        public List<JogadorNoCampo> Jogadores { get; } = [];

        public CampoDeTeste(int vida = 1000)
        {
            var def = new DefinicaoDeCriatura(
                Javali.Id,
                Javali.Nome,
                Javali.Categoria,
                nivel: 1,
                vidaBase: vida,
                danoBase: 10,
                experienciaBase: 10,
                chanceDeAparecer: 1.0,
                intervaloDeAparicaoSegundos: 5
            );

            Grupo = new GrupoDeInimigos(
                def,
                new SorteadorFixo(),
                tamanho: 1,
                () => AgoraMs,
                pontoX: 0.0,
                pontoZ: 0.0,
                primeiroId: 1001,
                queda: Queda
            );
        }

        public void Por(long id, double x, double z)
        {
            Jogadores.RemoveAll(j => j.PersonagemId == id);
            Jogadores.Add(new JogadorNoCampo(id, x, z));
        }

        public PassoDosInimigos Passo(long avancoMs = 100)
        {
            AgoraMs += avancoMs;
            return Grupo.Passo(Jogadores);
        }
    }

    [Fact]
    public void Dano_gera_ameaca_e_dano_alem_da_vida_restante_nao_conta()
    {
        var campo = new CampoDeTeste(vida: 80);
        long id = 1001;

        // Aplica 200 de dano, mas vida é só 80
        var acertou = campo.Grupo.TentarAplicarDano(id, 200, atacanteId: 42, out var resultado);

        Assert.True(acertou);
        Assert.Equal(80, resultado.Dano);
        Assert.Equal(0, resultado.VidaRestante);
        // Ao morrer a tabela é zerada
        Assert.Equal(0.0, campo.Grupo.ObterAmeaca(id, 42));
    }

    [Fact]
    public void Dano_parcial_gera_ameaca_correta_na_tabela()
    {
        var campo = new CampoDeTeste(vida: 200);
        long id = 1001;

        campo.Grupo.TentarAplicarDano(id, 75, atacanteId: 42, out _);

        Assert.Equal(75.0, campo.Grupo.ObterAmeaca(id, 42));
    }

    [Fact]
    public void Quem_bate_mais_troca_o_alvo_so_ao_superar_a_margem_de_110_por_cento_perto()
    {
        var campo = new CampoDeTeste(vida: 1000);
        long id = 1001;

        // Jogador 1 dá 100 de dano
        campo.Grupo.TentarAplicarDano(id, 100, atacanteId: 1, out _);

        // Inimigo nasce em (0.0, 4.0).
        // Alvo 1 a 2.5 m (em perseguição, sem travar em ataque)
        // Desafiante 2 a 1.8 m (<= 2.0 m: dentro do alcance de ataque = perto, 110%)
        campo.Por(1, 0.0, 1.5);
        campo.Por(2, 0.0, 2.2);

        // Passo inicial: acorda e foca no 1 (único com ameaça)
        campo.Passo(100);
        campo.Passo(1000); // passa tempo do alerta

        // Jogador 2 causa 105 de dano (não supera 100 * 1.10 = 110)
        campo.Grupo.TentarAplicarDano(id, 105, atacanteId: 2, out _);
        var passo = campo.Passo(100);

        var mudanca = passo.Mudancas.Find(m => m.Mensagem.InimigoId == id);
        if (mudanca.Mensagem != null && mudanca.Mensagem.AlvoId != RegrasInimigo.SemAlvo)
        {
            Assert.Equal(1, mudanca.Mensagem.AlvoId);
        }

        // Jogador 2 causa mais 10 de dano (total 115 > 110)
        campo.Grupo.TentarAplicarDano(id, 10, atacanteId: 2, out _);
        passo = campo.Passo(100);

        mudanca = passo.Mudancas.Find(m => m.Mensagem.InimigoId == id);
        Assert.Equal(2, mudanca.Mensagem.AlvoId);
    }

    [Fact]
    public void Alvo_longe_exige_130_por_cento_para_trocar()
    {
        var campo = new CampoDeTeste(vida: 1000);
        long id = 1001;

        campo.Grupo.TentarAplicarDano(id, 100, atacanteId: 1, out _);

        campo.Por(1, 1.0, 0.0); // perto
        campo.Por(2, 5.0, 0.0); // longe

        campo.Passo(100);
        campo.Passo(1000);

        // Jogador 2 (longe) causa 125 de dano (não supera 100 * 1.30 = 130)
        campo.Grupo.TentarAplicarDano(id, 125, atacanteId: 2, out _);
        var passo = campo.Passo(100);
        var mudanca = passo.Mudancas.Find(m => m.Mensagem.InimigoId == id);
        if (mudanca.Mensagem != null && mudanca.Mensagem.AlvoId != RegrasInimigo.SemAlvo)
        {
            Assert.Equal(1, mudanca.Mensagem.AlvoId);
        }

        // Jogador 2 causa mais 10 de dano (total 135 > 130)
        campo.Grupo.TentarAplicarDano(id, 10, atacanteId: 2, out _);
        passo = campo.Passo(100);
        mudanca = passo.Mudancas.Find(m => m.Mensagem.InimigoId == id);
        Assert.Equal(2, mudanca.Mensagem.AlvoId);
    }

    [Fact]
    public void Provocacao_vira_o_alvo_na_hora_e_segura_por_4_segundos()
    {
        var campo = new CampoDeTeste(vida: 2000);
        long id = 1001;

        campo.Grupo.TentarAplicarDano(id, 500, atacanteId: 1, out _);
        campo.Grupo.TentarAplicarDano(id, 50, atacanteId: 2, out _);

        campo.Por(1, 1.0, 0.0);
        campo.Por(2, 1.5, 0.0);
        campo.Passo(100);
        campo.Passo(1000);

        // Tank (2) provoca o inimigo
        var provocou = campo.Grupo.Provocar(id, 2);
        Assert.True(provocou);
        Assert.Equal(500.0, campo.Grupo.ObterAmeaca(id, 2));

        // Durante a provocação (+2000 ms), DPS (1) causa mais dano
        campo.Grupo.TentarAplicarDano(id, 200, atacanteId: 1, out _);
        var passo = campo.Passo(2000);

        var mudanca = passo.Mudancas.Find(m => m.Mensagem.InimigoId == id);
        if (mudanca.Mensagem != null && mudanca.Mensagem.AlvoId != RegrasInimigo.SemAlvo)
        {
            Assert.Equal(2, mudanca.Mensagem.AlvoId);
        }

        // Após expirar a provocação (+2500 ms, total 4500 ms > 4000 ms)
        passo = campo.Passo(2500);
        mudanca = passo.Mudancas.Find(m => m.Mensagem.InimigoId == id);
        // DPS tem 700 de dano vs 500 do Tank (700 > 500 * 1.10 = 550) -> retoma o alvo
        Assert.Equal(1, mudanca.Mensagem.AlvoId);
    }

    [Fact]
    public void Jogador_caido_e_removido_da_tabela_de_ameaca()
    {
        var campo = new CampoDeTeste(vida: 1000);
        long id = 1001;

        campo.Grupo.TentarAplicarDano(id, 100, atacanteId: 1, out _);
        campo.Grupo.TentarAplicarDano(id, 80, atacanteId: 2, out _);

        Assert.Equal(100.0, campo.Grupo.ObterAmeaca(id, 1));

        // Jogador 1 cai
        campo.Queda.Caidos.Add(1);

        campo.Por(1, 1.0, 0.0);
        campo.Por(2, 1.5, 0.0);
        campo.Passo(100);

        // 1 foi removido da tabela
        Assert.Equal(0.0, campo.Grupo.ObterAmeaca(id, 1));
        Assert.Equal(80.0, campo.Grupo.ObterAmeaca(id, 2));
    }

    [Fact]
    public void ChefeDaDungeon_respeita_ameaca_e_provocacao()
    {
        long relogio = 1000;
        var chefe = new ChefeDaDungeon(
            area: "dungeon-teste",
            origemX: 0.0,
            origemZ: 0.0,
            proximoId: () => 9001,
            relogioMs: () => relogio
        );

        chefe.TentarAplicarDano(9001, 150, atacanteId: 10, out _);
        chefe.TentarAplicarDano(9001, 50, atacanteId: 20, out _);

        Assert.Equal(150.0, chefe.ObterAmeaca(10));
        Assert.Equal(50.0, chefe.ObterAmeaca(20));

        var jogadores = new[]
        {
            new JogadorNoCampo(10, 2.0, 0.0),
            new JogadorNoCampo(20, 2.5, 0.0),
        };

        chefe.Passo(jogadores);
        relogio += 500;
        chefe.Passo(jogadores);

        // Provocação pelo 20
        var provocou = chefe.Provocar(9001, 20);
        Assert.True(provocou);
        Assert.Equal(150.0, chefe.ObterAmeaca(20));
    }
}
