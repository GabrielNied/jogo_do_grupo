using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Shared.Features.Instancias;
using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Tests.Features.Instancias;

public sealed class DificuldadeSemFimServidorTestes
{
    [Fact]
    public async Task Entrada_com_nivel_acima_do_permitido_e_recusada()
    {
        using var f = new InstanciasFixture();
        using var jogador = await f.EntrarAsync();

        jogador.Cliente.Enviar(
            InstanciasMessageIds.EntrarNaDungeon,
            new EntrarNaDungeon { Nivel = 2 }
        );

        var recusa = await jogador.Cliente.ReceberAsync<EntradaRecusada>(
            InstanciasMessageIds.EntradaRecusada
        );
        Assert.Equal(CodigosErroInstancias.NivelNaoDesbloqueado, recusa.Codigo);
    }

    [Fact]
    public async Task Entrada_com_nivel_valido_inicia_dungeon_com_nivel_e_tempo_limite()
    {
        using var f = new InstanciasFixture();
        using var jogador = await f.EntrarAsync();

        jogador.Cliente.Enviar(
            InstanciasMessageIds.EntrarNaDungeon,
            new EntrarNaDungeon { Nivel = 1 }
        );

        var inicio = await jogador.Cliente.ReceberAsync<DungeonIniciada>(
            InstanciasMessageIds.DungeonIniciada
        );
        Assert.Equal(1, inicio.Nivel);
        Assert.Equal(RegrasDificuldadeSemFim.TempoLimitePadraoSegundos, inicio.TempoLimiteSegundos);
    }

    [Fact]
    public async Task Vitoria_no_tempo_desbloqueia_proximo_nivel_e_permite_iniciar_nivel_superior()
    {
        using var f = new InstanciasFixture();
        using var jogador = await f.EntrarAsync();

        jogador.Cliente.Enviar(
            InstanciasMessageIds.EntrarNaDungeon,
            new EntrarNaDungeon { Nivel = 1 }
        );
        var inicio = await jogador.Cliente.ReceberAsync<DungeonIniciada>(
            InstanciasMessageIds.DungeonIniciada
        );

        f.Relogio.Advance(TimeSpan.FromSeconds(120));

        var derrota = new InimigoDerrotado(
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
        );
        f.Instancias.AoInimigoDerrotado(derrota);

        var chefeDerrotado = await jogador.Cliente.ReceberAsync<ChefeDerrotadoNaDungeon>(
            InstanciasMessageIds.ChefeDerrotadoNaDungeon
        );
        Assert.Equal(1, chefeDerrotado.Nivel);
        Assert.True(chefeDerrotado.NoTempo);
        Assert.True(chefeDerrotado.TempoTotalSegundos >= 120);

        jogador.Cliente.Enviar(InstanciasMessageIds.SairDaDungeon, new SairDaDungeon());

        var encerrada = await jogador.Cliente.ReceberAsync<DungeonEncerrada>(
            InstanciasMessageIds.DungeonEncerrada
        );
        Assert.Equal(1, encerrada.Nivel);
        Assert.True(encerrada.NoTempo);
        Assert.Equal(2, encerrada.ProximoNivelLiberado);

        // Agora o jogador tem nível 2 desbloqueado
        jogador.Cliente.Enviar(
            InstanciasMessageIds.EntrarNaDungeon,
            new EntrarNaDungeon { Nivel = 2 }
        );
        var proximoInicio = await jogador.Cliente.ReceberAsync<DungeonIniciada>(
            InstanciasMessageIds.DungeonIniciada
        );
        Assert.Equal(2, proximoInicio.Nivel);
    }

    [Fact]
    public async Task Vitoria_fora_do_tempo_nao_desbloqueia_proximo_nivel()
    {
        using var f = new InstanciasFixture();
        using var jogador = await f.EntrarAsync();

        jogador.Cliente.Enviar(
            InstanciasMessageIds.EntrarNaDungeon,
            new EntrarNaDungeon { Nivel = 1 }
        );
        var inicio = await jogador.Cliente.ReceberAsync<DungeonIniciada>(
            InstanciasMessageIds.DungeonIniciada
        );

        f.Relogio.Advance(TimeSpan.FromSeconds(950));

        var derrota = new InimigoDerrotado(
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
        );
        f.Instancias.AoInimigoDerrotado(derrota);

        var chefeDerrotado = await jogador.Cliente.ReceberAsync<ChefeDerrotadoNaDungeon>(
            InstanciasMessageIds.ChefeDerrotadoNaDungeon
        );
        Assert.Equal(1, chefeDerrotado.Nivel);
        Assert.False(chefeDerrotado.NoTempo);

        jogador.Cliente.Enviar(InstanciasMessageIds.SairDaDungeon, new SairDaDungeon());

        var encerrada = await jogador.Cliente.ReceberAsync<DungeonEncerrada>(
            InstanciasMessageIds.DungeonEncerrada
        );
        Assert.Equal(1, encerrada.Nivel);
        Assert.False(encerrada.NoTempo);
        Assert.Equal(1, encerrada.ProximoNivelLiberado);

        // Tentar entrar no nível 2 deve ser recusado
        jogador.Cliente.Enviar(
            InstanciasMessageIds.EntrarNaDungeon,
            new EntrarNaDungeon { Nivel = 2 }
        );
        var recusa = await jogador.Cliente.ReceberAsync<EntradaRecusada>(
            InstanciasMessageIds.EntradaRecusada
        );
        Assert.Equal(CodigosErroInstancias.NivelNaoDesbloqueado, recusa.Codigo);
    }

    [Fact]
    public async Task Grupo_herda_o_nivel_escolhido_pelo_lider()
    {
        using var f = new InstanciasFixture();
        using var lider = await f.EntrarAsync();
        using var membro = await f.EntrarAsync();

        f.Grupos.Definir(55, lider.PersonagemId, lider.PersonagemId, membro.PersonagemId);

        lider.Cliente.Enviar(
            InstanciasMessageIds.EntrarNaDungeon,
            new EntrarNaDungeon { Nivel = 1 }
        );

        var inicioLider = await lider.Cliente.ReceberAsync<DungeonIniciada>(
            InstanciasMessageIds.DungeonIniciada
        );
        var inicioMembro = await membro.Cliente.ReceberAsync<DungeonIniciada>(
            InstanciasMessageIds.DungeonIniciada
        );

        Assert.Equal(1, inicioLider.Nivel);
        Assert.Equal(1, inicioMembro.Nivel);
        Assert.Equal(inicioLider.IdInstancia, inicioMembro.IdInstancia);
    }
}
