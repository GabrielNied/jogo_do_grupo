using System;
using System.Collections.Generic;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Personagem.Editor.Playground
{
    [CreateAssetMenu(menuName = "Jogo do Grupo/Personagem/Catalogo do Playground")]
    public sealed class CatalogoPlayground : ScriptableObject
    {
        [Serializable]
        public sealed class Personagem
        {
            public string Nome;
            public GameObject Prefab;
        }

        public List<Personagem> Personagens = new List<Personagem>();
    }
}
