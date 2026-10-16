using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Personagem;
using JogoDoGrupo.Shared.Features.Talentos;
using TipoDeEfeito = JogoDoGrupo.Shared.Features.Talentos.TipoDeEfeito;

namespace JogoDoGrupo.Shared.Tests.Features.Talentos;

/// <summary>
/// As metas dos números da #627 (docs/features/talentos.md): se uma constante sair da faixa que a
/// simulação aprovou, estes testes avisam. A simulação completa fica em
/// <c>tools/JogoDoGrupo.Simulacoes</c>.
/// </summary>
public class NumerosDaArvoreTestes
{
    private static readonly ConfiguracaoDaArvore Arvore = ArvoreDoJogo.Criar();

    private static int PontosNoNivel(int nivel) => ConfigExperiencia.PontosPorNivel * (nivel - 1);

    // Compra o caminho inteiro na ordem: principais, grande, secundários, marco.
    private static Dictionary<string, int> ComprarCaminho(AtributoPrincipal caminho, int pontos)
    {
        var ranks = new Dictionary<string, int>();
        var alvos = new List<(string Id, int Rank)>();
        for (var i = 0; i < 3; i++)
        {
            alvos.Add((ArvoreDoJogo.IdPrincipal(caminho, i), ArvoreDoJogo.RankMaximoPrincipal));
        }

        alvos.Add((ArvoreDoJogo.IdGrande(caminho), ArvoreDoJogo.RankMaximoGrande));
        alvos.Add((ArvoreDoJogo.IdSecundario(caminho, 0), ArvoreDoJogo.RankMaximoSecundario));
        alvos.Add((ArvoreDoJogo.IdSecundario(caminho, 1), ArvoreDoJogo.RankMaximoSecundario));
        alvos.Add((ArvoreDoJogo.IdMarco(caminho), 1));
        foreach (var (id, alvo) in alvos)
        {
            for (var rank = 1; rank <= alvo; rank++)
            {
                var compra = RegrasDaArvore.AvaliarCompra(Arvore, ranks, pontos, id, rank);
                if (compra.Erro != null)
                {
                    return ranks;
                }

                pontos -= compra.Custo;
                ranks[id] = rank;
            }
        }

        return ranks;
    }

    [Fact]
    public void Um_ponto_por_nivel_da_99_no_nivel_100()
    {
        Assert.Equal(1, ConfigExperiencia.PontosPorNivel);
        Assert.Equal(99, PontosNoNivel(ConfigExperiencia.NivelMaximo));
    }

    [Fact]
    public void Um_caminho_completo_cabe_nos_pontos_do_nivel_100_com_folga_para_um_hibrido()
    {
        var ranks = ComprarCaminho(AtributoPrincipal.Forca, PontosNoNivel(100));

        var gasto = RegrasDaArvore.CalcularPontosGastos(Arvore, ranks);
        Assert.Equal(64, gasto);
        Assert.True(PontosNoNivel(100) - gasto >= 2 * ArvoreDoJogo.CustoHibrido[0]);
    }

    [Fact]
    public void Build_focada_em_forca_dobra_a_vida_do_vigor_e_nao_passa_de_65_por_cento_de_dano()
    {
        var forca = RegrasDaArvore.SomarAtributos(
            Arvore,
            ComprarCaminho(AtributoPrincipal.Forca, PontosNoNivel(100))
        );
        var vigor = RegrasDaArvore.SomarAtributos(
            Arvore,
            ComprarCaminho(AtributoPrincipal.Vigor, PontosNoNivel(100))
        );

        // 3 principais (30) + grande (6) + marco (4) no próprio caminho.
        Assert.Equal(40, forca.Forca);
        var bonusDeDano = RegrasAtaque.PercentualDeDanoPorPonto * forca.Forca;
        Assert.InRange(bonusDeDano, 0.5, 0.65);

        // O Vigor focado fica entre +80% e +100% da vida-base: pesa, sem tornar a vida infinita.
        var ganhoDeVida = RegrasRecursos.VidaMaxima(vigor.Vigor) - RegrasRecursos.VidaBase;
        Assert.InRange(ganhoDeVida / (double)RegrasRecursos.VidaBase, 0.8, 1.0);
    }

    [Fact]
    public void Os_secundarios_da_arvore_ficam_abaixo_dos_tetos()
    {
        double Soma(string chave, bool multiplica = false)
        {
            double total = 0;
            foreach (var no in Arvore.Nos)
            {
                foreach (var efeito in no.Efeitos)
                {
                    if (
                        efeito.Tipo == TipoDeEfeito.AtributoSecundario
                        && efeito.Chave == chave
                        && efeito.Multiplica == multiplica
                    )
                    {
                        total += efeito.ValorPorRank * no.RankMaximo;
                    }
                }
            }

            return total;
        }

        // Árvore inteira (mais do que qualquer build compra): ainda longe dos tetos de Combate.
        Assert.True(Soma("ChanceCritica") < ConfigAtributosSecundarios.TetoChanceCritica / 4);
        Assert.True(Soma("PenetracaoFisica") < ConfigAtributosSecundarios.TetoPenetracao / 3);
        Assert.True(Soma("PenetracaoMagica") < ConfigAtributosSecundarios.TetoPenetracao / 3);
        Assert.True(
            ConfigAtributosSecundarios.BaseVelocidadeAtaque + Soma("VelocidadeAtaqueFisico")
                < ConfigAtributosSecundarios.VelocidadeMaxima / 2
        );
    }
}
