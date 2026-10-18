using System;
using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Combate;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// Componente que anima a trajetória visual do projétil (como flecha) até atingir o alvo ou alcance máximo.
    /// Constrói a geometria 3D da flecha (haste de madeira, ponta metálica, penas) e rastro (TrailRenderer).
    /// </summary>
    internal sealed class ProjetilVisual : MonoBehaviour
    {
        private long _id;
        private long _atiradorId;
        private Vector3 _direcao;
        private float _velocidade;
        private float _alcanceMaximo;
        private float _distanciaPercorrida;
        private ControleImpacto _impacto;
        private Action<long> _aoDestruir;
        private readonly List<Material> _materiais = new List<Material>();

        public long Id => _id;
        public long AtacanteId => _atiradorId;
        public long AtiradorId => _atiradorId;
        public float DistanciaPercorrida => _distanciaPercorrida;
        public float AlcanceMaximo => _alcanceMaximo;
        public float Velocidade => _velocidade;
        public Vector3 Direcao => _direcao;

        /// <summary>
        /// Cria o objeto visual do projétil a partir dos dados recebidos do servidor.
        /// </summary>
        public static ProjetilVisual Criar(
            ProjetilLancado msg,
            ControleImpacto impacto,
            Action<long> aoDestruir = null
        )
        {
            var go = new GameObject("Projetil_" + msg.ProjetilId);
            go.transform.position = new Vector3(msg.OrigemX, msg.OrigemY, msg.OrigemZ);

            var visual = go.AddComponent<ProjetilVisual>();
            visual.MontarVisual();
            visual.Configurar(
                msg.ProjetilId,
                msg.AtacanteId,
                new Vector3(msg.DirecaoX, msg.DirecaoY, msg.DirecaoZ),
                msg.Velocidade,
                msg.AlcanceMaximo,
                impacto,
                aoDestruir
            );
            return visual;
        }

        public void Configurar(
            long id,
            long atiradorId,
            Vector3 direcao,
            float velocidade,
            float alcanceMaximo,
            ControleImpacto impacto,
            Action<long> aoDestruir = null
        )
        {
            _id = id;
            _atiradorId = atiradorId;
            _direcao = direcao.sqrMagnitude > 0.0001f ? direcao.normalized : Vector3.forward;
            _velocidade = Math.Max(1f, velocidade);
            _alcanceMaximo = Math.Max(1f, alcanceMaximo);
            _impacto = impacto;
            _aoDestruir = aoDestruir;
            transform.rotation = Quaternion.LookRotation(_direcao);
        }

        public void MontarVisual()
        {
            // Haste (cilindro de madeira fino)
            var haste = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            RemoverColisor(haste);
            haste.name = "Haste";
            haste.transform.SetParent(transform, false);
            haste.transform.localScale = new Vector3(0.025f, 0.35f, 0.025f);
            haste.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var matHaste = NovoMaterial(new Color(0.52f, 0.35f, 0.2f));
            haste.GetComponent<Renderer>().sharedMaterial = matHaste;

            // Ponta (cilindro metálico menor no início da haste)
            var ponta = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            RemoverColisor(ponta);
            ponta.name = "Ponta";
            ponta.transform.SetParent(transform, false);
            ponta.transform.localPosition = new Vector3(0f, 0f, 0.37f);
            ponta.transform.localScale = new Vector3(0.04f, 0.04f, 0.04f);
            ponta.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var matPonta = NovoMaterial(new Color(0.72f, 0.72f, 0.76f));
            ponta.GetComponent<Renderer>().sharedMaterial = matPonta;

            // Penas traseiras
            var pena = GameObject.CreatePrimitive(PrimitiveType.Cube);
            RemoverColisor(pena);
            pena.name = "Pena";
            pena.transform.SetParent(transform, false);
            pena.transform.localPosition = new Vector3(0f, 0f, -0.32f);
            pena.transform.localScale = new Vector3(0.01f, 0.07f, 0.12f);
            var matPena = NovoMaterial(new Color(0.85f, 0.25f, 0.22f));
            pena.GetComponent<Renderer>().sharedMaterial = matPena;

            // Rastro visual nítido (TrailRenderer)
            var trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = ConfigSensacaoCombate.DuracaoRastroProjetil;
            trail.startWidth = 0.06f;
            trail.endWidth = 0.005f;
            var shader =
                Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                var matTrail = new Material(shader);
                _materiais.Add(matTrail);
                trail.material = matTrail;
            }
            trail.startColor = new Color(1f, 0.95f, 0.8f, 0.7f);
            trail.endColor = new Color(1f, 0.9f, 0.7f, 0f);
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
        }

        private void Update()
        {
            Avancar(Time.deltaTime);
        }

        /// <summary>
        /// Avança o projétil no espaço conforme a velocidade e delta de tempo.
        /// </summary>
        public void Avancar(float dt)
        {
            float passo = _velocidade * dt;
            if (passo <= 0f)
            {
                return;
            }

            if (Physics.Raycast(transform.position, _direcao, out var hit, passo))
            {
                transform.position = hit.point;
                Impactar(hit.point);
                return;
            }

            transform.position += _direcao * passo;
            _distanciaPercorrida += passo;

            if (_distanciaPercorrida >= _alcanceMaximo)
            {
                Impactar(transform.position);
            }
        }

        /// <summary>
        /// Aplica efeito de impacto, toca som e destrói o projétil visual.
        /// </summary>
        public void Impactar(Vector3? pontoImpacto = null)
        {
            var pos = pontoImpacto ?? transform.position;
            CriarEfeitoImpacto(pos);
            _impacto?.TocarSom(
                SomDeCombate.ImpactoFlecha,
                ConfigSensacaoCombate.VolumeImpactoFlecha
            );
            _aoDestruir?.Invoke(_id);
            _aoDestruir = null;
            Destroy(gameObject);
        }

        /// <summary>
        /// Cria o efeito pontual de poeira/partículas na colisão do projétil.
        /// </summary>
        public static void CriarEfeitoImpacto(Vector3 posicao)
        {
            var efeito = new GameObject("ImpactoProjetil");
            efeito.transform.position = posicao;
            var ps = efeito.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = false;
            main.duration = 0.25f;
            main.startLifetime = 0.2f;
            main.startSpeed = 1.5f;
            main.startSize = 0.06f;
            main.startColor = new Color(0.85f, 0.8f, 0.7f, 0.8f);

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;

            var rend = ps.GetComponent<ParticleSystemRenderer>();
            var shader =
                Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                rend.material = new Material(shader);
            }
            ps.Play();
            Destroy(efeito, 0.4f);
        }

        private Material NovoMaterial(Color cor)
        {
            var shader =
                Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Sprites/Default");
            var mat =
                shader != null
                    ? new Material(shader)
                    : new Material(Shader.Find("Sprites/Default"));
            mat.color = cor;
            _materiais.Add(mat);
            return mat;
        }

        private static void RemoverColisor(GameObject objeto)
        {
            var col = objeto.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }
        }

        private void OnDestroy()
        {
            _aoDestruir?.Invoke(_id);
            _aoDestruir = null;
            foreach (var mat in _materiais)
            {
                if (mat != null)
                {
                    Destroy(mat);
                }
            }
            _materiais.Clear();
        }
    }
}
