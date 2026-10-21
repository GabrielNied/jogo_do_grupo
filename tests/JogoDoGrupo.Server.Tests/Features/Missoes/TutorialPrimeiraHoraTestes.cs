using JogoDoGrupo.Server.Features.Missoes;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Missoes;
using JogoDoGrupo.Shared.Features.Npcs;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Features.Missoes;

/// <summary>
/// Testes do Tutorial da Primeira Hora / Capítulo 1 da História Principal (#328, #1058, #1059, #1060).
/// </summary>
public sealed class TutorialPrimeiraHoraTestes(ServidorDeTeste servidor)
    : IClassFixture<ServidorDeTeste>
{
    private readonly AmbienteDeNegociacao _ambiente = new(servidor);

    private async Task<T> ComServicoAsync<T>(Func<ServicoDeMissoes, Task<T>> acao)
    {
        await using var escopo = servidor.Services.CreateAsyncScope();
        return await acao(escopo.ServiceProvider.GetRequiredService<ServicoDeMissoes>());
    }

    [Fact]
    public async Task PersonagemNovo_ComecaComEtapa1AceitaEAcompanhada()
    {
        var id = await _ambiente.NovoPersonagemAsync();

        var estado = await ComServicoAsync(s => s.GarantirMissaoInicialAsync(id, default));

        Assert.NotNull(estado);
        Assert.Single(estado.Ativas);

        var missao = estado.Ativas[0];
        Assert.Equal(CatalogoDeMissoes.Missao1PortoesDaBrasaMansa, missao.MissaoId);
        Assert.Equal(EstadoDaMissao.EmAndamento, missao.Estado);
        Assert.True(missao.Acompanhada);
        Assert.Single(missao.Objetivos);
        Assert.Equal(0, missao.Objetivos[0].QuantidadeAtual);
        Assert.False(missao.Objetivos[0].Concluido);

        // Repetir a garantia não duplica
        var repetido = await ComServicoAsync(s => s.GarantirMissaoInicialAsync(id, default));
        Assert.Single(repetido.Ativas);
    }

    [Fact]
    public async Task Capitulo1_FluxoCompletoSequencial_EntregaRecompensasEFinaliza()
    {
        var id = await _ambiente.NovoPersonagemAsync();
        var saldoInicial = await _ambiente.SaldoAsync(id);

        // Inicia com Etapa 1
        await ComServicoAsync(s => s.GarantirMissaoInicialAsync(id, default));

        // -----------------------------------------------------------------
        // ETAPA 1: Portões da Brasa Mansa (Alden)
        // -----------------------------------------------------------------
        var at1 = await ComServicoAsync(s =>
            s.RegistrarInteracaoNpcAsync(id, CatalogoNpcs.IdVendedor, default)
        );
        Assert.Single(at1);
        Assert.Equal(EstadoDaMissao.ProntaParaEntregar, at1[0].Estado);

        var (ok1, erro1, conc1, prox1) = await ComServicoAsync(s =>
            s.EntregarMissaoAsync(id, CatalogoDeMissoes.Missao1PortoesDaBrasaMansa, null, default)
        );
        Assert.True(ok1, erro1);
        Assert.NotNull(conc1);
        Assert.Equal(EstadoDaMissao.Concluida, conc1.Estado);
        Assert.NotNull(prox1);
        Assert.Equal(CatalogoDeMissoes.Missao2PrimeiraFerramenta, prox1.MissaoId);
        Assert.Equal(EstadoDaMissao.EmAndamento, prox1.Estado);

        // -----------------------------------------------------------------
        // ETAPA 2: A Primeira Ferramenta (Armeiro Gareth)
        // -----------------------------------------------------------------
        var at2 = await ComServicoAsync(s =>
            s.RegistrarInteracaoNpcAsync(id, CatalogoNpcs.IdArmeiro, default)
        );
        Assert.Single(at2);
        Assert.Equal(EstadoDaMissao.ProntaParaEntregar, at2[0].Estado);

        // Entrega escolhendo Espada de Ferro Curta (173)
        var armaEscolhida = CatalogoEquipamentos.PrimeiroModeloDeArma; // 173
        var (ok2, erro2, conc2, prox2) = await ComServicoAsync(s =>
            s.EntregarMissaoAsync(
                id,
                CatalogoDeMissoes.Missao2PrimeiraFerramenta,
                armaEscolhida,
                default
            )
        );
        Assert.True(ok2, erro2);
        Assert.NotNull(conc2);
        Assert.Equal(EstadoDaMissao.Concluida, conc2.Estado);
        Assert.NotNull(prox2);
        Assert.Equal(CatalogoDeMissoes.Missao3CascaDeFerroECouro, prox2.MissaoId);

        // -----------------------------------------------------------------
        // ETAPA 3: Casca de Ferro e Couro (Ferreiro Brom)
        // -----------------------------------------------------------------
        var at3 = await ComServicoAsync(s =>
            s.RegistrarInteracaoNpcAsync(id, CatalogoNpcs.IdFerreiro, default)
        );
        Assert.Single(at3);
        Assert.Equal(EstadoDaMissao.ProntaParaEntregar, at3[0].Estado);

        var peitoral = CatalogoEquipamentos.PrimeiroModelo; // 101
        var (ok3, erro3, conc3, prox3) = await ComServicoAsync(s =>
            s.EntregarMissaoAsync(
                id,
                CatalogoDeMissoes.Missao3CascaDeFerroECouro,
                peitoral,
                default
            )
        );
        Assert.True(ok3, erro3);
        Assert.NotNull(conc3);
        Assert.NotNull(prox3);
        Assert.Equal(CatalogoDeMissoes.Missao4ProvisoesEGuardaVolumes, prox3.MissaoId);

        // -----------------------------------------------------------------
        // ETAPA 4: Provisões e Guarda-Volumes (Barnabé + Cassian)
        // -----------------------------------------------------------------
        // Falar com Taverneiro Barnabé (Id 6)
        var at4a = await ComServicoAsync(s =>
            s.RegistrarInteracaoNpcAsync(id, CatalogoNpcs.IdTaverneiro, default)
        );
        Assert.Single(at4a);
        Assert.Equal(EstadoDaMissao.EmAndamento, at4a[0].Estado);
        Assert.True(at4a[0].Objetivos[0].Concluido);
        Assert.False(at4a[0].Objetivos[1].Concluido);

        // Falar com Banqueiro Cassian (Id 3)
        var at4b = await ComServicoAsync(s =>
            s.RegistrarInteracaoNpcAsync(id, CatalogoNpcs.IdBanqueiro, default)
        );
        Assert.Single(at4b);
        Assert.Equal(EstadoDaMissao.ProntaParaEntregar, at4b[0].Estado);
        Assert.True(at4b[0].Objetivos[1].Concluido);

        var (ok4, erro4, conc4, prox4) = await ComServicoAsync(s =>
            s.EntregarMissaoAsync(
                id,
                CatalogoDeMissoes.Missao4ProvisoesEGuardaVolumes,
                null,
                default
            )
        );
        Assert.True(ok4, erro4);
        Assert.NotNull(conc4);
        Assert.NotNull(prox4);
        Assert.Equal(CatalogoDeMissoes.Missao5LimpandoATrilhaNorte, prox4.MissaoId);

        // -----------------------------------------------------------------
        // ETAPA 5: Limpando a Trilha Norte (6x Javalis do Campo)
        // -----------------------------------------------------------------
        for (int i = 0; i < 6; i++)
        {
            await ComServicoAsync(s => s.RegistrarMorteInimigoAsync(id, 1, default));
        }

        var (ok5, erro5, conc5, prox5) = await ComServicoAsync(s =>
            s.EntregarMissaoAsync(id, CatalogoDeMissoes.Missao5LimpandoATrilhaNorte, null, default)
        );
        Assert.True(ok5, erro5);
        Assert.NotNull(conc5);
        Assert.NotNull(prox5);
        Assert.Equal(CatalogoDeMissoes.Missao6OEnxameNaMataFria, prox5.MissaoId);

        // -----------------------------------------------------------------
        // ETAPA 6: O Enxame na Mata Fria (5x Lobos do Campo)
        // -----------------------------------------------------------------
        for (int i = 0; i < 5; i++)
        {
            await ComServicoAsync(s => s.RegistrarMorteInimigoAsync(id, 2, default));
        }

        var (ok6, erro6, conc6, prox6) = await ComServicoAsync(s =>
            s.EntregarMissaoAsync(id, CatalogoDeMissoes.Missao6OEnxameNaMataFria, null, default)
        );
        Assert.True(ok6, erro6);
        Assert.NotNull(conc6);
        Assert.NotNull(prox6);
        Assert.Equal(CatalogoDeMissoes.Missao7OSenhorDaColinaDoVigia, prox6.MissaoId);

        // -----------------------------------------------------------------
        // ETAPA 7: O Senhor da Colina do Vigia (1x Presa Velha, Chefe Id 200)
        // -----------------------------------------------------------------
        var at7 = await ComServicoAsync(s => s.RegistrarMorteInimigoAsync(id, 200, default));
        Assert.Single(at7);
        Assert.Equal(EstadoDaMissao.ProntaParaEntregar, at7[0].Estado);

        // Entrega com escolha do item raro Adorno Presa-Velha (200)
        var itemRaro = CatalogoEquipamentos.AdornoPresaVelha; // 200
        var (ok7, erro7, conc7, prox7) = await ComServicoAsync(s =>
            s.EntregarMissaoAsync(
                id,
                CatalogoDeMissoes.Missao7OSenhorDaColinaDoVigia,
                itemRaro,
                default
            )
        );
        Assert.True(ok7, erro7);
        Assert.NotNull(conc7);
        Assert.Equal(EstadoDaMissao.Concluida, conc7.Estado);
        Assert.Null(prox7); // Fim do Capítulo 1!

        // Total de coroas ganhas: 25 + 30 + 35 + 40 + 65 + 85 + 200 = 480
        var saldoFinal = await _ambiente.SaldoAsync(id);
        Assert.Equal(saldoInicial + 480, saldoFinal);

        // Estado final: 7 missões concluídas e nenhuma principal ativa
        var estadoFinal = await ComServicoAsync(s => s.ObterEstadoDasMissoesAsync(id, default));
        Assert.Empty(estadoFinal.Ativas);
        Assert.Equal(7, estadoFinal.Concluidas.Length);
    }

    [Fact]
    public async Task Capitulo1_Concluido_LiberaSecundariasDaRegiao()
    {
        var id = await _ambiente.NovoPersonagemAsync();

        // 1. No nível 1, falha por nível insuficiente
        var (okNivel1, erroNivel1, _) = await ComServicoAsync(s =>
            s.AceitarMissaoAsync(id, CatalogoDeMissoes.Missao101CaldeiraoDaLareira, default)
        );
        Assert.False(okNivel1);
        Assert.Equal(CodigosErroMissoes.NivelInsuficiente, erroNivel1);

        // 2. Com nível 3 mas sem concluir o capítulo 1, falha por pré-requisitos não cumpridos
        await using (var escopo = servidor.Services.CreateAsyncScope())
        {
            await escopo
                .ServiceProvider.GetRequiredService<JogoDoGrupo.Server.Contratos.Personagem.IAjusteDeExperienciaDeTeste>()
                .AjustarAsync(id, 2000, default);
        }

        var (sucessoAntes, erroAntes, _) = await ComServicoAsync(s =>
            s.AceitarMissaoAsync(id, CatalogoDeMissoes.Missao101CaldeiraoDaLareira, default)
        );
        Assert.False(sucessoAntes);
        Assert.Equal(CodigosErroMissoes.PreRequisitosNaoCumpridos, erroAntes);

        // Conclui todas as 7 etapas
        await ComServicoAsync(s => s.GarantirMissaoInicialAsync(id, default));
        await ComServicoAsync(s =>
            s.RegistrarInteracaoNpcAsync(id, CatalogoNpcs.IdVendedor, default)
        );
        await ComServicoAsync(s =>
            s.EntregarMissaoAsync(id, CatalogoDeMissoes.Missao1PortoesDaBrasaMansa, null, default)
        );

        await ComServicoAsync(s =>
            s.RegistrarInteracaoNpcAsync(id, CatalogoNpcs.IdArmeiro, default)
        );
        await ComServicoAsync(s =>
            s.EntregarMissaoAsync(
                id,
                CatalogoDeMissoes.Missao2PrimeiraFerramenta,
                CatalogoEquipamentos.PrimeiroModeloDeArma,
                default
            )
        );

        await ComServicoAsync(s =>
            s.RegistrarInteracaoNpcAsync(id, CatalogoNpcs.IdFerreiro, default)
        );
        await ComServicoAsync(s =>
            s.EntregarMissaoAsync(
                id,
                CatalogoDeMissoes.Missao3CascaDeFerroECouro,
                CatalogoEquipamentos.PrimeiroModelo,
                default
            )
        );

        await ComServicoAsync(s =>
            s.RegistrarInteracaoNpcAsync(id, CatalogoNpcs.IdTaverneiro, default)
        );
        await ComServicoAsync(s =>
            s.RegistrarInteracaoNpcAsync(id, CatalogoNpcs.IdBanqueiro, default)
        );
        await ComServicoAsync(s =>
            s.EntregarMissaoAsync(
                id,
                CatalogoDeMissoes.Missao4ProvisoesEGuardaVolumes,
                null,
                default
            )
        );

        for (int i = 0; i < 6; i++)
            await ComServicoAsync(s => s.RegistrarMorteInimigoAsync(id, 1, default));
        await ComServicoAsync(s =>
            s.EntregarMissaoAsync(id, CatalogoDeMissoes.Missao5LimpandoATrilhaNorte, null, default)
        );

        for (int i = 0; i < 5; i++)
            await ComServicoAsync(s => s.RegistrarMorteInimigoAsync(id, 2, default));
        await ComServicoAsync(s =>
            s.EntregarMissaoAsync(id, CatalogoDeMissoes.Missao6OEnxameNaMataFria, null, default)
        );

        await ComServicoAsync(s => s.RegistrarMorteInimigoAsync(id, 200, default));
        await ComServicoAsync(s =>
            s.EntregarMissaoAsync(
                id,
                CatalogoDeMissoes.Missao7OSenhorDaColinaDoVigia,
                CatalogoEquipamentos.AdornoPresaVelha,
                default
            )
        );

        // Como as 7 missões deram mais de 2000 XP acumulado, o personagem atinge nível >= 3.
        // Agora tenta aceitar as secundárias desbloqueadas
        var (ok101, erro101, missao101) = await ComServicoAsync(s =>
            s.AceitarMissaoAsync(id, CatalogoDeMissoes.Missao101CaldeiraoDaLareira, default)
        );
        Assert.True(ok101, erro101);
        Assert.NotNull(missao101);
        Assert.Equal(CatalogoDeMissoes.Missao101CaldeiraoDaLareira, missao101.MissaoId);

        var (ok102, erro102, missao102) = await ComServicoAsync(s =>
            s.AceitarMissaoAsync(id, CatalogoDeMissoes.Missao102MinerioParaABigorna, default)
        );
        Assert.True(ok102, erro102);
        Assert.NotNull(missao102);

        var (ok105, erro105, missao105) = await ComServicoAsync(s =>
            s.AceitarMissaoAsync(id, CatalogoDeMissoes.Missao105VigilanciaNoPortaoLeste, default)
        );
        Assert.True(ok105, erro105);
        Assert.NotNull(missao105);
    }

    [Fact]
    public async Task RepetirEntrega_NaoDaRecompensaDuplicada()
    {
        var id = await _ambiente.NovoPersonagemAsync();
        await ComServicoAsync(s => s.GarantirMissaoInicialAsync(id, default));
        await ComServicoAsync(s =>
            s.RegistrarInteracaoNpcAsync(id, CatalogoNpcs.IdVendedor, default)
        );

        var saldoAntes = await _ambiente.SaldoAsync(id);

        var (ok1, erro1, _, _) = await ComServicoAsync(s =>
            s.EntregarMissaoAsync(id, CatalogoDeMissoes.Missao1PortoesDaBrasaMansa, null, default)
        );
        Assert.True(ok1, erro1);

        var saldoAposPrimeira = await _ambiente.SaldoAsync(id);
        Assert.Equal(saldoAntes + 25, saldoAposPrimeira);

        // Tentar entregar novamente a mesma missão concluída
        var (ok2, erro2, _, _) = await ComServicoAsync(s =>
            s.EntregarMissaoAsync(id, CatalogoDeMissoes.Missao1PortoesDaBrasaMansa, null, default)
        );
        Assert.False(ok2);
        Assert.Equal(CodigosErroMissoes.MissaoNaoEncontrada, erro2);

        var saldoAposRepetir = await _ambiente.SaldoAsync(id);
        Assert.Equal(saldoAposPrimeira, saldoAposRepetir);
    }

    [Fact]
    public async Task MissaoPrincipal_NaoPermiteAbandono()
    {
        var id = await _ambiente.NovoPersonagemAsync();
        await ComServicoAsync(s => s.GarantirMissaoInicialAsync(id, default));

        var (sucesso, erro) = await ComServicoAsync(s =>
            s.AbandonarMissaoAsync(id, CatalogoDeMissoes.Missao1PortoesDaBrasaMansa, default)
        );

        Assert.False(sucesso);
        Assert.Equal(CodigosErroMissoes.MissaoPrincipalNaoAbandona, erro);
    }

    [Fact]
    public async Task EntregaComItemDeEscolhaInvalido_EhRecusada()
    {
        var id = await _ambiente.NovoPersonagemAsync();
        await ComServicoAsync(s => s.GarantirMissaoInicialAsync(id, default));
        await ComServicoAsync(s =>
            s.RegistrarInteracaoNpcAsync(id, CatalogoNpcs.IdVendedor, default)
        );
        await ComServicoAsync(s =>
            s.EntregarMissaoAsync(id, CatalogoDeMissoes.Missao1PortoesDaBrasaMansa, null, default)
        );

        // Missão 2 exige escolher uma das 3 armas (173, 174, 175)
        await ComServicoAsync(s =>
            s.RegistrarInteracaoNpcAsync(id, CatalogoNpcs.IdArmeiro, default)
        );

        // Tenta entregar sem item de escolha
        var (okNulo, erroNulo, _, _) = await ComServicoAsync(s =>
            s.EntregarMissaoAsync(id, CatalogoDeMissoes.Missao2PrimeiraFerramenta, null, default)
        );
        Assert.False(okNulo);
        Assert.Equal(CodigosErroMissoes.ItemEscolhaInvalido, erroNulo);

        // Tenta entregar com item fora da lista permitida
        var (okInvalido, erroInvalido, _, _) = await ComServicoAsync(s =>
            s.EntregarMissaoAsync(id, CatalogoDeMissoes.Missao2PrimeiraFerramenta, 999, default)
        );
        Assert.False(okInvalido);
        Assert.Equal(CodigosErroMissoes.ItemEscolhaInvalido, erroInvalido);
    }
}
