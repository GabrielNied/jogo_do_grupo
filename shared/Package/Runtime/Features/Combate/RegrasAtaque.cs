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

        /// <summary>Quantos leves seguidos formam a cadeia da espada.</summary>
        public const int PassosDaCadeiaLeve = 3;

        /// <summary>Multiplicador do último leve da cadeia (os anteriores usam <see cref="MultiplicadorLeve"/>).</summary>
        public const double MultiplicadorDoTerceiroLeve = 1.2;

        /// <summary>
        /// Se o próximo golpe não começar em até estes segundos depois que a ação anterior acaba,
        /// a cadeia volta ao primeiro leve.
        /// </summary>
        public const double JanelaDaCadeiaSegundos = 0.8;

        /// <summary>Quanto o corpo pode girar por segundo durante o preparo do leve, em graus.</summary>
        public const double GiroMaximoLeveGrausPorSegundo = 240.0;

        /// <summary>Quanto o corpo pode girar por segundo durante o preparo do pesado, em graus.</summary>
        public const double GiroMaximoPesadoGrausPorSegundo = 60.0;

        /// <summary>
        /// Fração da velocidade de corrida permitida durante o preparo e o momento de acerto
        /// (0,4 = 40%). Na recuperação a velocidade volta ao normal.
        /// </summary>
        public const double VelocidadeDuranteGolpe = 0.4;

        /// <summary>
        /// Quanto tempo o botão do ataque fica segurado para o golpe ser o pesado; soltar antes
        /// disso é o leve. Só o client usa (o servidor recebe o tipo já decidido).
        /// </summary>
        public const double SegurarParaPesadoSegundos = 0.3;

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

        /// <summary>
        /// Multiplicador do golpe no passo da cadeia (0 é o primeiro leve). O último leve da
        /// cadeia é mais forte; o pesado não depende do passo.
        /// </summary>
        public static double Multiplicador(TipoDeAtaque tipo, int passo)
        {
            if (tipo == TipoDeAtaque.Pesado)
            {
                return MultiplicadorPesado;
            }

            return passo == PassosDaCadeiaLeve - 1
                ? MultiplicadorDoTerceiroLeve
                : MultiplicadorLeve;
        }

        /// <summary>
        /// O passo da cadeia (0 é o primeiro leve) de um golpe que começa agora. O pesado é
        /// sempre o passo 0. O leve segue de <paramref name="proximoPasso"/> se começar em até
        /// <see cref="JanelaDaCadeiaSegundos"/> depois do fim da ação anterior; senão, volta ao 0.
        /// </summary>
        public static int PassoDoGolpe(
            TipoDeAtaque tipo,
            int proximoPasso,
            double segundosDesdeOFimDaAnterior
        )
        {
            if (tipo == TipoDeAtaque.Pesado)
            {
                return 0;
            }

            bool dentroDaJanela =
                segundosDesdeOFimDaAnterior <= JanelaDaCadeiaSegundos
                && proximoPasso > 0
                && proximoPasso < PassosDaCadeiaLeve;
            return dentroDaJanela ? proximoPasso : 0;
        }

        /// <summary>
        /// O passo a que o golpe seguinte tem direito depois de um golpe de
        /// <paramref name="tipo"/> no <paramref name="passo"/>: o pesado reinicia a cadeia, e o
        /// 4º leve volta ao primeiro.
        /// </summary>
        public static int ProximoPasso(TipoDeAtaque tipo, int passo)
        {
            if (tipo == TipoDeAtaque.Pesado)
            {
                return 0;
            }

            return (passo + 1) % PassosDaCadeiaLeve;
        }

        public static double GiroMaximoGrausPorSegundo(TipoDeAtaque tipo) =>
            tipo == TipoDeAtaque.Pesado
                ? GiroMaximoPesadoGrausPorSegundo
                : GiroMaximoLeveGrausPorSegundo;

        /// <summary>
        /// A direção do golpe no momento do acerto: a que o personagem olha
        /// (<paramref name="desejadaGraus"/>), mas sem girar mais que o teto do ataque desde a
        /// direção do início do preparo (<paramref name="inicialGraus"/>) em
        /// <paramref name="segundosDePreparo"/>. Sem alvo fixo.
        /// </summary>
        public static double DirecaoDoGolpe(
            TipoDeAtaque tipo,
            double inicialGraus,
            double desejadaGraus,
            double segundosDePreparo
        )
        {
            double teto = GiroMaximoGrausPorSegundo(tipo) * Math.Max(0.0, segundosDePreparo);
            double giro = DiferencaDeAngulo(desejadaGraus, inicialGraus);
            return inicialGraus + Math.Max(-teto, Math.Min(teto, giro));
        }

        public static int CustoDeVigor(TipoDeAtaque tipo) =>
            tipo == TipoDeAtaque.Pesado ? CustoDeVigorPesado : CustoDeVigorLeve;

        /// <summary>Se o vigor atual paga o ataque. O leve sempre pode.</summary>
        public static bool TemVigor(TipoDeAtaque tipo, int vigorAtual) =>
            vigorAtual >= CustoDeVigor(tipo);

        /// <summary>
        /// Dano do golpe em um alvo: (dano da espada + Força) vezes o multiplicador do ataque,
        /// arredondado. Força negativa conta como zero e o dano nunca é menor que 1.
        /// </summary>
        public static int Dano(TipoDeAtaque tipo, int forca, int passo = 0)
        {
            double danoDaArma = DanoBaseDaEspada + DanoPorForca * Math.Max(0, forca);
            double dano = Math.Round(
                danoDaArma * Multiplicador(tipo, passo),
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
