using System;

namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Fórmulas puras do inimigo comum da primeira fatia: visão, alvo, perseguição, golpe
    /// telegrafado e desistência. Valores da proposta em docs/features/fatia-jogavel.md, seção 4,
    /// e docs/features/mobs.md. Vida, dano e tempo de volta são da ficha em
    /// <see cref="CatalogoCriaturas"/>. O servidor é quem decide; o client usa só para mostrar
    /// (aviso no chão, animação). Tudo é medido no plano do chão: a altura não conta.
    /// </summary>
    public static class RegrasInimigo
    {
        // ── Balanceamento (proposta, a calibrar) ─────────────────────────────────

        /// <summary>Criatura dos grupos de inimigos da primeira fatia.</summary>
        public const int CriaturaDaFatia = CatalogoCriaturas.JavaliDoCampo;

        /// <summary>Menor número de inimigos num grupo.</summary>
        public const int TamanhoMinimoDoGrupo = 3;

        /// <summary>Maior número de inimigos num grupo.</summary>
        public const int TamanhoMaximoDoGrupo = 5;

        /// <summary>Distância, em metros, em que o inimigo em repouso vê um jogador.</summary>
        public const double DistanciaDeVisaoMetros = 12.0;

        /// <summary>Segundos em alerta antes de começar a perseguir.</summary>
        public const double AlertaSegundos = 0.5;

        /// <summary>Velocidade da perseguição (mais lenta que o jogador correndo).</summary>
        public const double VelocidadeDePerseguicaoMetrosPorSegundo = 4.0;

        /// <summary>Velocidade da volta ao ponto de origem.</summary>
        public const double VelocidadeDeRetornoMetrosPorSegundo = 6.0;

        /// <summary>Distância do alvo, em metros, a partir da qual o inimigo já ataca.</summary>
        public const double AlcanceDeAtaqueMetros = 2.0;

        /// <summary>Segundos de aviso entre o começo do ataque e o golpe.</summary>
        public const double PreparoDoAtaqueSegundos = 1.0;

        /// <summary>Segundos parado depois do golpe, antes de agir de novo.</summary>
        public const double RecuperacaoDoAtaqueSegundos = 1.5;

        /// <summary>Distância do inimigo ao centro da área do golpe, à frente dele.</summary>
        public const double DistanciaDoGolpeMetros = 1.5;

        /// <summary>Raio da área do golpe (círculo no chão).</summary>
        public const double RaioDoGolpeMetros = 1.5;

        /// <summary>Raio do corpo do inimigo, usado como alvo dos golpes dos jogadores.</summary>
        public const double RaioDoCorpoMetros = 0.6;

        /// <summary>Longe assim do ponto de origem, em metros, o inimigo desiste e volta.</summary>
        public const double DistanciaDeDesistenciaMetros = 30.0;

        /// <summary>Quantas vezes por segundo o servidor atualiza a IA (passo fixo).</summary>
        public const int PassosPorSegundo = 10;

        // ── Regras ────────────────────────────────────────────────────────────────

        /// <summary>Valor de <c>AlvoId</c> quando o inimigo não tem alvo.</summary>
        public const long SemAlvo = 0;

        /// <summary>Duração de um passo da IA, em segundos.</summary>
        public static double PassoSegundos => 1.0 / PassosPorSegundo;

        /// <summary>Aviso em milissegundos, como vai em <see cref="InimigoPreparaAtaque.PreparoMs"/>.</summary>
        public static int PreparoDoAtaqueMs => (int)Math.Round(PreparoDoAtaqueSegundos * 1000.0);

        /// <summary>Se o valor recebido da rede é um estado que existe.</summary>
        public static bool EstadoValido(EstadoDeInimigo estado) =>
            estado >= EstadoDeInimigo.Repouso && estado <= EstadoDeInimigo.Morto;

        /// <summary>Se o inimigo neste estado pode levar dano e agir.</summary>
        public static bool EstaVivo(EstadoDeInimigo estado) =>
            EstadoValido(estado) && estado != EstadoDeInimigo.Morto;

        /// <summary>
        /// Se o inimigo neste estado procura alvo. Voltando ao ponto de origem ele ignora os
        /// jogadores até chegar; morto não age.
        /// </summary>
        public static bool ProcuraAlvo(EstadoDeInimigo estado) =>
            estado == EstadoDeInimigo.Repouso
            || estado == EstadoDeInimigo.Alerta
            || estado == EstadoDeInimigo.Perseguicao
            || estado == EstadoDeInimigo.Ataque;

        /// <summary>
        /// Se o inimigo pode passar de um estado a outro. Qualquer estado vivo pode ir a
        /// <see cref="EstadoDeInimigo.Morto"/>; morto não muda mais (a volta é um inimigo novo,
        /// por <see cref="InimigoApareceu"/>). Ficar no mesmo estado não é transição.
        /// </summary>
        public static bool TransicaoValida(EstadoDeInimigo de, EstadoDeInimigo para)
        {
            if (!EstaVivo(de) || !EstadoValido(para) || de == para)
            {
                return false;
            }
            if (para == EstadoDeInimigo.Morto)
            {
                return true;
            }
            switch (de)
            {
                case EstadoDeInimigo.Repouso:
                    return para == EstadoDeInimigo.Alerta;
                case EstadoDeInimigo.Alerta:
                    return para == EstadoDeInimigo.Perseguicao || para == EstadoDeInimigo.Retorno;
                case EstadoDeInimigo.Perseguicao:
                    return para == EstadoDeInimigo.Ataque || para == EstadoDeInimigo.Retorno;
                case EstadoDeInimigo.Ataque:
                    return para == EstadoDeInimigo.Perseguicao || para == EstadoDeInimigo.Retorno;
                case EstadoDeInimigo.Retorno:
                    return para == EstadoDeInimigo.Repouso;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Quantos inimigos tem um grupo, de <see cref="TamanhoMinimoDoGrupo"/> a
        /// <see cref="TamanhoMaximoDoGrupo"/>, com a mesma chance para cada tamanho. Recebe um
        /// sorteio de 0 (inclusive) a 1 (exclusive); valores fora disso são limitados.
        /// </summary>
        public static int TamanhoDoGrupo(double sorteio)
        {
            int opcoes = TamanhoMaximoDoGrupo - TamanhoMinimoDoGrupo + 1;
            int escolhido = (int)Math.Floor(Math.Max(0.0, sorteio) * opcoes);
            return TamanhoMinimoDoGrupo + Math.Min(opcoes - 1, escolhido);
        }

        /// <summary>Distância entre dois pontos no plano do chão, em metros.</summary>
        public static double Distancia(double x1, double z1, double x2, double z2)
        {
            double dx = x2 - x1;
            double dz = z2 - z1;
            return Math.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>Se o inimigo vê um jogador a esta distância (12 m vê; mais que isso não).</summary>
        public static bool Ve(double distanciaMetros) =>
            distanciaMetros >= 0.0 && distanciaMetros <= DistanciaDeVisaoMetros;

        /// <summary>Se o alvo a esta distância já está no alcance do ataque.</summary>
        public static bool NoAlcanceDeAtaque(double distanciaMetros) =>
            distanciaMetros >= 0.0 && distanciaMetros <= AlcanceDeAtaqueMetros;

        /// <summary>
        /// Se o inimigo desiste: recebe a distância dele até o próprio ponto de origem (30 m
        /// ainda persegue; mais que isso volta).
        /// </summary>
        public static bool DeveDesistir(double distanciaDoPontoDeOrigemMetros) =>
            distanciaDoPontoDeOrigemMetros > DistanciaDeDesistenciaMetros;

        /// <summary>
        /// Quem o inimigo persegue. Mantém o alvo atual enquanto ele estiver entre os candidatos,
        /// para não ficar trocando; sem alvo atual válido, escolhe o candidato mais próximo e, no
        /// empate, o de menor id. Os candidatos são os jogadores vivos que o inimigo (ou o grupo
        /// dele) já viu e que ainda estão na área; <paramref name="distanciasMetros"/> tem a
        /// distância de cada um, na mesma ordem. Devolve <see cref="SemAlvo"/> sem candidatos.
        /// </summary>
        public static long EscolherAlvo(
            long alvoAtual,
            long[] candidatos,
            double[] distanciasMetros
        )
        {
            if (candidatos == null || distanciasMetros == null)
            {
                return SemAlvo;
            }
            int quantidade = Math.Min(candidatos.Length, distanciasMetros.Length);
            long escolhido = SemAlvo;
            double menorDistancia = double.MaxValue;
            for (int i = 0; i < quantidade; i++)
            {
                long candidato = candidatos[i];
                if (candidato == SemAlvo)
                {
                    continue;
                }
                if (candidato == alvoAtual)
                {
                    return alvoAtual;
                }
                double distancia = distanciasMetros[i];
                bool maisPerto = distancia < menorDistancia;
                bool empateComIdMenor = distancia == menorDistancia && candidato < escolhido;
                if (escolhido == SemAlvo || maisPerto || empateComIdMenor)
                {
                    escolhido = candidato;
                    menorDistancia = distancia;
                }
            }
            return escolhido;
        }

        /// <summary>
        /// Direção, em graus, de um ponto a outro (0 aponta para +Z e cresce para +X, de -180 a
        /// 180). Pontos iguais devolvem <paramref name="direcaoAtualGraus"/>.
        /// </summary>
        public static double DirecaoPara(
            double x,
            double z,
            double destinoX,
            double destinoZ,
            double direcaoAtualGraus = 0.0
        )
        {
            double dx = destinoX - x;
            double dz = destinoZ - z;
            if (dx == 0.0 && dz == 0.0)
            {
                return direcaoAtualGraus;
            }
            return Math.Atan2(dx, dz) * 180.0 / Math.PI;
        }

        /// <summary>
        /// Um passo de movimento em linha reta até o destino, sem passar dele. Para quando falta
        /// <paramref name="distanciaDeParadaMetros"/> (ou menos) para chegar; quem já está nessa
        /// distância não se move. Velocidade ou tempo negativos contam como zero.
        /// </summary>
        public static void Avancar(
            double x,
            double z,
            double destinoX,
            double destinoZ,
            double velocidadeMetrosPorSegundo,
            double segundos,
            double distanciaDeParadaMetros,
            out double novoX,
            out double novoZ
        )
        {
            novoX = x;
            novoZ = z;
            double distancia = Distancia(x, z, destinoX, destinoZ);
            double falta = distancia - Math.Max(0.0, distanciaDeParadaMetros);
            if (falta <= 0.0 || distancia <= 0.0)
            {
                return;
            }
            double passo = Math.Max(0.0, velocidadeMetrosPorSegundo) * Math.Max(0.0, segundos);
            double andado = Math.Min(passo, falta);
            novoX = x + (destinoX - x) / distancia * andado;
            novoZ = z + (destinoZ - z) / distancia * andado;
        }

        /// <summary>
        /// Centro da área do golpe: <see cref="DistanciaDoGolpeMetros"/> à frente do inimigo, na
        /// direção dada (0 grau aponta para +Z e cresce para +X). É calculado no começo do aviso
        /// e não muda até o golpe.
        /// </summary>
        public static void CentroDoGolpe(
            double inimigoX,
            double inimigoZ,
            double direcaoYGraus,
            out double centroX,
            out double centroZ
        )
        {
            double radianos = direcaoYGraus * Math.PI / 180.0;
            centroX = inimigoX + Math.Sin(radianos) * DistanciaDoGolpeMetros;
            centroZ = inimigoZ + Math.Cos(radianos) * DistanciaDoGolpeMetros;
        }

        /// <summary>
        /// Se um personagem está na área do golpe (círculo de <see cref="RaioDoGolpeMetros"/> em
        /// volta do centro). <paramref name="raioDoAlvo"/> aproxima o corpo do alvo e desconta da
        /// distância. A borda ainda conta.
        /// </summary>
        public static bool EstaNaAreaDoGolpe(
            double centroX,
            double centroZ,
            double alvoX,
            double alvoZ,
            double raioDoAlvo = 0.0
        )
        {
            double distancia = Distancia(centroX, centroZ, alvoX, alvoZ);
            return distancia - Math.Max(0.0, raioDoAlvo) <= RaioDoGolpeMetros;
        }
    }
}
