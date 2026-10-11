using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Features.Mobs;
using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Tests.Features.Mobs;

/// <summary>
/// A IA do grupo de inimigos comuns, sem rede: relógio controlado e jogadores postos à mão.
/// Com um inimigo só, o ponto de origem dele é (0, 4), olhando para o sul.
/// </summary>
public class GrupoDeInimigosTestes
{
    private const long Jogador = 9;
    private const long PrimeiroId = ConfigInimigosDoCampo.PrimeiroInimigoId;
    private const string Area = ConfigInimigosDoCampo.Area;

    private static readonly DefinicaoDeCriatura Javali = CatalogoCriaturas.Obter(
        CatalogoCriaturas.JavaliDoCampo
    );

    /// <summary>Devolve sempre a mesma variante e conta os sorteios.</summary>
    private sealed class SorteadorFixo : ISorteadorDeCriaturas
    {
        public VarianteDeCriatura Variante { get; set; } = VarianteDeCriatura.Normal;
        public int Sorteios { get; private set; }

        public CriaturaSorteada Sortear(DefinicaoDeCriatura definicao)
        {
            Sorteios++;
            return new CriaturaSorteada(
                definicao,
                Variante,
                RegrasCriaturas.VidaMaxima(definicao.VidaBase, Variante),
                RegrasCriaturas.Dano(definicao.DanoBase, Variante),
                RegrasCriaturas.Experiencia(definicao.ExperienciaBase, Variante),
                RegrasCriaturas.MultiplicadorDeRecompensa(Variante),
                RegrasCriaturas.Escala(Variante)
            );
        }

        public bool Aparece(DefinicaoDeCriatura definicao) => true;
    }

    /// <summary>Um grupo no ponto (0, 0), com relógio e jogadores controlados pelo teste.</summary>
    private sealed class Campo
    {
        private const int PassoMs = 100;

        public Campo(int tamanho = 1, VarianteDeCriatura variante = VarianteDeCriatura.Normal)
        {
            Sorteador.Variante = variante;
            Grupo = new GrupoDeInimigos(
                Javali,
                Sorteador,
                tamanho,
                () => AgoraMs,
                pontoX: 0.0,
                pontoZ: 0.0
            );
        }

        public long AgoraMs { get; set; } = 10_000;
        public SorteadorFixo Sorteador { get; } = new();
        public GrupoDeInimigos Grupo { get; }
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

        /// <summary>Avança o tempo pedido, passo a passo, e devolve todos os passos.</summary>
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

        /// <summary>Avança até a condição valer; falha se passar do limite.</summary>
        public PassoDosInimigos Ate(Func<PassoDosInimigos, bool> condicao, double limiteSegundos)
        {
            var quantidade = (int)Math.Round(limiteSegundos * 1000.0 / PassoMs);
            for (var i = 0; i < quantidade; i++)
            {
                var passo = Passo();
                if (condicao(passo))
                {
                    return passo;
                }
            }

            var estado = Grupo.Vivos().FirstOrDefault()?.Estado;
            throw new Xunit.Sdk.XunitException(
                $"A condição não valeu em {limiteSegundos} s (estado: {estado})."
            );
        }

        public PassoDosInimigos AteOEstado(EstadoDeInimigo estado, double limiteSegundos) =>
            Ate(_ => Grupo.Vivos().Any(i => i.Estado == estado), limiteSegundos);
    }

    [Fact]
    public void Grupo_nasce_em_repouso_com_o_tamanho_pedido_e_ids_seguidos()
    {
        var campo = new Campo(tamanho: 4);

        var vivos = campo.Grupo.Vivos();

        Assert.Equal(4, campo.Grupo.Tamanho);
        Assert.Equal(
            new[] { PrimeiroId, PrimeiroId + 1, PrimeiroId + 2, PrimeiroId + 3 },
            vivos.Select(i => i.InimigoId)
        );
        Assert.All(
            vivos,
            i =>
            {
                Assert.Equal(CatalogoCriaturas.JavaliDoCampo, i.CriaturaId);
                Assert.Equal(EstadoDeInimigo.Repouso, i.Estado);
                Assert.Equal(RegrasInimigo.SemAlvo, i.AlvoId);
                Assert.Equal(Javali.VidaBase, i.Vida);
                Assert.Equal(Javali.VidaBase, i.VidaMaxima);
            }
        );
        Assert.Equal(4, campo.Sorteador.Sorteios);
    }

