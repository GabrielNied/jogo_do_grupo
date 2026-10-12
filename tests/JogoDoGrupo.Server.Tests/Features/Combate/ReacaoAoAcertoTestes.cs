using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Features.Combate;
using JogoDoGrupo.Server.Features.Mobs;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Tests.Features.Combate;

/// <summary>
/// A reação ao acerto (#577) e a guarda segurada (#579) nas regras do servidor, com relógio em
/// milissegundos controlado pelo teste. A integração com a rede está em <c>ReacaoEGuardaRedeTestes</c>.
/// </summary>
public class ReacaoAoAcertoTestes
{
    private const long Ana = 1;

    private static PedidoDeAtaque Leve() => new(TipoDeAtaque.Leve, 0f, 1);

    private static PedidoDeAtaque Pesado() => new(TipoDeAtaque.Pesado, 0f, 1);

    [Fact]
    public void Acerto_no_preparo_do_leve_cancela_o_contato()
    {
        var controle = new ControleDeAcoes();
        controle.Pedir(Ana, Leve(), 1_000, out var acao);

        Assert.True(controle.TentarReagir(Ana, 1_200)); // Antes do contato (1 500).

        Assert.True(controle.FoiInterrompida(Ana, acao!.Id));
    }

    [Fact]
    public void Acerto_na_recuperacao_do_leve_corta_a_acao_mas_o_contato_ja_aconteceu()
    {
        var controle = new ControleDeAcoes();
        controle.Pedir(Ana, Leve(), 1_000, out var acao);

        Assert.True(controle.TentarReagir(Ana, 1_600)); // Depois do contato.

        Assert.False(controle.FoiInterrompida(Ana, acao!.Id));
    }

    [Fact]
    public void Acerto_no_pesado_em_andamento_nao_interrompe_nem_atordoa()
    {
        var controle = new ControleDeAcoes();
        controle.Pedir(Ana, Pesado(), 1_000, out var acao);

        Assert.False(controle.TentarReagir(Ana, 1_400));
        Assert.False(controle.TentarReagir(Ana, 2_200)); // Na recuperação do pesado também.

        Assert.False(controle.FoiInterrompida(Ana, acao!.Id));
        Assert.False(controle.EstaGuardando(Ana));
        Assert.Equal(ResultadoDoPedido.Recusado, controle.Pedir(Ana, Leve(), 2_000, out _));
    }

    [Fact]
    public void Depois_do_acerto_ataque_e_esquiva_sao_recusados_por_300_ms()
    {
        var controle = new ControleDeAcoes();
        Assert.True(controle.TentarReagir(Ana, 5_000));

        Assert.Equal(ResultadoDoPedido.Recusado, controle.Pedir(Ana, Leve(), 5_000, out _));
        Assert.Equal(ResultadoDoPedido.Recusado, controle.Pedir(Ana, Leve(), 5_299, out _));
        Assert.False(controle.TentarOcupar(Ana, 5_299, RegrasEsquiva.DuracaoMs, out _));

        Assert.Equal(ResultadoDoPedido.Iniciado, controle.Pedir(Ana, Leve(), 5_300, out _));
    }

    [Fact]
    public void Esquiva_e_aceita_depois_dos_300_ms()
    {
        var controle = new ControleDeAcoes();
        controle.TentarReagir(Ana, 5_000);

        Assert.True(controle.TentarOcupar(Ana, 5_300, RegrasEsquiva.DuracaoMs, out _));
    }

    [Fact]
    public void A_reacao_nao_renova_e_a_folga_de_600_ms_depois_dela_tambem_nao_interrompe()
    {
        var controle = new ControleDeAcoes();
        Assert.True(controle.TentarReagir(Ana, 5_000));

        Assert.False(controle.TentarReagir(Ana, 5_200)); // Durante a reação.
        Assert.False(controle.TentarReagir(Ana, 5_899)); // Durante a folga.

        // O segundo acerto não somou tempo: aos 300 ms o personagem já age.
        Assert.Equal(ResultadoDoPedido.Iniciado, controle.Pedir(Ana, Leve(), 5_300, out _));

        // Fim da folga: um acerto volta a interromper.
        Assert.True(controle.TentarReagir(Ana, 5_900));
    }

