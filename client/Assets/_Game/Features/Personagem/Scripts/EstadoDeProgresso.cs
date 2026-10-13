using System;
using JogoDoGrupo.Shared.Features.Personagem;

namespace JogoDoGrupo.Client.Features.Personagem
{
    /// <summary>
    /// Nível, experiência e pontos da árvore como o servidor mandou. Os pontos gastam na árvore
    /// de atributos (Talentos); aqui só se mostra o saldo.
    /// </summary>
    internal sealed class EstadoDeProgresso
    {
        private readonly System.Collections.Generic.Dictionary<long, int> _niveisNaArea =
            new System.Collections.Generic.Dictionary<long, int>();

        public int Nivel { get; private set; } = 1;
        public long XpAtual { get; private set; }
        public long XpDoNivel { get; private set; }
        public int PontosLivres { get; private set; }

        public event Action Mudou;

        /// <summary>Trecho da barra, de 0 a 1. No nível máximo o custo é só visual.</summary>
        public float FracaoDaBarra
        {
            get
            {
                if (XpDoNivel <= 0)
                {
                    return 0f;
                }

                if (XpAtual >= XpDoNivel)
                {
                    return 1f;
                }

                return (float)XpAtual / XpDoNivel;
            }
        }

        public void Aplicar(ProgressoResponse progresso)
        {
            if (progresso == null)
            {
                return;
            }

            Nivel = progresso.Nivel;
            XpAtual = progresso.XpAtual;
            XpDoNivel = progresso.XpDoNivel;
            PontosLivres = progresso.PontosLivres;
            Mudou?.Invoke();
        }

        /// <summary>
        /// O servidor já manda o estado depois do ganho. <see cref="ExperienciaGanha.Quantidade"/>
        /// não é somada de novo.
        /// </summary>
        public void Aplicar(ExperienciaGanha ganho)
        {
            if (ganho == null)
            {
                return;
            }

            Nivel = ganho.Nivel;
            XpAtual = ganho.XpAtual;
            XpDoNivel = ganho.XpDoNivel;
            PontosLivres = ganho.PontosLivres;
            Mudou?.Invoke();
        }

        public void Aplicar(SubiuDeNivel aviso)
        {
            if (aviso == null)
            {
                return;
            }

            _niveisNaArea[aviso.PersonagemId] = aviso.Nivel;
            Mudou?.Invoke();
        }

        public bool TentarNivelNaArea(long personagemId, out int nivel) =>
            _niveisNaArea.TryGetValue(personagemId, out nivel);
    }
}
