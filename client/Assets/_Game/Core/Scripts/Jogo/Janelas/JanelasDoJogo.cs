using System;
using UnityEngine;

namespace JogoDoGrupo.Client.Core.Jogo.Janelas
{
    /// <summary>
    /// Janelas do jogo abertas por atalho (C personagem, I inventário, L missões, M mapa).
    /// Cada funcionalidade registra a sua, sem conhecer as outras; o Core cuida do atalho, da
    /// moldura, do Esc, do cursor e da barra de atalhos. Várias janelas abrem ao mesmo tempo e
    /// o personagem anda com elas; o Esc fecha a de cima. Regras em <c>docs/features/interface.md</c>.
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

        /// <summary>
        /// Chave da janela de cima (a última aberta ou focada), ou <c>null</c> (também fora do
        /// mundo). Para saber se uma janela específica está aberta, use <see cref="EstaAberta"/>.
        /// </summary>
        public static string ChaveAberta => _gerente?.ChaveAberta;

        /// <summary>
        /// Se há janela ou menu do Esc na tela. Não use para bloquear ações: com janelas comuns
        /// abertas, E, F, montaria, chat e portal seguem valendo (estilo WoW). Para o que para
        /// no menu, use <see cref="MenuAberto"/>; o combate segue o cursor preso. Falso fora do mundo.
        /// </summary>
        public static bool AlgoAberto => _gerente != null && _gerente.AlgoAberto;

        /// <summary>
        /// Se o menu do Esc está aberto: o personagem para e nenhuma ação do jogo dispara.
        /// Falso fora do mundo.
        /// </summary>
        public static bool MenuAberto => _gerente != null && _gerente.MenuAberto;

        /// <summary>Se há ao menos uma janela aberta (sem contar o menu do Esc).</summary>
        public static bool JanelaAberta => _gerente != null && _gerente.JanelaAberta;

        /// <summary>Se a janela com essa chave está aberta. Falso fora do mundo.</summary>
        public static bool EstaAberta(string chave)
        {
            return _gerente != null && _gerente.EstaAberta(chave);
        }

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
        /// o inventário). Abre por cima das outras; se já está aberta, só sobe. Não abre fora do mundo, com o menu do Esc aberto ou sem registro.
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

        /// <summary>Fecha a janela de cima.</summary>
        /// <returns><c>true</c> se havia uma janela aberta.</returns>
        public static bool Fechar()
        {
            return _gerente != null && _gerente.Fechar();
        }

        /// <summary>Fecha a janela com essa chave, esteja ela em cima ou não.</summary>
        /// <returns><c>true</c> se ela estava aberta.</returns>
        public static bool Fechar(string chave)
        {
            return _gerente != null && _gerente.Fechar(chave);
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
