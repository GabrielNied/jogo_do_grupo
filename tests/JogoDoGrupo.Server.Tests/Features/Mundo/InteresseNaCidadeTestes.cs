using System.Net;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Features.Mundo;
using JogoDoGrupo.Server.Tests.Core.TempoReal;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Features.Mundo;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;

namespace JogoDoGrupo.Server.Tests.Features.Mundo;

/// <summary>Grade de interesse (#500): quem vê quem pela distância.</summary>
public class GradeDeInteresseTestes
{
    [Fact]
    public void Longe_nao_se_veem_e_perto_se_veem_nos_dois_sentidos()
    {
        var grade = new GradeDeInteresse();
        grade.Atualizar(1, 0, 0);
        var perto = grade.Atualizar(2, 100, 0);
        var longe = grade.Atualizar(3, 900, 900);

        Assert.Equal([1L], perto.Entraram);
        Assert.Empty(longe.Entraram);
        Assert.Contains(2L, grade.Vistos(1));
        Assert.Contains(1L, grade.Vistos(2));
        Assert.Empty(grade.Vistos(3));
    }

    [Fact]
    public void Entra_no_raio_de_visao_e_so_sai_depois_do_raio_de_saida()
    {
        var grade = new GradeDeInteresse();
        grade.Atualizar(1, 0, 0);
        grade.Atualizar(2, 500, 0);

        var entrou = grade.Atualizar(2, GradeDeInteresse.RaioDeVisao - 1, 0);
        Assert.Equal([1L], entrou.Entraram);

        // Entre o raio de visão e o de saída continua visto (sem piscar na borda).
        var borda = grade.Atualizar(2, GradeDeInteresse.RaioDeSaida - 1, 0);
        Assert.Empty(borda.Sairam);
        Assert.Empty(borda.Entraram);

        var saiu = grade.Atualizar(2, GradeDeInteresse.RaioDeSaida + 1, 0);
        Assert.Equal([1L], saiu.Sairam);
        Assert.Empty(grade.Vistos(1));
    }

    [Fact]
    public void Teleporte_para_longe_tira_todos_e_para_perto_traz()
    {
        var grade = new GradeDeInteresse();
        grade.Atualizar(1, 0, 0);
        grade.Atualizar(2, 10, 0);
        grade.Atualizar(3, 1000, 1000);

        var saiu = grade.Atualizar(2, 1000, 1010);
        Assert.Equal([1L], saiu.Sairam);
        Assert.Equal([3L], saiu.Entraram);
    }

    [Fact]
    public void Remover_devolve_quem_via_e_limpa_as_relacoes()
    {
        var grade = new GradeDeInteresse();
        grade.Atualizar(1, 0, 0);
        grade.Atualizar(2, 10, 0);
        grade.Atualizar(3, 2000, 0);

        Assert.Equal([1L], grade.Remover(2));
        Assert.Empty(grade.Vistos(1));
        Assert.Empty(grade.Remover(2));
        Assert.Empty(grade.Remover(3));
    }

    [Fact]
    public void Perto_acha_pelo_raio_atravessando_celulas()
    {
        var grade = new GradeDeInteresse();
        grade.Atualizar(1, 95, 0);
        grade.Atualizar(2, 105, 0);
        grade.Atualizar(3, 400, 0);

        var achados = grade.Perto(100, 0, 20).OrderBy(i => i).ToArray();
        Assert.Equal([1L, 2L], achados);
    }
}

/// <summary>Integração: a área de interesse na rede, com nome e raça nas mensagens.</summary>
public class InteresseNaCidadeTestes(TempoRealFixture f) : IClassFixture<TempoRealFixture>
{
    private static void Mover(
        Infra.ClienteTempoRealDeTeste cliente,
        float x,
        int sequencia,
        float z = float.NaN
    )
    {
        var zFinal = float.IsNaN(z) ? ValidadorDeMovimento.Nascimento.Z : z;
        cliente.Enviar(
            MundoMessageIds.IntencaoDeMovimento,
            new IntencaoDeMovimento
            {
                X = x,
                Y = MapaDoMundo.AlturaEm(x, zFinal),
                Z = zFinal,
                Sequencia = sequencia,
            }
        );
    }

