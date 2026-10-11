using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Controle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace JogoDoGrupo.Client.Core.Tests
{
    /// <summary>
    /// Fallback da entrada no mundo: sem registro (ou com registro que falha), o Core monta
    /// chão e cápsula em vez de travar.
    /// </summary>
    public class EntradaNoJogoFallbackTestes
    {
        private readonly List<GameObject> _criados = new List<GameObject>();

        [TearDown]
        public void Limpar()
        {
            foreach (var objeto in _criados)
            {
                if (objeto != null)
                {
                    Object.DestroyImmediate(objeto);
                }
            }

            _criados.Clear();
        }

        [Test]
        public void SemConstrutor_MontaMundoPadraoComChao_ENasceNaOrigem()
        {
            var inicio = EntradaNoJogo.MontarMundo(null, out var mundo);
            _criados.Add(mundo);

            Assert.IsNotNull(mundo);
            Assert.AreEqual(Vector3.zero, inicio.Posicao);
            Assert.AreEqual(Quaternion.identity, inicio.Rotacao);
            var chao = mundo.transform.Find("Chao");
            Assert.IsNotNull(chao, "O mundo padrão precisa de chão.");
            Assert.IsNotNull(chao.GetComponent<Collider>(), "O chão precisa colidir.");
        }

        [Test]
        public void ComConstrutor_UsaAPosicaoDele_SemMundoPadrao()
        {
            var esperado = new PosicaoInicial(
                new Vector3(3f, 0f, -2f),
                Quaternion.Euler(0f, 90f, 0f)
            );

            var inicio = EntradaNoJogo.MontarMundo(() => esperado, out var mundo);

            Assert.IsNull(mundo);
            Assert.AreEqual(esperado.Posicao, inicio.Posicao);
            Assert.AreEqual(esperado.Rotacao.eulerAngles.y, inicio.Rotacao.eulerAngles.y, 0.01f);
        }

        [Test]
        public void ConstrutorDevolveRotacaoVazia_UsaIdentidade()
        {
            var inicio = EntradaNoJogo.MontarMundo(
                () => new PosicaoInicial(Vector3.one, default),
                out _
            );

            Assert.AreEqual(Quaternion.identity, inicio.Rotacao);
        }

        [Test]
        public void ConstrutorFalha_MontaMundoPadrao()
        {
            LogAssert.Expect(LogType.Exception, "InvalidOperationException: falhou");
            LogAssert.Expect(
                LogType.Warning,
                new System.Text.RegularExpressions.Regex("mundo padrão")
            );

            var inicio = EntradaNoJogo.MontarMundo(
                () => throw new InvalidOperationException("falhou"),
                out var mundo
            );
            _criados.Add(mundo);

            Assert.IsNotNull(mundo);
            Assert.AreEqual(Vector3.zero, inicio.Posicao);
        }

        [Test]
        public void SemVisual_CriaCapsulaSemColisor()
        {
            var visual = EntradaNoJogo.CriarVisual(null, 1);
            _criados.Add(visual);

            Assert.AreEqual(MundoPadrao.NomeCapsula, visual.name);
            Assert.IsNotNull(visual.GetComponentInChildren<MeshRenderer>());
            Assert.IsEmpty(visual.GetComponentsInChildren<Collider>());
        }

        [Test]
        public void VisualNulo_CriaCapsula()
        {
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("nulo"));

            var visual = EntradaNoJogo.CriarVisual(_ => null, 7);
            _criados.Add(visual);

            Assert.AreEqual(MundoPadrao.NomeCapsula, visual.name);
        }

        [Test]
        public void VisualFalha_CriaCapsula()
        {
            LogAssert.Expect(LogType.Exception, "InvalidOperationException: sem prefab");
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("cápsula"));

            var visual = EntradaNoJogo.CriarVisual(
                _ => throw new InvalidOperationException("sem prefab"),
                7
            );
            _criados.Add(visual);

            Assert.AreEqual(MundoPadrao.NomeCapsula, visual.name);
        }

        [Test]
        public void ComVisual_RecebeOIdEDevolveOVisualDele()
        {
            long recebido = 0;
            var meu = new GameObject("MeuVisual");
            _criados.Add(meu);

            var visual = EntradaNoJogo.CriarVisual(
                id =>
                {
                    recebido = id;
                    return meu;
                },
                42
            );

            Assert.AreEqual(42, recebido);
            Assert.AreSame(meu, visual);
        }

        [Test]
        public void Animador_SemAnimator_NaoFalha()
        {
            var animador = new AnimadorDoVisual(null);

            Assert.DoesNotThrow(() => animador.Atualizar(3f, true));
        }
    }
}
