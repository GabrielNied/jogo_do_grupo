using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Shared.Features.Combate;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// Valores de sensação do golpe (regra 17): flash, recuo, hit-stop, tremor, som, vinheta,
    /// rastro e número de dano. Só apresentação: dano e acerto são do servidor. Ficam na
    /// <see cref="Sintonia"/> e mudam ao vivo no painel F10; o valor aqui é o padrão do jogo.
    /// </summary>
    internal static class ConfigSensacaoCombate
    {
        private const string Grupo = "Combate";

        /// <summary>Duração do brilho no alvo, em segundos.</summary>
        public static float FlashLeve = 0.08f;
        public static float FlashPesado = 0.14f;
        public static float FlashRecebido = 0.18f;

        /// <summary>Quanto o alvo é empurrado para trás no acerto, em metros.</summary>
        public static float RecuoLeve = 0.08f;
        public static float RecuoPesado = 0.22f;

        /// <summary>Tempo de jogo quase parado no acerto, em segundos de relógio real.</summary>
        public static float HitStopPesado = 0.07f;
        public static float HitStopRecebidoForte = 0.09f;

        /// <summary>Hit-stop mais longo aceito, somando pedidos ao mesmo tempo.</summary>
        public static float HitStopMaximo = 0.12f;

        /// <summary>Velocidade do jogo durante o hit-stop (quase parado, sem zerar).</summary>
        public static float EscalaDeTempoNoHitStop = 0.05f;

        /// <summary>
        /// Quanto a pose de quem foi acertado pelo jogador fica congelada, em segundos. O padrão
        /// é o de <see cref="RegrasReacao.PausaVisualNoAcertoMs"/>.
        /// </summary>
        public static float PausaDaPoseNoAcerto = RegrasReacao.PausaVisualNoAcertoMs / 1000f;

        /// <summary>Deslocamento máximo da câmera no tremor, em metros.</summary>
        public static float TremorPesado = 0.05f;
        public static float TremorRecebido = 0.04f;
        public static float TremorRecebidoForte = 0.09f;

        /// <summary>Tremor mais forte aceito, em metros.</summary>
        public static float TremorMaximo = 0.12f;

        /// <summary>Duração do tremor, em segundos.</summary>
        public static float TremorSegundos = 0.18f;

        /// <summary>Volume de cada som de acerto, de 0 a 1, antes do volume geral.</summary>
        public static float VolumeLeve = 0.5f;
        public static float VolumePesado = 0.85f;
        public static float VolumeDeOutro = 0.25f;
        public static float VolumeRecebido = 0.7f;

        /// <summary>Força da vinheta vermelha ao apanhar, de 0 a 1, e quanto ela some por segundo.</summary>
        public static float VinhetaNormal = 0.5f;
        public static float VinhetaForte = 0.9f;
        public static float VinhetaQuedaPorSegundo = 3f;

        /// <summary>Quanto o rastro do golpe que não pegou fica no chão, em segundos.</summary>
        public static float RastroSegundos = 0.22f;

        /// <summary>Quanto o número de dano fica na tela, em segundos.</summary>
        public static float NumeroSegundos = 1f;

        /// <summary>Quanto o número sobe até sumir: em metros (sobre o alvo) e em pixels (na mira).</summary>
        public static float NumeroSubidaMetros = 0.9f;
        public static float NumeroSubidaPixels = 90f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            Sintonia.Registrar(
                Grupo,
                nameof(FlashLeve),
                () => FlashLeve,
                v => FlashLeve = v,
                0f,
                0.5f,
                "s de brilho no alvo, golpe leve"
            );
            Sintonia.Registrar(
                Grupo,
                nameof(FlashPesado),
                () => FlashPesado,
                v => FlashPesado = v,
                0f,
                0.5f,
                "s de brilho no alvo, golpe pesado"
            );
            Sintonia.Registrar(
                Grupo,
                nameof(FlashRecebido),
                () => FlashRecebido,
                v => FlashRecebido = v,
                0f,
                0.5f,
                "s de brilho no jogador ao apanhar"
            );
            Sintonia.Registrar(
                Grupo,
                nameof(RecuoLeve),
                () => RecuoLeve,
                v => RecuoLeve = v,
                0f,
                1f,
                "m que o alvo recua, golpe leve"
            );
            Sintonia.Registrar(
                Grupo,
                nameof(RecuoPesado),
                () => RecuoPesado,
                v => RecuoPesado = v,
                0f,
                1f,
                "m que o alvo recua, golpe pesado"
            );
            Sintonia.Registrar(
                Grupo,
                nameof(HitStopPesado),
                () => HitStopPesado,
                v => HitStopPesado = v,
                0f,
                0.3f,
                "s de jogo quase parado, golpe pesado"
            );
            Sintonia.Registrar(
                Grupo,
                nameof(HitStopRecebidoForte),
                () => HitStopRecebidoForte,
                v => HitStopRecebidoForte = v,
                0f,
                0.3f,
                "s de jogo quase parado ao levar golpe forte"
            );
            Sintonia.Registrar(
                Grupo,
                nameof(HitStopMaximo),
                () => HitStopMaximo,
                v => HitStopMaximo = v,
                0f,
                0.3f,
                "s, teto do hit-stop"
            );
            Sintonia.Registrar(
                Grupo,
                nameof(EscalaDeTempoNoHitStop),
                () => EscalaDeTempoNoHitStop,
                v => EscalaDeTempoNoHitStop = v,
                0f,
                1f,
                "velocidade do jogo no hit-stop (1 = normal)"
            );
            Sintonia.Registrar(
                Grupo,
                nameof(PausaDaPoseNoAcerto),
                () => PausaDaPoseNoAcerto,
                v => PausaDaPoseNoAcerto = v,
                0f,
                0.3f,
                "s de pose congelada em quem o jogador acertou"
            );
            Sintonia.Registrar(
                Grupo,
                nameof(TremorPesado),
                () => TremorPesado,
                v => TremorPesado = v,
                0f,
                0.3f,
                "m de tremor da câmera, golpe pesado"
            );
            Sintonia.Registrar(
                Grupo,
                nameof(TremorRecebido),
                () => TremorRecebido,
                v => TremorRecebido = v,
                0f,
                0.3f,
                "m de tremor ao apanhar"
            );
            Sintonia.Registrar(
                Grupo,
                nameof(TremorRecebidoForte),
                () => TremorRecebidoForte,
                v => TremorRecebidoForte = v,
                0f,
                0.3f,
                "m de tremor ao levar golpe forte"
            );
            Sintonia.Registrar(
                Grupo,
                nameof(TremorMaximo),
                () => TremorMaximo,
                v => TremorMaximo = v,
                0f,
                0.3f,
                "m, teto do tremor"
            );
            Sintonia.Registrar(
                Grupo,
                nameof(TremorSegundos),
                () => TremorSegundos,
                v => TremorSegundos = v,
                0f,
                1f,
                "s de duração do tremor"
            );
            Sintonia.Registrar(
                Grupo,
                nameof(VolumeLeve),
                () => VolumeLeve,
                v => VolumeLeve = v,
                0f,
                1f,
                "volume do acerto leve"
            );
            Sintonia.Registrar(
                Grupo,
                nameof(VolumePesado),
                () => VolumePesado,
                v => VolumePesado = v,
                0f,
                1f,
                "volume do acerto pesado"
            );
            Sintonia.Registrar(
                Grupo,
                nameof(VolumeDeOutro),
                () => VolumeDeOutro,
                v => VolumeDeOutro = v,
                0f,
                1f,
                "volume do acerto de outro jogador"
            );
            Sintonia.Registrar(
                Grupo,
                nameof(VolumeRecebido),
                () => VolumeRecebido,
                v => VolumeRecebido = v,
                0f,
                1f,
                "volume ao apanhar"
            );
            Sintonia.Registrar(
                Grupo,
                nameof(VinhetaNormal),
                () => VinhetaNormal,
                v => VinhetaNormal = v,
                0f,
                1f,
                "força da vinheta ao apanhar"
            );
            Sintonia.Registrar(
                Grupo,
                nameof(VinhetaForte),
                () => VinhetaForte,
                v => VinhetaForte = v,
                0f,
                1f,
                "força da vinheta no golpe forte"
            );
            Sintonia.Registrar(
                Grupo,
                nameof(VinhetaQuedaPorSegundo),
                () => VinhetaQuedaPorSegundo,
                v => VinhetaQuedaPorSegundo = v,
                0f,
                10f,
                "quanto a vinheta some por segundo"
            );
            Sintonia.Registrar(
                Grupo,
                nameof(RastroSegundos),
                () => RastroSegundos,
                v => RastroSegundos = v,
                0.05f,
                1f,
                "s do rastro do golpe no vazio"
            );
            Sintonia.Registrar(
                Grupo,
                nameof(NumeroSegundos),
                () => NumeroSegundos,
                v => NumeroSegundos = v,
                0.2f,
                3f,
                "s do número de dano na tela"
            );
            Sintonia.Registrar(
                Grupo,
                nameof(NumeroSubidaMetros),
                () => NumeroSubidaMetros,
                v => NumeroSubidaMetros = v,
                0f,
                3f,
                "m que o número sobe sobre o alvo"
            );
            Sintonia.Registrar(
                Grupo,
                nameof(NumeroSubidaPixels),
                () => NumeroSubidaPixels,
                v => NumeroSubidaPixels = v,
                0f,
                300f,
                "px que o número sobe na mira"
            );
        }
    }
}
