using System;
using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Combate;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// Fábrica e utilitários procedurais para apresentação visual e sonora de projéteis elementais
    /// de Cajado (Bola de Fogo, Esfera de Gelo, Orbe de Cura) e Flecha do Arco.
    /// Constrói malhas, materiais, rastros (TrailRenderer) e partículas de impacto sem assets externos.
    /// </summary>
    internal static class VisualProjetilElemental
    {
        public static void Montar(
            GameObject raiz,
            TipoDeProjetil tipo,
            List<Material> materiaisGuardados
        )
        {
            switch (tipo)
            {
                case TipoDeProjetil.BolaDeFogo:
                    MontarBolaDeFogo(raiz, materiaisGuardados);
                    break;
                case TipoDeProjetil.EsferaDeGelo:
                    MontarEsferaDeGelo(raiz, materiaisGuardados);
                    break;
                case TipoDeProjetil.OrbeDeCura:
                    MontarOrbeDeCura(raiz, materiaisGuardados);
                    break;
                case TipoDeProjetil.Flecha:
                default:
                    MontarFlecha(raiz, materiaisGuardados);
                    break;
            }
        }

        private static void MontarBolaDeFogo(GameObject raiz, List<Material> materiais)
        {
            // Núcleo incandescente da bola de fogo
            var esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            RemoverColisor(esfera);
            esfera.name = "NucleoFogo";
            esfera.transform.SetParent(raiz.transform, false);
            esfera.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);

            var matFogo = CriarMaterialEmissivo(
                new Color(1f, 0.45f, 0.05f),
                new Color(1f, 0.65f, 0.1f)
            );
            materiais.Add(matFogo);
            esfera.GetComponent<Renderer>().sharedMaterial = matFogo;

            // Rastro flamejante de fogo e brasa
            ConfigurarRastro(
                raiz,
                materiais,
                new Color(1f, 0.65f, 0.1f, 0.9f),
                new Color(0.7f, 0.1f, 0.02f, 0f),
                larguraInicial: 0.14f,
                larguraFinal: 0.01f,
                duracao: 0.22f
            );
        }

        private static void MontarEsferaDeGelo(GameObject raiz, List<Material> materiais)
        {
            // Projétil cristalino pontiagudo de gelo
            var cristal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            RemoverColisor(cristal);
            cristal.name = "CristalGelo";
            cristal.transform.SetParent(raiz.transform, false);
            cristal.transform.localScale = new Vector3(0.22f, 0.22f, 0.45f);
            cristal.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);

            var matGelo = CriarMaterialEmissivo(
                new Color(0.2f, 0.85f, 1f, 0.85f),
                new Color(0.4f, 0.95f, 1f)
            );
            materiais.Add(matGelo);
            cristal.GetComponent<Renderer>().sharedMaterial = matGelo;

            // Rastro de névoa fria / geada
            ConfigurarRastro(
                raiz,
                materiais,
                new Color(0.4f, 0.95f, 1f, 0.8f),
                new Color(0.1f, 0.35f, 0.8f, 0f),
                larguraInicial: 0.12f,
                larguraFinal: 0.01f,
                duracao: 0.20f
            );
        }

        private static void MontarOrbeDeCura(GameObject raiz, List<Material> materiais)
        {
            // Orbe externo luminoso verde-esmeralda
            var externo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            RemoverColisor(externo);
            externo.name = "OrbeCuraExterno";
            externo.transform.SetParent(raiz.transform, false);
            externo.transform.localScale = new Vector3(0.36f, 0.36f, 0.36f);

            var matExterno = CriarMaterialEmissivo(
                new Color(0.2f, 0.95f, 0.4f, 0.9f),
                new Color(0.3f, 1f, 0.5f)
            );
            materiais.Add(matExterno);
            externo.GetComponent<Renderer>().sharedMaterial = matExterno;

            // Núcleo interno de luz dourada suave
            var interno = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            RemoverColisor(interno);
            interno.name = "NucleoDourado";
            interno.transform.SetParent(raiz.transform, false);
            interno.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);

            var matInterno = CriarMaterialEmissivo(
                new Color(1f, 0.92f, 0.4f, 0.95f),
                new Color(1f, 0.95f, 0.6f)
            );
            materiais.Add(matInterno);
            interno.GetComponent<Renderer>().sharedMaterial = matInterno;

            // Rastro suave de partículas de luz
            ConfigurarRastro(
                raiz,
                materiais,
                new Color(0.3f, 1f, 0.5f, 0.85f),
                new Color(0.95f, 0.85f, 0.25f, 0f),
                larguraInicial: 0.14f,
                larguraFinal: 0.01f,
                duracao: 0.25f
            );
        }

        private static void MontarFlecha(GameObject raiz, List<Material> materiais)
        {
            // Haste (cilindro de madeira fino)
            var haste = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            RemoverColisor(haste);
            haste.name = "Haste";
            haste.transform.SetParent(raiz.transform, false);
            haste.transform.localScale = new Vector3(0.025f, 0.35f, 0.025f);
            haste.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var matHaste = CriarMaterialBasico(new Color(0.52f, 0.35f, 0.2f));
            materiais.Add(matHaste);
            haste.GetComponent<Renderer>().sharedMaterial = matHaste;

            // Ponta metálica
            var ponta = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            RemoverColisor(ponta);
            ponta.name = "Ponta";
            ponta.transform.SetParent(raiz.transform, false);
            ponta.transform.localPosition = new Vector3(0f, 0f, 0.37f);
            ponta.transform.localScale = new Vector3(0.04f, 0.04f, 0.04f);
            ponta.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var matPonta = CriarMaterialBasico(new Color(0.72f, 0.72f, 0.76f));
            materiais.Add(matPonta);
            ponta.GetComponent<Renderer>().sharedMaterial = matPonta;

            // Penas traseiras
            var pena = GameObject.CreatePrimitive(PrimitiveType.Cube);
            RemoverColisor(pena);
            pena.name = "Pena";
            pena.transform.SetParent(raiz.transform, false);
            pena.transform.localPosition = new Vector3(0f, 0f, -0.32f);
            pena.transform.localScale = new Vector3(0.01f, 0.07f, 0.12f);
            var matPena = CriarMaterialBasico(new Color(0.85f, 0.25f, 0.22f));
            materiais.Add(matPena);
            pena.GetComponent<Renderer>().sharedMaterial = matPena;

            ConfigurarRastro(
                raiz,
                materiais,
                new Color(1f, 0.95f, 0.8f, 0.7f),
                new Color(1f, 0.9f, 0.7f, 0f),
                larguraInicial: 0.06f,
                larguraFinal: 0.005f,
                duracao: ConfigSensacaoCombate.DuracaoRastroProjetil
            );
        }

        private static void ConfigurarRastro(
            GameObject raiz,
            List<Material> materiais,
            Color corInicio,
            Color corFim,
            float larguraInicial,
            float larguraFinal,
            float duracao
        )
        {
            var trail = raiz.AddComponent<TrailRenderer>();
            trail.time = duracao;
            trail.startWidth = larguraInicial;
            trail.endWidth = larguraFinal;
            trail.startColor = corInicio;
            trail.endColor = corFim;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;

            var shader =
                Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                var matTrail = new Material(shader);
                materiais.Add(matTrail);
                trail.material = matTrail;
            }
        }

        /// <summary>
        /// Cria o efeito procedural de partículas na colisão de acordo com o elemento.
        /// </summary>
        public static void CriarEfeitoImpacto(Vector3 posicao, TipoDeProjetil tipo)
        {
            var efeito = new GameObject("Impacto_" + tipo);
            efeito.transform.position = posicao;
            var ps = efeito.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;

            var shape = ps.shape;
            var emission = ps.emission;

            switch (tipo)
            {
                case TipoDeProjetil.BolaDeFogo:
                    main.duration = 0.35f;
                    main.startLifetime = 0.30f;
                    main.startSpeed = 2.6f;
                    main.startSize = 0.08f;
                    main.startColor = new Color(1f, 0.5f, 0.1f, 0.9f);
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius = 0.15f;
                    break;

                case TipoDeProjetil.EsferaDeGelo:
                    main.duration = 0.30f;
                    main.startLifetime = 0.25f;
                    main.startSpeed = 2.2f;
                    main.startSize = 0.07f;
                    main.startColor = new Color(0.4f, 0.95f, 1f, 0.85f);
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 16) });
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius = 0.12f;
                    break;

                case TipoDeProjetil.OrbeDeCura:
                    main.duration = 0.45f;
                    main.startLifetime = 0.40f;
                    main.startSpeed = 1.4f;
                    main.startSize = 0.08f;
                    main.startColor = new Color(0.3f, 1f, 0.55f, 0.9f);
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 16) });
                    shape.shapeType = ParticleSystemShapeType.Circle;
                    shape.radius = 0.25f;
                    break;

                case TipoDeProjetil.Flecha:
                default:
                    main.duration = 0.25f;
                    main.startLifetime = 0.2f;
                    main.startSpeed = 1.5f;
                    main.startSize = 0.06f;
                    main.startColor = new Color(0.85f, 0.8f, 0.7f, 0.8f);
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius = 0.05f;
                    break;
            }

            var rend = ps.GetComponent<ParticleSystemRenderer>();
            var shader =
                Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                rend.material = new Material(shader);
            }

            ps.Play();
            if (Application.isPlaying)
            {
                GameObject.Destroy(efeito, main.duration + 0.15f);
            }
            else
            {
                GameObject.DestroyImmediate(efeito);
            }
        }

        public static SomDeCombate ObterSomDisparo(TipoDeProjetil tipo)
        {
            switch (tipo)
            {
                case TipoDeProjetil.BolaDeFogo:
                    return SomDeCombate.DisparoFogo;
                case TipoDeProjetil.EsferaDeGelo:
                    return SomDeCombate.DisparoGelo;
                case TipoDeProjetil.OrbeDeCura:
                    return SomDeCombate.DisparoCura;
                case TipoDeProjetil.Flecha:
                default:
                    return SomDeCombate.DisparoFlecha;
            }
        }

        public static SomDeCombate ObterSomImpacto(TipoDeProjetil tipo)
        {
            switch (tipo)
            {
                case TipoDeProjetil.BolaDeFogo:
                    return SomDeCombate.ImpactoFogo;
                case TipoDeProjetil.EsferaDeGelo:
                    return SomDeCombate.ImpactoGelo;
                case TipoDeProjetil.OrbeDeCura:
                    return SomDeCombate.ImpactoCura;
                case TipoDeProjetil.Flecha:
                default:
                    return SomDeCombate.ImpactoFlecha;
            }
        }

        public static float ObterVolumeDisparo(TipoDeProjetil tipo)
        {
            switch (tipo)
            {
                case TipoDeProjetil.BolaDeFogo:
                    return 0.75f;
                case TipoDeProjetil.EsferaDeGelo:
                    return 0.65f;
                case TipoDeProjetil.OrbeDeCura:
                    return 0.60f;
                case TipoDeProjetil.Flecha:
                default:
                    return ConfigSensacaoCombate.VolumeDisparoFlecha;
            }
        }

        public static float ObterVolumeImpacto(TipoDeProjetil tipo)
        {
            switch (tipo)
            {
                case TipoDeProjetil.BolaDeFogo:
                    return 0.80f;
                case TipoDeProjetil.EsferaDeGelo:
                    return 0.70f;
                case TipoDeProjetil.OrbeDeCura:
                    return 0.65f;
                case TipoDeProjetil.Flecha:
                default:
                    return ConfigSensacaoCombate.VolumeImpactoFlecha;
            }
        }

        private static Material CriarMaterialEmissivo(Color corBase, Color corEmissao)
        {
            var shader =
                Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            mat.color = corBase;
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", corEmissao);
            }
            return mat;
        }

        private static Material CriarMaterialBasico(Color cor)
        {
            var shader =
                Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            mat.color = cor;
            return mat;
        }

        private static void RemoverColisor(GameObject objeto)
        {
            var col = objeto.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying)
                {
                    GameObject.Destroy(col);
                }
                else
                {
                    GameObject.DestroyImmediate(col);
                }
            }
        }
    }
}
