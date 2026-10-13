using System;
using JogoDoGrupo.Shared.Features.Personagem;

namespace JogoDoGrupo.Client.Features.Personagem
{
    /// <summary>
    /// Nível, experiência e pontos como o servidor mandou. O rascunho de gasto só existe na
    /// tela: some quando chega um <see cref="ProgressoResponse"/> e não muda os pontos oficiais
    /// se o pedido for recusado.
    /// </summary>
    internal sealed class EstadoDeProgresso
    {
        private readonly System.Collections.Generic.Dictionary<long, int> _niveisNaArea =
            new System.Collections.Generic.Dictionary<long, int>();

        public int Nivel { get; private set; } = 1;
        public long XpAtual { get; private set; }
        public long XpDoNivel { get; private set; }
        public int PontosLivres { get; private set; }
        public PontosDeAtributos Distribuidos { get; private set; } = new PontosDeAtributos();
        public PontosDeAtributos Rascunho { get; private set; } = new PontosDeAtributos();

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

        public bool PodeConfirmar =>
            RegrasAtributos.ValidarDistribuicao(PontosLivres, Rascunho) == null;

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
            Distribuidos = Copiar(progresso.Distribuidos);
            Rascunho = new PontosDeAtributos();
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

        /// <summary>Soma um ponto no rascunho se ainda cabe nos pontos livres.</summary>
        public bool AumentarRascunho(string atributo)
        {
            if (RegrasAtributos.Total(Rascunho) >= PontosLivres)
            {
                return false;
            }

            if (!Somar(Rascunho, atributo, 1))
            {
                return false;
            }

            Mudou?.Invoke();
            return true;
        }

        public void LimparRascunho()
        {
            Rascunho = new PontosDeAtributos();
            Mudou?.Invoke();
        }

        /// <summary>
        /// O pedido a enviar, ou <c>null</c> se <see cref="RegrasAtributos.ValidarDistribuicao"/>
        /// recusa. Não altera o progresso oficial.
        /// </summary>
        public DistribuirAtributosRequest MontarPedido()
        {
            if (!PodeConfirmar)
            {
                return null;
            }

            return new DistribuirAtributosRequest { Pontos = Copiar(Rascunho) };
        }

        private static PontosDeAtributos Copiar(PontosDeAtributos pontos)
        {
            if (pontos == null)
            {
                return new PontosDeAtributos();
            }

            return new PontosDeAtributos
            {
                Forca = pontos.Forca,
                Agilidade = pontos.Agilidade,
                Intelecto = pontos.Intelecto,
                Vigor = pontos.Vigor,
                Espirito = pontos.Espirito,
            };
        }

        private static bool Somar(PontosDeAtributos pontos, string atributo, int quantidade)
        {
            switch (atributo)
            {
                case "Forca":
                    pontos.Forca += quantidade;
                    return true;
                case "Agilidade":
                    pontos.Agilidade += quantidade;
                    return true;
                case "Intelecto":
                    pontos.Intelecto += quantidade;
                    return true;
                case "Vigor":
                    pontos.Vigor += quantidade;
                    return true;
                case "Espirito":
                    pontos.Espirito += quantidade;
                    return true;
                default:
                    return false;
            }
        }
    }
}
