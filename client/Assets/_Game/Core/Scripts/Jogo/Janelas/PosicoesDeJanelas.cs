using System;
using System.Globalization;
using UnityEngine;

namespace JogoDoGrupo.Client.Core.Jogo.Janelas
{
    /// <summary>
    /// Onde cada janela ficou da última vez que a pessoa arrastou, por chave, guardado neste
    /// computador. A posição vale só para o mesmo tamanho de área (mesma resolução e escala da
    /// interface); com outro tamanho a janela volta a abrir no lugar padrão.
    /// </summary>
    internal sealed class PosicoesDeJanelas
    {
        internal const string PrefixoChave = "core.janela.posicao.";

        // Diferença de tamanho da área (em pixels do painel) que ainda conta como a mesma tela.
        private const float ToleranciaDaArea = 1f;

        private readonly Func<string, string> _ler;
        private readonly Action<string, string> _gravar;

        /// <summary>Guarda em <see cref="PlayerPrefs"/>.</summary>
        public PosicoesDeJanelas()
            : this(
                chave => PlayerPrefs.GetString(chave, ""),
                (chave, valor) =>
                {
                    PlayerPrefs.SetString(chave, valor);
                    PlayerPrefs.Save();
                }
            ) { }

        /// <summary>Guarda onde quiser (os testes usam um dicionário).</summary>
        public PosicoesDeJanelas(Func<string, string> ler, Action<string, string> gravar)
        {
            _ler = ler ?? throw new ArgumentNullException(nameof(ler));
            _gravar = gravar ?? throw new ArgumentNullException(nameof(gravar));
        }

        /// <summary>Grava a posição (canto de cima à esquerda) e o tamanho da área naquele momento.</summary>
        public void Gravar(string janela, Vector2 posicao, Vector2 area)
        {
            if (string.IsNullOrEmpty(janela))
            {
                return;
            }

            var c = CultureInfo.InvariantCulture;
            _gravar(
                PrefixoChave + janela,
                string.Join(
                    ";",
                    posicao.x.ToString("0.##", c),
                    posicao.y.ToString("0.##", c),
                    area.x.ToString("0.##", c),
                    area.y.ToString("0.##", c)
                )
            );
        }

        /// <summary>
        /// A posição gravada da janela, se houver uma para uma área do mesmo tamanho que
        /// <paramref name="area"/>.
        /// </summary>
        public bool TentarLer(string janela, Vector2 area, out Vector2 posicao)
        {
            posicao = default;
            if (
                string.IsNullOrEmpty(janela)
                || !TentarLerGravada(janela, out var gravada, out var areaGravada)
            )
            {
                return false;
            }

            if (!MesmaArea(area, areaGravada))
            {
                return false;
            }

            posicao = gravada;
            return true;
        }

        /// <summary>A posição gravada e a área em que foi gravada, sem conferir a tela atual.</summary>
        public bool TentarLerGravada(string janela, out Vector2 posicao, out Vector2 area)
        {
            posicao = default;
            area = default;
            var texto = string.IsNullOrEmpty(janela) ? null : _ler(PrefixoChave + janela);
            if (string.IsNullOrEmpty(texto))
            {
                return false;
            }

            var partes = texto.Split(';');
            if (partes.Length != 4)
            {
                return false;
            }

            var numeros = new float[4];
            for (var i = 0; i < 4; i++)
            {
                if (
                    !float.TryParse(
                        partes[i],
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out numeros[i]
                    )
                    || float.IsNaN(numeros[i])
                    || float.IsInfinity(numeros[i])
                )
                {
                    return false;
                }
            }

            posicao = new Vector2(numeros[0], numeros[1]);
            area = new Vector2(numeros[2], numeros[3]);
            return area.x > 0f && area.y > 0f;
        }

        /// <summary>Se os dois tamanhos de área são a mesma tela (tolera arredondamento).</summary>
        public static bool MesmaArea(Vector2 a, Vector2 b)
        {
            return Mathf.Abs(a.x - b.x) <= ToleranciaDaArea
                && Mathf.Abs(a.y - b.y) <= ToleranciaDaArea;
        }

        /// <summary>
        /// Prende a janela dentro da área para ela não sair da vista. Janela maior que a área fica
        /// encostada em cima à esquerda (o título e o X à vista).
        /// </summary>
        public static Vector2 Limitar(Vector2 posicao, Vector2 tamanhoJanela, Vector2 area)
        {
            return Limitar(posicao, tamanhoJanela, new Rect(Vector2.zero, area));
        }

        /// <summary>
        /// Como o outro, num retângulo que não começa no zero (a área menos o padding, para a
        /// janela não cobrir a barra de atalhos embaixo).
        /// </summary>
        public static Vector2 Limitar(Vector2 posicao, Vector2 tamanhoJanela, Rect limite)
        {
            return new Vector2(
                LimitarEixo(posicao.x, tamanhoJanela.x, limite.xMin, limite.width),
                LimitarEixo(posicao.y, tamanhoJanela.y, limite.yMin, limite.height)
            );
        }

        private static float LimitarEixo(float valor, float tamanho, float inicio, float extensao)
        {
            if (float.IsNaN(valor) || float.IsInfinity(valor))
            {
                return inicio;
            }

            var maximo = inicio + Mathf.Max(0f, extensao - Mathf.Max(0f, tamanho));
            return Mathf.Clamp(valor, inicio, maximo);
        }
    }
}
