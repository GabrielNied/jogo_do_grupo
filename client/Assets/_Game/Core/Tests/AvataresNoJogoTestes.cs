using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JogoDoGrupo.Client.Core.Tests
{
    /// <summary>Registro dos avatares por Id: registrar, obter, remover e limpar ao sair.</summary>
    public class AvataresNoJogoTestes
    {
        private readonly List<GameObject> _criados = new List<GameObject>();

        [TearDown]
        public void Limpar()
        {
            AvataresNoJogo.Limpar();
            foreach (var objeto in _criados)
            {
                if (objeto != null)
                {
                    Object.DestroyImmediate(objeto);
                }
            }

            _criados.Clear();
        }

        private GameObject Criar(string nome)
        {
            var objeto = new GameObject(nome);
            _criados.Add(objeto);
            return objeto;
        }

        [Test]
        public void Registrado_EhObtidoPeloId()
        {
            var avatar = Criar("Avatar");
            AvataresNoJogo.Registrar(7, avatar.transform, "Ana");

            Assert.IsTrue(AvataresNoJogo.TentarObter(7, out var raiz));
            Assert.AreSame(avatar.transform, raiz);
            Assert.AreSame(avatar, AvataresNoJogo.Obter(7));
            Assert.IsFalse(AvataresNoJogo.TentarObter(8, out _));
            Assert.IsNull(AvataresNoJogo.Obter(8));
            Assert.AreEqual(1, AvataresNoJogo.Todos.Count);
        }

        [Test]
        public void RegistrarDeNovo_TrocaOAvatarEONome()
        {
            AvataresNoJogo.Registrar(7, Criar("Antigo").transform, "Ana");
            var novo = Criar("Novo");
            AvataresNoJogo.Registrar(7, novo.transform, "Bia");

            Assert.IsTrue(AvataresNoJogo.TentarObter(7, out var raiz));
            Assert.AreSame(novo.transform, raiz);
            foreach (var avatar in AvataresNoJogo.Todos)
            {
                Assert.AreEqual("Bia", avatar.Nome);
            }
        }

        [Test]
        public void IdInvalidoOuRaizNula_NaoRegistra()
        {
            AvataresNoJogo.Registrar(0, Criar("Zero").transform);
            AvataresNoJogo.Registrar(-1, Criar("Negativo").transform);
            AvataresNoJogo.Registrar(3, null);

            Assert.AreEqual(0, AvataresNoJogo.Todos.Count);
        }

        [Test]
        public void Removido_NaoEhMaisObtido()
        {
            AvataresNoJogo.Registrar(7, Criar("Avatar").transform);
            AvataresNoJogo.Remover(7);
            AvataresNoJogo.Remover(99);

            Assert.IsFalse(AvataresNoJogo.TentarObter(7, out _));
        }

        [Test]
        public void Destruido_SaiDoRegistroAoObter()
        {
            var avatar = Criar("Avatar");
            AvataresNoJogo.Registrar(7, avatar.transform);
            Object.DestroyImmediate(avatar);

            Assert.IsFalse(AvataresNoJogo.TentarObter(7, out _));
            Assert.AreEqual(0, AvataresNoJogo.Todos.Count);
        }

        [Test]
        public void SairDoJogo_LimpaORegistro()
        {
            AvataresNoJogo.Registrar(7, Criar("Avatar").transform);

            EntradaNoJogo.Desmontar();

            Assert.AreEqual(0, AvataresNoJogo.Todos.Count);
        }
    }
}
