using System;
using System.Collections.Generic;

namespace JogoDoGrupo.Client.Core.Jogo
{
    /// <summary>Uma linha das mensagens de sistema.</summary>
    internal sealed class LinhaDeAviso
    {
        public LinhaDeAviso(string texto, TipoDeAviso tipo, double agora)
        {
            Texto = texto;
            Tipo = tipo;
            Contagem = 1;
            UltimaVez = agora;
        }

        public string Texto { get; }

        public TipoDeAviso Tipo { get; }

        /// <summary>Quantas vezes a mensagem chegou seguida (aparece como "x2", "x3"...).</summary>
        public int Contagem { get; private set; }

        /// <summary>Quando a mensagem chegou pela última vez, em segundos do relógio.</summary>
        public double UltimaVez { get; private set; }

        /// <summary>O que a pessoa lê: o texto e, se repetiu, o contador.</summary>
        public string TextoNaTela => Contagem > 1 ? $"{Texto} x{Contagem}" : Texto;

        public void Repetir(double agora)
        {
            Contagem++;
            UltimaVez = agora;
        }
    }

    /// <summary>
    /// Regras das mensagens de sistema, sem interface: até <see cref="Maximo"/> linhas, cada
    /// uma some depois de <see cref="Duracao"/> segundos, a linha nova entra embaixo e a mesma
    /// mensagem repetida em menos de <see cref="JanelaDeRepeticao"/> segundos vira contador
    /// e recomeça o tempo. Regras em <c>docs/features/interface.md</c>, seção "Mensagens de
    /// sistema".
    /// </summary>
    internal sealed class ListaDeAvisos
    {
        public const int Maximo = 5;
        public const double Duracao = 6.0;
        public const double JanelaDeRepeticao = 2.0;

        private readonly List<LinhaDeAviso> _linhas = new List<LinhaDeAviso>();
        private readonly Func<double> _relogio;

        /// <param name="relogio">Hora atual em segundos (injetável para os testes).</param>
        public ListaDeAvisos(Func<double> relogio)
        {
            _relogio = relogio ?? throw new ArgumentNullException(nameof(relogio));
        }

        /// <summary>Linhas na tela, da mais antiga (em cima) para a mais nova (embaixo).</summary>
        public IReadOnlyList<LinhaDeAviso> Linhas => _linhas;

        /// <summary>Acrescenta a mensagem (ou soma no contador da repetida). Texto vazio é ignorado.</summary>
        /// <returns><c>true</c> se as linhas mudaram.</returns>
        public bool Mostrar(string texto, TipoDeAviso tipo)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return false;
            }

            var agora = _relogio();
            RemoverVencidas(agora);

            for (var i = _linhas.Count - 1; i >= 0; i--)
            {
                var linha = _linhas[i];
                if (
                    linha.Texto == texto
                    && linha.Tipo == tipo
                    && agora - linha.UltimaVez < JanelaDeRepeticao
                )
                {
                    linha.Repetir(agora);
                    return true;
                }
            }

            _linhas.Add(new LinhaDeAviso(texto, tipo, agora));
            while (_linhas.Count > Maximo)
            {
                _linhas.RemoveAt(0);
            }

            return true;
        }

        /// <summary>Tira as linhas que já passaram de <see cref="Duracao"/>.</summary>
        /// <returns><c>true</c> se alguma linha saiu.</returns>
        public bool Atualizar()
        {
            return RemoverVencidas(_relogio());
        }

        public void Limpar()
        {
            _linhas.Clear();
        }

        private bool RemoverVencidas(double agora)
        {
            return _linhas.RemoveAll(linha => agora - linha.UltimaVez >= Duracao) > 0;
        }
    }
}
