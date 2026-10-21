using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace JogoDoGrupo.Client.Core.Visual.Editor
{
    /// <summary>
    /// Roda os testes EditMode no editor já aberto (o <c>npm run unity:testes</c> pede o editor
    /// fechado) e grava o resumo num arquivo ao terminar: contagens e uma linha por falha. Para
    /// agentes que dirigem o editor pelo CLI: chame <see cref="Rodar"/> por <c>eval</c> e leia o
    /// arquivo quando ele aparecer.
    /// </summary>
    public static class TestesNoEditorAberto
    {
        public static void Rodar(string arquivo) => Rodar(arquivo, null);

        /// <summary>Só os testes cujo nome completo contém <paramref name="filtro"/>.</summary>
        public static void Rodar(string arquivo, string filtro)
        {
            if (File.Exists(arquivo))
                File.Delete(arquivo);
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Coletor(arquivo));
            var selecao = new Filter { testMode = TestMode.EditMode };
            if (!string.IsNullOrEmpty(filtro))
                selecao.groupNames = new[] { System.Text.RegularExpressions.Regex.Escape(filtro) };
            api.Execute(new ExecutionSettings(selecao));
        }

        private sealed class Coletor : ICallbacks
        {
            private readonly string _arquivo;
            private readonly StringBuilder _falhas = new StringBuilder();
            private int _passaram;
            private int _falharam;
            private int _outros;

            public Coletor(string arquivo) => _arquivo = arquivo;

            public void RunStarted(ITestAdaptor testsToRun) { }

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.HasChildren)
                    return;

                switch (result.TestStatus)
                {
                    case TestStatus.Passed:
                        _passaram++;
                        break;
                    case TestStatus.Failed:
                        _falharam++;
                        var mensagem = (result.Message ?? string.Empty)
                            .Replace('\n', ' ')
                            .Replace('\r', ' ');
                        if (mensagem.Length > 300)
                            mensagem = mensagem.Substring(0, 300);
                        _falhas.AppendLine(result.Test.FullName + " :: " + mensagem);
                        break;
                    default:
                        _outros++;
                        break;
                }
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                File.WriteAllText(
                    _arquivo,
                    $"passaram={_passaram} falharam={_falharam} outros={_outros}\n{_falhas}"
                );
            }
        }
    }
}
