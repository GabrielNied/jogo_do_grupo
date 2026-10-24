using System.Linq;
using JogoDoGrupo.Shared.Features.Npcs;
using JogoDoGrupo.Shared.Features.Profissoes;
using Xunit;

namespace JogoDoGrupo.Shared.Tests.Features.Npcs
{
    /// <summary>Um NPC mestre de ofício por profissão, cada um com só a sua (#1257).</summary>
    public class MestresDeOficioTestes
    {
        public static TheoryData<Profissao, int, PapelNpc> Mestres =>
            new TheoryData<Profissao, int, PapelNpc>
            {
                { Profissao.Lenhador, CatalogoNpcs.IdMestreLenhador, PapelNpc.MestreLenhador },
                { Profissao.Minerador, CatalogoNpcs.IdMestreMinerador, PapelNpc.MestreMinerador },
                { Profissao.Curtidor, CatalogoNpcs.IdMestreCurtidor, PapelNpc.MestreCurtidor },
                { Profissao.Ferreiro, CatalogoNpcs.IdMestreFerreiro, PapelNpc.MestreFerreiro },
                {
                    Profissao.Alquimista,
                    CatalogoNpcs.IdMestreAlquimista,
                    PapelNpc.MestreAlquimista
                },
                {
                    Profissao.Cozinheiro,
                    CatalogoNpcs.IdMestreCozinheiro,
                    PapelNpc.MestreCozinheiro
                },
            };

        [Fact]
        public void TodaProfissaoDisponivel_TemUmMestre()
        {
            Assert.Equal(ConfigProfissoes.Disponiveis.Length, Mestres.Count());
        }

        [Theory]
        [MemberData(nameof(Mestres))]
        public void MestreDeOficio_ExisteNaCidadeEAbreSoASuaProfissao(
            Profissao profissao,
            int npcId,
            PapelNpc papel
        )
        {
            var npc = CatalogoNpcs.Obter(npcId);
            Assert.NotNull(npc);
            Assert.Equal(papel, npc.Papel);
            Assert.Equal(CatalogoNpcs.AreaCidade, npc.Area);

            var dialogo = CatalogoDialogosNpc.ObterDialogo(npc);
            var servicos = dialogo
                .Opcoes.Where(o => o.Acao == AcaoDeDialogo.AbrirServico)
                .Select(o => o.Parametro)
                .ToArray();

            var unico = Assert.Single(servicos);
            Assert.Equal(
                "profissoes." + ConfigProfissoes.NomeDaProfissao(profissao)!.ToLowerInvariant(),
                unico
            );
        }

        [Fact]
        public void MestresDeOficio_TemNomesEPosicoesDiferentes()
        {
            var mestres = Mestres.Select(m => CatalogoNpcs.Obter((int)m[1])).ToArray();

            Assert.Equal(mestres.Length, mestres.Select(m => m.Nome).Distinct().Count());
            Assert.Equal(mestres.Length, mestres.Select(m => (m.X, m.Z)).Distinct().Count());
            foreach (var mestre in mestres)
            {
                Assert.NotEqual(CatalogoNpcs.Ferreiro.Nome, mestre.Nome);
            }
        }

        [Fact]
        public void FerreiroBrom_MantemSoAForjaDoEquipamento()
        {
            var dialogo = CatalogoDialogosNpc.ObterDialogo(CatalogoNpcs.Ferreiro);

            var servico = Assert.Single(dialogo.Opcoes, o => o.Acao == AcaoDeDialogo.AbrirServico);
            Assert.Equal("economia.ferreiro", servico.Parametro);
        }

        [Theory]
        [InlineData(Profissao.Ferreiro, CatalogoNpcs.IdMestreFerreiro)]
        [InlineData(Profissao.Curtidor, CatalogoNpcs.IdMestreCurtidor)]
        [InlineData(Profissao.Alquimista, CatalogoNpcs.IdMestreAlquimista)]
        [InlineData(Profissao.Cozinheiro, CatalogoNpcs.IdMestreCozinheiro)]
        public void MestreDeProducao_FicaAoAlcanceDaPropriaBancada(Profissao profissao, int npcId)
        {
            var npc = CatalogoNpcs.Obter(npcId);

            Assert.True(ConfigProfissoes.PertoDaEstacao(profissao, npc.X, npc.Z));
        }

        [Theory]
        [InlineData(Profissao.Lenhador, 0)]
        [InlineData(Profissao.Ferreiro, 4)]
        [InlineData(Profissao.Cozinheiro, 3)]
        public void JanelaDoMestre_ListaSoAsReceitasDaSuaProfissao(Profissao profissao, int total)
        {
            var daProfissao = ConfigProfissoes.ReceitasDaProfissao(profissao);

            Assert.Equal(total, daProfissao.Length);
            Assert.All(daProfissao, r => Assert.Equal(profissao, r.Profissao));
        }

        [Fact]
        public void MateriaisDaProfissao_SaoOsQueElaColetaOuGasta()
        {
            var lenhador = ConfigProfissoes
                .MateriaisDaProfissao(Profissao.Lenhador)
                .Select(m => m.Id)
                .ToArray();
            var ferreiro = ConfigProfissoes
                .MateriaisDaProfissao(Profissao.Ferreiro)
                .Select(m => m.Id)
                .ToArray();

            Assert.Equal(
                new[]
                {
                    ConfigProfissoes.MaterialMadeira,
                    ConfigProfissoes.MaterialErvaMedicinal,
                    ConfigProfissoes.MaterialGraos,
                },
                lenhador
            );
            Assert.Equal(
                new[]
                {
                    ConfigProfissoes.MaterialMadeira,
                    ConfigProfissoes.MaterialMinerioDeFerro,
                    ConfigProfissoes.MaterialCouroCru,
                },
                ferreiro
            );
        }
    }
}
