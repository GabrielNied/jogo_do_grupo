using System.Collections.Concurrent;
using System.Net;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Features.Combate;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Itens;
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

/// <summary>
/// O que o equipamento dá ao combate (já convertido, como o contrato de Itens entrega) e o bônus de
/// efeito, que o teste controla.
/// </summary>
public sealed class BonusFalso : IBonusDeAtributos, IAtributosDosEquipamentos
{
    public volatile AtributosDosEquipamentos Equipamentos = new();
    public volatile AtributosCombate Efeitos = new();

    /// <summary>Atraso de cada leitura de equipamentos: simula um banco lento.</summary>
    public volatile int AtrasoMs;

    private int _leituras;

    /// <summary>Quantas vezes os equipamentos foram lidos (uma ida ao banco por golpe).</summary>
    public int Leituras => Volatile.Read(ref _leituras);

    /// <summary>Quando (<see cref="Environment.TickCount64"/>) começou cada leitura de equipamentos.</summary>
    public ConcurrentQueue<long> InicioDasLeituras { get; } = new();

    public async Task<AtributosDosEquipamentos> ObterAsync(long personagemId, CancellationToken c)
    {
        Interlocked.Increment(ref _leituras);
        InicioDasLeituras.Enqueue(Environment.TickCount64);
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
                s.AddSingleton<IAtributosDosEquipamentos>(Bonus);
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
        servidor.Bonus.Equipamentos = new AtributosDosEquipamentos();
        servidor.Bonus.Efeitos = new AtributosCombate();
        var cliente = await ServidorDeTeste.CriarClienteLogadoEmAsync(servidor.Falso);
        return (cliente, await CriarPersonagem(cliente));
    }

