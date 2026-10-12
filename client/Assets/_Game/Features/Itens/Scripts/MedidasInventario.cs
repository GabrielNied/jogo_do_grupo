using System;

namespace JogoDoGrupo.Client.Features.Itens
{
    /// <summary>
    /// Conta do tamanho da casa do inventário: o maior que cabe no espaço da janela, entre os
    /// limites. Pura (sem Unity) para ter teste; o <see cref="PainelInventario"/> mede a tela e
    /// aplica o resultado. Valores em unidades do painel (a interface escala por
    /// ScaleWithScreenSize 1920x1080).
    /// </summary>
    public static class MedidasInventario
    {
        public const int CasaMinima = 40;
        public const int CasaMaxima = 64;

        /// <summary>Margem de cada casa no USS (2px de cada lado).</summary>
        public const int MargemCasa = 4;

        /// <summary>Linhas da coluna mais alta: 5 do paperdoll e 2 dos conjuntos de armas.</summary>
        public const int LinhasPaperdoll = 7;

        /// <summary>Linhas da bolsa que ficam visíveis antes de rolar.</summary>
        public const int LinhasVisiveisBolsa = 5;

        /// <summary>Larguras que não dependem da casa: rótulo do conjunto + margens das colunas.</summary>
        public const int LarguraFixa = 160;

        /// <summary>Casas lado a lado quando o paperdoll e a bolsa dividem a linha: 2 + 8.</summary>
        public const int CasasNaLargura = 10;

        /// <summary>Folga para o arredondamento do layout não quebrar a linha das colunas.</summary>
        public const int FolgaLargura = 8;

        /// <param name="larguraCorpo">Largura do conteúdo da janela.</param>
        /// <param name="alturaCorpo">Altura que sobra para o corpo (paperdoll e bolsa).</param>
        /// <param name="alturaFixaPaperdoll">
        /// Altura do paperdoll que não depende da casa (cabeçalho, margens, espaço dos conjuntos).
        /// </param>
        public static int TamanhoDaCasa(
            float larguraCorpo,
            float alturaCorpo,
            float alturaFixaPaperdoll
        )
        {
            var porAltura = (alturaCorpo - alturaFixaPaperdoll) / LinhasPaperdoll - MargemCasa;
            var porLargura = (larguraCorpo - LarguraFixa - FolgaLargura) / CasasNaLargura;
            var casa = (int)Math.Floor(Math.Min(porAltura, porLargura));
            return Math.Max(CasaMinima, Math.Min(CasaMaxima, casa));
        }

        /// <summary>Largura da grade da bolsa para a casa e o número de colunas.</summary>
        public static float LarguraDaGrade(int casa, int colunas, bool comRolagem)
        {
            return colunas * (casa + MargemCasa) + (comRolagem ? 16 : 0);
        }

        /// <summary>Altura da parte visível da grade da bolsa.</summary>
        public static float AlturaDaGrade(int casa, int linhas)
        {
            return Math.Min(linhas, LinhasVisiveisBolsa) * (casa + MargemCasa);
        }
    }
}
