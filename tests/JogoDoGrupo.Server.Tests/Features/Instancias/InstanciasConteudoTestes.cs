using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Features.Instancias;
using JogoDoGrupo.Server.Features.Mobs;
using JogoDoGrupo.Shared.Features.Instancias;
using JogoDoGrupo.Shared.Features.Mobs;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Instancias;

public sealed class InstanciasConteudoTestes
{
    [Fact]
    public void Inimigos_da_instancia_nao_renascem_mas_os_do_campo_sim()
    {
        var agora = 1_000L;
        var sorteador = new SorteadorDeCriaturas();
        var pedido = PopuladorDeInstancia.GruposDosSaloes()[0];
        var daInstancia = GrupoDeInimigos.ParaInstancia(
            AreasDoMundo.Instancia("teste"),
            pedido,
            sorteador,
            1,
            relogioMs: () => agora
        );
        var doCampo = new GrupoDeInimigos(
            CatalogoCriaturas.Obter(pedido.CriaturaId),
            sorteador,
            pedido.Quantidade,
            () => agora
        );

        foreach (var grupo in new[] { daInstancia, doCampo })
        {
            foreach (var inimigo in grupo.Vivos())
            {
                Assert.True(grupo.TentarAplicarDano(inimigo.InimigoId, 1_000_000, 1, out _));
            }

            Assert.Empty(grupo.Vivos());
        }

        agora += 24L * 3600 * 1000;
        daInstancia.Passo([]);
        doCampo.Passo([]);

        Assert.Empty(daInstancia.Vivos());
        Assert.NotEmpty(doCampo.Vivos());
    }

    [Fact]
    public void Populador_cria_os_tres_saloes_e_o_boss_e_limpa_ao_fechar()
    {
        var inimigos = new InimigosPorInstancia(new SorteadorDeCriaturas(), new QuedaDeTeste());
        var chefes = new ChefesDaDungeon();
        var populador = new PopuladorDeInstancia(inimigos, chefes);
        var area = AreasDoMundo.Instancia("a");

        populador.AoAbrir(area);

        Assert.Equal(3, inimigos.Simulados().Count());
        Assert.All(inimigos.Simulados(), g => Assert.NotEmpty(g.Vivos()));
        Assert.NotNull(chefes.Obter(area));

        populador.AoFechar(area);

        Assert.Empty(inimigos.Simulados());
        Assert.Null(chefes.Obter(area));
    }

    [Fact]
    public async Task Boss_morto_agenda_vitoria_e_encerra_apos_prazo_de_loot()
    {
        using var f = new InstanciasFixture();
        using var jogador = await f.EntrarAsync();
        jogador.Cliente.Enviar(InstanciasMessageIds.EntrarNaDungeon, new EntrarNaDungeon());
        var inicio = await jogador.Cliente.ReceberAsync<DungeonIniciada>(
            InstanciasMessageIds.DungeonIniciada
        );

        f.Instancias.AoInimigoDerrotado(
            new InimigoDerrotado(
                1,
                CatalogoCriaturas.GuardiaoDoReflexo,
                VarianteDeCriatura.Normal,
                500,
                1,
                inicio.Area,
                LayoutDaMinaDoReflexo.BossCentroX,
                LayoutDaMinaDoReflexo.BossCentroZ,
                [jogador.PersonagemId],
                1,
                5
            )
        );

        var aviso = await jogador.Cliente.ReceberAsync<ChefeDerrotadoNaDungeon>(
            InstanciasMessageIds.ChefeDerrotadoNaDungeon
        );
        Assert.Equal(inicio.IdInstancia, aviso.IdInstancia);
        Assert.True(aviso.SegundosParaColeta > 0);
        Assert.Equal(1, f.Instancias.CorridasAtivas);

        f.Relogio.Advance(TimeSpan.FromSeconds(aviso.SegundosParaColeta + 1));
        f.Instancias.Varrer();

        var encerrada = await jogador.Cliente.ReceberAsync<DungeonEncerrada>(
            InstanciasMessageIds.DungeonEncerrada
        );
        Assert.Equal(ResultadoDungeon.Vitoria, encerrada.Resultado);
        Assert.Equal(0, f.Instancias.CorridasAtivas);
    }

