using System;
using System.Collections.Generic;
using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Features.Combate;
using JogoDoGrupo.Server.Tests.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Itens;
using Microsoft.Extensions.Logging.Abstractions;

namespace JogoDoGrupo.Server.Tests.Features.Combate;

[Collection("TempoRealSerial")]
public class ProjeteisElementaisTestes(TempoRealFixture f) : IClassFixture<TempoRealFixture>
{
    private readonly ServidorTempoReal _tempoReal = f.TempoReal;

    private sealed class AlvoLocal : IAlvosDeCombate
    {
        public long Id { get; }
        public long PersonagemId { get; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double Raio { get; set; } = 0.5;
        public double Altura { get; set; } = 2.0;
        public int DanoRecebido { get; private set; }
        public int Aplicacoes { get; private set; }

        public AlvoLocal(long id, double x, double y, double z, long personagemId = 0)
        {
            Id = id;
            PersonagemId = personagemId;
            X = x;
            Y = y;
            Z = z;
        }

        public IReadOnlyList<AlvoDeCombate> AlvosNoVolume(
            string area,
            double origemX,
            double origemZ,
            double direcaoYGraus,
            long instanteReferenciaMs = 0
        ) => AlvosProximos(area, origemX, origemZ, 50, instanteReferenciaMs);

        public IReadOnlyList<AlvoDeCombate> AlvosProximos(
            string area,
            double origemX,
            double origemZ,
            double alcanceMetros,
            long instanteReferenciaMs = 0
        )
        {
            double dx = X - origemX;
            double dz = Z - origemZ;
            double dist = Math.Sqrt(dx * dx + dz * dz);
            if (dist <= alcanceMetros + Raio)
            {
                return [new AlvoDeCombate(Id, X, Z, Raio, 100, 100, PersonagemId, Altura)];
            }
            return [];
        }

        public bool TentarAplicarDano(
            long alvoId,
            int dano,
            long atacanteId,
            out ResultadoDeDano resultado
        )
        {
            resultado = default;
            if (alvoId != Id)
            {
                return false;
            }

            DanoRecebido += dano;
            Aplicacoes++;
            resultado = new ResultadoDeDano(Id, dano, Math.Max(0, 100 - DanoRecebido), 100);
            return true;
        }

        public bool Interromper(long alvoId, int duracaoMs) => true;
    }

    private sealed class AliadosLocais : IAliadosDeCombate
    {
        public HashSet<long> Aliados { get; } = [];

        public bool SaoAliados(long personagemA, long personagemB) =>
            personagemA == personagemB
            || (Aliados.Contains(personagemA) && Aliados.Contains(personagemB));
    }

    [Fact]
    public void DispararCajado_seleciona_projetil_conforme_elemento()
    {
        var posicoes = new PosicoesFalsas();
        posicoes.Colocar(1, "cidade", 0f, 0f);

        long agora = 1000;
        using var servico = new ServicoDeProjeteis(
            _tempoReal,
            posicoes,
            [],
            NullLogger<ServicoDeProjeteis>.Instance,
            relogioMs: () => agora
        );

        var parcelas = new ParcelasDeDano { DanoBase = 40, Magico = true };
        var acao = new AcaoDeAtaque(
            1,
            new PedidoDeAtaque(TipoDeAtaque.Leve, 0f, 1, agora),
            agora,
            0
        );

        long idFogo = servico.DispararCajado(1, acao, parcelas, ElementoDaArma.Fogo);
        Assert.True(idFogo > 0);

        long idGelo = servico.DispararCajado(1, acao, parcelas, ElementoDaArma.Gelo);
        Assert.True(idGelo > 0);

        long idCura = servico.DispararCajado(1, acao, parcelas, ElementoDaArma.Cura);
        Assert.True(idCura > 0);

        Assert.Equal(3, servico.ProjeteisAtivos);
    }

    [Fact]
    public void BolaDeFogo_pesada_causa_dano_e_aplica_queimadura()
    {
        var posicoes = new PosicoesFalsas();
        posicoes.Colocar(1, "cidade", 0f, 0f);

        var alvo = new AlvoLocal(100, 0, 0, 10);
        long agora = 1000;
        var efeitos = new GerenciadorDeEfeitosDeStatus(() => agora);

        using var servico = new ServicoDeProjeteis(
            _tempoReal,
            posicoes,
            [alvo],
            NullLogger<ServicoDeProjeteis>.Instance,
            efeitos: efeitos,
            relogioMs: () => agora
        );

        servico.Disparar(
            atacanteId: 1,
            area: "cidade",
            tipoProjetil: TipoDeProjetil.BolaDeFogo,
            tipoAtaque: TipoDeAtaque.Pesado,
            origemX: 0f,
            origemY: 1.2f,
            origemZ: 0f,
            direcaoYGraus: 0f, // +Z (velocidade 30 m/s)
            dano: 60
        );

        // Avança 0.4s -> 12 metros, atinge o alvo em Z=10
        agora += 400;
        servico.Atualizar(agora, 0.4);

        Assert.Equal(1, alvo.Aplicacoes);
        Assert.Equal(60, alvo.DanoRecebido);
        Assert.True(efeitos.TemEfeito(100, TipoEfeitoDeStatus.Queimadura));
    }

