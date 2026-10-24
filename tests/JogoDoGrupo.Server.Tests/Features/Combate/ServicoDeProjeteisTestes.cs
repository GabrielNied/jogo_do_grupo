using System.Collections.Concurrent;
using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Features.Combate;
using JogoDoGrupo.Server.Tests.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;
using Microsoft.Extensions.Logging.Abstractions;

namespace JogoDoGrupo.Server.Tests.Features.Combate;

[Collection("TempoRealSerial")]
public class ServicoDeProjeteisTestes(TempoRealFixture f) : IClassFixture<TempoRealFixture>
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
                return [new AlvoDeCombate(Id, X, Z, Raio, 100, 100, PersonagemId, Altura, Y)];
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
    public void Disparar_registra_projetil_e_envia_mensagem_para_area()
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

        long id = servico.Disparar(
            atacanteId: 1,
            area: "cidade",
            tipoProjetil: TipoDeProjetil.Flecha,
            tipoAtaque: TipoDeAtaque.Leve,
            origemX: 0f,
            origemY: 1.2f,
            origemZ: 0f,
            direcaoYGraus: 0f, // +Z
            dano: 25
        );

        Assert.True(id > 0);
        Assert.Equal(1, servico.ProjeteisAtivos);
    }

    [Fact]
    public void Atualizar_avanca_projetil_e_acerta_alvo_em_linha()
    {
        var posicoes = new PosicoesFalsas();
        posicoes.Colocar(1, "cidade", 0f, 0f);

        // Alvo a 10 metros à frente (Z = 10)
        var alvo = new AlvoLocal(100, 0, 0, 10);
        long agora = 1000;

        using var servico = new ServicoDeProjeteis(
            _tempoReal,
            posicoes,
            [alvo],
            NullLogger<ServicoDeProjeteis>.Instance,
            relogioMs: () => agora
        );

        servico.Disparar(
            atacanteId: 1,
            area: "cidade",
            tipoProjetil: TipoDeProjetil.Flecha,
            tipoAtaque: TipoDeAtaque.Leve,
            origemX: 0f,
            origemY: 1.2f,
            origemZ: 0f,
            direcaoYGraus: 0f, // +Z (velocidade 35 m/s)
            dano: 50
        );

        Assert.Equal(1, servico.ProjeteisAtivos);
        Assert.Equal(0, alvo.Aplicacoes);

        // Avança 0.1s -> percorre 3.5m (ainda não chegou em Z=10)
        agora += 100;
        servico.Atualizar(agora, 0.1);
        Assert.Equal(1, servico.ProjeteisAtivos);
        Assert.Equal(0, alvo.Aplicacoes);

        // Avança mais 0.25s -> percorre mais 8.75m (total 12.25m, cruza Z=10)
        agora += 250;
        servico.Atualizar(agora, 0.25);

        Assert.Equal(1, alvo.Aplicacoes);
        Assert.Equal(50, alvo.DanoRecebido);
        Assert.Equal(0, servico.ProjeteisAtivos); // Projétil consumido no impacto
    }

    [Fact]
    public void Alvo_fora_da_linha_nao_e_atingido()
    {
        var posicoes = new PosicoesFalsas();
        posicoes.Colocar(1, "cidade", 0f, 0f);

        // Alvo a 10m de distância no eixo X (fora do caminho do disparo em Z)
        var alvo = new AlvoLocal(100, 10, 0, 10);
        long agora = 1000;

        using var servico = new ServicoDeProjeteis(
            _tempoReal,
            posicoes,
            [alvo],
            NullLogger<ServicoDeProjeteis>.Instance,
            relogioMs: () => agora
        );

        servico.Disparar(
            atacanteId: 1,
            area: "cidade",
            tipoProjetil: TipoDeProjetil.Flecha,
            tipoAtaque: TipoDeAtaque.Leve,
            origemX: 0f,
            origemY: 1.2f,
            origemZ: 0f,
            direcaoYGraus: 0f, // +Z
            dano: 50
        );

        // Avança simulação
        agora += 500;
        servico.Atualizar(agora, 0.5);

        Assert.Equal(0, alvo.Aplicacoes);
        Assert.Equal(0, alvo.DanoRecebido);
    }

    [Fact]
    public void Projetil_expira_ao_alcancar_distancia_maxima()
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

        servico.Disparar(
            atacanteId: 1,
            area: "cidade",
            tipoProjetil: TipoDeProjetil.Flecha,
            tipoAtaque: TipoDeAtaque.Leve,
            origemX: 0f,
            origemY: 1.2f,
            origemZ: 0f,
            direcaoYGraus: 0f,
            dano: 30
        );

        Assert.Equal(1, servico.ProjeteisAtivos);

        // Flecha leve tem alcance 35m e vel 35m/s. Avançando 1.5s (52.5m)
        agora += 1500;
        servico.Atualizar(agora, 1.5);

        Assert.Equal(0, servico.ProjeteisAtivos);
    }

    [Fact]
    public void Aliado_e_proprio_atacante_nao_sao_atingidos()
    {
        var posicoes = new PosicoesFalsas();
        posicoes.Colocar(1, "cidade", 0f, 0f);

        var atacanteComoAlvo = new AlvoLocal(1, 0, 0, 1, personagemId: 1);
        var aliadoComoAlvo = new AlvoLocal(2, 0, 0, 5, personagemId: 2);

        var aliados = new AliadosLocais();
        aliados.Aliados.Add(1);
        aliados.Aliados.Add(2);

        long agora = 1000;
        using var servico = new ServicoDeProjeteis(
            _tempoReal,
            posicoes,
            [atacanteComoAlvo, aliadoComoAlvo],
            NullLogger<ServicoDeProjeteis>.Instance,
            aliados: aliados,
            relogioMs: () => agora
        );

        servico.Disparar(
            atacanteId: 1,
            area: "cidade",
            tipoProjetil: TipoDeProjetil.Flecha,
            tipoAtaque: TipoDeAtaque.Leve,
            origemX: 0f,
            origemY: 1.2f,
            origemZ: 0f,
            direcaoYGraus: 0f,
            dano: 40
        );

        agora += 500;
        servico.Atualizar(agora, 0.5);

        Assert.Equal(0, atacanteComoAlvo.Aplicacoes);
        Assert.Equal(0, aliadoComoAlvo.Aplicacoes);
    }

    [Fact]
    public void DispararArco_executa_disparo_corretamente()
    {
        var posicoes = new PosicoesFalsas();
        posicoes.Colocar(1, "cidade", 10f, 20f);

        long agora = 1000;
        using var servico = new ServicoDeProjeteis(
            _tempoReal,
            posicoes,
            [],
            NullLogger<ServicoDeProjeteis>.Instance,
            relogioMs: () => agora
        );

        var acao = new AcaoDeAtaque(
            1,
            new PedidoDeAtaque(TipoDeAtaque.Pesado, 90f, 1, agora),
            agora,
            0
        );

        var parcelas = new ParcelasDeDano
        {
            DanoBase = 40,
            Magico = false,
            PercentualPersonagem = 0.3,
            DanoFlatEquipamento = 10,
        };

        long id = servico.DispararArco(1, acao, parcelas);
        Assert.True(id > 0);
        Assert.Equal(1, servico.ProjeteisAtivos);
    }

    [Fact]
    public void Atualizar_acerta_alvo_em_terreno_elevado()
    {
        var posicoes = new PosicoesFalsas();
        posicoes.Colocar(1, "mundo", 0f, 0f);

        // Alvo elevado em Y = 28 metros, a 10 metros à frente (+Z)
        var alvoElevado = new AlvoLocal(50, 0, 28, 10);

        long agora = 1000;
        using var servico = new ServicoDeProjeteis(
            _tempoReal,
            posicoes,
            [alvoElevado],
            NullLogger<ServicoDeProjeteis>.Instance,
            relogioMs: () => agora
        );

        // Disparo a partir de Y = 28 + 1.2 (altura dos olhos do personagem em Y = 28)
        servico.Disparar(
            atacanteId: 1,
            area: "mundo",
            tipoProjetil: TipoDeProjetil.Flecha,
            tipoAtaque: TipoDeAtaque.Leve,
            origemX: 0f,
            origemY: 28f + RegrasProjetil.AlturaOrigemDisparo,
            origemZ: 0f,
            direcaoYGraus: 0f,
            dano: 35
        );

        agora += 350;
        servico.Atualizar(agora, 0.35);

        Assert.Equal(1, alvoElevado.Aplicacoes);
        Assert.Equal(35, alvoElevado.DanoRecebido);
        Assert.Equal(0, servico.ProjeteisAtivos);
    }
}