    [Fact]
    public async Task Boss_morto_permite_pedir_saida_antecipada()
    {
        using var f = new InstanciasFixture();
        using var jogador = await f.EntrarAsync();
        jogador.Cliente.Enviar(InstanciasMessageIds.EntrarNaDungeon, new EntrarNaDungeon());
        var inicio = await jogador.Cliente.ReceberAsync<DungeonIniciada>(
            InstanciasMessageIds.DungeonIniciada
        );

        f.Instancias.AoInimigoDerrotado(
            new InimigoDerrotado(
                1,
                CatalogoCriaturas.GuardiaoDoReflexo,
                VarianteDeCriatura.Normal,
                500,
                1,
                inicio.Area,
                LayoutDaMinaDoReflexo.BossCentroX,
                LayoutDaMinaDoReflexo.BossCentroZ,
                [jogador.PersonagemId],
                1,
                5
            )
        );

        await jogador.Cliente.ReceberAsync<ChefeDerrotadoNaDungeon>(
            InstanciasMessageIds.ChefeDerrotadoNaDungeon
        );

        jogador.Cliente.Enviar(InstanciasMessageIds.SairDaDungeon, new SairDaDungeon());

        var encerrada = await jogador.Cliente.ReceberAsync<DungeonEncerrada>(
            InstanciasMessageIds.DungeonEncerrada
        );
        Assert.Equal(ResultadoDungeon.Vitoria, encerrada.Resultado);
        await InstanciasFixture.EsperarAsync(() => f.Instancias.CorridasAtivas == 0);
        Assert.Equal(0, f.Instancias.CorridasAtivas);
    }

    [Fact]
    public async Task Loot_do_boss_alcancavel_antes_do_encerramento()
    {
        using var f = new InstanciasFixture();
        using var jogador = await f.EntrarAsync();
        jogador.Cliente.Enviar(InstanciasMessageIds.EntrarNaDungeon, new EntrarNaDungeon());
        var inicio = await jogador.Cliente.ReceberAsync<DungeonIniciada>(
            InstanciasMessageIds.DungeonIniciada
        );

        // Move jogador para perto do centro do boss dentro da instancia
        f.Posicoes.Definir(
            jogador.PersonagemId,
            new PosicaoNoMundo(
                inicio.Area,
                LayoutDaMinaDoReflexo.BossCentroX,
                0,
                LayoutDaMinaDoReflexo.BossCentroZ,
                0
            )
        );

        var eventos =
            f.Fabrica.Services.GetRequiredService<JogoDoGrupo.Server.Core.Eventos.EventosDoServidor>();
        eventos.Publicar(
            new InimigoDerrotado(
                1,
                CatalogoCriaturas.GuardiaoDoReflexo,
                VarianteDeCriatura.Normal,
                500,
                1,
                inicio.Area,
                LayoutDaMinaDoReflexo.BossCentroX,
                LayoutDaMinaDoReflexo.BossCentroZ,
                [jogador.PersonagemId],
                1,
                5
            )
        );

        var aviso = await jogador.Cliente.ReceberAsync<ChefeDerrotadoNaDungeon>(
            InstanciasMessageIds.ChefeDerrotadoNaDungeon
        );
        Assert.NotNull(aviso);

        var loot =
            await jogador.Cliente.ReceberAsync<JogoDoGrupo.Shared.Features.Itens.LootApareceu>(
                JogoDoGrupo.Shared.Features.Itens.ItensMessageIds.LootApareceu
            );
        Assert.NotNull(loot);

        // Jogador ainda na dungeon coleta o loot na sala do boss
        jogador.Cliente.Enviar(
            JogoDoGrupo.Shared.Features.Itens.ItensMessageIds.ColetarLoot,
            new JogoDoGrupo.Shared.Features.Itens.ColetarLoot { LootId = loot.Loot.LootId }
        );

        var coletado =
            await jogador.Cliente.ReceberAsync<JogoDoGrupo.Shared.Features.Itens.LootColetado>(
                JogoDoGrupo.Shared.Features.Itens.ItensMessageIds.LootColetado
            );
        Assert.Equal(loot.Loot.LootId, coletado.LootId);

        // Agora pede saída antecipada
        jogador.Cliente.Enviar(InstanciasMessageIds.SairDaDungeon, new SairDaDungeon());
        var encerrada = await jogador.Cliente.ReceberAsync<DungeonEncerrada>(
            InstanciasMessageIds.DungeonEncerrada
        );
        Assert.Equal(ResultadoDungeon.Vitoria, encerrada.Resultado);
    }

