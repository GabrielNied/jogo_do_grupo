using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>O que fazer com um clique de ataque (ou esquiva) segundo a <see cref="ProntidaoLocal"/>.</summary>
    internal enum ResultadoDoClique
    {
        /// <summary>Livre: envia e começa a animar na hora.</summary>
        EnviarAgora,

        /// <summary>
        /// Na janela da fila do servidor: envia na hora, mas só começa a animar quando a ação em
        /// curso acabar.
        /// </summary>
        EnviarNaFila,

        /// <summary>Cedo demais para a fila, mas dentro do buffer: um pedido guardado sai depois.</summary>
        Guardado,

        /// <summary>Longe demais do fim da ação, ou já há um pedido guardado: o clique se perde.</summary>
        Descartado,

        /// <summary>O vigor previsto não paga a ação.</summary>
        SemVigor,
    }

    /// <summary>
    /// O que o client já sabe do próprio vigor e da ação em curso, para não pedir nem animar
    /// o que o servidor recusaria. O vigor é uma previsão: o último retrato do servidor mais a
    /// regeneração (<see cref="RegrasRecursos.RegenerarVigor"/>) que ele faria desde então, parada
    /// enquanto a guarda está segurada. Sem leitura de vigor ainda, a ação segue (o servidor
    /// continua conferindo). A recarga é a duração da ação (<see cref="RegrasAtaque.DuracaoSegundos"/>
    /// ou <see cref="RegrasEsquiva.DuracaoSegundos"/>). Um clique durante outra ação entra na fila
    /// do servidor, fica guardado (um só) ou se perde (ver <see cref="ClicarAtaque"/>).
    /// </summary>
    internal sealed class ProntidaoLocal
    {
        /// <summary>
        /// Diferença, em pontos de vigor, entre o retrato do servidor e a previsão que ainda conta
        /// como a mesma coisa (a regeneração arredonda para baixo nos dois lados). Mais que isto
        /// é um gasto e reinicia a espera da regeneração.
        /// </summary>
        public const int ToleranciaDaPrevisao = 2;

        /// <summary>Quanto tempo um pedido guardado vale depois da hora de sair, em segundos.</summary>
        public const double ValidadeDoPedidoGuardadoSegundos = 0.25;

        private double _vigorDesde;
        private double _ultimoGasto = double.NegativeInfinity;
        private int _vigorMaximo = RegrasRecursos.VigorBase;
        private bool _ataqueEmCurso;

        // Para desfazer o golpe que o servidor recusou.
        private int? _vigorAntesDoGolpe;
        private double _ultimoGastoAntesDoGolpe = double.NegativeInfinity;

        private bool _guardadoEsquiva;
        private TipoDeAtaque _guardadoAtaque;
        private bool _temGuardado;
        private double _guardadoEnviarEm;

        private TipoDeAtaque? _naFila;

        /// <summary>O último retrato do vigor (do servidor ou do gasto previsto), em <c>_vigorDesde</c>.</summary>
        public int? Vigor { get; private set; }

        /// <summary>O vigor previsto para <paramref name="agora"/>, já com a regeneração.</summary>
        public int? VigorEm(double agora)
        {
            if (Vigor is not int vigor)
            {
                return null;
            }

            var passados = agora - _vigorDesde;
            if (passados <= 0 || Guardando)
            {
                return vigor;
            }

            return RegrasRecursos.RegenerarVigor(
                vigor,
                _vigorMaximo,
                agora - _ultimoGasto,
                passados
            );
        }

        /// <summary>O teto de velocidade do corpo, dividido entre o golpe e a guarda.</summary>
        public TetoDeVelocidade Teto { get; } = new TetoDeVelocidade();

        public double LivreEm { get; private set; }

        /// <summary>Quando o último acerto sofrido interrompeu a ação (golpe cancelado não erra o alvo).</summary>
        public double UltimaReacaoEm { get; private set; } = double.NegativeInfinity;

        /// <summary>
        /// Quando o golpe em curso acerta (fim do preparo). Até lá o personagem está
        /// comprometido: anda devagar e não esquiva. Depois, na recuperação, a esquiva vale.
        /// </summary>
        public double AcertoEm { get; private set; }

        /// <summary>Se o personagem está no preparo ou no momento de acerto de um golpe.</summary>
        public bool Comprometido(double agora) => agora < AcertoEm;

        /// <summary>
        /// O servidor mandou o vigor (e o máximo, quando o tem). Um vigor bem abaixo do previsto é
        /// um gasto: a espera da regeneração recomeça em <paramref name="agora"/>.
        /// </summary>
        public void RegistrarVigor(int vigor, double agora = 0, int? maximo = null)
        {
            var limitado = vigor < 0 ? 0 : vigor;
            if (VigorEm(agora) is int previsto && limitado < previsto - ToleranciaDaPrevisao)
            {
                _ultimoGasto = agora;
            }

            Vigor = limitado;
            _vigorDesde = agora;
            if (maximo is int teto && teto > 0)
            {
                _vigorMaximo = teto;
            }
        }

        public bool PodeAtacar(TipoDeAtaque tipo, double agora)
        {
            if (agora < LivreEm)
            {
                return false;
            }

            return TemVigorPara(tipo, agora);
        }

        /// <summary>Se o vigor previsto para <paramref name="quando"/> paga o ataque.</summary>
        public bool TemVigorPara(TipoDeAtaque tipo, double quando) =>
            VigorEm(quando) is not int vigor || RegrasAtaque.TemVigor(tipo, vigor);

        /// <summary>Se a guarda está segurada (o servidor confirmou ou o client antecipou).</summary>
        public bool Guardando { get; private set; }

        /// <summary>
        /// Se a guarda pode subir: fora do preparo e do acerto de um golpe, da esquiva e da
        /// reação ao acerto. Na recuperação do golpe ela vale e corta o que falta.
        /// </summary>
        public bool PodeGuardar(double agora)
        {
            if (Comprometido(agora) || (agora < LivreEm && !EmRecuperacao(agora)))
            {
                return false;
            }

            return true;
        }

        public void MarcarGuarda(bool segurando, double agora)
        {
            if (Guardando != segurando)
            {
                // O servidor para a regeneração ao subir e recomeça a espera de 2 s ao soltar.
                Consolidar(agora);
                _ultimoGasto = agora;
            }

            Guardando = segurando;
            if (segurando && EmRecuperacao(agora))
            {
                // Segurar a guarda corta o resto da recuperação do golpe.
                _ataqueEmCurso = false;
                LivreEm = agora;
            }
        }

        /// <summary>
        /// O servidor confirmou um acerto no próprio personagem que interrompeu a ação: o golpe
        /// em curso para, a guarda cai e nada novo começa por <paramref name="duracaoSegundos"/>.
        /// </summary>
        public void MarcarReacao(double agora, double duracaoSegundos)
        {
            SoltarGuarda(agora);
            _ataqueEmCurso = false;
            AcertoEm = 0;
            LivreEm = agora + duracaoSegundos;
            UltimaReacaoEm = agora;
            _naFila = null;
            _temGuardado = false;
        }

        public bool PodeEsquivar(double agora)
        {
            // No preparo e no acerto não esquiva; na recuperação ela corta o resto do golpe.
            if (Comprometido(agora) || (agora < LivreEm && !EmRecuperacao(agora)))
            {
                return false;
            }

            return VigorEm(agora) is not int vigor || RegrasEsquiva.TemVigor(vigor);
        }

        // Dentro de uma ação de ataque, depois do acerto (a esquiva de antes ocupa até LivreEm).
        private bool EmRecuperacao(double agora) => _ataqueEmCurso && agora >= AcertoEm;

        public void MarcarAtaque(TipoDeAtaque tipo, double agora)
        {
            SoltarGuarda(agora);
            _ataqueEmCurso = true;
            AcertoEm = agora + RegrasAtaque.PreparoSegundos(tipo);
            LivreEm = agora + RegrasAtaque.DuracaoSegundos(tipo);
            Gastar(RegrasAtaque.CustoDeVigor(tipo), agora);
        }

        public void MarcarEsquiva(double agora)
        {
            // A esquiva corta o resto do golpe: a recuperação acaba aqui.
            SoltarGuarda(agora);
            _ataqueEmCurso = false;
            AcertoEm = 0;
            LivreEm = agora + RegrasEsquiva.DuracaoSegundos;
            Gastar(RegrasEsquiva.CustoDeVigor, agora);
        }

        /// <summary>
        /// O servidor recusou o golpe que o client já começou (ADR 0024): a ação acaba aqui e o
        /// vigor previsto volta ao que era antes do gasto.
        /// </summary>
        public void DesfazerAtaque(double agora)
        {
            if (_vigorAntesDoGolpe is int antes)
            {
                Vigor = antes;
                _vigorDesde = agora;
                _ultimoGasto = _ultimoGastoAntesDoGolpe;
                _vigorAntesDoGolpe = null;
            }

            _ataqueEmCurso = false;
            AcertoEm = 0;
            LivreEm = agora;
        }

        // ── Buffer de entrada (#480) ─────────────────────────────────────────────

        /// <summary>O ataque que já está na fila do servidor e começa quando a ação em curso acabar.</summary>
        public TipoDeAtaque? NaFila => _naFila;

        /// <summary>Se há um clique guardado esperando a hora de sair.</summary>
        public bool TemPedidoGuardado => _temGuardado;

        /// <summary>
        /// Decide o que fazer com o clique do ataque. Livre: envia e anima. Em outra ação: se
        /// falta até <see cref="RegrasAtaque.JanelaDaFilaSegundos"/> para ela acabar, envia já
        /// (entra na fila do servidor); se falta até <see cref="RegrasAtaque.JanelaDoBufferSegundos"/>,
        /// guarda um pedido que sai a tempo; senão o clique se perde. Só cabe um pedido de cada vez.
        /// </summary>
        public ResultadoDoClique ClicarAtaque(TipoDeAtaque tipo, double agora)
        {
            if (agora >= LivreEm)
            {
                return TemVigorPara(tipo, agora)
                    ? ResultadoDoClique.EnviarAgora
                    : ResultadoDoClique.SemVigor;
            }

            if (_naFila != null || _temGuardado)
            {
                return ResultadoDoClique.Descartado;
            }

            var quando = RegrasAtaque.QuandoEnviarOCliqueGuardado(LivreEm - agora);
            if (quando is not double espera)
            {
                return ResultadoDoClique.Descartado;
            }

            if (!TemVigorPara(tipo, LivreEm))
            {
                return ResultadoDoClique.SemVigor;
            }

            if (espera <= 0)
            {
                _naFila = tipo;
                return ResultadoDoClique.EnviarNaFila;
            }

            Guardar(tipo, false, agora + espera);
            return ResultadoDoClique.Guardado;
        }

        /// <summary>
        /// Decide o que fazer com o clique da esquiva. Só há buffer enquanto o personagem está
        /// comprometido com um golpe ou na própria esquiva; o clique sai quando a esquiva pode
        /// começar, se isso acontecer em até <see cref="RegrasAtaque.JanelaDoBufferSegundos"/>.
        /// </summary>
        public ResultadoDoClique ClicarEsquiva(double agora)
        {
            if (PodeEsquivar(agora))
            {
                return ResultadoDoClique.EnviarAgora;
            }

            var liberaEm = Comprometido(agora) ? AcertoEm : LivreEm;
            if (agora >= liberaEm)
            {
                return ResultadoDoClique.SemVigor; // Livre de ação: só o vigor impede.
            }

            if (
                _temGuardado
                || _naFila != null
                || liberaEm - agora > RegrasAtaque.JanelaDoBufferSegundos
            )
            {
                return ResultadoDoClique.Descartado;
            }

            if (VigorEm(liberaEm) is int vigor && !RegrasEsquiva.TemVigor(vigor))
            {
                return ResultadoDoClique.SemVigor;
            }

            Guardar(TipoDeAtaque.Leve, true, liberaEm);
            return ResultadoDoClique.Guardado;
        }

        /// <summary>
        /// Se o ataque guardado já pode sair para o servidor (hora de enviar chegou). Tira o pedido
        /// do buffer; quem chama refaz o clique com <see cref="ClicarAtaque"/>. Pedido vencido some.
        /// </summary>
        public bool LiberarAtaqueGuardado(double agora, out TipoDeAtaque tipo)
        {
            tipo = _guardadoAtaque;
            return LiberarGuardado(agora, esquiva: false);
        }

        /// <summary>Se a esquiva guardada já pode começar. Tira o pedido do buffer.</summary>
        public bool LiberarEsquivaGuardada(double agora) => LiberarGuardado(agora, esquiva: true);

        /// <summary>
        /// Se o ataque da fila já começa (a ação em curso acabou). Tira o pedido da fila; quem chama
        /// marca o golpe com <see cref="MarcarAtaque"/> e anima, sem enviar de novo.
        /// </summary>
        public bool IniciarDaFila(double agora, out TipoDeAtaque tipo)
        {
            if (_naFila is TipoDeAtaque fila && agora >= LivreEm)
            {
                tipo = fila;
                _naFila = null;
                return true;
            }

            tipo = TipoDeAtaque.Leve;
            return false;
        }

        /// <summary>O servidor recusou o pedido da fila: nada vai começar.</summary>
        public void CancelarFila() => _naFila = null;

        private bool LiberarGuardado(double agora, bool esquiva)
        {
            if (!_temGuardado || _guardadoEsquiva != esquiva || agora < _guardadoEnviarEm)
            {
                return false;
            }

            _temGuardado = false;
            return agora - _guardadoEnviarEm <= ValidadeDoPedidoGuardadoSegundos;
        }

        private void Guardar(TipoDeAtaque tipo, bool esquiva, double enviarEm)
        {
            _temGuardado = true;
            _guardadoEsquiva = esquiva;
            _guardadoAtaque = tipo;
            _guardadoEnviarEm = enviarEm;
        }

        // ── Vigor previsto ───────────────────────────────────────────────────────

        // Fixa o vigor previsto em agora para mudar a regra da regeneração dali em diante.
        private void Consolidar(double agora)
        {
            if (VigorEm(agora) is int vigor)
            {
                Vigor = vigor;
            }

            _vigorDesde = agora;
        }

        private void SoltarGuarda(double agora)
        {
            if (Guardando)
            {
                MarcarGuarda(false, agora);
            }
        }

        private void Gastar(int custo, double agora)
        {
            _vigorAntesDoGolpe = null;
            if (VigorEm(agora) is not int vigor)
            {
                return;
            }

            _vigorAntesDoGolpe = vigor;
            _ultimoGastoAntesDoGolpe = _ultimoGasto;
            Vigor = vigor - custo;
            _vigorDesde = agora;
            if (custo > 0)
            {
                _ultimoGasto = agora;
            }
        }
    }
}
