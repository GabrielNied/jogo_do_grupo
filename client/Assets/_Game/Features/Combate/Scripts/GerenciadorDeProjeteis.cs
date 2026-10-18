using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// Componente que anima a trajetória visual do projétil até atingir o alcance máximo.
    /// </summary>
    internal sealed class ProjetilVisual : MonoBehaviour
    {
        private Vector3 _direcao;
        private float _velocidade;
        private float _alcanceMaximo;
        private float _distanciaPercorrida;
        private ControleImpacto _impacto;

        public void Configurar(
            Vector3 direcao,
            float velocidade,
            float alcanceMaximo,
            ControleImpacto impacto
        )
        {
            _direcao = direcao.sqrMagnitude > 0.0001f ? direcao.normalized : Vector3.forward;
            _velocidade = Math.Max(1f, velocidade);
            _alcanceMaximo = Math.Max(1f, alcanceMaximo);
            _impacto = impacto;
            transform.rotation = Quaternion.LookRotation(_direcao);
        }

        private void Update()
        {
            float passo = _velocidade * Time.deltaTime;
            transform.position += _direcao * passo;
            _distanciaPercorrida += passo;

            if (_distanciaPercorrida >= _alcanceMaximo)
            {
                Destruir();
            }
        }

        private void Destruir()
        {
            _impacto?.TocarSom(SomDeCombate.ImpactoFlecha, 0.4f);
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Gerencia a apresentação visual e sonora de projéteis (como flechas e projéteis mágicos).
    /// Ouve <see cref="CombateMessageIds.ProjetilLancado"/> e instancia as flechas com rastro.
    /// </summary>
    internal sealed class GerenciadorDeProjeteis : MonoBehaviour
    {
        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();
        private ControleImpacto _impacto;
        private long _personagemId;

        public void Configurar(long personagemId, ControleImpacto impacto)
        {
            _personagemId = personagemId;
            _impacto = impacto;

            _assinaturas.Add(
                ConexaoTempoReal.Assinar<ProjetilLancado>(
                    CombateMessageIds.ProjetilLancado,
                    AoLancarProjetil
                )
            );
        }

        private void OnDestroy()
        {
            foreach (var assinatura in _assinaturas)
            {
                assinatura?.Dispose();
            }
            _assinaturas.Clear();
        }

        private void AoLancarProjetil(ProjetilLancado msg)
        {
            if (msg == null)
            {
                return;
            }

            _impacto?.TocarSom(SomDeCombate.DisparoFlecha, 0.7f);

            var origem = new Vector3(msg.OrigemX, msg.OrigemY, msg.OrigemZ);
            var direcao = new Vector3(msg.DirecaoX, msg.DirecaoY, msg.DirecaoZ);

            var go = new GameObject("Projetil_" + msg.ProjetilId);
            go.transform.position = origem;

            // Corpo da flecha (cilindro fino alinhado com o eixo Z)
            var haste = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            var col = haste.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }

            haste.transform.SetParent(go.transform, false);
            haste.transform.localScale = new Vector3(0.04f, 0.4f, 0.04f);
            haste.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var renderer = haste.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.6f, 0.4f, 0.2f);
            }

            // Rastro visual
            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.25f;
            trail.startWidth = 0.08f;
            trail.endWidth = 0.01f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = new Color(0.9f, 0.9f, 0.9f, 0.7f);
            trail.endColor = new Color(0.8f, 0.8f, 0.8f, 0f);

            var visual = go.AddComponent<ProjetilVisual>();
            visual.Configurar(direcao, msg.Velocidade, msg.AlcanceMaximo, _impacto);
        }
    }
}