    // Personagem de verdade no banco (nome e raça vêm de lá), autenticado no tempo real.
    private async Task<(
        Infra.ClienteTempoRealDeTeste Cliente,
        PersonagemResumo Personagem
    )> EntrarComRacaAsync(Raca raca)
    {
        using var http = await Infra.ServidorDeTeste.CriarClienteLogadoEmAsync(f.Fabrica);
        var token = http.DefaultRequestHeaders.Authorization!.Parameter!;
        var contaId = long.Parse(new JsonWebToken(token).Subject);
        var criado = await Infra.ServidorDeTeste.PostJsonAsync(
            http,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = Infra.ServidorDeTeste.NomeUnico("P"), Raca = raca }
        );
        Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
        var personagem = await Infra.ServidorDeTeste.LerJsonAsync<PersonagemResumo>(criado);
        f.Verificador.Permitir(contaId, personagem.Id);
        var (cliente, resultado) = await Infra.ClienteTempoRealDeTeste.AutenticarAsync(
            f.Porta,
            token,
            personagem.Id
        );
        Assert.True(resultado.Aceita, resultado.Codigo);
        return (cliente, personagem);
    }

    [Fact]
    public async Task Estado_e_entrada_trazem_nome_e_raca()
    {
        var (a, ana) = await EntrarComRacaAsync(Raca.Aurideo);
        using var _a = a;
        var (b, bia) = await EntrarComRacaAsync(Raca.Silvano);
        using var _b = b;
        a.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        var estadoA = await a.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);
        b.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        var estadoB = await b.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);

        Assert.Equal(ana.Nome, estadoA.Eu.Nome);
        Assert.Equal((int)Raca.Aurideo, estadoA.Eu.Raca);
        var visto = Assert.Single(estadoB.Outros, o => o.PersonagemId == ana.Id);
        Assert.Equal(ana.Nome, visto.Nome);
        Assert.Equal((int)Raca.Aurideo, visto.Raca);

        var entrou = await a.ReceberAsync<JogadorEntrou>(MundoMessageIds.JogadorEntrou);
        Assert.Equal(bia.Id, entrou.Jogador.PersonagemId);
        Assert.Equal(bia.Nome, entrou.Jogador.Nome);
        Assert.Equal((int)Raca.Silvano, entrou.Jogador.Raca);
    }

    [Fact]
    public async Task Quem_esta_longe_nao_aparece_no_estado_nem_recebe_posicao()
    {
        var (a, _, idA) = await f.EntrarAsync();
        using var _a = a;
        var (b, _, idB) = await f.EntrarAsync();
        using var _b = b;
        a.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        await a.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);

        // B sai do raio de A antes de A olhar: A anda para longe e só depois B entra.
        Mover(a, 800, 1);
        await Task.Delay(300);
        b.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        var estadoB = await b.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);
        Assert.DoesNotContain(estadoB.Outros, o => o.PersonagemId == idA);

        // Longe, A anda e B não recebe a posição.
        Mover(a, 810, 2);
        Mover(a, 820, 3);
        await Task.Delay(300);
        Assert.DoesNotContain(MundoMessageIds.PosicaoJogador, b.IdsRecebidos);
        Assert.DoesNotContain(MundoMessageIds.JogadorEntrou, a.IdsRecebidos);
        Assert.NotEqual(idA, idB);
    }

    [Fact]
    public async Task Entrar_e_sair_do_raio_manda_entrou_e_saiu_nos_dois_lados()
    {
        var (a, _, idA) = await f.EntrarAsync();
        using var _a = a;
        var (b, _, idB) = await f.EntrarAsync();
        using var _b = b;
        a.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        await a.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);
        b.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        await b.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);
        await a.ReceberAsync<JogadorEntrou>(MundoMessageIds.JogadorEntrou);

        // B se afasta: os dois deixam de se ver.
        Mover(b, 600, 1);
        var saiuParaA = await a.ReceberAsync<JogadorSaiu>(MundoMessageIds.JogadorSaiu);
        var saiuParaB = await b.ReceberAsync<JogadorSaiu>(MundoMessageIds.JogadorSaiu);
        Assert.Equal(idB, saiuParaA.PersonagemId);
        Assert.Equal(idA, saiuParaB.PersonagemId);

        // Já longe, o que B faz não chega a A.
        var posicoesAntes = a.IdsRecebidos.Count(id => id == MundoMessageIds.PosicaoJogador);
        Mover(b, 610, 2);
        await Task.Delay(300);
        Assert.Equal(
            posicoesAntes,
            a.IdsRecebidos.Count(id => id == MundoMessageIds.PosicaoJogador)
        );

        // B volta: os dois se veem de novo, com nome e raça.
        Mover(b, 20, 3);
        var voltouParaA = await a.ReceberAsync<JogadorEntrou>(MundoMessageIds.JogadorEntrou);
        var voltouParaB = await b.ReceberAsync<JogadorEntrou>(MundoMessageIds.JogadorEntrou);
        Assert.Equal(idB, voltouParaA.Jogador.PersonagemId);
        Assert.Equal(idA, voltouParaB.Jogador.PersonagemId);
    }

    [Fact]
    public async Task Sair_do_jogo_so_avisa_quem_via()
    {
        var (a, _, _) = await f.EntrarAsync();
        using var _a = a;
        var (b, _, _) = await f.EntrarAsync();
        using var _b = b;
        a.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        await a.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);
        Mover(a, 900, 1);
        await Task.Delay(300);
        b.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        await b.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);

        b.Sair();
        await Task.Delay(500);
        Assert.DoesNotContain(MundoMessageIds.JogadorSaiu, a.IdsRecebidos);
    }

    [Fact]
    public async Task Vizinhanca_responde_quem_ve_e_quem_esta_perto()
    {
        var vizinhanca = f.Fabrica.Services.GetRequiredService<IVizinhanca>();
        var (a, _, idA) = await f.EntrarAsync();
        using var _a = a;
        var (b, _, idB) = await f.EntrarAsync();
        using var _b = b;
        a.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        await a.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);
        b.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        await b.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);

        Assert.Contains(idB, vizinhanca.QuemVe(idA));
        var nascimento = ValidadorDeMovimento.Nascimento;
        Assert.Contains(
            idA,
            vizinhanca.PersonagensPerto(AreasDoMundo.Cidade, nascimento.X, nascimento.Z, 5f)
        );

        Mover(b, 700, 1);
        await a.ReceberAsync<JogadorSaiu>(MundoMessageIds.JogadorSaiu);
        Assert.DoesNotContain(idB, vizinhanca.QuemVe(idA));
        Assert.Empty(vizinhanca.QuemVe(long.MaxValue));
    }
}
