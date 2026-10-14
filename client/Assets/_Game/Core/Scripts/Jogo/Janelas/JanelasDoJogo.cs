using System;
using UnityEngine;

namespace JogoDoGrupo.Client.Core.Jogo.Janelas
{
    /// <summary>
    /// Janelas do jogo abertas por atalho (C personagem, I inventário, L missões, M mapa).
    /// Cada funcionalidade registra a sua, sem conhecer as outras; o Core cuida do atalho, da
    /// moldura, do Esc, do cursor e da barra de atalhos. Uma janela por vez, e com janela
    /// aberta o personagem fica parado. Regras em <c>docs/features/interface.md</c>.
    /// </summary>
    /// <remarks>
    /// Registre num método com
    /// <c>[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]</c>:
    /// <code>
    /// [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    /// private static void RegistrarJanela()
    /// {
    ///     JanelasDoJogo.Registrar(
    ///         new DefinicaoDeJanela("missoes.registro", "Missões", "L", 30, () => new JanelaMissoes())
    ///     );
    /// }
    /// </code>
    /// </remarks>
    public static class JanelasDoJogo
    {
        private static readonly RegistroDeJanelas _registro = new RegistroDeJanelas();

        private static GerenteDeJanelas _gerente;

        /// <summary>Chave da janela aberta agora, ou <c>null</c> (também fora do mundo).</summary>
        public static string ChaveAberta => _gerente?.ChaveAberta;

        /// <summary>
        /// Se há janela ou menu do Esc na tela (o personagem está parado e o cursor, livre).
        /// Falso fora do mundo.
        /// </summary>
        public static bool AlgoAberto => _gerente != null && _gerente.AlgoAberto;

        /// <summary>Registra a janela de uma funcionalidade.</summary>
        /// <exception cref="ArgumentNullException">Definição ou fábrica nula.</exception>
        /// <exception cref="ArgumentException">
        /// Chave ou título vazios, tecla inválida ou reservada, ou chave ou tecla repetidas.
        /// </exception>
        public static void Registrar(DefinicaoDeJanela definicao)
        {
            _registro.Registrar(definicao);
            TeclasDoJogo.RegistrarJanela(definicao.Chave, definicao.Tecla);
        }

        /// <summary>Se existe janela registrada com essa chave.</summary>
        public static bool EstaRegistrada(string chave)
        {
            return _registro.Procurar(chave) != null;
        }

        /// <summary>
        /// Abre a janela pela chave, como se a pessoa tivesse apertado a tecla (ex.: o baú abre
        /// o inventário). Não abre fora do mundo, com o menu do Esc aberto ou sem registro.
        /// </summary>
        /// <returns><c>true</c> se a janela está aberta ao final.</returns>
        public static bool Abrir(string chave)
        {
            if (_gerente == null)
            {
                return false;
            }

            if (_registro.Procurar(chave) == null)
            {
                Debug.LogError(
                    $"[JanelasDoJogo] Nenhuma janela registrada com a chave \"{chave}\"."
                );
                return false;
            }

            return _gerente.Abrir(chave);
        }

        /// <summary>Fecha a janela aberta.</summary>
        /// <returns><c>true</c> se havia uma janela aberta.</returns>
        public static bool Fechar()
        {
            return _gerente != null && _gerente.Fechar();
        }

        /// <summary>Janelas registradas, usadas pela sessão no mundo.</summary>
        internal static RegistroDeJanelas Registro => _registro;

        /// <summary>Chamado pela sessão no mundo ao entrar: a partir daqui as janelas abrem.</summary>
        internal static void Ligar(GerenteDeJanelas gerente)
        {
            _gerente = gerente;
        }

        /// <summary>Chamado pela sessão no mundo ao sair.</summary>
        internal static void Desligar(GerenteDeJanelas gerente)
        {
            if (_gerente == gerente)
            {
                _gerente = null;
            }
        }

        // Com "Enter Play Mode Options" sem recarregar o domínio, os campos estáticos
        // sobrevivem entre execuções; limpa antes de qualquer registro.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _registro.Limpar();
            _gerente = null;
        }
    }
}
