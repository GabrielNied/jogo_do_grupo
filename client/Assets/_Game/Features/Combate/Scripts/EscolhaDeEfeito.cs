using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>O som que acompanha um efeito (os clipes são gerados por <see cref="SintetizadorDeSons"/>).</summary>
    internal enum SomDeCombate
    {
        Nenhum,
        Golpe,
        AcertoLeve,
        AcertoPesado,
        Esquiva,
        EsquivaBemSucedida,
        GolpeRecebido,
        AlertaDeArea,
    }

    /// <summary>De onde vem o impacto (decide só a intensidade).</summary>
    internal enum OrigemDoImpacto
    {
        GolpeLeve,
        GolpePesado,
        GolpeDeOutro,
        GolpeRecebido,
        GolpeRecebidoForte,
    }

    /// <summary>
    /// Tudo o que um impacto faz na tela, já decidido. Zero em um campo significa "sem esse efeito".
    /// </summary>
    internal readonly struct EfeitoDeImpacto
    {
        public EfeitoDeImpacto(
            OrigemDoImpacto origem,
            float flashSegundos,
            float recuoMetros,
            float hitStopSegundos,
            float tremorAmplitude,
            float tremorSegundos,
            SomDeCombate som,
            float volumeDoSom
        )
        {
            Origem = origem;
            FlashSegundos = flashSegundos;
            RecuoMetros = recuoMetros;
            HitStopSegundos = hitStopSegundos;
            TremorAmplitude = tremorAmplitude;
            TremorSegundos = tremorSegundos;
            Som = som;
            VolumeDoSom = volumeDoSom;
        }

        public OrigemDoImpacto Origem { get; }
        public float FlashSegundos { get; }
        public float RecuoMetros { get; }
        public float HitStopSegundos { get; }

        /// <summary>Deslocamento máximo da câmera, em metros.</summary>
        public float TremorAmplitude { get; }

        public float TremorSegundos { get; }
        public SomDeCombate Som { get; }

        /// <summary>De 0 a 1, antes do volume geral.</summary>
        public float VolumeDoSom { get; }
    }

    /// <summary>
    /// Escolhe o efeito de cada golpe confirmado pelo servidor (leve, pesado, de outro jogador,
    /// recebido). Só decide a apresentação: dano e acerto vêm do servidor e nunca são calculados
    /// aqui. Dano zero ou vida que não caiu não gera efeito.
    /// </summary>
    internal static class EscolhaDeEfeito
    {
        // Valores de sensação: ajuste aqui, sem mexer nas escolhas.
        public const float FlashLeve = 0.08f;
        public const float FlashPesado = 0.14f;
        public const float FlashRecebido = 0.18f;
        public const float RecuoLeve = 0.08f;
        public const float RecuoPesado = 0.22f;
        public const float HitStopPesado = 0.07f;
        public const float HitStopRecebidoForte = 0.09f;
        public const float TremorPesado = 0.05f;
        public const float TremorRecebido = 0.04f;
        public const float TremorRecebidoForte = 0.09f;
        public const float TremorSegundosPadrao = 0.18f;
        public const float VolumeLeve = 0.5f;
        public const float VolumePesado = 0.85f;
        public const float VolumeDeOutro = 0.25f;
        public const float VolumeRecebido = 0.7f;

        /// <summary>Fração da vida máxima, perdida de uma vez, que conta como golpe forte.</summary>
        public const float FracaoDeGolpeForte = 0.2f;

        /// <summary>Golpe do próprio jogador que o servidor confirmou ter acertado.</summary>
        public static bool DoGolpeProprio(TipoDeAtaque tipo, int dano, out EfeitoDeImpacto efeito)
        {
            if (dano <= 0)
            {
                efeito = default;
                return false;
            }

            efeito =
                tipo == TipoDeAtaque.Pesado
                    ? new EfeitoDeImpacto(
                        OrigemDoImpacto.GolpePesado,
                        FlashPesado,
                        RecuoPesado,
                        HitStopPesado,
                        TremorPesado,
                        TremorSegundosPadrao,
                        SomDeCombate.AcertoPesado,
                        VolumePesado
                    )
                    : new EfeitoDeImpacto(
                        OrigemDoImpacto.GolpeLeve,
                        FlashLeve,
                        RecuoLeve,
                        0f,
                        0f,
                        0f,
                        SomDeCombate.AcertoLeve,
                        VolumeLeve
                    );
            return true;
        }

        /// <summary>
        /// Golpe de outro jogador no mesmo inimigo: só um flash curto e som baixo, sem hit-stop,
        /// recuo nem tremor, para não poluir a tela.
        /// </summary>
        public static bool DoGolpeDeOutro(int dano, out EfeitoDeImpacto efeito)
        {
            if (dano <= 0)
            {
                efeito = default;
                return false;
            }

            efeito = new EfeitoDeImpacto(
                OrigemDoImpacto.GolpeDeOutro,
                FlashLeve,
                0f,
                0f,
                0f,
                0f,
                SomDeCombate.AcertoLeve,
                VolumeDeOutro
            );
            return true;
        }

        /// <summary>
        /// Golpe recebido, visto pela vida que o servidor mandou: só vale quando a vida caiu.
        /// Perder pelo menos <see cref="FracaoDeGolpeForte"/> da vida máxima é golpe forte.
        /// </summary>
        public static bool DoGolpeRecebido(
            int vidaAnterior,
            int vidaAtual,
            int vidaMaxima,
            out EfeitoDeImpacto efeito
        )
        {
            var perdida = vidaAnterior - vidaAtual;
            if (vidaMaxima <= 0 || perdida <= 0)
            {
                efeito = default;
                return false;
            }

            var forte = (float)perdida / vidaMaxima >= FracaoDeGolpeForte - 0.0001f;
            efeito = new EfeitoDeImpacto(
                forte ? OrigemDoImpacto.GolpeRecebidoForte : OrigemDoImpacto.GolpeRecebido,
                FlashRecebido,
                0f,
                forte ? HitStopRecebidoForte : 0f,
                forte ? TremorRecebidoForte : TremorRecebido,
                TremorSegundosPadrao,
                SomDeCombate.GolpeRecebido,
                VolumeRecebido
            );
            return true;
        }
    }

    /// <summary>
    /// Limita o que acontece ao mesmo tempo, para vários golpes não virarem bagunça: flashes,
    /// sons e um único hit-stop e tremor (o maior vale; não se somam).
    /// </summary>
    internal sealed class LimiteDeEfeitos
    {
        public const int MaximoDeFlashes = 8;
        public const int MaximoDeSons = 4;
        public const float JanelaDosSonsSegundos = 0.12f;
        public const float HitStopMaximo = 0.12f;
        public const float TremorMaximo = 0.12f;

        private readonly System.Collections.Generic.List<float> _flashes =
            new System.Collections.Generic.List<float>();
        private readonly System.Collections.Generic.List<float> _sons =
            new System.Collections.Generic.List<float>();

        private float _tremorRestante;
        private float _tremorDuracao;

        public int FlashesAtivos => _flashes.Count;
        public int SonsNaJanela => _sons.Count;
        public float HitStopRestante { get; private set; }
        public float TremorAmplitude { get; private set; }

        /// <summary>Pedido de flash aceito? Passando do máximo, o mais antigo sai.</summary>
        public bool RegistrarFlash(float segundos)
        {
            if (segundos <= 0f)
            {
                return false;
            }

            if (_flashes.Count >= MaximoDeFlashes)
            {
                _flashes.RemoveAt(0);
            }

            _flashes.Add(segundos);
            return true;
        }

        /// <summary>O som pode tocar agora? Muitos na mesma janela curta são descartados.</summary>
        public bool PodeTocarSom()
        {
            if (_sons.Count >= MaximoDeSons)
            {
                return false;
            }

            _sons.Add(JanelaDosSonsSegundos);
            return true;
        }

        public void PedirHitStop(float segundos)
        {
            var limitado = segundos > HitStopMaximo ? HitStopMaximo : segundos;
            if (limitado > HitStopRestante)
            {
                HitStopRestante = limitado;
            }
        }

        public void PedirTremor(float amplitude, float segundos)
        {
            if (amplitude <= 0f || segundos <= 0f)
            {
                return;
            }

            var limitada = amplitude > TremorMaximo ? TremorMaximo : amplitude;
            if (limitada >= TremorAmplitude || _tremorRestante <= 0f)
            {
                TremorAmplitude = limitada;
                _tremorRestante = segundos;
                _tremorDuracao = segundos;
            }
        }

        /// <summary>Intensidade atual do tremor (cai até zero no fim).</summary>
        public float TremorAtual =>
            _tremorRestante <= 0f || _tremorDuracao <= 0f
                ? 0f
                : TremorAmplitude * (_tremorRestante / _tremorDuracao);

        public void Atualizar(float segundosReais)
        {
            DecairLista(_flashes, segundosReais);
            DecairLista(_sons, segundosReais);

            if (HitStopRestante > 0f)
            {
                HitStopRestante -= segundosReais;
                if (HitStopRestante < 0f)
                {
                    HitStopRestante = 0f;
                }
            }

            if (_tremorRestante > 0f)
            {
                _tremorRestante -= segundosReais;
                if (_tremorRestante <= 0f)
                {
                    _tremorRestante = 0f;
                    TremorAmplitude = 0f;
                }
            }
        }

        private static void DecairLista(System.Collections.Generic.List<float> lista, float dt)
        {
            for (var i = lista.Count - 1; i >= 0; i--)
            {
                lista[i] -= dt;
                if (lista[i] <= 0f)
                {
                    lista.RemoveAt(i);
                }
            }
        }
    }
}
