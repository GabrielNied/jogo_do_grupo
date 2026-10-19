using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Personagem;
using JogoDoGrupo.Shared.Features.Profissoes;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Features.Profissoes;

/// <summary>De ponta a ponta (#192): aprender, coletar, produzir e achar o item na bolsa.</summary>
public sealed class ProfissoesTempoRealTestes(ServidorDeTeste servidor)
    : IClassFixture<ServidorDeTeste>
{
    private readonly AmbienteDeNegociacao _ambiente = new(servidor);

    private async Task<(ClienteTempoRealDeTeste Cliente, long Id)> EntrarAsync()
    {
        var http = await servidor.CriarClienteLogadoAsync();
        var resposta = await ServidorDeTeste.PostJsonAsync(
            http,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest
            {
                Nome = ServidorDeTeste.NomeUnico("P"),
                Raca = Raca.Valdren,
            }
        );
        resposta.EnsureSuccessStatusCode();
        var id = (await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(resposta)).Id;
        var porta = servidor.Services.GetRequiredService<ServidorTempoReal>().Porta;
        var token = http.DefaultRequestHeaders.Authorization!.Parameter!;
        var (cliente, resultado) = await ClienteTempoRealDeTeste.AutenticarAsync(porta, token, id);
        Assert.True(resultado.Aceita, resultado.Codigo);
        return (cliente, id);
    }

    [Fact]
    public async Task Do_ponto_de_coleta_a_espada_na_bolsa()
    {
        var (jogador, id) = await EntrarAsync();
        using var _ = jogador;
        foreach (
            var profissao in new[] { Profissao.Minerador, Profissao.Lenhador, Profissao.Ferreiro }
        )
        {
            jogador.Enviar(
                ProfissoesMessageIds.AprenderProfissao,
                new AprenderProfissao { Profissao = profissao }
            );
            await jogador.ReceberAsync<EstadoDasProfissoes>(
                ProfissoesMessageIds.EstadoDasProfissoes
            );
        }

        foreach (
            var no in new[]
            {
                ConfigProfissoes.NoVeioDeFerro1,
                ConfigProfissoes.NoVeioDeFerro2,
                ConfigProfissoes.NoArvore1,
            }
        )
        {
            jogador.Enviar(ProfissoesMessageIds.ColetarNo, new ColetarNo { NoId = no });
            var coleta = await jogador.ReceberAsync<ColetaConcluida>(
                ProfissoesMessageIds.ColetaConcluida
            );
            Assert.Equal(no, coleta.NoId);
        }

        jogador.Enviar(
            ProfissoesMessageIds.ProduzirReceita,
            new ProduzirReceita { ReceitaId = ConfigProfissoes.ReceitaEspada }
        );
        var producao = await jogador.ReceberAsync<ProducaoConcluida>(
            ProfissoesMessageIds.ProducaoConcluida
        );

        Assert.Equal(173, producao.ModeloId);
        Assert.Equal(173, (await _ambiente.BolsaAsync(id)).Single().Equipamento!.ModeloId);
    }

    [Fact]
    public async Task Pedido_invalido_recebe_a_recusa_com_o_motivo()
    {
        var (jogador, _) = await EntrarAsync();
        using var _j = jogador;

        jogador.Enviar(
            ProfissoesMessageIds.ProduzirReceita,
            new ProduzirReceita { ReceitaId = 999 }
        );

        var recusa = await jogador.ReceberAsync<ProfissoesRecusada>(
            ProfissoesMessageIds.ProfissoesRecusada
        );
        Assert.Equal(CodigosErroProfissoes.ReceitaInexistente, recusa.Codigo);
    }
}
