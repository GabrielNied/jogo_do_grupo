using JogoDoGrupo.Server.Features.Mobs;
using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Tests.Features.Mobs;

public class AparicaoDeChefeRaroTestes
{
    private static readonly DateTimeOffset Inicio = new(2026, 10, 11, 12, 0, 0, TimeSpan.Zero);
    private static readonly DefinicaoDeCriatura Raro = CatalogoCriaturas.Obter(
        CatalogoCriaturas.PresaVelha
    );
    private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(
        Raro.IntervaloDeAparicaoSegundos
    );

    /// <summary>Devolve os resultados na ordem dada e conta os sorteios.</summary>
    private sealed class SorteadorCombinado(params bool[] resultados) : ISorteadorDeCriaturas
    {
        private readonly Queue<bool> _resultados = new(resultados);

        public int Sorteios { get; private set; }

        public CriaturaSorteada Sortear(DefinicaoDeCriatura definicao) =>
            throw new NotSupportedException();

        public bool Aparece(DefinicaoDeCriatura definicao)
        {
            Sorteios++;
            return _resultados.Dequeue();
        }
    }

    [Fact]
    public void Primeiro_sorteio_e_no_inicio_e_o_chefe_pode_aparecer()
    {
        var sorteador = new SorteadorCombinado(true);
        var aparicao = new AparicaoDeChefeRaro(Raro, sorteador, Inicio);

        Assert.False(aparicao.Presente);
        Assert.True(aparicao.Atualizar(Inicio));
        Assert.True(aparicao.Presente);
        Assert.Same(Raro, aparicao.Definicao);
    }

    [Fact]
    public void Antes_do_inicio_nao_sorteia()
    {
        var sorteador = new SorteadorCombinado(true);
        var aparicao = new AparicaoDeChefeRaro(Raro, sorteador, Inicio);

        Assert.False(aparicao.Atualizar(Inicio.AddSeconds(-1)));
        Assert.Equal(0, sorteador.Sorteios);
    }

    [Fact]
    public void Se_nao_apareceu_so_tenta_de_novo_no_intervalo_seguinte()
    {
        var sorteador = new SorteadorCombinado(false, true);
        var aparicao = new AparicaoDeChefeRaro(Raro, sorteador, Inicio);

        Assert.False(aparicao.Atualizar(Inicio));
        Assert.False(aparicao.Atualizar(Inicio + Intervalo - TimeSpan.FromSeconds(1)));
        Assert.Equal(1, sorteador.Sorteios);
        Assert.False(aparicao.Presente);

        Assert.True(aparicao.Atualizar(Inicio + Intervalo));
        Assert.Equal(2, sorteador.Sorteios);
    }

    [Fact]
    public void Enquanto_esta_no_mundo_nao_sorteia_de_novo()
    {
        var sorteador = new SorteadorCombinado(true);
        var aparicao = new AparicaoDeChefeRaro(Raro, sorteador, Inicio);
        aparicao.Atualizar(Inicio);

        Assert.False(aparicao.Atualizar(Inicio + Intervalo * 10));
        Assert.True(aparicao.Presente);
        Assert.Equal(1, sorteador.Sorteios);
    }

    [Fact]
    public void Morte_recomeca_a_espera_a_partir_da_morte()
    {
        var sorteador = new SorteadorCombinado(true, true);
        var aparicao = new AparicaoDeChefeRaro(Raro, sorteador, Inicio);
        aparicao.Atualizar(Inicio);
        var morte = Inicio + Intervalo * 3;

        Assert.True(aparicao.RegistrarMorte(morte));
        Assert.False(aparicao.Presente);
        Assert.False(aparicao.Atualizar(morte + Intervalo - TimeSpan.FromSeconds(1)));
        Assert.Equal(1, sorteador.Sorteios);

        Assert.True(aparicao.Atualizar(morte + Intervalo));
    }

    [Fact]
    public void Morte_repetida_ou_sem_o_chefe_no_mundo_nao_muda_nada()
    {
        var sorteador = new SorteadorCombinado(false, true);
        var aparicao = new AparicaoDeChefeRaro(Raro, sorteador, Inicio);
        aparicao.Atualizar(Inicio);

        // Ausente: registrar morte não pode empurrar o próximo sorteio.
        Assert.False(aparicao.RegistrarMorte(Inicio + Intervalo - TimeSpan.FromSeconds(1)));
        Assert.True(aparicao.Atualizar(Inicio + Intervalo));

        Assert.True(aparicao.RegistrarMorte(Inicio + Intervalo));
        Assert.False(aparicao.RegistrarMorte(Inicio + Intervalo));
    }

    [Fact]
    public void Intervalos_perdidos_nao_viram_sorteios_acumulados()
    {
        var sorteador = new SorteadorCombinado(false, false);
        var aparicao = new AparicaoDeChefeRaro(Raro, sorteador, Inicio);

        Assert.False(aparicao.Atualizar(Inicio + Intervalo * 50));
        Assert.Equal(1, sorteador.Sorteios);
        Assert.False(aparicao.Atualizar(Inicio + Intervalo * 50 + TimeSpan.FromSeconds(1)));
        Assert.Equal(1, sorteador.Sorteios);
    }

    [Fact]
    public async Task Atualizacoes_ao_mesmo_tempo_fazem_um_sorteio_so()
    {
        var sorteador = new SorteadorCombinado(true);
        var aparicao = new AparicaoDeChefeRaro(Raro, sorteador, Inicio);

        var resultados = await Task.WhenAll(
            Enumerable.Range(0, 16).Select(_ => Task.Run(() => aparicao.Atualizar(Inicio)))
        );

        Assert.Single(resultados, apareceu => apareceu);
        Assert.Equal(1, sorteador.Sorteios);
    }

    [Theory]
    [InlineData(CatalogoCriaturas.JavaliDoCampo)]
    [InlineData(CatalogoCriaturas.GuardiaoDoReflexo)]
    public void Recusa_criatura_que_nao_e_chefe_raro(int criaturaId)
    {
        var definicao = CatalogoCriaturas.Obter(criaturaId);

        Assert.Throws<ArgumentException>(() =>
            new AparicaoDeChefeRaro(definicao, new SorteadorCombinado(), Inicio)
        );
    }

    [Fact]
    public void Recusa_chefe_raro_sem_intervalo()
    {
        var semIntervalo = new DefinicaoDeCriatura(
            id: 299,
            nome: "Sem intervalo",
            categoria: CategoriaDeCriatura.ChefeRaro,
            nivel: 1,
            vidaBase: 1,
            danoBase: 1,
            experienciaBase: 1,
            chanceDeAparecer: 0.5,
            intervaloDeAparicaoSegundos: 0
        );

        Assert.Throws<ArgumentException>(() =>
            new AparicaoDeChefeRaro(semIntervalo, new SorteadorCombinado(), Inicio)
        );
    }
}
