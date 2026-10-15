using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Assemblies;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.Tests
{
    /// <summary>
    /// Fumaça das janelas registradas pelas funcionalidades (issue #447): cada uma tem ponto
    /// de entrada, monta sem exceção nem erro no console e fecha pelo Esc e pela tecla,
    /// deixando o jogo livre.
    /// </summary>
    /// <remarks>
    /// Em EditMode o <c>[RuntimeInitializeOnLoadMethod]</c> não roda. A lista sai sozinha:
    /// procura nos assemblies <c>JogoDoGrupo.Client.*</c> todo método que chama
    /// <see cref="JanelasDoJogo.Registrar"/> e o executa. Funcionalidade nova entra aqui sem
    /// mexer no teste.
    /// </remarks>
    public class FumacaDasJanelasTestes
    {
        private const string PrefixoDosAssemblies = "JogoDoGrupo.Client.";

        private static Descoberta _descoberta;

        private List<DefinicaoDeJanela> _registradasAntes;
        private GerenteDeJanelas _gerente;
        private ApresentadorDeJanelas _apresentador;
        private VisualElement _container;
        private List<string> _erros;
        private bool _ignoravaLogs;

        private static Descoberta Janelas => _descoberta ??= Descobrir();

        /// <summary>Uma chave por janela, para cada uma virar um teste com o próprio nome.</summary>
        public static IEnumerable<TestCaseData> Chaves()
        {
            foreach (var definicao in Janelas.Definicoes)
            {
                yield return new TestCaseData(definicao.Chave).SetName(
                    "Janela(" + definicao.Chave + ")"
                );
            }
        }

        [SetUp]
        public void Preparar()
        {
            _registradasAntes = JanelasDoJogo.Registro.Definicoes.ToList();
            JanelasDoJogo.Registro.Limpar();
            foreach (var definicao in Janelas.Definicoes)
            {
                JanelasDoJogo.Registro.Registrar(definicao);
            }

            _container = new VisualElement();
            _apresentador = new ApresentadorDeJanelas(_container, () => _gerente.Fechar());
            _gerente = new GerenteDeJanelas(
                JanelasDoJogo.Registro,
                _apresentador.Mostrar,
                _apresentador.Esconder
            );
            JanelasDoJogo.Ligar(_gerente);

            // Os erros são conferidos à mão, com o nome da janela na mensagem.
            _erros = new List<string>();
            _ignoravaLogs = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
            Application.logMessageReceived += GuardarErro;
        }

        [TearDown]
        public void Encerrar()
        {
            Application.logMessageReceived -= GuardarErro;
            LogAssert.ignoreFailingMessages = _ignoravaLogs;

            _gerente.Fechar();
            JanelasDoJogo.Desligar(_gerente);
            JanelasDoJogo.Registro.Limpar();
            foreach (var definicao in _registradasAntes)
            {
                JanelasDoJogo.Registro.Registrar(definicao);
            }
        }

        [Test]
        public void Descoberta_AchaJanelasDasFuncionalidades()
        {
            Assert.IsEmpty(
                Janelas.Problemas,
                "Registro de janela com problema:\n" + string.Join("\n", Janelas.Problemas)
            );
            Assert.IsNotEmpty(
                Janelas.Definicoes,
                "Nenhuma janela encontrada nos assemblies " + PrefixoDosAssemblies + "*."
            );
        }

        [TestCaseSource(nameof(Chaves))]
        public void Janela_TemEntrada_MontaSemErro_EFecha(string chave)
        {
            var definicao = JanelasDoJogo.Registro.Procurar(chave);

            ConferirPontoDeEntrada(definicao);

            // Esc fecha.
            AbrirSemErro(definicao);
            Assert.AreEqual(
                ResultadoDoEsc.FechouJanela,
                _gerente.ApertarEsc(),
                $"[{chave}] O Esc não fechou a janela."
            );
            ConferirFechada(chave, "Esc");

            // A mesma tecla abre e fecha.
            Assert.IsTrue(_gerente.Alternar(chave), $"[{chave}] A tecla não abriu a janela.");
            Assert.IsFalse(_gerente.Alternar(chave), $"[{chave}] A tecla não fechou a janela.");
            ConferirFechada(chave, "tecla " + definicao.Tecla);

            Assert.IsEmpty(
                _erros,
                $"[{chave}] Erro no console ao abrir ou fechar:\n" + string.Join("\n", _erros)
            );
        }

        private void ConferirPontoDeEntrada(DefinicaoDeJanela definicao)
        {
            var chave = definicao.Chave;
            Assert.IsTrue(
                RegistroDeJanelas.TeclaValida(definicao.Tecla),
                $"[{chave}] Sem tecla de A a Z para abrir a janela."
            );
            Assert.AreEqual(
                -1,
                RegistroDeJanelas.TeclasReservadas.IndexOf(definicao.Tecla[0]),
                $"[{chave}] A tecla {definicao.Tecla} é reservada a outra ação."
            );

            var barra = new BarraDeAtalhos(new VisualElement(), JanelasDoJogo.Registro.Definicoes);
            var textos = barra.Raiz.Query<Label>().ToList().Select(rotulo => rotulo.text).ToList();
            CollectionAssert.Contains(
                textos,
                definicao.Tecla,
                $"[{chave}] A tecla não aparece na barra de atalhos."
            );
            CollectionAssert.Contains(
                textos,
                definicao.Titulo,
                $"[{chave}] O título não aparece na barra de atalhos."
            );
        }

        private void AbrirSemErro(DefinicaoDeJanela definicao)
        {
            var chave = definicao.Chave;
            Assert.IsTrue(JanelasDoJogo.Abrir(chave), $"[{chave}] A janela não abriu.");
            Assert.IsTrue(JanelasDoJogo.AlgoAberto, $"[{chave}] Aberta, mas o jogo não parou.");
            Assert.AreEqual(chave, JanelasDoJogo.ChaveAberta);

            var raiz = _apresentador.Raiz;
            Assert.IsNotNull(raiz, $"[{chave}] A janela não apareceu na tela.");
            var falhou = raiz.Query<Label>()
                .ToList()
                .Any(rotulo => rotulo.text == ApresentadorDeJanelas.TextoFalha);
            Assert.IsFalse(
                falhou,
                $"[{chave}] A janela quebrou ao montar:\n" + string.Join("\n", _erros)
            );
        }

        private void ConferirFechada(string chave, string como)
        {
            Assert.IsFalse(
                JanelasDoJogo.AlgoAberto,
                $"[{chave}] Ficou algo aberto depois de {como}."
            );
            Assert.IsNull(
                JanelasDoJogo.ChaveAberta,
                $"[{chave}] Continua aberta depois de {como}."
            );
            Assert.AreEqual(
                0,
                _container.childCount,
                $"[{chave}] A janela ficou na tela depois de {como}."
            );
        }

        private void GuardarErro(string mensagem, string pilha, LogType tipo)
        {
            if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert)
            {
                _erros.Add(mensagem);
            }
        }

        private sealed class Descoberta
        {
            public readonly List<DefinicaoDeJanela> Definicoes = new List<DefinicaoDeJanela>();
            public readonly List<string> Problemas = new List<string>();
        }

        /// <summary>
        /// Executa, num registro limpo, cada método das funcionalidades que chama
        /// <see cref="JanelasDoJogo.Registrar"/>, e devolve o que ficou registrado.
        /// </summary>
        private static Descoberta Descobrir()
        {
            var resultado = new Descoberta();
            var registrar = typeof(JanelasDoJogo).GetMethod(nameof(JanelasDoJogo.Registrar));
            var antes = JanelasDoJogo.Registro.Definicoes.ToList();
            JanelasDoJogo.Registro.Limpar();
            try
            {
                foreach (var metodo in MetodosQueChamam(registrar))
                {
                    var nome = metodo.DeclaringType.FullName + "." + metodo.Name;
                    if (!metodo.IsStatic || metodo.GetParameters().Length > 0)
                    {
                        resultado.Problemas.Add(
                            $"{nome}: registre a janela num método estático sem parâmetros "
                                + "com [RuntimeInitializeOnLoadMethod]."
                        );
                        continue;
                    }

                    try
                    {
                        metodo.Invoke(null, null);
                    }
                    catch (TargetInvocationException e)
                    {
                        resultado.Problemas.Add($"{nome}: {e.InnerException?.Message}");
                    }
                }

                resultado.Definicoes.AddRange(JanelasDoJogo.Registro.Definicoes);
            }
            finally
            {
                JanelasDoJogo.Registro.Limpar();
                foreach (var definicao in antes)
                {
                    JanelasDoJogo.Registro.Registrar(definicao);
                }
            }

            return resultado;
        }

        private static IEnumerable<MethodInfo> MetodosQueChamam(MethodInfo alvo)
        {
            const BindingFlags Todos =
                BindingFlags.Public
                | BindingFlags.NonPublic
                | BindingFlags.Static
                | BindingFlags.Instance
                | BindingFlags.DeclaredOnly;

            foreach (var assembly in CurrentAssemblies.GetLoadedAssemblies())
            {
                var nome = assembly.GetName().Name;
                if (!nome.StartsWith(PrefixoDosAssemblies, StringComparison.Ordinal))
                {
                    continue;
                }

                if (nome.EndsWith(".Tests", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (var tipo in TiposDe(assembly))
                {
                    foreach (var metodo in tipo.GetMethods(Todos))
                    {
                        if (Chama(metodo, alvo))
                        {
                            yield return metodo;
                        }
                    }
                }
            }
        }

        private static IEnumerable<Type> TiposDe(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(tipo => tipo != null);
            }
        }

        /// <summary>
        /// Se o IL do método tem um <c>call</c> para <paramref name="alvo"/>. Lê o token depois
        /// de cada byte 0x28; um falso positivo precisaria resolver exatamente para o alvo.
        /// </summary>
        private static bool Chama(MethodInfo metodo, MethodInfo alvo)
        {
            byte[] il;
            try
            {
                il = metodo.GetMethodBody()?.GetILAsByteArray();
            }
            catch (Exception)
            {
                return false;
            }

            if (il == null)
            {
                return false;
            }

            const byte Call = 0x28;
            for (var i = 0; i + 4 < il.Length; i++)
            {
                if (il[i] != Call)
                {
                    continue;
                }

                var token = BitConverter.ToInt32(il, i + 1);
                try
                {
                    if (metodo.Module.ResolveMethod(token) == alvo)
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                    // Não era um token de método: segue procurando.
                }
            }

            return false;
        }
    }
}
