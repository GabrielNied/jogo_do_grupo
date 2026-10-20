using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Missoes;
using Xunit;

namespace JogoDoGrupo.Shared.Tests.Features.Missoes
{
    public class RegrasMissoesTestes
    {
        [Fact]
        public void PodeAceitar_ComNivelInsuficiente_RetornaErro()
        {
            var def = CatalogoDeMissoes.Obter(CatalogoDeMissoes.Missao4PatrulhaDiaria)!;
            var ok = RegrasMissoes.PodeAceitar(
                def,
                nivelPersonagem: 1,
                missoesConcluidas: new int[] { CatalogoDeMissoes.Missao2PrimeiraCacada },
                missoesAtivas: new int[0],
                out var erro
            );

            Assert.False(ok);
            Assert.Equal(CodigosErroMissoes.NivelInsuficiente, erro);
        }

        [Fact]
        public void PodeAceitar_SemPreRequisitos_RetornaErro()
        {
            var def = CatalogoDeMissoes.Obter(CatalogoDeMissoes.Missao2PrimeiraCacada)!;
            var ok = RegrasMissoes.PodeAceitar(
                def,
                nivelPersonagem: 5,
                missoesConcluidas: new int[0], // falta a Missao1Chegada
                missoesAtivas: new int[0],
                out var erro
            );

            Assert.False(ok);
            Assert.Equal(CodigosErroMissoes.PreRequisitosNaoCumpridos, erro);
        }

        [Fact]
        public void PodeAceitar_ComRequisitosCumpridos_RetornaSucesso()
        {
            var def = CatalogoDeMissoes.Obter(CatalogoDeMissoes.Missao2PrimeiraCacada)!;
            var ok = RegrasMissoes.PodeAceitar(
                def,
                nivelPersonagem: 2,
                missoesConcluidas: new int[] { CatalogoDeMissoes.Missao1Chegada },
                missoesAtivas: new int[0],
                out var erro
            );

            Assert.True(ok);
            Assert.Null(erro);
        }

        [Fact]
        public void PodeAceitar_LimiteAtingidoEmSecundaria_RetornaErro()
        {
            var def = CatalogoDeMissoes.Obter(CatalogoDeMissoes.Missao3AbastecerForja)!;
            var ativas = new List<int>();
            for (int i = 10; i < 10 + ConfigMissoes.LimiteMissoesEmAndamento; i++)
            {
                ativas.Add(i);
            }

            var ok = RegrasMissoes.PodeAceitar(
                def,
                nivelPersonagem: 5,
                missoesConcluidas: new int[] { CatalogoDeMissoes.Missao7OSenhorDaColinaDoVigia },
                missoesAtivas: ativas,
                out var erro
            );

            Assert.False(ok);
            Assert.Equal(CodigosErroMissoes.LimiteMissoesAtingido, erro);
        }

        [Fact]
        public void PodeAceitar_Principal_IgnoraLimiteDeVinteCinco()
        {
            var def = CatalogoDeMissoes.Obter(CatalogoDeMissoes.Missao1Chegada)!;
            var ativas = new List<int>();
            for (int i = 10; i < 10 + ConfigMissoes.LimiteMissoesEmAndamento; i++)
            {
                ativas.Add(i);
            }

            var ok = RegrasMissoes.PodeAceitar(
                def,
                nivelPersonagem: 1,
                missoesConcluidas: new int[0],
                missoesAtivas: ativas,
                out var erro
            );

            Assert.True(ok);
            Assert.Null(erro);
        }

        [Fact]
        public void PodeAbandonar_Principal_RetornaErro()
        {
            var def = CatalogoDeMissoes.Obter(CatalogoDeMissoes.Missao1Chegada)!;
            var ok = RegrasMissoes.PodeAbandonar(def, out var erro);

            Assert.False(ok);
            Assert.Equal(CodigosErroMissoes.MissaoPrincipalNaoAbandona, erro);
        }

        [Fact]
        public void PodeAbandonar_Secundaria_RetornaSucesso()
        {
            var def = CatalogoDeMissoes.Obter(CatalogoDeMissoes.Missao3AbastecerForja)!;
            var ok = RegrasMissoes.PodeAbandonar(def, out var erro);

            Assert.True(ok);
            Assert.Null(erro);
        }

        [Fact]
        public void PodeEntregar_ObjetivosIncompletos_RetornaErro()
        {
            var def = CatalogoDeMissoes.Obter(CatalogoDeMissoes.Missao2PrimeiraCacada)!;
            var progresso = new ProgressoDeMissaoDto
            {
                MissaoId = def.Id,
                Estado = EstadoDaMissao.EmAndamento,
                Objetivos = new ProgressoDeObjetivo[]
                {
                    new ProgressoDeObjetivo
                    {
                        ObjetivoId = 1,
                        QuantidadeAtual = 2,
                        Concluido = false,
                    },
                },
            };

            var ok = RegrasMissoes.PodeEntregar(def, progresso, itemIdEscolhido: 1, out var erro);

            Assert.False(ok);
            Assert.Equal(CodigosErroMissoes.ObjetivosNaoCumpridos, erro);
        }

