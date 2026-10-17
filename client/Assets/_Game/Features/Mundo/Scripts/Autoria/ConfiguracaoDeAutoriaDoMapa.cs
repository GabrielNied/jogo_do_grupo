using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Autoria
{
    /// <summary>Associa cada camada pintada do Terrain a um bioma durante a publicação.</summary>
    public sealed class ConfiguracaoDeAutoriaDoMapa : MonoBehaviour
    {
        [Tooltip("Ative depois de criar e pintar uma camada de Terrain para cada bioma.")]
        public bool UsarBiomasPintados;

        public Bioma[] BiomasDasCamadas = new Bioma[0];
    }
}
