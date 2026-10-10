using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Contas;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.UI
{
    /// <summary>
    /// Troca a tela visível do client. Qualquer assembly registra telas por chave e o Core
    /// abre pela chave, sem referenciar a funcionalidade dona da tela.
    /// Só uma tela fica aberta por vez.
    /// </summary>
    /// <remarks>
    /// Registre as telas da funcionalidade num método estático com
    /// <c>[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]</c>:
    /// o Core limpa os registros antes (em <c>SubsystemRegistration</c>) e só abre a primeira
    /// tela depois (em <c>AfterSceneLoad</c>).
    /// <code>
    /// [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    /// private static void RegistrarTelas()
    /// {
    ///     Navegacao.Registrar("personagem.selecao", () => new TelaSelecao());
    ///     Navegacao.Registrar("personagem.criacao", () => new TelaCriacao());
    ///     Navegacao.DefinirTelaAposLogin("personagem.selecao");
    /// }
    /// </code>
    /// </remarks>
    public static class Navegacao
    {
        private static readonly Dictionary<string, Func<Tela>> _fabricas =
            new Dictionary<string, Func<Tela>>();

        private static VisualElement _container;
        private static Tela _telaAtual;
        private static string _chaveAtual;
        private static string _chaveTelaAposLogin;

        /// <summary>Chave da tela aberta agora, ou <c>null</c> se nenhuma.</summary>
        public static string ChaveAtual => _chaveAtual;

        /// <summary>Tela aberta agora, ou <c>null</c> se nenhuma.</summary>
        public static Tela TelaAtual => _telaAtual;

        /// <summary>
        /// Chave definida em <see cref="DefinirTelaAposLogin"/>, ou <c>null</c> se nenhuma.
        /// </summary>
        public static string ChaveTelaAposLogin => _chaveTelaAposLogin;

        /// <summary>
        /// Registra uma tela. <paramref name="criar"/> é chamado a cada abertura e deve devolver
        /// uma instância nova.
        /// </summary>
        /// <exception cref="ArgumentException">Chave vazia ou já registrada.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="criar"/> nulo.</exception>
        public static void Registrar(string chave, Func<Tela> criar)
        {
            if (string.IsNullOrEmpty(chave))
            {
                throw new ArgumentException("A chave da tela não pode ser vazia.", nameof(chave));
            }

            if (criar == null)
            {
                throw new ArgumentNullException(nameof(criar));
            }

            if (_fabricas.ContainsKey(chave))
            {
                throw new ArgumentException(
                    $"Já existe uma tela registrada com a chave \"{chave}\".",
                    nameof(chave)
                );
            }

            _fabricas.Add(chave, criar);
        }

        /// <summary>Se existe tela registrada com essa chave.</summary>
        public static bool EstaRegistrada(string chave)
        {
            return !string.IsNullOrEmpty(chave) && _fabricas.ContainsKey(chave);
        }

        /// <summary>
        /// Fecha a tela atual e abre a tela registrada com <paramref name="chave"/>
        /// (sempre uma instância nova, mesmo se for a mesma chave).
        /// Se a chave não estiver registrada, registra o erro no log e mantém a tela atual.
        /// </summary>
        /// <returns><c>true</c> se a tela foi aberta.</returns>
        public static bool Abrir(string chave)
        {
            if (_container == null)
            {
                Debug.LogError(
                    $"[Navegacao] A interface ainda não foi criada; não foi possível abrir \"{chave}\"."
                );
                return false;
            }

            if (!EstaRegistrada(chave))
            {
                Debug.LogError($"[Navegacao] Nenhuma tela registrada com a chave \"{chave}\".");
                return false;
            }

            Tela nova;
            try
            {
                nova = _fabricas[chave]();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }

            if (nova == null)
            {
                Debug.LogError($"[Navegacao] A fábrica da tela \"{chave}\" devolveu null.");
                return false;
            }

            FecharAtual();

            var raiz = new VisualElement { name = chave };
            raiz.AddToClassList(EstiloCore.Tela);
            _telaAtual = nova;
            _chaveAtual = chave;
            _container.Add(raiz);

            try
            {
                nova.Abrir(raiz);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            return true;
        }

        /// <summary>
        /// Define qual tela abrir depois do login (ex.: a seleção de personagem).
        /// A chave pode ser registrada antes ou depois; ela só é procurada na hora de abrir.
        /// Se nada for definido, o Core mostra a tela <see cref="TelasCore.SemTelaAposLogin"/>.
        /// </summary>
        public static void DefinirTelaAposLogin(string chave)
        {
            _chaveTelaAposLogin = chave;
        }

        /// <summary>
        /// Abre a tela definida em <see cref="DefinirTelaAposLogin"/> ou, se não houver uma
        /// registrada, a tela <see cref="TelasCore.SemTelaAposLogin"/>. Usado pelo login.
        /// </summary>
        public static void AbrirTelaAposLogin()
        {
            if (EstaRegistrada(_chaveTelaAposLogin))
            {
                Abrir(_chaveTelaAposLogin);
                return;
            }

            if (!string.IsNullOrEmpty(_chaveTelaAposLogin))
            {
                Debug.LogWarning(
                    $"[Navegacao] A tela após o login \"{_chaveTelaAposLogin}\" não foi registrada."
                );
            }

            Abrir(TelasCore.SemTelaAposLogin);
        }

        /// <summary>
        /// Encerra a sessão (<see cref="Sessao.Encerrar"/>) e abre o login. Use para "Sair".
        /// </summary>
        /// <param name="aviso">
        /// Mensagem opcional mostrada no login (ex.: "Sua sessão expirou. Entre de novo.").
        /// </param>
        public static void VoltarParaLogin(string aviso = null)
        {
            Sessao.Encerrar();
            TelaLogin.AvisoAoAbrir = aviso;
            Abrir(TelasCore.Login);
        }

        /// <summary>Chamado pelo inicializador do Core com o elemento que recebe as telas.</summary>
        internal static void Inicializar(VisualElement container)
        {
            _container = container;
        }

        private static void FecharAtual()
        {
            var atual = _telaAtual;
            if (atual == null)
            {
                return;
            }

            _telaAtual = null;
            _chaveAtual = null;

            try
            {
                atual.Fechar();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            atual.Raiz?.RemoveFromHierarchy();
        }

        // Com "Enter Play Mode Options" sem recarregar o domínio, os campos estáticos
        // sobrevivem entre execuções; limpa tudo antes de qualquer registro.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _fabricas.Clear();
            _container = null;
            _telaAtual = null;
            _chaveAtual = null;
            _chaveTelaAposLogin = null;
        }
    }
}
