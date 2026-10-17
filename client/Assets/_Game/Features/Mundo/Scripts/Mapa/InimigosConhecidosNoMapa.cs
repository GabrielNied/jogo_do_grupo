using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Mobs;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo
{
    /// <summary>
    /// Inimigos que o client já conheceu: permanecem no minimapa (#903) mesmo fora do raio de
    /// interesse, até morrerem ou saírem da área.
    /// </summary>
    internal sealed class InimigosConhecidosNoMapa : MonoBehaviour
    {
        private readonly Dictionary<long, Marcador> _conhecidos = new Dictionary<long, Marcador>();

        public static InimigosConhecidosNoMapa Atual { get; private set; }

        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();

        private void OnEnable()
        {
            Atual = this;
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<InimigosDaArea>(
                    MobsMessageIds.InimigosDaArea,
                    AoReceberArea
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<InimigoApareceu>(
                    MobsMessageIds.InimigoApareceu,
                    m => Guardar(m?.Inimigo)
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<InimigoMudou>(MobsMessageIds.InimigoMudou, AoMudar)
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<InimigoMorreu>(
                    MobsMessageIds.InimigoMorreu,
                    m => Remover(m?.InimigoId ?? 0)
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<InimigoSaiuDoInteresse>(
                    MobsMessageIds.InimigoSaiuDoInteresse,
                    m => MarcarLonge(m?.InimigoId ?? 0)
                )
            );
        }

        private void OnDisable()
        {
            if (Atual == this)
            {
                Atual = null;
            }

            foreach (var assinatura in _assinaturas)
            {
                assinatura.Dispose();
            }

            _assinaturas.Clear();
        }

        public void CopiarVivos(List<MarcadorDoMapa> destino)
        {
            destino.Clear();
            foreach (var par in _conhecidos)
            {
                if (!par.Value.Vivo)
                {
                    continue;
                }

                destino.Add(
                    new MarcadorDoMapa(
                        par.Value.X,
                        par.Value.Z,
                        0f,
                        ProjecaoDoMapa.AreaDaCidade,
                        false,
                        par.Value.Longe
                    )
                );
            }
        }

        private void AoReceberArea(InimigosDaArea mensagem)
        {
            _conhecidos.Clear();
            if (mensagem?.Inimigos == null)
            {
                return;
            }

            foreach (var inimigo in mensagem.Inimigos)
            {
                Guardar(inimigo);
            }
        }

        private void AoMudar(InimigoMudou mensagem)
        {
            if (mensagem == null || !_conhecidos.TryGetValue(mensagem.InimigoId, out var marcador))
            {
                return;
            }

            marcador.X = mensagem.X;
            marcador.Z = mensagem.Z;
            marcador.Vivo = mensagem.Estado != EstadoDeInimigo.Morto && mensagem.Vida > 0;
            marcador.Longe = false;
        }

        private void Guardar(InimigoNoMundo dados)
        {
            if (dados == null)
            {
                return;
            }

            var vivo = dados.Estado != EstadoDeInimigo.Morto && dados.Vida > 0;
            if (!vivo)
            {
                Remover(dados.InimigoId);
                return;
            }

            _conhecidos[dados.InimigoId] = new Marcador
            {
                X = dados.X,
                Z = dados.Z,
                Vivo = true,
                Longe = false,
            };
        }

        internal void MarcarLonge(long inimigoId)
        {
            if (_conhecidos.TryGetValue(inimigoId, out var marcador))
            {
                marcador.Longe = true;
            }
        }

        private void Remover(long inimigoId)
        {
            if (inimigoId != 0)
            {
                _conhecidos.Remove(inimigoId);
            }
        }

        private sealed class Marcador
        {
            public float X;
            public float Z;
            public bool Vivo;
            public bool Longe;
        }
    }
}