    [Fact]
    public void Grupo_sem_inimigos_nao_existe()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new GrupoDeInimigos(Javali, new SorteadorFixo(), 0)
        );
    }

    [Fact]
    public void Sem_jogadores_nada_acontece()
    {
        var campo = new Campo(tamanho: 3);

        Assert.All(campo.Avancar(3), passo => Assert.True(passo.Vazio));
        Assert.All(campo.Grupo.Vivos(), i => Assert.Equal(EstadoDeInimigo.Repouso, i.Estado));
    }

    [Fact]
    public void Ve_a_12_metros_e_nao_ve_mais_longe()
    {
        var longe = new Campo();
        longe.Por(Jogador, 0, 4 + RegrasInimigo.DistanciaDeVisaoMetros + 0.5);
        Assert.All(longe.Avancar(2), p => Assert.True(p.Vazio));
        Assert.Equal(EstadoDeInimigo.Repouso, longe.Unico.Estado);

        var noLimite = new Campo();
        noLimite.Por(Jogador, 0, 4 + RegrasInimigo.DistanciaDeVisaoMetros);
        var passo = noLimite.Passo();

        var mudanca = Assert.Single(passo.Mudancas);
        Assert.False(mudanca.SoPosicao);
        Assert.Equal(EstadoDeInimigo.Alerta, mudanca.Mensagem.Estado);
        Assert.Equal(Jogador, mudanca.Mensagem.AlvoId);
        Assert.Equal(1, mudanca.Mensagem.Sequencia);
        Assert.Equal(0.0, mudanca.Mensagem.RotacaoY, 3);
    }

    [Fact]
    public void Fica_meio_segundo_em_alerta_antes_de_perseguir()
    {
        var campo = new Campo();
        campo.Por(Jogador, 0, 14);

        campo.Avancar(RegrasInimigo.AlertaSegundos);
        Assert.Equal(EstadoDeInimigo.Alerta, campo.Unico.Estado);
        Assert.Equal(4.0, campo.Unico.Z, 3);

        campo.Avancar(0.2);
        Assert.Equal(EstadoDeInimigo.Perseguicao, campo.Unico.Estado);
        Assert.Equal(Jogador, campo.Unico.AlvoId);
    }

    [Fact]
    public void O_grupo_todo_entra_em_alerta_quando_um_ve()
    {
        var campo = new Campo(tamanho: 5);
        // A 11 m do primeiro (em (0, 4)) e a mais de 12 m de todos os outros.
        campo.Por(Jogador, 0, 15);

        var passo = campo.Passo();

        Assert.Equal(5, passo.Mudancas.Count);
        Assert.All(
            campo.Grupo.Vivos(),
            i =>
            {
                Assert.Equal(EstadoDeInimigo.Alerta, i.Estado);
                Assert.Equal(Jogador, i.AlvoId);
            }
        );
    }

    [Fact]
    public void Persegue_em_linha_reta_na_velocidade_da_regra()
    {
        var campo = new Campo();
        campo.Por(Jogador, 0, 14);
        campo.AteOEstado(EstadoDeInimigo.Perseguicao, 2);
        var antes = campo.Unico;

        var passo = campo.Passo();

        var esperado =
            antes.Z
            + (RegrasInimigo.VelocidadeDePerseguicaoMetrosPorSegundo * RegrasInimigo.PassoSegundos);
        Assert.Equal(esperado, campo.Unico.Z, 3);
        Assert.Equal(0.0, campo.Unico.X, 3);
        Assert.True(Assert.Single(passo.Mudancas).SoPosicao);
    }

    [Fact]
    public void Passo_atrasado_nao_faz_o_inimigo_saltar()
    {
        var campo = new Campo();
        campo.Por(Jogador, 0, 14);
        campo.AteOEstado(EstadoDeInimigo.Perseguicao, 2);
        campo.Por(Jogador, 0, 200);
        campo.Passo();
        var antes = campo.Unico.Z;

        campo.AgoraMs += 10_000;
        campo.Passo();

        var maximo =
            RegrasInimigo.VelocidadeDePerseguicaoMetrosPorSegundo
            * (ConfigInimigosDoCampo.PassoMaximoSegundos + RegrasInimigo.PassoSegundos);
        Assert.InRange(campo.Unico.Z - antes, 0.0, maximo + 0.001);
    }

    [Fact]
    public void Avisa_o_golpe_e_acerta_quem_ficou_na_area()
    {
        var campo = new Campo();
        campo.Por(Jogador, 0, 14);

        var aviso = Assert.Single(campo.Ate(p => p.Preparos.Count > 0, 6).Preparos);
        var inimigo = campo.Unico;

        Assert.Equal(EstadoDeInimigo.Ataque, inimigo.Estado);
        Assert.Equal(inimigo.InimigoId, aviso.InimigoId);
        Assert.Equal(RegrasInimigo.PreparoDoAtaqueMs, aviso.PreparoMs);
        Assert.Equal(RegrasInimigo.RaioDoGolpeMetros, aviso.Raio, 3);
        Assert.Equal(0.0, aviso.DirecaoY, 3);
        Assert.Equal(0.0, aviso.CentroX, 3);
        Assert.Equal(inimigo.Z + RegrasInimigo.DistanciaDoGolpeMetros, aviso.CentroZ, 3);
        Assert.True(RegrasInimigo.NoAlcanceDeAtaque(14 - inimigo.Z));

        // Durante o aviso ainda não há dano.
        var durante = campo.Avancar(RegrasInimigo.PreparoDoAtaqueSegundos - 0.1);
        Assert.All(durante, p => Assert.Empty(p.Golpes));

        var golpe = Assert.Single(campo.Passo().Golpes);
        Assert.Equal(
            new GolpeDeInimigoAcertou(inimigo.InimigoId, Javali.Id, Jogador, Javali.DanoBase, Area),
            golpe
        );

        // Na recuperação ele não bate de novo.
        var depois = campo.Avancar(RegrasInimigo.RecuperacaoDoAtaqueSegundos - 0.1);
        Assert.All(depois, p => Assert.Empty(p.Golpes));
        Assert.All(depois, p => Assert.Empty(p.Preparos));
        Assert.Equal(EstadoDeInimigo.Ataque, campo.Unico.Estado);
    }

    [Fact]
    public void Depois_da_recuperacao_volta_a_perseguir_e_ataca_de_novo()
    {
        var campo = new Campo();
        campo.Por(Jogador, 0, 14);
        campo.Ate(p => p.Preparos.Count > 0, 6);

        campo.Ate(p => p.Golpes.Count > 0, 2);
        var proximo = campo.Ate(p => p.Preparos.Count > 0, 3);

        Assert.Single(proximo.Preparos);
    }

    [Fact]
    public void Sair_da_area_durante_o_aviso_evita_o_dano()
    {
        var campo = new Campo();
        campo.Por(Jogador, 0, 14);
        campo.Ate(p => p.Preparos.Count > 0, 6);

        // A esquiva de 5 m tira o jogador do círculo.
        campo.Por(Jogador, 5, 14);

        var passos = campo.Avancar(RegrasInimigo.PreparoDoAtaqueSegundos + 0.3);
        Assert.All(passos, p => Assert.Empty(p.Golpes));
    }

    [Fact]
    public void O_golpe_pega_todos_que_estao_na_area_e_a_area_nao_segue_o_alvo()
    {
        var campo = new Campo();
        campo.Por(Jogador, 0, 14);
        var aviso = Assert.Single(campo.Ate(p => p.Preparos.Count > 0, 6).Preparos);

        // O alvo sai; outros dois entram no círculo que foi avisado.
        campo.Por(Jogador, 5, 14);
        campo.Por(20, aviso.CentroX, aviso.CentroZ);
        campo.Por(10, aviso.CentroX + 1.0, aviso.CentroZ);

        var golpes = campo.Ate(p => p.Golpes.Count > 0, 2).Golpes;

        Assert.Equal(new long[] { 10, 20 }, golpes.Select(g => g.PersonagemId));
    }

    [Fact]
    public void Fortalecida_bate_mais_forte_e_tem_mais_vida()
    {
        var campo = new Campo(variante: VarianteDeCriatura.Fortalecida);
        campo.Por(Jogador, 0, 14);

        Assert.Equal(VarianteDeCriatura.Fortalecida, campo.Unico.Variante);
        Assert.Equal(
            RegrasCriaturas.VidaMaxima(Javali.VidaBase, VarianteDeCriatura.Fortalecida),
            campo.Unico.VidaMaxima
        );

        var golpe = Assert.Single(campo.Ate(p => p.Golpes.Count > 0, 8).Golpes);
        Assert.Equal(
            RegrasCriaturas.Dano(Javali.DanoBase, VarianteDeCriatura.Fortalecida),
            golpe.Dano
        );
    }

    [Fact]
    public void Desiste_a_mais_de_30_metros_e_volta_com_a_vida_cheia_sem_escolher_alvo()
    {
        var campo = new Campo();
        campo.Por(Jogador, 0, 14);
        campo.AteOEstado(EstadoDeInimigo.Perseguicao, 2);
        var id = campo.Unico.InimigoId;
        Assert.True(campo.Grupo.TentarAplicarDano(id, 50, 1, out _));

        // O jogador foge para muito longe; o inimigo vai atrás até o limite.
        campo.Por(Jogador, 0, 200);
        campo.Avancar(6);
        Assert.Equal(EstadoDeInimigo.Perseguicao, campo.Unico.Estado);
        Assert.Equal(Javali.VidaBase - 50, campo.Unico.Vida);

        var desistiu = campo.AteOEstado(EstadoDeInimigo.Retorno, 4);
        var mudanca = Assert.Single(desistiu.Mudancas);
        Assert.False(mudanca.SoPosicao);
        Assert.Equal(Javali.VidaBase, mudanca.Mensagem.Vida);
        Assert.Equal(RegrasInimigo.SemAlvo, mudanca.Mensagem.AlvoId);
        Assert.InRange(campo.Unico.Z - 4, 29.99, 31.0);

        // O jogador fica no caminho da volta: o inimigo passa por ele sem escolher alvo.
        campo.Por(Jogador, 0, 30);
        for (var i = 0; i < 100 && campo.Unico.Estado == EstadoDeInimigo.Retorno; i++)
        {
            var passo = campo.Passo();
            Assert.Empty(passo.Preparos);
            Assert.Equal(RegrasInimigo.SemAlvo, campo.Unico.AlvoId);
        }

        var emCasa = campo.Unico;
        Assert.Equal(EstadoDeInimigo.Repouso, emCasa.Estado);
        Assert.Equal(0.0, emCasa.X, 3);
        Assert.Equal(4.0, emCasa.Z, 3);
        Assert.Equal(ConfigInimigosDoCampo.DirecaoDeRepousoGraus, emCasa.RotacaoY, 3);
        Assert.Equal(Javali.VidaBase, emCasa.Vida);
        Assert.Equal(id, emCasa.InimigoId);

        // A 26 m ele não vê o jogador: continua em repouso.
        Assert.All(campo.Avancar(1), p => Assert.True(p.Vazio));
    }

    [Fact]
    public void A_volta_e_mais_rapida_que_a_perseguicao()
    {
        var campo = new Campo();
        campo.Por(Jogador, 0, 14);
        campo.AteOEstado(EstadoDeInimigo.Perseguicao, 2);
        campo.Por(Jogador, 0, 200);
        campo.Avancar(3);
        campo.Jogadores.Clear();
        campo.AteOEstado(EstadoDeInimigo.Retorno, 1);
        var antes = campo.Unico.Z;

        campo.Passo();

        var esperado =
            RegrasInimigo.VelocidadeDeRetornoMetrosPorSegundo * RegrasInimigo.PassoSegundos;
        Assert.Equal(esperado, antes - campo.Unico.Z, 3);
        Assert.Equal(180.0, Math.Abs(campo.Unico.RotacaoY), 3);
    }

    [Fact]
    public void Alvo_e_o_de_menor_id_no_empate_e_nao_troca_por_outro_mais_perto()
    {
        var campo = new Campo();
        campo.Por(7, 5, 4);
        campo.Por(3, -5, 4);

        campo.Passo();
        Assert.Equal(3, campo.Unico.AlvoId);

        campo.Por(7, 2.5, 4);
        campo.Avancar(0.3);
        Assert.Equal(3, campo.Unico.AlvoId);
    }

    [Fact]
    public void Alvo_que_sai_da_area_e_trocado_e_sem_ninguem_o_inimigo_volta()
    {
        var campo = new Campo();
        campo.Por(7, 5, 4);
        campo.Por(3, -5, 4);
        campo.Passo();

        campo.Jogadores.RemoveAll(j => j.PersonagemId == 3);
        campo.Passo();
        Assert.Equal(7, campo.Unico.AlvoId);

        campo.Jogadores.Clear();
        campo.Passo();
        Assert.Equal(EstadoDeInimigo.Retorno, campo.Unico.Estado);
        Assert.Equal(RegrasInimigo.SemAlvo, campo.Unico.AlvoId);

        campo.Passo();
        Assert.Equal(EstadoDeInimigo.Repouso, campo.Unico.Estado);
    }

    [Fact]
    public void Toda_passagem_de_estado_de_uma_luta_e_valida_e_a_sequencia_so_cresce()
    {
        var campo = new Campo(tamanho: 3);
        var estados = campo.Grupo.Vivos().ToDictionary(i => i.InimigoId, i => i.Estado);
        var sequencias = estados.Keys.ToDictionary(id => id, _ => 0);
        var vistos = new HashSet<EstadoDeInimigo>();

        void Conferir(IEnumerable<PassoDosInimigos> passos)
        {
            foreach (var mudanca in passos.SelectMany(p => p.Mudancas))
            {
                var m = mudanca.Mensagem;
                Assert.True(m.Sequencia > sequencias[m.InimigoId]);
                sequencias[m.InimigoId] = m.Sequencia;
                if (m.Estado != estados[m.InimigoId])
                {
                    Assert.True(
                        RegrasInimigo.TransicaoValida(estados[m.InimigoId], m.Estado),
                        $"{estados[m.InimigoId]} -> {m.Estado}"
                    );
                    Assert.False(mudanca.SoPosicao);
                    estados[m.InimigoId] = m.Estado;
                    vistos.Add(m.Estado);
                }
            }
        }

        campo.Por(Jogador, 0, 14);
        Conferir(campo.Avancar(8));
        campo.Por(Jogador, 0, 200);
        Conferir(campo.Avancar(25));

        Assert.Equal(
            new[]
            {
                EstadoDeInimigo.Repouso,
                EstadoDeInimigo.Alerta,
                EstadoDeInimigo.Perseguicao,
                EstadoDeInimigo.Ataque,
                EstadoDeInimigo.Retorno,
            },
            vistos.Order()
        );
        Assert.All(campo.Grupo.Vivos(), i => Assert.Equal(EstadoDeInimigo.Repouso, i.Estado));
    }

    [Fact]
    public void Vida_e_a_mesma_com_1_ou_5_jogadores()
    {
        var sozinho = new Campo(tamanho: 3);
        sozinho.Por(1, 0, 10);
        sozinho.Avancar(1);

        var emGrupo = new Campo(tamanho: 3);
        for (var id = 1; id <= 5; id++)
        {
            emGrupo.Por(id, id, 10);
        }

        emGrupo.Avancar(1);

        Assert.All(sozinho.Grupo.Vivos(), i => Assert.Equal(Javali.VidaBase, i.VidaMaxima));
        Assert.All(emGrupo.Grupo.Vivos(), i => Assert.Equal(Javali.VidaBase, i.VidaMaxima));
        Assert.All(emGrupo.Grupo.Vivos(), i => Assert.Equal(Javali.VidaBase, i.Vida));
    }

    [Fact]
    public void E_alvo_dos_golpes_so_na_propria_area_e_dentro_do_volume()
    {
        var campo = new Campo();

        var alvo = Assert.Single(campo.Grupo.AlvosNoVolume(Area, 0, 3, 0));
        Assert.Equal(PrimeiroId, alvo.Id);
        Assert.Equal(0.0, alvo.X, 3);
        Assert.Equal(4.0, alvo.Z, 3);
        Assert.Equal(RegrasInimigo.RaioDoCorpoMetros, alvo.Raio);
        Assert.Equal(Javali.VidaBase, alvo.VidaAtual);
        Assert.Equal(Javali.VidaBase, alvo.VidaMaxima);

        Assert.Empty(campo.Grupo.AlvosNoVolume("outra-area", 0, 3, 0));
        Assert.Empty(campo.Grupo.AlvosNoVolume(Area, 0, -50, 0));
        Assert.Empty(campo.Grupo.AlvosNoVolume(Area, 0, 3, 180));
    }

    [Fact]
    public void Dano_tira_vida_e_sai_no_proximo_aviso_de_mudanca()
    {
        var campo = new Campo();
        campo.Por(Jogador, 0, 6);
        campo.Passo();

        Assert.True(campo.Grupo.TentarAplicarDano(PrimeiroId, 30, 1, out var resultado));
        Assert.Equal(
            new ResultadoDeDano(PrimeiroId, 30, Javali.VidaBase - 30, Javali.VidaBase),
            resultado
        );

        var mudanca = Assert.Single(campo.Passo().Mudancas);
        Assert.False(mudanca.SoPosicao);
        Assert.Equal(Javali.VidaBase - 30, mudanca.Mensagem.Vida);
    }

    [Theory]
    [InlineData(PrimeiroId, 0)]
    [InlineData(PrimeiroId, -5)]
    [InlineData(PrimeiroId + 50, 10)]
    [InlineData(-1L, 10)]
    public void Dano_invalido_ou_em_alvo_de_outro_dono_e_recusado(long alvoId, int dano)
    {
        var campo = new Campo();

        Assert.False(campo.Grupo.TentarAplicarDano(alvoId, dano, 1, out _));
        Assert.Equal(Javali.VidaBase, campo.Unico.Vida);
    }

    [Fact]
    public void Inimigo_morto_nao_age_nem_leva_dano()
    {
        var campo = new Campo();
        campo.Por(Jogador, 0, 6);
        campo.Passo();

        Assert.True(campo.Grupo.TentarAplicarDano(PrimeiroId, 1000, 1, out var resultado));
        Assert.Equal(Javali.VidaBase, resultado.Dano);
        Assert.Equal(0, resultado.VidaRestante);

        Assert.False(campo.Grupo.TentarAplicarDano(PrimeiroId, 10, 1, out _));
        Assert.Empty(campo.Grupo.Vivos());
        Assert.Empty(campo.Grupo.AlvosNoVolume(Area, 0, 3, 0));

        var passo = campo.Passo();
        Assert.Equal(PrimeiroId, Assert.Single(passo.Mortes).InimigoId);
        Assert.Single(passo.Derrotas);
        Assert.Empty(passo.Mudancas);

        Assert.All(campo.Avancar(5), p => Assert.True(p.Vazio));
    }

    [Fact]
    public void Morte_publica_a_derrota_com_a_recompensa_e_so_quem_deu_dano()
    {
        var campo = new Campo(variante: VarianteDeCriatura.Fortalecida);
        campo.Por(Jogador, 0, 6);
        campo.Por(4, 1, 6);
        campo.Por(50, 0, 100);
        campo.Passo();

        Assert.True(campo.Grupo.TentarAplicarDano(PrimeiroId, 1000, Jogador, out _));

        var derrota = Assert.Single(campo.Passo().Derrotas);
        Assert.Equal(PrimeiroId, derrota.InimigoId);
        Assert.Equal(Javali.Id, derrota.CriaturaId);
        Assert.Equal(VarianteDeCriatura.Fortalecida, derrota.Variante);
        Assert.Equal(
            RegrasCriaturas.Experiencia(Javali.ExperienciaBase, VarianteDeCriatura.Fortalecida),
            derrota.Experiencia
        );
        Assert.Equal(
            RegrasCriaturas.MultiplicadorDeRecompensaFortalecida,
            derrota.MultiplicadorDeRecompensa
        );
        Assert.Equal(Area, derrota.Area);
        Assert.Equal(0.0, derrota.X, 3);
        Assert.Equal(4.0, derrota.Z, 3);
        Assert.Equal(new[] { Jogador }, derrota.Participantes);
    }

    [Fact]
    public void Dois_golpes_no_mesmo_instante_um_mata_e_os_outros_nao_acertam()
    {
        var campo = new Campo();
        Assert.True(campo.Grupo.TentarAplicarDano(PrimeiroId, Javali.VidaBase - 10, 1, out _));

        var acertos = 0;
        Parallel.For(
            0,
            16,
            _ =>
            {
                if (campo.Grupo.TentarAplicarDano(PrimeiroId, 50, 1, out var resultado))
                {
                    Assert.Equal(10, resultado.Dano);
                    Interlocked.Increment(ref acertos);
                }
            }
        );

        Assert.Equal(1, acertos);
        var passo = campo.Passo();
        Assert.Single(passo.Mortes);
        Assert.Single(passo.Derrotas);
    }

    [Fact]
    public void Volta_depois_do_intervalo_da_ficha_uma_vez_so_com_id_e_sorteio_novos()
    {
        var campo = new Campo();
        var morte = campo.AgoraMs;
        Assert.True(campo.Grupo.TentarAplicarDano(PrimeiroId, 1000, 1, out _));
        Assert.Single(campo.Passo().Mortes);

        campo.AgoraMs = morte + (Javali.IntervaloDeAparicaoSegundos * 1000L) - 1;
        Assert.True(campo.Grupo.Passo(campo.Jogadores).Vazio);
        Assert.Empty(campo.Grupo.Vivos());

        campo.AgoraMs = morte + (Javali.IntervaloDeAparicaoSegundos * 1000L);
        var novo = Assert.Single(campo.Grupo.Passo(campo.Jogadores).Aparicoes).Inimigo;

        Assert.Equal(PrimeiroId + 1, novo.InimigoId);
        Assert.Equal(EstadoDeInimigo.Repouso, novo.Estado);
        Assert.Equal(Javali.VidaBase, novo.Vida);
        Assert.Equal(0.0, novo.X, 3);
        Assert.Equal(4.0, novo.Z, 3);
        Assert.Equal(2, campo.Sorteador.Sorteios);
        Assert.Equal(novo.InimigoId, campo.Unico.InimigoId);

        Assert.All(campo.Avancar(300), p => Assert.True(p.Vazio));
        Assert.Single(campo.Grupo.Vivos());
    }

    [Fact]
    public void Quem_volta_nao_persegue_o_jogador_que_o_grupo_viu_antes_e_ja_esta_longe()
    {
        var campo = new Campo();
        campo.Por(Jogador, 0, 6);
        campo.Passo();
        Assert.True(campo.Grupo.TentarAplicarDano(PrimeiroId, 1000, Jogador, out _));
        Assert.Equal(new[] { Jogador }, Assert.Single(campo.Passo().Derrotas).Participantes);

        // O jogador continua na área, mas voltou para longe do grupo.
        campo.Por(Jogador, 0, 150);
        campo.AgoraMs += Javali.IntervaloDeAparicaoSegundos * 1000L;
        var volta = campo.Passo();

        Assert.Single(volta.Aparicoes);
        Assert.Empty(volta.Mudancas);
        Assert.All(campo.Avancar(2), p => Assert.True(p.Vazio));
        Assert.Equal(EstadoDeInimigo.Repouso, campo.Unico.Estado);
    }

    [Fact]
    public void Quem_volta_depois_de_morto_recomeca_a_sequencia()
    {
        var campo = new Campo();
        campo.Por(Jogador, 0, 14);
        campo.Avancar(2);
        Assert.True(campo.Grupo.TentarAplicarDano(PrimeiroId, 1000, 1, out _));
        campo.Jogadores.Clear();
        campo.AgoraMs += Javali.IntervaloDeAparicaoSegundos * 1000L;
        Assert.Single(campo.Passo().Aparicoes);

        campo.Por(Jogador, 0, 14);
        var mudanca = Assert.Single(campo.Passo().Mudancas);

        Assert.Equal(PrimeiroId + 1, mudanca.Mensagem.InimigoId);
        Assert.Equal(1, mudanca.Mensagem.Sequencia);
    }

    [Fact]
    public void Quem_bateu_participa_e_quem_so_estava_perto_nao_em_ordem_de_id()
    {
        var campo = new Campo();
        campo.Por(9, 0, 6);
        campo.Por(4, 1, 6);
        campo.Por(5, 0, 7);
        campo.Passo();

        Assert.True(campo.Grupo.TentarAplicarDano(PrimeiroId, 10, 9, out _));
        Assert.True(campo.Grupo.TentarAplicarDano(PrimeiroId, 10, 4, out _));
        Assert.True(campo.Grupo.TentarAplicarDano(PrimeiroId, 10, 9, out _));
        Assert.True(campo.Grupo.TentarAplicarDano(PrimeiroId, 1000, 4, out _));

        var derrota = Assert.Single(campo.Passo().Derrotas);
        Assert.Equal(new[] { 4L, 9L }, derrota.Participantes);
    }

    [Fact]
    public void Quem_bateu_e_se_afastou_continua_participando()
    {
        var campo = new Campo();
        campo.Por(Jogador, 0, 6);
        campo.Passo();
        Assert.True(campo.Grupo.TentarAplicarDano(PrimeiroId, 10, Jogador, out _));
        campo.Jogadores.Clear();

        Assert.True(campo.Grupo.TentarAplicarDano(PrimeiroId, 1000, 7, out _));

        Assert.Equal(new[] { 7L, Jogador }, Assert.Single(campo.Passo().Derrotas).Participantes);
    }

    [Fact]
    public void Dano_recusado_nao_conta_o_atacante()
    {
        var campo = new Campo();

        Assert.False(campo.Grupo.TentarAplicarDano(PrimeiroId, 0, Jogador, out _));
        Assert.True(campo.Grupo.TentarAplicarDano(PrimeiroId, 1000, 7, out _));

        Assert.Equal(new[] { 7L }, Assert.Single(campo.Passo().Derrotas).Participantes);
    }

    [Fact]
    public void Lista_de_atacantes_zera_quando_o_inimigo_reaparece()
    {
        var campo = new Campo();
        Assert.True(campo.Grupo.TentarAplicarDano(PrimeiroId, 1000, Jogador, out _));
        Assert.Equal(new[] { Jogador }, Assert.Single(campo.Passo().Derrotas).Participantes);

        campo.AgoraMs += Javali.IntervaloDeAparicaoSegundos * 1000L;
        Assert.Single(campo.Passo().Aparicoes);
        Assert.True(campo.Grupo.TentarAplicarDano(PrimeiroId + 1, 1000, 7, out _));

        Assert.Equal(new[] { 7L }, Assert.Single(campo.Passo().Derrotas).Participantes);
    }

    [Fact]
    public void Lista_de_atacantes_zera_quando_o_inimigo_desiste_e_volta()
    {
        var campo = new Campo();
        campo.Por(Jogador, 0, 14);
        campo.AteOEstado(EstadoDeInimigo.Perseguicao, 2);
        Assert.True(campo.Grupo.TentarAplicarDano(campo.Unico.InimigoId, 50, Jogador, out _));
        campo.Por(Jogador, 0, 200);
        campo.Avancar(6);
        campo.AteOEstado(EstadoDeInimigo.Retorno, 4);

        Assert.True(campo.Grupo.TentarAplicarDano(PrimeiroId, 1000, 7, out _));

        Assert.Equal(new[] { 7L }, Assert.Single(campo.Passo().Derrotas).Participantes);
    }
}
