using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Shared.Features.Combate;
using NUnit.Framework;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Combate.Tests
{
    /// <summary>
    /// Textos e avisos da defesa no client (sem regra de dano, que é do servidor).
    /// </summary>
    public class ApresentacaoDefesaTestes
    {
        [TestCase(12, 40, "12 de 40 passaram")]
        [TestCase(40, 40, "40 de 40 passaram")]
        [TestCase(2, 5, "2 de 5 passaram")]
        public void TextoDefesaInsuficiente_DanoValidoPlural_MostraQuantoPassou(
            int danoResidual,
            int danoOriginal,
            string esperado
        )
        {
            var texto = ApresentacaoDefesa.TextoDefesaInsuficiente(danoResidual, danoOriginal);
            Assert.AreEqual(esperado, texto);
        }

        [TestCase(1, 40, "1 de 40 passou")]
        [TestCase(1, 1, "1 de 1 passou")]
        public void TextoDefesaInsuficiente_DanoValidoSingular_MostraNoSingular(
            int danoResidual,
            int danoOriginal,
            string esperado
        )
        {
            var texto = ApresentacaoDefesa.TextoDefesaInsuficiente(danoResidual, danoOriginal);
            Assert.AreEqual(esperado, texto);
        }

        [TestCase(0, 40)]
        [TestCase(-1, 40)]
        [TestCase(10, 0)]
        [TestCase(10, -5)]
        [TestCase(0, 0)]
        public void TextoDefesaInsuficiente_NumerosInvalidosOuZero_UsaTextoPadrao(
            int danoResidual,
            int danoOriginal
        )
        {
            var texto = ApresentacaoDefesa.TextoDefesaInsuficiente(danoResidual, danoOriginal);
            Assert.AreEqual(ApresentacaoDefesa.TextoDefesaInsuficientePadrao, texto);
        }

        [Test]
        public void AoDefesaInsuficiente_MesmoPersonagem_DisparaAvisoCombateComDano()
        {
            var objeto = new GameObject();
            try
            {
                var controle = objeto.AddComponent<ControleDefesa>();
                controle.DefinirPersonagemIdParaTestes(123);

                string textoRecebido = null;
                TipoDeAviso tipoRecebido = TipoDeAviso.Informacao;
                CategoriaDeAviso categoriaRecebida = CategoriaDeAviso.Sistema;

                void AoMostrar(string texto, TipoDeAviso tipo, CategoriaDeAviso cat)
                {
                    textoRecebido = texto;
                    tipoRecebido = tipo;
                    categoriaRecebida = cat;
                }

                AvisosDoJogo.MostradoComCategoria += AoMostrar;
                try
                {
                    var aviso = new DefesaInsuficiente
                    {
                        PersonagemId = 123,
                        DanoOriginal = 40,
                        DanoResidual = 12,
                    };

                    controle.AoDefesaInsuficiente(aviso);

                    Assert.AreEqual("12 de 40 passaram", textoRecebido);
                    Assert.AreEqual(TipoDeAviso.Aviso, tipoRecebido);
                    Assert.AreEqual(CategoriaDeAviso.Combate, categoriaRecebida);
                }
                finally
                {
                    AvisosDoJogo.MostradoComCategoria -= AoMostrar;
                }
            }
            finally
            {
                Object.DestroyImmediate(objeto);
            }
        }

        [Test]
        public void AoDefesaInsuficiente_OutroPersonagem_NaoDisparaAviso()
        {
            var objeto = new GameObject();
            try
            {
                var controle = objeto.AddComponent<ControleDefesa>();
                controle.DefinirPersonagemIdParaTestes(123);

                var disparou = false;
                void AoMostrar(string texto, TipoDeAviso tipo, CategoriaDeAviso cat) =>
                    disparou = true;

                AvisosDoJogo.MostradoComCategoria += AoMostrar;
                try
                {
                    var aviso = new DefesaInsuficiente
                    {
                        PersonagemId = 999,
                        DanoOriginal = 40,
                        DanoResidual = 12,
                    };

                    controle.AoDefesaInsuficiente(aviso);

                    Assert.IsFalse(disparou);
                }
                finally
                {
                    AvisosDoJogo.MostradoComCategoria -= AoMostrar;
                }
            }
            finally
            {
                Object.DestroyImmediate(objeto);
            }
        }

        [Test]
        public void AoDefesaInsuficiente_AvisoNulo_NaoDisparaNemLanca()
        {
            var objeto = new GameObject();
            try
            {
                var controle = objeto.AddComponent<ControleDefesa>();
                controle.DefinirPersonagemIdParaTestes(123);

                var disparou = false;
                void AoMostrar(string texto, TipoDeAviso tipo, CategoriaDeAviso cat) =>
                    disparou = true;

                AvisosDoJogo.MostradoComCategoria += AoMostrar;
                try
                {
                    Assert.DoesNotThrow(() => controle.AoDefesaInsuficiente(null));
                    Assert.IsFalse(disparou);
                }
                finally
                {
                    AvisosDoJogo.MostradoComCategoria -= AoMostrar;
                }
            }
            finally
            {
                Object.DestroyImmediate(objeto);
            }
        }
    }
}
