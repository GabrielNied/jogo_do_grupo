using JogoDoGrupo.Shared.Features.Npcs;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Npcs
{
    /// <summary>Nomes dos parâmetros do Animator dos NPCs (iguais aos do gerador de editor).</summary>
    public static class ParametrosDeNpc
    {
        public const string Andando = "Andando";
        public const string Falando = "Falando";
        public const string Trabalhando = "Trabalhando";
    }

    /// <summary>
    /// Cria o corpo visual de um NPC (sem colisor): cápsula tingida pelo papel enquanto não há
    /// prefab (#1069); o visual por papel entra na #1064.
    /// </summary>
    public static class FabricaDeCorpoDeNpc
    {
        public static GameObject Criar(PapelNpc papel, int semente, Transform pai)
        {
            var corpo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            corpo.name = "CorpoVisual";
            corpo.transform.SetParent(pai, false);
            corpo.transform.localScale = NpcNoMundo.TamanhoCorpo;
            corpo.transform.localPosition = new Vector3(0f, NpcNoMundo.TamanhoCorpo.y * 0.5f, 0f);

            var colisor = corpo.GetComponent<Collider>();
            if (colisor != null)
            {
                Object.Destroy(colisor);
            }

            var renderer = corpo.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = NpcNoMundo.ObterCorDoPapel(papel);
            }

            return corpo;
        }
    }
}
