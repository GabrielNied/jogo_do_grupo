using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// Gerencia a apresentação visual e sonora de projéteis (como flechas e projéteis mágicos).
    /// Ouve <see cref="CombateMessageIds.ProjetilLancado"/> e instancia as flechas com rastro.
    /// Responde a <see cref="CombateMessageIds.AtaqueAcertou"/> para sincronizar o impacto visual.
    /// </summary>
    internal sealed class GerenciadorDeProjeteis : MonoBehaviour
    {
        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();
        private readonly Dictionary<long, ProjetilVisual> _projeteis =
            new Dictionary<long, ProjetilVisual>();
        private ControleImpacto _impacto;
        private long _personagemId;

        public int QuantidadeAtivos => _projeteis.Count;

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
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<AtaqueAcertou>(
                    CombateMessageIds.AtaqueAcertou,
                    AoAcertarAtaque
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
            _projeteis.Clear();
        }

        private void AoLancarProjetil(ProjetilLancado msg)
        {
            if (msg == null)
            {
                return;
            }

            _impacto?.TocarSom(
                SomDeCombate.DisparoFlecha,
                ConfigSensacaoCombate.VolumeDisparoFlecha
            );

            var visual = ProjetilVisual.Criar(msg, _impacto, AoDestruirProjetil);
            _projeteis[msg.ProjetilId] = visual;
        }

        private void AoAcertarAtaque(AtaqueAcertou msg)
        {
            if (msg == null || msg.Acertos == null || msg.Acertos.Length == 0)
            {
                return;
            }

            // Procura projétil ativo deste atacante para colidir no alvo
            long? idParaImpactar = null;
            ProjetilVisual projetilParaImpactar = null;

            foreach (var kvp in _projeteis)
            {
                if (kvp.Value != null && kvp.Value.AtacanteId == msg.AtacanteId)
                {
                    idParaImpactar = kvp.Key;
                    projetilParaImpactar = kvp.Value;
                    break;
                }
            }

            if (projetilParaImpactar != null)
            {
                var alvo = RegistroDeAlvos.Achar(msg.Acertos[0].AlvoId);
                var pos =
                    alvo != null
                        ? alvo.transform.position + Vector3.up * 1f
                        : projetilParaImpactar.transform.position;

                projetilParaImpactar.Impactar(pos);
                if (idParaImpactar.HasValue)
                {
                    _projeteis.Remove(idParaImpactar.Value);
                }
            }
        }

        private void AoDestruirProjetil(long id)
        {
            _projeteis.Remove(id);
        }
    }
}