    [Fact]
    public void EsferaDeGelo_causa_dano_e_aplica_lentidao()
    {
        var posicoes = new PosicoesFalsas();
        posicoes.Colocar(1, "cidade", 0f, 0f);

        var alvo = new AlvoLocal(100, 0, 0, 8);
        long agora = 1000;
        var efeitos = new GerenciadorDeEfeitosDeStatus(() => agora);

        using var servico = new ServicoDeProjeteis(
            _tempoReal,
            posicoes,
            [alvo],
            NullLogger<ServicoDeProjeteis>.Instance,
            efeitos: efeitos,
            relogioMs: () => agora
        );

        servico.Disparar(
            atacanteId: 1,
            area: "cidade",
            tipoProjetil: TipoDeProjetil.EsferaDeGelo,
            tipoAtaque: TipoDeAtaque.Leve,
            origemX: 0f,
            origemY: 1.2f,
            origemZ: 0f,
            direcaoYGraus: 0f,
            dano: 45
        );

        agora += 400;
        servico.Atualizar(agora, 0.4);

        Assert.Equal(1, alvo.Aplicacoes);
        Assert.Equal(45, alvo.DanoRecebido);
        Assert.True(efeitos.TemEfeito(100, TipoEfeitoDeStatus.Lentidao));
    }

    [Fact]
    public void OrbeDeCura_em_rota_de_aliado_cura_e_publica_evento()
    {
        var posicoes = new PosicoesFalsas();
        posicoes.Colocar(1, "cidade", 0f, 0f);
        posicoes.Colocar(2, "cidade", 0f, 6f); // Aliado à frente em Z=6

        var aliados = new AliadosLocais();
        aliados.Aliados.Add(1);
        aliados.Aliados.Add(2);

        var eventos = new EventosDoServidor();
        PersonagemCurouAliado? eventoCura = null;
        eventos.Assinar<PersonagemCurouAliado>(e => eventoCura = e);

        long agora = 1000;
        using var servico = new ServicoDeProjeteis(
            _tempoReal,
            posicoes,
            [],
            NullLogger<ServicoDeProjeteis>.Instance,
            aliados: aliados,
            eventos: eventos,
            relogioMs: () => agora
        );

        servico.Disparar(
            atacanteId: 1,
            area: "cidade",
            tipoProjetil: TipoDeProjetil.OrbeDeCura,
            tipoAtaque: TipoDeAtaque.Leve,
            origemX: 0f,
            origemY: 1.2f,
            origemZ: 0f,
            direcaoYGraus: 0f,
            dano: 50
        );

        // Avança 0.4s -> 8m (cruza o aliado em Z=6)
        agora += 400;
        servico.Atualizar(agora, 0.4);

        Assert.NotNull(eventoCura);
        Assert.Equal(1, eventoCura.CuradorId);
        Assert.Equal(2, eventoCura.CuradoId);
        Assert.Equal("cidade", eventoCura.Area);
        Assert.Equal(0, servico.ProjeteisAtivos); // Consumido no impacto com o aliado
    }

    [Fact]
    public void OrbeDeCura_em_rota_de_inimigo_aplica_dano()
    {
        var posicoes = new PosicoesFalsas();
        posicoes.Colocar(1, "cidade", 0f, 0f);

        var inimigo = new AlvoLocal(200, 0, 0, 8);
        long agora = 1000;

        using var servico = new ServicoDeProjeteis(
            _tempoReal,
            posicoes,
            [inimigo],
            NullLogger<ServicoDeProjeteis>.Instance,
            relogioMs: () => agora
        );

        servico.Disparar(
            atacanteId: 1,
            area: "cidade",
            tipoProjetil: TipoDeProjetil.OrbeDeCura,
            tipoAtaque: TipoDeAtaque.Leve,
            origemX: 0f,
            origemY: 1.2f,
            origemZ: 0f,
            direcaoYGraus: 0f,
            dano: 35
        );

        agora += 450;
        servico.Atualizar(agora, 0.45);

        Assert.Equal(1, inimigo.Aplicacoes);
        Assert.Equal(35, inimigo.DanoRecebido);
        Assert.Equal(0, servico.ProjeteisAtivos);
    }

    [Fact]
    public void OrbeDeCura_nao_cura_o_proprio_atacante()
    {
        var posicoes = new PosicoesFalsas();
        posicoes.Colocar(1, "cidade", 0f, 0f);

        var eventos = new EventosDoServidor();
        PersonagemCurouAliado? eventoCura = null;
        eventos.Assinar<PersonagemCurouAliado>(e => eventoCura = e);

        long agora = 1000;
        using var servico = new ServicoDeProjeteis(
            _tempoReal,
            posicoes,
            [],
            NullLogger<ServicoDeProjeteis>.Instance,
            eventos: eventos,
            relogioMs: () => agora
        );

        servico.Disparar(
            atacanteId: 1,
            area: "cidade",
            tipoProjetil: TipoDeProjetil.OrbeDeCura,
            tipoAtaque: TipoDeAtaque.Leve,
            origemX: 0f,
            origemY: 1.2f,
            origemZ: 0f,
            direcaoYGraus: 0f,
            dano: 50
        );

        agora += 100;
        servico.Atualizar(agora, 0.1);

        Assert.Null(eventoCura);
    }
}
