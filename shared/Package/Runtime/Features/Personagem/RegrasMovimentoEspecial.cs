using System;
using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Shared.Features.Personagem
{
    /// <summary>
    /// Regras puras de natação, queda e escalada (#349), iguais no client e no servidor. O
    /// servidor decide; o client usa as mesmas regras para prever. Valores em
    /// <see cref="ConfigMovimentoEspecial"/>.
    /// </summary>
    public static class RegrasMovimentoEspecial
    {
        /// <summary>Folga (m) por passo para a descida a pé não virar queda.</summary>
        public const float FolgaDaDescidaAPe = 0.15f;

        // Quanto acima da superfície (m) ainda se considera "sobre" o volume de água (queda).
        private const float AlcanceAcimaDaAgua = 200f;

        /// <summary>
        /// Procura água sob o ponto (x, y, z): primeiro os volumes marcados, depois o terreno
        /// (rios, lagos e pântano, onde o chão fica abaixo do nível da água). Devolve a altura
        /// da superfície e a profundidade (m) naquele ponto.
        /// </summary>
        public static bool TentarObterAgua(
            float x,
            float y,
            float z,
            IReadOnlyList<VolumeMarcado> volumes,
            out float superficie,
            out float profundidade
        )
        {
            superficie = 0f;
            profundidade = 0f;

            if (volumes != null)
            {
                for (var i = 0; i < volumes.Count; i++)
                {
                    var volume = volumes[i];
                    if (
                        volume.Tipo == TipoDeVolume.Agua
                        && volume.ContemNoPlano(x, z)
                        && y >= volume.Fundo - 1f
                        && y <= volume.Topo + AlcanceAcimaDaAgua
                    )
                    {
                        superficie = volume.Topo;
                        profundidade = volume.TamanhoY;
                        return true;
                    }
                }
            }

            return TentarObterAguaDoTerreno(x, y, z, out superficie, out profundidade);
        }

        private static bool TentarObterAguaDoTerreno(
            float x,
            float y,
            float z,
            out float superficie,
            out float profundidade
        )
        {
            superficie = 0f;
            profundidade = 0f;

            if (!MapaDoMundo.DentroDoMundo(x, z) || PrimeiraCaverna.DentroDoTunel(x, z, y))
            {
                return false;
            }

            if ((MapaDoMundo.AreasEspeciaisEm(x, z) & AreaEspecialDoMapa.Lava) != 0)
            {
                return false;
            }

            var nivel = MapaDoMundo.DentroDoRioAlto(x, z)
                ? MapaDoMundo.NivelDoRioAlto(z)
                : MapaDoMundo.NivelDaAgua;
            var chao = MapaDoMundo.AlturaEm(x, z);
            if (chao >= nivel)
            {
                return false;
            }

            superficie = nivel;
            profundidade = nivel - chao;
            return true;
        }

        /// <summary>
        /// Se um personagem com os pés em (x, y, z) está nadando: há água com profundidade para
        /// nadar e os pés estão abaixo da superfície (com a folga de
        /// <see cref="ConfigMovimentoEspecial.FolgaDaSuperficie"/>).
        /// </summary>
        public static bool EstaNadando(
            float x,
            float y,
            float z,
            IReadOnlyList<VolumeMarcado> volumes
        )
        {
            return TentarObterAgua(x, y, z, volumes, out var superficie, out var profundidade)
                && profundidade >= ConfigMovimentoEspecial.ProfundidadeMinimaParaNadar
                && y <= superficie - ConfigMovimentoEspecial.FolgaDaSuperficie;
        }

        /// <summary>A altura dos pés de quem boia na água com esta <paramref name="superficie"/>.</summary>
        public static float AlturaDeQuemBoia(float superficie) =>
            superficie - ConfigMovimentoEspecial.AfundamentoAoBoiar;

        /// <summary>
        /// Se a água em (x, z) amortece uma queda que chegou até a altura y: ela tem pelo menos
        /// <see cref="ConfigMovimentoEspecial.ProfundidadeQueAmortece"/> e os pés já estão dentro.
        /// </summary>
        public static bool AguaAmortece(
            float x,
            float y,
            float z,
            IReadOnlyList<VolumeMarcado> volumes
        )
        {
            return TentarObterAgua(x, y, z, volumes, out var superficie, out var profundidade)
                && profundidade >= ConfigMovimentoEspecial.ProfundidadeQueAmortece
                && y <= superficie;
        }

        /// <summary>
        /// Dano de uma queda de <paramref name="altura"/> metros: zero até a altura segura, depois
        /// <see cref="ConfigMovimentoEspecial.DanoPorMetroDeQueda"/> por metro, até o teto.
        /// </summary>
        public static int DanoDaQueda(float altura)
        {
            if (float.IsNaN(altura) || altura <= ConfigMovimentoEspecial.AlturaSegura)
            {
                return 0;
            }

            var bruto =
                (altura - ConfigMovimentoEspecial.AlturaSegura)
                * ConfigMovimentoEspecial.DanoPorMetroDeQueda;
            return (int)Math.Min(Math.Ceiling(bruto), ConfigMovimentoEspecial.DanoMaximoDeQueda);
        }

        /// <summary>
        /// Se um passo de <paramref name="descida"/> metros com <paramref name="deslocamentoHorizontal"/>
        /// de avanço foi queda livre, e não uma descida a pé: andar numa rampa de até 45° desce
        /// no máximo o que avança.
        /// </summary>
        public static bool EhQuedaLivre(float descida, float deslocamentoHorizontal) =>
            descida > deslocamentoHorizontal + FolgaDaDescidaAPe;

        /// <summary>
        /// Procura a via de escalada que contém o ponto (com a
        /// <see cref="ConfigMovimentoEspecial.MargemDaVia"/>); <c>null</c> se não há.
        /// </summary>
        public static VolumeMarcado ProcurarVia(
            float x,
            float y,
            float z,
            IReadOnlyList<VolumeMarcado> volumes
        )
        {
            if (volumes == null)
            {
                return null;
            }

            for (var i = 0; i < volumes.Count; i++)
            {
                var volume = volumes[i];
                if (
                    volume.Tipo == TipoDeVolume.ViaDeEscalada
                    && volume.Contem(x, y, z, ConfigMovimentoEspecial.MargemDaVia)
                )
                {
                    return volume;
                }
            }

            return null;
        }
    }
}
