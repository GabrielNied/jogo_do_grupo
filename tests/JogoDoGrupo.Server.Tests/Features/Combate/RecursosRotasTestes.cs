using System.Net;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Features.Combate;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Combate;

/// <summary>Relógio de mentira: só anda quando o teste manda.</summary>
public sealed class RelogioFalso : TimeProvider
{
    private long _ticks = DateTimeOffset.UtcNow.UtcTicks;

    public override DateTimeOffset GetUtcNow() => new(Interlocked.Read(ref _ticks), TimeSpan.Zero);

    /// <summary>Volta ao horário real, para o token do login nascer no presente.</summary>
    public void Reiniciar() => Interlocked.Exchange(ref _ticks, DateTimeOffset.UtcNow.UtcTicks);

    public void Avancar(double segundos) =>
        Interlocked.Add(ref _ticks, TimeSpan.FromSeconds(segundos).Ticks);
}

/// <summary>Bônus de equipamento e de efeito que o teste controla.</summary>
public sealed class BonusFalso : IBonusDeAtributos
{
    public volatile AtributosCombate Equipamentos = new();
    public volatile AtributosCombate Efeitos = new();

    /// <summary>Atraso de cada leitura de equipamentos: simula um banco lento.</summary>
    public volatile int AtrasoMs;

    private int _leituras;

    /// <summary>Quantas vezes os equipamentos foram lidos (uma ida ao banco por golpe).</summary>
    public int Leituras => Volatile.Read(ref _leituras);

    public async Task<AtributosCombate> EquipamentosAsync(long personagemId, CancellationToken c)
    {
        Interlocked.Increment(ref _leituras);
        if (AtrasoMs > 0)
        {
            await Task.Delay(AtrasoMs, c);
        }

        return Equipamentos;
    }

    public Task<AtributosCombate> EfeitosAsync(long personagemId, CancellationToken c) =>
        Task.FromResult(Efeitos);
}

/// <summary>Servidor de teste com relógio e bônus falsos, sobre o mesmo banco.</summary>
public sealed class ServidorDeCombate : IDisposable
{
    public ServidorDeTeste Real { get; } = new();
    public RelogioFalso Relogio { get; } = new();
    public BonusFalso Bonus { get; } = new();
    public WebApplicationFactory<Program> Falso { get; }

    public ServidorDeCombate()
    {
        Falso = Real.WithWebHostBuilder(b =>
            b.ConfigureServices(s =>
            {
                s.AddSingleton<TimeProvider>(Relogio);
                s.AddSingleton<IBonusDeAtributos>(Bonus);
            })
        );
    }

    public void Dispose()
    {
        Falso.Dispose();
        Real.Dispose();
    }
}