    [Fact]
    public void Acerto_na_folga_nao_corta_o_golpe_em_curso()
    {
        var controle = new ControleDeAcoes();
        controle.TentarReagir(Ana, 5_000);
        controle.Pedir(Ana, Leve(), 5_300, out var acao);

        Assert.False(controle.TentarReagir(Ana, 5_500)); // No preparo, mas na folga.

        Assert.False(controle.FoiInterrompida(Ana, acao!.Id));
    }

    [Fact]
    public void Acerto_tira_o_pedido_da_fila()
    {
        var controle = new ControleDeAcoes();
        controle.Pedir(Ana, Leve(), 1_000, out var acao);
        Assert.Equal(ResultadoDoPedido.Enfileirado, controle.Pedir(Ana, Leve(), 1_600, out _));

        controle.TentarReagir(Ana, 1_650);

        Assert.Null(controle.AvancarFila(Ana, acao!.Id, 1_800));
    }

    [Fact]
    public void Esquiva_em_curso_nao_e_interrompida_por_acerto()
    {
        var controle = new ControleDeAcoes();
        controle.TentarOcupar(Ana, 1_000, RegrasEsquiva.DuracaoMs, out _);

        Assert.False(controle.TentarReagir(Ana, 1_400));
    }

    // ── Inimigo comum e chefe ────────────────────────────────────────────────────

    private static readonly DefinicaoDeCriatura Javali = CatalogoCriaturas.Obter(
        CatalogoCriaturas.JavaliDoCampo
    );

    private sealed class SorteadorFixo : ISorteadorDeCriaturas
    {
        public CriaturaSorteada Sortear(DefinicaoDeCriatura definicao) =>
            new(
                definicao,
                VarianteDeCriatura.Normal,
                RegrasCriaturas.VidaMaxima(definicao.VidaBase, VarianteDeCriatura.Normal),
                RegrasCriaturas.Dano(definicao.DanoBase, VarianteDeCriatura.Normal),
                RegrasCriaturas.Experiencia(definicao.ExperienciaBase, VarianteDeCriatura.Normal),
                RegrasCriaturas.MultiplicadorDeRecompensa(VarianteDeCriatura.Normal),
                RegrasCriaturas.Escala(VarianteDeCriatura.Normal)
            );

        public bool Aparece(DefinicaoDeCriatura definicao) => true;

        public double Proximo() => 0.0;
    }

    private static (
        GrupoDeInimigos Grupo,
        Func<PassoDosInimigos> Passo,
        long Id
    ) JavaliAteOPreparo()
    {
        long agora = 10_000;
        var grupo = new GrupoDeInimigos(
            Javali,
            new SorteadorFixo(),
            1,
            () => agora,
            pontoX: 0.0,
            pontoZ: 0.0
        );
        var jogadores = new List<JogadorNoCampo> { new(9, 0, 14) };
        PassoDosInimigos Passo()
        {
            agora += 100;
            return grupo.Passo(jogadores);
        }

        for (var i = 0; i < 80; i++)
        {
            if (Passo().Preparos.Count > 0)
            {
                return (grupo, Passo, grupo.Vivos().Single().InimigoId);
            }
        }

        throw new Xunit.Sdk.XunitException("O javali nunca preparou o golpe.");
    }

