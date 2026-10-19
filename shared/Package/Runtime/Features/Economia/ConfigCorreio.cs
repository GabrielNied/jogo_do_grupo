using System;
using JogoDoGrupo.Shared.Features.Npcs;

namespace JogoDoGrupo.Shared.Features.Economia
{
    /// <summary>
    /// Configurações e limites do sistema de correio entre jogadores (#212).
    /// </summary>
    public static class ConfigCorreio
    {
        /// <summary>Taxa fixa em coroas para enviar uma carta (saída de moeda).</summary>
        public const long TaxaEnvioCoroas = 5;

        /// <summary>Quantidade máxima de itens que podem ser anexados a uma carta.</summary>
        public const int MaximoItensAnexados = 5;

        /// <summary>Tamanho máximo do assunto da carta.</summary>
        public const int MaximoTamanhoAssunto = 60;

        /// <summary>Tamanho máximo da mensagem da carta.</summary>
        public const int MaximoTamanhoCorpo = 1000;

        /// <summary>Tempo de validade da carta em dias antes de expirar/devolver.</summary>
        public const int ExpiracaoDias = 30;

        /// <summary>Tempo de validade da carta em segundos.</summary>
        public const long ExpiracaoSegundos = ExpiracaoDias * 24L * 60L * 60L;

        /// <summary>Área onde o correio físico está localizado na cidade.</summary>
        public const string Area = CatalogoNpcs.AreaCidade;

        /// <summary>Posição X do correio na cidade (praça central, perto dos serviços).</summary>
        public const float CorreioX = -3f;

        /// <summary>Posição Z do correio na cidade.</summary>
        public const float CorreioZ = -14f;

        /// <summary>Raio horizontal de interação com o correio em metros.</summary>
        public const float RaioInteracaoMetros = 4f;

        public static bool DentroDoRaioCorreio(float x, float z)
        {
            var dx = x - CorreioX;
            var dz = z - CorreioZ;
            return dx * dx + dz * dz <= RaioInteracaoMetros * RaioInteracaoMetros;
        }
    }
}
