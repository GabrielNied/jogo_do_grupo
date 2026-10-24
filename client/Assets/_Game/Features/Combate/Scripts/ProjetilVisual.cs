using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
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
        private TipoDeProjetil _tipoProjetil;
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
        public TipoDeProjetil TipoProjetil => _tipoProjetil;
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
            visual.Configurar(
                msg.ProjetilId,
                msg.AtacanteId,
                msg.TipoProjetil,
                new Vector3(msg.DirecaoX, msg.DirecaoY, msg.DirecaoZ),
                msg.Velocidade,
                msg.AlcanceMaximo,
                impacto,
                aoDestruir
            );
            visual.MontarVisual();
            return visual;
        }

        public void Configurar(
            long id,
            long atiradorId,
            TipoDeProjetil tipoProjetil,
            Vector3 direcao,
            float velocidade,
            float alcanceMaximo,
            ControleImpacto impacto,
            Action<long> aoDestruir = null
        )
        {
            _id = id;
            _atiradorId = atiradorId;
            _tipoProjetil = tipoProjetil;
            _direcao = direcao.sqrMagnitude > 0.0001f ? direcao.normalized : Vector3.forward;
            _velocidade = Math.Max(1f, velocidade);
            _alcanceMaximo = Math.Max(1f, alcanceMaximo);
            _impacto = impacto;
            _aoDestruir = aoDestruir;
            transform.rotation = Quaternion.LookRotation(_direcao);
        }

        public void Configurar(
            long id,
            long atiradorId,
            Vector3 direcao,
            float velocidade,
            float alcanceMaximo,
            ControleImpacto impacto,
            Action<long> aoDestruir = null
        ) =>
            Configurar(
                id,
                atiradorId,
                TipoDeProjetil.Flecha,
                direcao,
                velocidade,
                alcanceMaximo,
                impacto,
                aoDestruir
            );

        public void MontarVisual()
        {
            VisualProjetilElemental.Montar(gameObject, _tipoProjetil, _materiais);
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

            if (
                Physics.Raycast(
                    transform.position,
                    _direcao,
                    out var hit,
                    passo,
                    ~0,
                    QueryTriggerInteraction.Ignore
                )
            )
            {
                var jogador = EntradaNoJogo.Jogador;
                if (
                    jogador == null
                    || (
                        hit.transform != jogador.transform
                        && !hit.transform.IsChildOf(jogador.transform)
                    )
                )
                {
                    transform.position = hit.point;
                    Impactar(hit.point);
                    return;
                }
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
            VisualProjetilElemental.CriarEfeitoImpacto(pos, _tipoProjetil);
            _impacto?.TocarSom(
                VisualProjetilElemental.ObterSomImpacto(_tipoProjetil),
                VisualProjetilElemental.ObterVolumeImpacto(_tipoProjetil)
            );
            _aoDestruir?.Invoke(_id);
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        /// <summary>
        /// Cria o efeito pontual de poeira/partículas na colisão do projétil (retrocompatibilidade).
        /// </summary>
        public static void CriarEfeitoImpacto(Vector3 posicao) =>
            VisualProjetilElemental.CriarEfeitoImpacto(posicao, TipoDeProjetil.Flecha);

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
