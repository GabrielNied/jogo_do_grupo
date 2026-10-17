using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Autoria
{
    /// <summary>Ponto de jogabilidade colocado visualmente na cena de autoria do mapa.</summary>
    public sealed class MarcadorDePontoDoMapa : MonoBehaviour
    {
        public string Id = "novo-ponto";
        public TipoDePontoMarcadoDoMapa Tipo = TipoDePontoMarcadoDoMapa.Inimigos;
    }
}