    [Fact]
    public void Acerto_no_preparo_do_javali_cancela_o_contato_e_ele_para_300_ms()
    {
        var (grupo, passo, id) = JavaliAteOPreparo();
        IAlvosDeCombate alvo = grupo;

        Assert.True(alvo.Interromper(id, RegrasReacao.AtordoamentoNoAcertoMs));

        // Nos 300 ms seguintes ele não prepara outro golpe; e dentro do tempo do preparo
        // cancelado nenhum golpe chega ao contato.
        var passos = (int)(RegrasInimigo.PreparoDoAtaqueMs / 100);
        for (var i = 0; i < passos; i++)
        {
            var p = passo();
            Assert.Empty(p.Golpes);
            if (i < RegrasReacao.AtordoamentoNoAcertoMs / 100 - 1)
            {
                Assert.Empty(p.Preparos);
            }
        }
    }

    [Fact]
    public void Acerto_na_recuperacao_do_javali_nao_interrompe()
    {
        var (grupo, passo, id) = JavaliAteOPreparo();

        // Deixa o contato acontecer: entra a recuperação.
        PassoDosInimigos p;
        do
        {
            p = passo();
        } while (p.Golpes.Count == 0);

        Assert.False(((IAlvosDeCombate)grupo).Interromper(id, RegrasReacao.AtordoamentoNoAcertoMs));
    }

    [Fact]
    public void Alvo_sem_preparo_a_interromper_usa_o_padrao_e_nao_e_interrompido()
    {
        // O chefe de dungeon e o boneco não implementam Interromper: o padrão do contrato vale.
        IAlvosDeCombate boneco = new BonecoDeTreino();

        Assert.False(boneco.Interromper(ConfigBonecoDeTreino.AlvoId, 300));
        Assert.Null(typeof(ChefeDaDungeon).GetMethod("Interromper"));
    }

    // ── Guarda ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Guarda_custa_1_de_vigor_por_ponto_de_dano_e_zera_o_golpe()
    {
        Assert.Equal(1, RegrasGuarda.VigorPorDanoBloqueado);
        Assert.Equal(30, RegrasGuarda.CustoDoBloqueio(30));
        Assert.True(RegrasGuarda.Segura(30, 30));
        Assert.False(RegrasGuarda.Segura(29, 30));
    }

    [Fact]
    public void Guarda_e_recusada_no_preparo_e_no_momento_de_acerto_e_vale_na_recuperacao()
    {
        var controle = new ControleDeAcoes();
        controle.Pedir(Ana, Leve(), 1_000, out var acao);

        Assert.False(controle.TentarGuardar(Ana, 1_300)); // Preparo.
        Assert.True(controle.TentarGuardar(Ana, 1_600)); // Recuperação: corta o que falta.

        Assert.True(controle.EstaGuardando(Ana));
        Assert.True(controle.AcaoAtual(Ana)!.FimMs <= 1_600);
        Assert.Equal(acao!.Id, controle.AcaoAtual(Ana)!.Id);
    }

    [Fact]
    public void Guarda_e_recusada_na_reacao_e_na_esquiva_e_segurando_ataque_e_esquiva_nao_valem()
    {
        var controle = new ControleDeAcoes();
        controle.TentarReagir(Ana, 1_000);
        Assert.False(controle.TentarGuardar(Ana, 1_100));
        Assert.True(controle.TentarGuardar(Ana, 1_300));

        Assert.Equal(ResultadoDoPedido.Recusado, controle.Pedir(Ana, Leve(), 1_400, out _));
        Assert.False(controle.TentarOcupar(Ana, 1_400, RegrasEsquiva.DuracaoMs, out _));

        Assert.True(controle.SoltarGuarda(Ana));
        Assert.Equal(ResultadoDoPedido.Iniciado, controle.Pedir(Ana, Leve(), 1_400, out _));
    }

    [Fact]
    public void Reacao_derruba_a_guarda()
    {
        var controle = new ControleDeAcoes();
        controle.TentarGuardar(Ana, 1_000);

        Assert.True(controle.TentarReagir(Ana, 1_100));

        Assert.False(controle.EstaGuardando(Ana));
    }
}
