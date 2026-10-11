using System;
using System.Collections.Generic;

namespace JogoDoGrupo.Client.Core.Jogo.Janelas
{
    /// <summary>
    /// Lista das janelas registradas, sem interface: confere chave e tecla e devolve as
    /// janelas na ordem da barra de atalhos. Usado pela <see cref="JanelasDoJogo"/>.
    /// </summary>
    internal sealed class RegistroDeJanelas
    {
        /// <summary>
        /// Teclas que uma janela não pode usar: já são de outra ação na tabela de
        /// <c>docs/features/interface.md</c> (andar, bloqueio, interagir, trocar de arma).
        /// </summary>
        internal const string TeclasReservadas = "WASDQER";

        private readonly List<DefinicaoDeJanela> _definicoes = new List<DefinicaoDeJanela>();

        /// <summary>Janelas registradas, por <see cref="DefinicaoDeJanela.Ordem"/>.</summary>
        public IReadOnlyList<DefinicaoDeJanela> Definicoes => _definicoes;

        /// <exception cref="ArgumentNullException">Definição ou fábrica nula.</exception>
        /// <exception cref="ArgumentException">
        /// Chave ou título vazios, tecla que não é uma letra de A a Z, tecla reservada, ou
        /// chave ou tecla já usadas por outra janela.
        /// </exception>
        public void Registrar(DefinicaoDeJanela definicao)
        {
            if (definicao == null)
            {
                throw new ArgumentNullException(nameof(definicao));
            }

            if (definicao.Criar == null)
            {
                throw new ArgumentNullException(nameof(definicao), "A janela precisa de fábrica.");
            }

            if (string.IsNullOrEmpty(definicao.Chave))
            {
                throw new ArgumentException("A chave da janela não pode ser vazia.");
            }

            if (string.IsNullOrEmpty(definicao.Titulo))
            {
                throw new ArgumentException(
                    $"A janela \"{definicao.Chave}\" precisa de um título."
                );
            }

            if (!TeclaValida(definicao.Tecla))
            {
                throw new ArgumentException(
                    $"A tecla da janela \"{definicao.Chave}\" deve ser uma letra de A a Z."
                );
            }

            if (TeclasReservadas.IndexOf(definicao.Tecla[0]) >= 0)
            {
                throw new ArgumentException(
                    $"A tecla {definicao.Tecla} é reservada e não pode abrir \"{definicao.Chave}\"."
                );
            }

            foreach (var outra in _definicoes)
            {
                if (outra.Chave == definicao.Chave)
                {
                    throw new ArgumentException(
                        $"Já existe uma janela registrada com a chave \"{definicao.Chave}\"."
                    );
                }

                if (outra.Tecla == definicao.Tecla)
                {
                    throw new ArgumentException(
                        $"A tecla {definicao.Tecla} já abre \"{outra.Chave}\"; "
                            + $"\"{definicao.Chave}\" precisa de outra."
                    );
                }
            }

            // Mantém a lista por ordem; empate fica na ordem de registro.
            var posicao = _definicoes.Count;
            for (var i = 0; i < _definicoes.Count; i++)
            {
                if (_definicoes[i].Ordem > definicao.Ordem)
                {
                    posicao = i;
                    break;
                }
            }

            _definicoes.Insert(posicao, definicao);
        }

        /// <summary>A janela com essa chave, ou <c>null</c>.</summary>
        public DefinicaoDeJanela Procurar(string chave)
        {
            if (string.IsNullOrEmpty(chave))
            {
                return null;
            }

            foreach (var definicao in _definicoes)
            {
                if (definicao.Chave == chave)
                {
                    return definicao;
                }
            }

            return null;
        }

        public void Limpar()
        {
            _definicoes.Clear();
        }

        /// <summary>Se é exatamente uma letra de A a Z, em maiúscula.</summary>
        internal static bool TeclaValida(string tecla)
        {
            return tecla != null && tecla.Length == 1 && tecla[0] >= 'A' && tecla[0] <= 'Z';
        }
    }
}