    /// <summary>Muda o que está guardado, como um gasto de recurso faria.</summary>
    private async Task Gastar(long id, int? vida = null, int? stamina = null, int? mana = null)
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
        if (stamina != null)
        {
            r.Stamina = stamina.Value;
            r.UltimoGastoStaminaEm = agora;
            r.StaminaContadaAte = agora;
        }
        if (mana != null)
        {
            r.Mana = mana.Value;
            r.UltimoGastoManaEm = agora;
            r.ManaContadaAte = agora;
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
        Assert.Equal(RegrasRecursos.StaminaBase, r.StaminaMaxima);
        Assert.Equal(r.StaminaMaxima, r.StaminaAtual);
        Assert.Equal(RegrasRecursos.ManaBase, r.ManaMaxima);
        Assert.Equal(r.ManaMaxima, r.ManaAtual);
        Assert.Equal(0, r.Atributos.Forca + r.Atributos.Vigor + r.Atributos.Espirito);
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
    public async Task Atributos_do_personagem_somam_so_os_efeitos_e_o_equipamento_vai_a_parte()
    {
        var (cliente, id) = await NovoPersonagem();
        // O equipamento dá vida direto; mana máxima vem só do personagem (#709).
        servidor.Bonus.Equipamentos = new AtributosDosEquipamentos { VidaMaxima = 100 };
        servidor.Bonus.Efeitos = new AtributosCombate { Vigor = 2 };

        var r = await Recursos(cliente, id);
        var outra = await Recursos(cliente, id);

        Assert.Equal(2, r.Atributos.Vigor);
        Assert.Equal(0, r.Atributos.Espirito);
        Assert.Equal(620, r.VidaMaxima);
        Assert.Equal(RegrasRecursos.ManaBase, r.ManaMaxima);
        Assert.Equal(r.Atributos.Vigor, outra.Atributos.Vigor);
        Assert.Equal(r.VidaMaxima, outra.VidaMaxima);
    }

    [Fact]
    public async Task Stamina_maxima_vem_so_do_personagem_mesmo_com_bonus_de_equipamento()
    {
        var (cliente, id) = await NovoPersonagem();
        servidor.Bonus.Equipamentos = new AtributosDosEquipamentos();
        servidor.Bonus.Efeitos = new AtributosCombate { Agilidade = 5 };

        var r = await Recursos(cliente, id);

        Assert.Equal(5, r.Atributos.Agilidade);
        Assert.Equal(RegrasRecursos.StaminaMaxima(5), r.StaminaMaxima);
        Assert.Equal(r.StaminaMaxima, r.StaminaAtual);
    }

    [Fact]
    public async Task Equipar_e_desequipar_nao_duplica_o_que_o_equipamento_da()
    {
        var (cliente, id) = await NovoPersonagem();
        var bonus = new AtributosDosEquipamentos { VidaMaxima = 100 };

        servidor.Bonus.Equipamentos = bonus;
        Assert.Equal(600, (await Recursos(cliente, id)).VidaMaxima);
        servidor.Bonus.Equipamentos = new AtributosDosEquipamentos();
        Assert.Equal(500, (await Recursos(cliente, id)).VidaMaxima);
        servidor.Bonus.Equipamentos = bonus;
        Assert.Equal(600, (await Recursos(cliente, id)).VidaMaxima);
    }

    [Fact]
    public async Task Maximo_reduzido_limita_o_atual_e_nao_cura()
    {
        var (cliente, id) = await NovoPersonagem();
        servidor.Bonus.Equipamentos = new AtributosDosEquipamentos { VidaMaxima = 100 };
        var cheio = await Recursos(cliente, id);
        Assert.Equal(600, cheio.VidaAtual);
        Assert.Equal(RegrasRecursos.ManaBase, cheio.ManaAtual);

        servidor.Bonus.Equipamentos = new AtributosDosEquipamentos();
        var reduzido = await Recursos(cliente, id);
        Assert.Equal(500, reduzido.VidaAtual);
        Assert.Equal(100, reduzido.ManaAtual);

        // Vida abaixo do máximo não sobe quando o máximo aumenta.
        await Gastar(id, vida: 300);
        servidor.Bonus.Equipamentos = new AtributosDosEquipamentos { VidaMaxima = 100 };
        var depois = await Recursos(cliente, id);
        Assert.Equal(300, depois.VidaAtual);
        Assert.Equal(600, depois.VidaMaxima);
    }

    [Fact]
    public async Task Stamina_so_regenera_depois_de_2_segundos_do_gasto()
    {
        var (cliente, id) = await NovoPersonagem();
        await Recursos(cliente, id);
        await Gastar(id, stamina: 40);

        servidor.Relogio.Avancar(1.5);
        Assert.Equal(40, (await Recursos(cliente, id)).StaminaAtual);

        servidor.Relogio.Avancar(0.5); // 2,0 s: o atraso acabou, ainda não passou nada dele
        Assert.Equal(40, (await Recursos(cliente, id)).StaminaAtual);

        servidor.Relogio.Avancar(1.0); // 1 s depois do atraso: +10
        Assert.Equal(50, (await Recursos(cliente, id)).StaminaAtual);
    }

    [Fact]
    public async Task Mana_regenera_5_por_segundo_e_nunca_passa_do_maximo()
    {
        var (cliente, id) = await NovoPersonagem();
        await Recursos(cliente, id);
        await Gastar(id, mana: 0);

        servidor.Relogio.Avancar(4.0); // 2 s pagos: +10
        Assert.Equal(10, (await Recursos(cliente, id)).ManaAtual);

        servidor.Relogio.Avancar(30);
        var r = await Recursos(cliente, id);
        Assert.Equal(r.ManaMaxima, r.ManaAtual);
    }

    [Fact]
    public async Task Consultas_frequentes_nao_perdem_a_fracao_da_regeneracao()
    {
        var (cliente, id) = await NovoPersonagem();
        await Recursos(cliente, id);
        await Gastar(id, stamina: 0);
        servidor.Relogio.Avancar(2.0);

        // 20 consultas de 0,05 s (1 s no total): tem que dar os 10 pontos de 1 s.
        for (var i = 0; i < 20; i++)
        {
            servidor.Relogio.Avancar(0.05);
            await Recursos(cliente, id);
        }

        Assert.Equal(10, (await Recursos(cliente, id)).StaminaAtual);
    }

    [Fact]
    public async Task Recurso_guardado_negativo_ou_acima_do_maximo_volta_para_os_limites()
    {
        var (cliente, id) = await NovoPersonagem();
        await Recursos(cliente, id);
        await Gastar(id, vida: -50, stamina: -10, mana: 9999);

        var r = await Recursos(cliente, id);

        Assert.Equal(0, r.VidaAtual);
        Assert.True(r.StaminaAtual >= 0);
        Assert.Equal(r.ManaMaxima, r.ManaAtual);
    }

    [Fact]
    public async Task Consultas_simultaneas_nao_pagam_a_regeneracao_duas_vezes()
    {
        var (cliente, id) = await NovoPersonagem();
        await Recursos(cliente, id);
        await Gastar(id, stamina: 0);
        servidor.Relogio.Avancar(5.0); // 3 s pagos: +30

        var respostas = await Task.WhenAll(
            Enumerable.Range(0, 10).Select(_ => Recursos(cliente, id))
        );

        Assert.All(respostas, r => Assert.Equal(30, r.StaminaAtual));
        Assert.Equal(30, (await Recursos(cliente, id)).StaminaAtual);
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
