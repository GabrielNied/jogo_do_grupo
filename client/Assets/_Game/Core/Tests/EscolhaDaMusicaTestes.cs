using JogoDoGrupo.Client.Core.Audio;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Core.Tests
{
    public class EscolhaDaMusicaTestes
    {
        [Test]
        public void Fora_do_mundo_toca_abertura()
        {
            Assert.AreEqual(
                MusicaDoJogo.Abertura,
                EscolhaDaMusica.Escolher(false, MusicaDoJogo.Cidade)
            );
            Assert.AreEqual(
                MusicaDoJogo.Abertura,
                EscolhaDaMusica.Escolher(false, MusicaDoJogo.Dungeon)
            );
            Assert.AreEqual(
                MusicaDoJogo.Abertura,
                EscolhaDaMusica.Escolher(false, MusicaDoJogo.Chefe)
            );
        }

        [Test]
        public void No_mundo_sem_pedido_ou_com_abertura_toca_cidade()
        {
            Assert.AreEqual(
                MusicaDoJogo.Cidade,
                EscolhaDaMusica.Escolher(true, MusicaDoJogo.Nenhuma)
            );
            Assert.AreEqual(
                MusicaDoJogo.Cidade,
                EscolhaDaMusica.Escolher(true, MusicaDoJogo.Abertura)
            );
            Assert.AreEqual(
                MusicaDoJogo.Cidade,
                EscolhaDaMusica.Escolher(true, MusicaDoJogo.Cidade)
            );
        }

        [Test]
        public void Area_dungeon_escolhe_faixa_da_dungeon()
        {
            Assert.AreEqual(
                MusicaDoJogo.Dungeon,
                EscolhaDaMusica.Escolher(true, MusicaDoJogo.Dungeon)
            );
        }

        [Test]
        public void Luta_do_boss_troca_para_chefe_e_terminar_devolve_faixa_da_area()
        {
            // Na dungeon, iniciar luta com o boss troca para Chefe
            var emLutaDungeon = EscolhaDaMusica.ResolverFaixa(
                noMundo: true,
                areaBase: MusicaDoJogo.Dungeon,
                bossEmLuta: true
            );
            Assert.AreEqual(MusicaDoJogo.Chefe, emLutaDungeon);

            // Ao terminar a luta do boss (wipe ou saída), volta para Dungeon
            var fimLutaDungeon = EscolhaDaMusica.ResolverFaixa(
                noMundo: true,
                areaBase: MusicaDoJogo.Dungeon,
                bossEmLuta: false
            );
            Assert.AreEqual(MusicaDoJogo.Dungeon, fimLutaDungeon);

            // Na cidade (ex: boss de mundo), funciona da mesma forma
            var emLutaCidade = EscolhaDaMusica.ResolverFaixa(
                noMundo: true,
                areaBase: MusicaDoJogo.Cidade,
                bossEmLuta: true
            );
            Assert.AreEqual(MusicaDoJogo.Chefe, emLutaCidade);

            var fimLutaCidade = EscolhaDaMusica.ResolverFaixa(
                noMundo: true,
                areaBase: MusicaDoJogo.Cidade,
                bossEmLuta: false
            );
            Assert.AreEqual(MusicaDoJogo.Cidade, fimLutaCidade);
        }

        [Test]
        public void Vitoria_toca_faixa_de_vitoria_e_ao_acabar_devolve_faixa_da_dungeon()
        {
            var vitoria = EscolhaDaMusica.ResolverFaixa(
                noMundo: true,
                areaBase: MusicaDoJogo.Dungeon,
                bossEmLuta: false,
                vitoriaAtiva: true
            );
            Assert.AreEqual(MusicaDoJogo.Vitoria, vitoria);

            Assert.AreEqual(MusicaDoJogo.Dungeon, EscolhaDaMusica.DepoisDe(MusicaDoJogo.Vitoria));
            Assert.AreEqual(MusicaDoJogo.Cidade, EscolhaDaMusica.DepoisDe(MusicaDoJogo.Cidade));
        }

        [Test]
        public void Apenas_vitoria_nao_repete_outras_faixas_repetem()
        {
            Assert.IsFalse(EscolhaDaMusica.Repete(MusicaDoJogo.Vitoria));
            Assert.IsTrue(EscolhaDaMusica.Repete(MusicaDoJogo.Abertura));
            Assert.IsTrue(EscolhaDaMusica.Repete(MusicaDoJogo.Cidade));
            Assert.IsTrue(EscolhaDaMusica.Repete(MusicaDoJogo.Dungeon));
            Assert.IsTrue(EscolhaDaMusica.Repete(MusicaDoJogo.Chefe));
        }
    }
}