public class RecursosRotasTestes(ServidorDeCombate servidor) : IClassFixture<ServidorDeCombate>
{
    private static async Task<long> CriarPersonagem(HttpClient cliente)
    {
        var resposta = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = ServidorDeTeste.NomeUnico(), Raca = Raca.Valdren }
        );
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(resposta)).Id;
    }

    private static async Task<RecursosResponse> Recursos(HttpClient cliente, long id)
    {
        var resposta = await cliente.GetAsync(RotasCombate.RecursosDe(id));
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return await ServidorDeTeste.LerJsonAsync<RecursosResponse>(resposta);
    }

    private static async Task AssertErro(
        HttpResponseMessage resposta,
        HttpStatusCode status,
        string codigo
    )
    {
        Assert.Equal(status, resposta.StatusCode);
        var erro = await ServidorDeTeste.LerJsonAsync<ErroApi>(resposta);
        Assert.Equal(codigo, erro.Codigo);
    }

    private async Task<(HttpClient Cliente, long Id)> NovoPersonagem()
    {
        servidor.Relogio.Reiniciar();
        servidor.Bonus.Equipamentos = new AtributosCombate();
        servidor.Bonus.Efeitos = new AtributosCombate();
        var cliente = await ServidorDeTeste.CriarClienteLogadoEmAsync(servidor.Falso);
        return (cliente, await CriarPersonagem(cliente));
    }

    /// <summary>Muda o que está guardado, como um gasto de recurso faria.</summary>
    private async Task Gastar(long id, int? vida = null, int? vigor = null, int? energia = null)
    {
        var memoria = servidor.Falso.Services.GetRequiredService<RecursosEmMemoria>();
        await memoria.GravarAsync(id); // O banco precisa ter o que a memória já mudou.
        using var escopo = servidor.Falso.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var r = await db.Set<RecursosPersonagem>().SingleAsync(x => x.PersonagemId == id);
        var agora = servidor.Relogio.GetUtcNow().UtcDateTime;
        if (vida != null)
        {
            r.Vida = vida.Value;
        }
        if (vigor != null)
        {
            r.Vigor = vigor.Value;
            r.UltimoGastoVigorEm = agora;
            r.VigorContadoAte = agora;
        }
        if (energia != null)
        {
            r.Energia = energia.Value;
            r.UltimoGastoEnergiaEm = agora;
            r.EnergiaContadaAte = agora;
        }
        await db.SaveChangesAsync();
        memoria.Descartar(id); // E a memória precisa reler o que o teste mudou.
    }

    [Fact]
    public async Task Personagem_novo_vem_com_tudo_cheio()
    {
        var (cliente, id) = await NovoPersonagem();

        var r = await Recursos(cliente, id);

        Assert.Equal(RegrasRecursos.VidaBase, r.VidaMaxima);
        Assert.Equal(r.VidaMaxima, r.VidaAtual);
        Assert.Equal(RegrasRecursos.VigorBase, r.VigorMaximo);
        Assert.Equal(r.VigorMaximo, r.VigorAtual);
        Assert.Equal(RegrasRecursos.EnergiaBase, r.EnergiaMaxima);
        Assert.Equal(r.EnergiaMaxima, r.EnergiaAtual);
        Assert.Equal(0, r.Atributos.Forca + r.Atributos.Vitalidade + r.Atributos.Espirito);
    }

    [Fact]
    public async Task Sem_token_responde_401()
    {
        var anonimo = servidor.Falso.CreateClient();

        var resposta = await anonimo.GetAsync(RotasCombate.RecursosDe(1));

        await AssertErro(resposta, HttpStatusCode.Unauthorized, CodigosErro.NaoAutenticado);
    }

    [Fact]
    public async Task Personagem_inexistente_ou_de_outra_conta_responde_404()
    {
        var (_, idAlheio) = await NovoPersonagem();
        var (cliente, _) = await NovoPersonagem();

        await AssertErro(
            await cliente.GetAsync(RotasCombate.RecursosDe(idAlheio)),
            HttpStatusCode.NotFound,
            CodigosErroCombate.PersonagemNaoEncontrado
        );
        await AssertErro(
            await cliente.GetAsync(RotasCombate.RecursosDe(999999999)),
            HttpStatusCode.NotFound,
            CodigosErroCombate.PersonagemNaoEncontrado
        );
        await AssertErro(
            await cliente.GetAsync("/api/personagens/abc/recursos"),
            HttpStatusCode.NotFound,
            CodigosErroCombate.PersonagemNaoEncontrado
        );
    }

    [Fact]
    public async Task Atributos_somam_equipamentos_e_efeitos_uma_vez_so()
    {
        var (cliente, id) = await NovoPersonagem();
        servidor.Bonus.Equipamentos = new AtributosCombate { Vitalidade = 10, Espirito = 5 };
        servidor.Bonus.Efeitos = new AtributosCombate { Vitalidade = 2 };

        var r = await Recursos(cliente, id);
        var outra = await Recursos(cliente, id);

        Assert.Equal(12, r.Atributos.Vitalidade);
        Assert.Equal(620, r.VidaMaxima);
        Assert.Equal(110, r.EnergiaMaxima);
        Assert.Equal(r.Atributos.Vitalidade, outra.Atributos.Vitalidade);
        Assert.Equal(r.VidaMaxima, outra.VidaMaxima);
    }

    [Fact]
    public async Task Equipar_e_desequipar_nao_duplica_bonus()
    {
        var (cliente, id) = await NovoPersonagem();
        var bonus = new AtributosCombate { Vitalidade = 10 };

        servidor.Bonus.Equipamentos = bonus;
        Assert.Equal(600, (await Recursos(cliente, id)).VidaMaxima);
        servidor.Bonus.Equipamentos = new AtributosCombate();
        Assert.Equal(500, (await Recursos(cliente, id)).VidaMaxima);
        servidor.Bonus.Equipamentos = bonus;
        Assert.Equal(600, (await Recursos(cliente, id)).VidaMaxima);
    }

    [Fact]
    public async Task Maximo_reduzido_limita_o_atual_e_nao_cura()
    {
        var (cliente, id) = await NovoPersonagem();
        servidor.Bonus.Equipamentos = new AtributosCombate { Vitalidade = 10, Espirito = 10 };
        var cheio = await Recursos(cliente, id);
        Assert.Equal(600, cheio.VidaAtual);
        Assert.Equal(120, cheio.EnergiaAtual);

        servidor.Bonus.Equipamentos = new AtributosCombate();
        var reduzido = await Recursos(cliente, id);
        Assert.Equal(500, reduzido.VidaAtual);
        Assert.Equal(100, reduzido.EnergiaAtual);

        // Vida abaixo do máximo não sobe quando o máximo aumenta.
        await Gastar(id, vida: 300);
        servidor.Bonus.Equipamentos = new AtributosCombate { Vitalidade = 10 };
        var depois = await Recursos(cliente, id);
        Assert.Equal(300, depois.VidaAtual);
        Assert.Equal(600, depois.VidaMaxima);
    }

    [Fact]
    public async Task Vigor_so_regenera_depois_de_2_segundos_do_gasto()
    {
        var (cliente, id) = await NovoPersonagem();
        await Recursos(cliente, id);
        await Gastar(id, vigor: 40);

        servidor.Relogio.Avancar(1.5);
        Assert.Equal(40, (await Recursos(cliente, id)).VigorAtual);

        servidor.Relogio.Avancar(0.5); // 2,0 s: o atraso acabou, ainda não passou nada dele
        Assert.Equal(40, (await Recursos(cliente, id)).VigorAtual);

        servidor.Relogio.Avancar(1.0); // 1 s depois do atraso: +10
        Assert.Equal(50, (await Recursos(cliente, id)).VigorAtual);
    }

    [Fact]
    public async Task Energia_regenera_5_por_segundo_e_nunca_passa_do_maximo()
    {
        var (cliente, id) = await NovoPersonagem();
        await Recursos(cliente, id);
        await Gastar(id, energia: 0);

        servidor.Relogio.Avancar(4.0); // 2 s pagos: +10
        Assert.Equal(10, (await Recursos(cliente, id)).EnergiaAtual);

        servidor.Relogio.Avancar(30);
        var r = await Recursos(cliente, id);
        Assert.Equal(r.EnergiaMaxima, r.EnergiaAtual);
    }

    [Fact]
    public async Task Consultas_frequentes_nao_perdem_a_fracao_da_regeneracao()
    {
        var (cliente, id) = await NovoPersonagem();
        await Recursos(cliente, id);
        await Gastar(id, vigor: 0);
        servidor.Relogio.Avancar(2.0);

        // 20 consultas de 0,05 s (1 s no total): tem que dar os 10 pontos de 1 s.
        for (var i = 0; i < 20; i++)
        {
            servidor.Relogio.Avancar(0.05);
            await Recursos(cliente, id);
        }

        Assert.Equal(10, (await Recursos(cliente, id)).VigorAtual);
    }

    [Fact]
    public async Task Recurso_guardado_negativo_ou_acima_do_maximo_volta_para_os_limites()
    {
        var (cliente, id) = await NovoPersonagem();
        await Recursos(cliente, id);
        await Gastar(id, vida: -50, vigor: -10, energia: 9999);

        var r = await Recursos(cliente, id);

        Assert.Equal(0, r.VidaAtual);
        Assert.True(r.VigorAtual >= 0);
        Assert.Equal(r.EnergiaMaxima, r.EnergiaAtual);
    }

    [Fact]
    public async Task Consultas_simultaneas_nao_pagam_a_regeneracao_duas_vezes()
    {
        var (cliente, id) = await NovoPersonagem();
        await Recursos(cliente, id);
        await Gastar(id, vigor: 0);
        servidor.Relogio.Avancar(5.0); // 3 s pagos: +30

        var respostas = await Task.WhenAll(
            Enumerable.Range(0, 10).Select(_ => Recursos(cliente, id))
        );

        Assert.All(respostas, r => Assert.Equal(30, r.VigorAtual));
        Assert.Equal(30, (await Recursos(cliente, id)).VigorAtual);
    }

    [Fact]
    public async Task Primeiras_consultas_simultaneas_criam_uma_linha_so()
    {
        var (cliente, id) = await NovoPersonagem();

        var respostas = await Task.WhenAll(
            Enumerable.Range(0, 10).Select(_ => Recursos(cliente, id))
        );

        Assert.All(respostas, r => Assert.Equal(RegrasRecursos.VidaBase, r.VidaAtual));
        using var escopo = servidor.Falso.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        Assert.Equal(1, await db.Set<RecursosPersonagem>().CountAsync(x => x.PersonagemId == id));
    }
}
