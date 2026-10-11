using System;

namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Fórmulas puras do ataque da primeira arma (espada): tempos, custo, dano, volume do golpe e
    /// fila. Valores da proposta em docs/features/fatia-jogavel.md, seção 2; o servidor é quem
    /// decide, o client usa só para tocar a animação no tempo certo.
    /// </summary>
    public static class RegrasAtaque
    {
        // ── Balanceamento (proposta, a calibrar) ─────────────────────────────────

        /// <summary>Segundos entre o começo do ataque leve e o momento de acerto.</summary>
        public const double PreparoLeveSegundos = 0.5;

        /// <summary>Segundos entre o começo do ataque pesado e o momento de acerto.</summary>
        public const double PreparoPesadoSegundos = 1.0;

        /// <summary>Segundos depois do acerto em que o personagem ainda não age (leve).</summary>
        public const double RecuperacaoLeveSegundos = 0.3;

        /// <summary>Segundos depois do acerto em que o personagem ainda não age (pesado).</summary>
        public const double RecuperacaoPesadoSegundos = 0.6;

        /// <summary>Multiplicador do dano da arma no ataque leve.</summary>
        public const double MultiplicadorLeve = 1.0;

        /// <summary>Multiplicador do dano da arma no ataque pesado.</summary>
        public const double MultiplicadorPesado = 2.2;

        /// <summary>Vigor gasto pelo ataque leve (sempre pode).</summary>
        public const int CustoDeVigorLeve = 0;

        /// <summary>Vigor gasto pelo ataque pesado; sem esse vigor o pesado é recusado.</summary>
        public const int CustoDeVigorPesado = 20;

        /// <summary>Dano da espada com Força 0, antes do multiplicador do ataque.</summary>
        public const int DanoBaseDaEspada = 20;

        /// <summary>Dano a mais por ponto de Força, antes do multiplicador do ataque.</summary>
        public const double DanoPorForca = 2.0;

        /// <summary>Alcance do golpe, em metros, a partir do atacante.</summary>
        public const double AlcanceMetros = 2.5;

        /// <summary>Abertura total do cone à frente do atacante, em graus.</summary>
        public const double AberturaDoConeGraus = 90.0;

        /// <summary>
        /// Um pedido feito durante outra ação entra na fila se faltar no máximo este tempo, em
        /// segundos, para a ação acabar; antes disso é recusado.
        /// </summary>
        public const double JanelaDaFilaSegundos = 0.3;

        // ── Regras ────────────────────────────────────────────────────────────────

        /// <summary>Se o valor recebido da rede é um ataque que existe.</summary>
        public static bool TipoValido(TipoDeAtaque tipo) =>
            tipo == TipoDeAtaque.Leve || tipo == TipoDeAtaque.Pesado;

        public static double PreparoSegundos(TipoDeAtaque tipo) =>
            tipo == TipoDeAtaque.Pesado ? PreparoPesadoSegundos : PreparoLeveSegundos;

        public static double RecuperacaoSegundos(TipoDeAtaque tipo) =>
            tipo == TipoDeAtaque.Pesado ? RecuperacaoPesadoSegundos : RecuperacaoLeveSegundos;

        /// <summary>Preparo mais recuperação: o tempo em que o personagem fica na ação.</summary>
        public static double DuracaoSegundos(TipoDeAtaque tipo) =>
            PreparoSegundos(tipo) + RecuperacaoSegundos(tipo);

        /// <summary>Preparo em milissegundos, como vai em <see cref="AtaqueIniciado.PreparoMs"/>.</summary>
        public static int PreparoMs(TipoDeAtaque tipo) =>
            (int)Math.Round(PreparoSegundos(tipo) * 1000.0);

        public static double Multiplicador(TipoDeAtaque tipo) =>
            tipo == TipoDeAtaque.Pesado ? MultiplicadorPesado : MultiplicadorLeve;

        public static int CustoDeVigor(TipoDeAtaque tipo) =>
            tipo == TipoDeAtaque.Pesado ? CustoDeVigorPesado : CustoDeVigorLeve;

        /// <summary>Se o vigor atual paga o ataque. O leve sempre pode.</summary>
        public static bool TemVigor(TipoDeAtaque tipo, int vigorAtual) =>
            vigorAtual >= CustoDeVigor(tipo);

        /// <summary>
        /// Dano do golpe em um alvo: (dano da espada + Força) vezes o multiplicador do ataque,
        /// arredondado. Força negativa conta como zero e o dano nunca é menor que 1.
        /// </summary>
        public static int Dano(TipoDeAtaque tipo, int forca)
        {
            double danoDaArma = DanoBaseDaEspada + DanoPorForca * Math.Max(0, forca);
            double dano = Math.Round(
                danoDaArma * Multiplicador(tipo),
                MidpointRounding.AwayFromZero
            );
            return (int)Math.Max(1.0, Math.Min(dano, int.MaxValue));
        }

        /// <summary>
        /// Se um pedido feito durante outra ação entra na fila (de um pedido só). Recebe quanto
        /// falta, em segundos, para a ação atual acabar.
        /// </summary>
        public static bool EntraNaFila(double segundosAteOFimDaAcao) =>
            segundosAteOFimDaAcao > 0 && segundosAteOFimDaAcao <= JanelaDaFilaSegundos;

        /// <summary>
        /// Se o alvo está no volume do golpe: um cone à frente do atacante, no plano do chão
        /// (a altura não conta). A direção segue a convenção de
        /// <see cref="IntencaoDeAtaque.DirecaoY"/>: 0 grau aponta para +Z e cresce para +X.
        /// <paramref name="raioDoAlvo"/> aproxima o corpo do alvo: desconta da distância, mas o
        /// ângulo é medido até o centro. Alvo na mesma posição do atacante sempre está no volume.
        /// </summary>
        public static bool EstaNoVolume(
            double atacanteX,
            double atacanteZ,
            double direcaoYGraus,
            double alvoX,
            double alvoZ,
            double raioDoAlvo = 0.0
        )
        {
            double dx = alvoX - atacanteX;
            double dz = alvoZ - atacanteZ;
            double distancia = Math.Sqrt(dx * dx + dz * dz);
            if (distancia - Math.Max(0.0, raioDoAlvo) > AlcanceMetros)
            {
                return false;
            }
            if (distancia <= 0.0)
            {
                return true;
            }
            double anguloDoAlvo = Math.Atan2(dx, dz) * 180.0 / Math.PI;
            return Math.Abs(DiferencaDeAngulo(anguloDoAlvo, direcaoYGraus))
                <= AberturaDoConeGraus / 2.0;
        }

        /// <summary>Menor diferença entre dois ângulos em graus, de -180 a 180.</summary>
        public static double DiferencaDeAngulo(double a, double b)
        {
            double diferenca = (a - b) % 360.0;
            if (diferenca > 180.0)
            {
                diferenca -= 360.0;
            }
            else if (diferenca < -180.0)
            {
                diferenca += 360.0;
            }
            return diferenca;
        }
    }
}