    [Fact]
    public async Task Morte_de_inimigo_comum_nao_encerra_a_corrida()
    {
        using var f = new InstanciasFixture();
        using var jogador = await f.EntrarAsync();
        jogador.Cliente.Enviar(InstanciasMessageIds.EntrarNaDungeon, new EntrarNaDungeon());
        var inicio = await jogador.Cliente.ReceberAsync<DungeonIniciada>(
            InstanciasMessageIds.DungeonIniciada
        );

        f.Instancias.AoInimigoDerrotado(
            new InimigoDerrotado(
                1,
                CatalogoCriaturas.JavaliDoCampo,
                VarianteDeCriatura.Normal,
                10,
                1,
                inicio.Area,
                0,
                0,
                [jogador.PersonagemId],
                1,
                3
            )
        );

        Assert.Equal(1, f.Instancias.CorridasAtivas);
    }

    [Fact]
    public async Task Todos_caidos_encerra_a_corrida_com_derrota()
    {
        using var f = new InstanciasFixture();
        using var jogador = await f.EntrarAsync();
        jogador.Cliente.Enviar(InstanciasMessageIds.EntrarNaDungeon, new EntrarNaDungeon());
        await jogador.Cliente.ReceberAsync<DungeonIniciada>(InstanciasMessageIds.DungeonIniciada);

        // Dentro da carência (3 s) a corrida não termina, mesmo com todos caídos.
        f.Queda.Derrubar(jogador.PersonagemId);
        f.Instancias.VarrerDerrota();
        Assert.Equal(1, f.Instancias.CorridasAtivas);

        f.Relogio.Advance(TimeSpan.FromSeconds(4));
        f.Instancias.VarrerDerrota();

        var encerrada = await jogador.Cliente.ReceberAsync<DungeonEncerrada>(
            InstanciasMessageIds.DungeonEncerrada
        );
        Assert.Equal(ResultadoDungeon.Derrota, encerrada.Resultado);
    }

    [Fact]
    public async Task Quem_esta_em_pe_mantem_a_corrida_e_o_que_caiu_da_conexao_nao_conta_como_fora()
    {
        using var f = new InstanciasFixture();
        using var lider = await f.EntrarAsync();
        var membro = await f.EntrarAsync();
        f.Grupos.Definir(60, lider.PersonagemId, lider.PersonagemId, membro.PersonagemId);
        lider.Cliente.Enviar(InstanciasMessageIds.EntrarNaDungeon, new EntrarNaDungeon());
        await lider.Cliente.ReceberAsync<DungeonIniciada>(InstanciasMessageIds.DungeonIniciada);
        await membro.Cliente.ReceberAsync<DungeonIniciada>(InstanciasMessageIds.DungeonIniciada);
        f.Relogio.Advance(TimeSpan.FromSeconds(4));

        f.Queda.Derrubar(lider.PersonagemId);
        f.Instancias.VarrerDerrota();
        Assert.Equal(1, f.Instancias.CorridasAtivas);

        Assert.True(
            f.TempoReal.TentarObterSessao(membro.PersonagemId, out var sessaoMembro)
                && sessaoMembro != null
        );
        f.Queda.Derrubar(membro.PersonagemId);
        membro.Dispose();
        f.Instancias.AoDesconectar(sessaoMembro!);
        f.Instancias.VarrerDerrota();
        // Todos caídos, mas a vaga do desconectado está reservada: ainda não é derrota.
        Assert.Equal(1, f.Instancias.CorridasAtivas);
    }

