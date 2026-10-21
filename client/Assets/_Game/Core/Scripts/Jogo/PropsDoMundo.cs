using UnityEngine;

namespace JogoDoGrupo.Client.Core.Jogo
{
    /// <summary>
    /// Adereços do mundo vindos de <c>Resources</c> (modelos de terceiros CC0, como o Survival
    /// Kit do Kenney em <c>Props/</c>): instancia sob um pai, com a altura pedida (escala
    /// uniforme pelos limites do modelo), a base apoiada no ponto do pai e sem os colisores do
    /// FBX (quem chama decide o colisor). Sem o modelo, devolve <c>null</c> e quem chama desenha
    /// a forma simples de reserva.
    /// </summary>
    public static class PropsDoMundo
    {
        public static GameObject Instanciar(
            string recurso,
            Transform pai,
            float altura,
            float giroY = 0f
        )
        {
            var prefab = Resources.Load<GameObject>(recurso);
            if (prefab == null || pai == null)
            {
                return null;
            }

            var modelo = Object.Instantiate(prefab, pai);
            modelo.transform.localPosition = Vector3.zero;
            modelo.transform.localRotation = Quaternion.Euler(0f, giroY, 0f);
            modelo.transform.localScale = Vector3.one;
            if (TentarLimites(modelo, out var limites) && limites.size.y > 0.001f && altura > 0f)
            {
                modelo.transform.localScale = Vector3.one * (altura / limites.size.y);
                TentarLimites(modelo, out limites);
                modelo.transform.position += Vector3.up * (pai.position.y - limites.min.y);
            }

            foreach (var colisor in modelo.GetComponentsInChildren<Collider>())
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(colisor);
                }
                else
                {
                    Object.DestroyImmediate(colisor);
                }
            }

            return modelo;
        }

        private static bool TentarLimites(GameObject modelo, out Bounds limites)
        {
            limites = default;
            var achou = false;
            foreach (var renderizador in modelo.GetComponentsInChildren<Renderer>())
            {
                if (!achou)
                {
                    limites = renderizador.bounds;
                    achou = true;
                }
                else
                {
                    limites.Encapsulate(renderizador.bounds);
                }
            }

            return achou;
        }
    }
}
