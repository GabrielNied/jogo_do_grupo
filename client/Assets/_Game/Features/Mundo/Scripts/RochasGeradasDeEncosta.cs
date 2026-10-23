using System;
using System.Collections.Generic;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo
{
    /// <summary>Identifica exclusivamente o grupo decorativo criado pela ferramenta de encostas.</summary>
    [AddComponentMenu("")]
    public sealed class RochasGeradasDeEncosta : MonoBehaviour
    {
        public List<PecaDeEncostaGerada> Pecas = new List<PecaDeEncostaGerada>();
    }

    [Serializable]
    public sealed class PecaDeEncostaGerada
    {
        public GameObject Objeto;
        public Vector2 Ancora;
    }
}