    [Fact]
    public async Task Entrada_longe_da_porta_e_recusada()
    {
        using var f = new InstanciasFixture();
        using var jogador = await f.EntrarAsync();
        f.Posicoes.Definir(
            jogador.PersonagemId,
            new PosicaoNoMundo(AreasDoMundo.Cidade, 0f, 0f, 0f, 0f)
        );

        jogador.Cliente.Enviar(InstanciasMessageIds.EntrarNaDungeon, new EntrarNaDungeon());

        var recusa = await jogador.Cliente.ReceberAsync<EntradaRecusada>(
            InstanciasMessageIds.EntradaRecusada
        );
        Assert.Equal(CodigosErroInstancias.LongeDemais, recusa.Codigo);
        Assert.Equal(0, f.Instancias.CorridasAtivas);
    }

    [Fact]
    public async Task Jogador_nasce_na_antecamara_com_altura_do_chao_e_inimigos_nos_saloes()
    {
        using var f = new InstanciasFixture();
        using var jogador = await f.EntrarAsync();
        jogador.Cliente.Enviar(InstanciasMessageIds.EntrarNaDungeon, new EntrarNaDungeon());
        var inicio = await jogador.Cliente.ReceberAsync<DungeonIniciada>(
            InstanciasMessageIds.DungeonIniciada
        );

        var posicoes = f.Fabrica.Services.GetRequiredService<IPosicoesDoMundo>();
        Assert.True(posicoes.TentarObter(jogador.PersonagemId, out var dentro));
        Assert.Equal(inicio.Area, dentro.Area);
        Assert.Equal(LayoutDaMinaDoReflexo.SpawnX, dentro.X, 1);
        Assert.Equal(LayoutDaMinaDoReflexo.SpawnY, dentro.Y, 2);

        var chefes = f.Fabrica.Services.GetRequiredService<ChefesDaDungeon>();
        Assert.NotNull(chefes.Obter(inicio.Area));
    }

    [Fact]
    public async Task Grupo_de_dois_um_caido_mantem_instancia_e_reviver_evita_derrota()
    {
        using var f = new InstanciasFixture();
        using var lider = await f.EntrarAsync();
        using var membro = await f.EntrarAsync();
        f.Grupos.Definir(100, lider.PersonagemId, lider.PersonagemId, membro.PersonagemId);

        lider.Cliente.Enviar(InstanciasMessageIds.EntrarNaDungeon, new EntrarNaDungeon());
        var inicio = await lider.Cliente.ReceberAsync<DungeonIniciada>(
            InstanciasMessageIds.DungeonIniciada
        );
        await membro.Cliente.ReceberAsync<DungeonIniciada>(InstanciasMessageIds.DungeonIniciada);

        // Passa a carência inicial de derrota
        f.Relogio.Advance(TimeSpan.FromSeconds(5));

        // Membro cai; líder continua de pé
        f.Queda.Derrubar(membro.PersonagemId);
        f.Instancias.VarrerDerrota();

        // Corrida não encerra por derrota porque o líder está vivo e na dungeon
        Assert.Equal(1, f.Instancias.CorridasAtivas);

        // Líder levanta o membro
        f.Queda.Levantar(membro.PersonagemId);
        f.Instancias.VarrerDerrota();
        Assert.Equal(1, f.Instancias.CorridasAtivas);

        // Se agora AMBOS caírem:
        f.Queda.Derrubar(lider.PersonagemId);
        f.Queda.Derrubar(membro.PersonagemId);
        f.Instancias.VarrerDerrota();

        // Agora sim todos caídos encerra a corrida com derrota
        var encerrada = await lider.Cliente.ReceberAsync<DungeonEncerrada>(
            InstanciasMessageIds.DungeonEncerrada
        );
        Assert.Equal(ResultadoDungeon.Derrota, encerrada.Resultado);
        Assert.Equal(0, f.Instancias.CorridasAtivas);
    }
}
