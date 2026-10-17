using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Autoria
{
    /// <summary>
    /// Área retangular marcada na cena de autoria. A última área que contém uma célula define o
    /// bioma e soma suas áreas especiais quando o mapa é publicado.
    /// </summary>
    public sealed class MarcadorDeAreaDoMapa : MonoBehaviour
    {
        public Bioma Bioma = Bioma.Campo;
        public AreaEspecialDoMapa Areas;
        public Vector2 Tamanho = new Vector2(100f, 100f);

        public bool Contem(float x, float z)
        {
            var posicao = transform.position;
            return Mathf.Abs(x - posicao.x) <= Tamanho.x * 0.5f
                && Mathf.Abs(z - posicao.z) <= Tamanho.y * 0.5f;
        }
    }
}
