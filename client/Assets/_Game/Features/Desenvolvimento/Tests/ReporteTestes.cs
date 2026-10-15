using System;
using System.Collections.Generic;
using System.IO;
using JogoDoGrupo.Client.Features.Desenvolvimento.Relato;
using NUnit.Framework;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Desenvolvimento.Tests
{
    public class ReporteTestes
    {
        private string _pasta;

        [SetUp]
        public void Preparar()
        {
            _pasta = Path.Combine(Path.GetTempPath(), "reporte-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_pasta);
        }

        [TearDown]
        public void Limpar()
        {
            Directory.Delete(_pasta, true);
        }

        // Os nomes são o contrato com scripts/relatos/reporte.mjs: trocar um quebra o envio.
        [Test]
        public void ManifestoTemOsCamposQueOScriptLe()
        {
            var json = JsonUtility.ToJson(
                new ManifestoDoReporte { Id = "2026-10-15_120430", Posicao = new[] { 1f, 2f, 3f } }
            );

            foreach (
                var campo in new[]
                {
                    "Id",
                    "Tipo",
                    "Severidade",
                    "Area",
                    "Frase",
                    "Quando",
                    "QuandoUtc",
                    "Versao",
                    "Editor",
                    "Tela",
                    "LatenciaMs",
                    "Posicao",
                    "AjustesF10",
                    "Branch",
                    "Commit",
                    "SegundosDaLinhaDoTempo",
                    "SegundosDeQuadros",
                    "Status",
                    "Issue",
                    "Url",
                    "Erro",
                }
            )
            {
                StringAssert.Contains($"\"{campo}\":", json);
            }

            StringAssert.Contains("\"Status\":\"pendente\"", json);
            Assert.AreEqual(
                "2026-10-15_120430",
                ManifestoDoReporte.NovoId(new DateTime(2026, 10, 15, 12, 4, 30))
            );
        }

        [Test]
        public void OpcaoDesconhecidaFicaComAPrimeira()
        {
            Assert.AreEqual("sensacao", OpcoesDoReporte.Valor(OpcoesDoReporte.Tipos, "Sensação"));
            Assert.AreEqual("bug", OpcoesDoReporte.Valor(OpcoesDoReporte.Tipos, "???"));
        }

        [Test]
        public void GitLeBranchECommitDaRefSoltaOuEmpacotada()
        {
            var git = Path.Combine(_pasta, ".git");
            Directory.CreateDirectory(Path.Combine(git, "refs", "heads", "feature"));
            File.WriteAllText(Path.Combine(git, "HEAD"), "ref: refs/heads/feature/x\n");
            File.WriteAllText(
                Path.Combine(git, "refs", "heads", "feature", "x"),
                "a0b28d3ffffffffffffffffffffffffffffffff\n"
            );

            Assert.AreEqual(("feature/x", "a0b28d3"), InfoDoGit.Ler(_pasta));

            File.Delete(Path.Combine(git, "refs", "heads", "feature", "x"));
            File.WriteAllText(
                Path.Combine(git, "packed-refs"),
                "# pack-refs\nb1c2d3e4ffffffffffffffffffffffffffffffff refs/heads/feature/x\n"
            );

            Assert.AreEqual(("feature/x", "b1c2d3e"), InfoDoGit.Ler(_pasta));
        }

        [Test]
        public void GitDoWorktreeSegueOGitdirEAPastaComum()
        {
            var principal = Path.Combine(_pasta, "principal", ".git");
            var doWorktree = Path.Combine(principal, "worktrees", "lote");
            Directory.CreateDirectory(doWorktree);
            Directory.CreateDirectory(Path.Combine(principal, "refs", "heads"));
            File.WriteAllText(Path.Combine(doWorktree, "HEAD"), "ref: refs/heads/lote\n");
            File.WriteAllText(Path.Combine(doWorktree, "commondir"), "../..\n");
            File.WriteAllText(
                Path.Combine(principal, "refs", "heads", "lote"),
                "c0ffee1fffffffffffffffffffffffffffffffff\n"
            );
            var worktree = Path.Combine(_pasta, "worktree");
            Directory.CreateDirectory(worktree);
            File.WriteAllText(Path.Combine(worktree, ".git"), "gitdir: " + doWorktree + "\n");

            Assert.AreEqual(("lote", "c0ffee1"), InfoDoGit.Ler(worktree));
        }

        [Test]
        public void SemGitVoltaVazio()
        {
            Assert.AreEqual(("", ""), InfoDoGit.Ler(_pasta));
        }

        [Test]
        public void EstadoListaAUltimaMensagemDeCadaTipoComAIdade()
        {
            var texto = EstadoDoMomento.Estado(
                "Cidade",
                "itens.inventario",
                32,
                "1 2 3",
                new List<EventoDeRede>
                {
                    new EventoDeRede(10.0, 3005, "JogadorMorreu", "{\"Id\":7}"),
                },
                12.5
            );

            StringAssert.Contains("cena: Cidade", texto);
            StringAssert.Contains("janela aberta antes do report: itens.inventario", texto);
            StringAssert.Contains("JogadorMorreu (3005) há 2.5 s: {\"Id\":7}", texto);
        }

        [Test]
        public void ConsoleMostraAPilhaSoDosErros()
        {
            var texto = EstadoDoMomento.Console(
                new List<LinhaDoConsole>
                {
                    new LinhaDoConsole(1.0, LogType.Warning, "cuidado", "pilha do aviso"),
                    new LinhaDoConsole(2.0, LogType.Exception, "quebrou", "A.B()\nC.D()\n"),
                },
                3.0
            );

            StringAssert.Contains("há 2.0 s  Warning  cuidado", texto);
            StringAssert.DoesNotContain("pilha do aviso", texto);
            StringAssert.Contains("há 1.0 s  Exception  quebrou\n    A.B()\n    C.D()\n", texto);
            Assert.AreEqual("", EstadoDoMomento.Console(new List<LinhaDoConsole>(), 0));
        }
    }
}