        [Fact]
        public void PodeEntregar_ItemEscolhaInvalido_RetornaErro()
        {
            var def = CatalogoDeMissoes.Obter(CatalogoDeMissoes.Missao2PrimeiraCacada)!;
            var progresso = new ProgressoDeMissaoDto
            {
                MissaoId = def.Id,
                Estado = EstadoDaMissao.ProntaParaEntregar,
                Objetivos = new ProgressoDeObjetivo[]
                {
                    new ProgressoDeObjetivo
                    {
                        ObjetivoId = 1,
                        QuantidadeAtual = 1,
                        Concluido = true,
                    },
                },
            };

            var ok = RegrasMissoes.PodeEntregar(def, progresso, itemIdEscolhido: 999, out var erro);

            Assert.False(ok);
            Assert.Equal(CodigosErroMissoes.ItemEscolhaInvalido, erro);
        }

        [Fact]
        public void PodeEntregar_ComObjetivosEItemValido_RetornaSucesso()
        {
            var def = CatalogoDeMissoes.Obter(CatalogoDeMissoes.Missao2PrimeiraCacada)!;
            var progresso = new ProgressoDeMissaoDto
            {
                MissaoId = def.Id,
                Estado = EstadoDaMissao.ProntaParaEntregar,
                Objetivos = new ProgressoDeObjetivo[]
                {
                    new ProgressoDeObjetivo
                    {
                        ObjetivoId = 1,
                        QuantidadeAtual = 1,
                        Concluido = true,
                    },
                },
            };

            var ok = RegrasMissoes.PodeEntregar(
                def,
                progresso,
                itemIdEscolhido: CatalogoEquipamentos.PrimeiroModeloDeArma,
                out var erro
            );

            Assert.True(ok);
            Assert.Null(erro);
        }

        [Fact]
        public void PodeCompartilhar_NaoLider_RetornaErro()
        {
            var def = CatalogoDeMissoes.Obter(CatalogoDeMissoes.Missao2PrimeiraCacada)!;
            var ok = RegrasMissoes.PodeCompartilhar(
                def,
                ehLiderGrupo: false,
                distanciaDoLiderMetros: 5f,
                out var erro
            );

            Assert.False(ok);
            Assert.Equal(CodigosErroMissoes.ApenasLiderCompartilha, erro);
        }

        [Fact]
        public void PodeCompartilhar_LongeDoLider_RetornaErro()
        {
            var def = CatalogoDeMissoes.Obter(CatalogoDeMissoes.Missao2PrimeiraCacada)!;
            var ok = RegrasMissoes.PodeCompartilhar(
                def,
                ehLiderGrupo: true,
                distanciaDoLiderMetros: ConfigMissoes.RaioCompartilhamentoMetros + 10f,
                out var erro
            );

            Assert.False(ok);
            Assert.Equal(CodigosErroMissoes.LongeDoLider, erro);
        }

        [Fact]
        public void PodeCompartilhar_PertoDoLider_RetornaSucesso()
        {
            var def = CatalogoDeMissoes.Obter(CatalogoDeMissoes.Missao2PrimeiraCacada)!;
            var ok = RegrasMissoes.PodeCompartilhar(
                def,
                ehLiderGrupo: true,
                distanciaDoLiderMetros: 10f,
                out var erro
            );

            Assert.True(ok);
            Assert.Null(erro);
        }

        [Theory]
        [InlineData(1, 1, "amarelo")]
        [InlineData(1, 5, "vermelho")]
        [InlineData(10, 2, "cinza")]
        public void ObterCorDoIndicador_CalculaCoresCorretamente(
            int nivelPersonagem,
            int nivelRecomendado,
            string corEsperada
        )
        {
            var cor = RegrasMissoes.ObterCorDoIndicador(nivelPersonagem, nivelRecomendado);
            Assert.Equal(corEsperada, cor);
        }

        [Fact]
        public void EstaPertoDoNpc_CalculaDistanciaCorretamente()
        {
            Assert.True(RegrasMissoes.EstaPertoDoNpc(0f, 0f, 2f, 2f));
            Assert.False(RegrasMissoes.EstaPertoDoNpc(0f, 0f, 20f, 20f));
        }

        [Fact]
        public void CatalogoDeMissoes_PossuiMissaoPrincipalInicial()
        {
            var inicial = CatalogoDeMissoes.Obter(ConfigMissoes.MissaoPrincipalInicialId);
            Assert.NotNull(inicial);
            Assert.Equal(TipoDeMissao.Principal, inicial.Tipo);
            Assert.Equal(1, inicial.NivelMinimo);
        }
    }
}
