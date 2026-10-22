using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Audio;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Shared.Features.Mobs;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Mobs
{
    /// <summary>
    /// Monta as barras do topo quando o HUD existe e atualiza a cada quadro com o registro de
    /// inimigos do <see cref="ApresentadorDeInimigos"/>.
    /// </summary>
    internal static class ObservadorDaBarraAlvoTopo
    {
        private static PainelBarraAlvoTopo _painel;
        private static VisualElement _regiaoAtendida;

        internal static void GarantirMontado()
        {
            var regiao = Hud.Regiao(RegiaoDoHud.TopoCentro);
            if (regiao == null)
            {
                return;
            }

            if (_regiaoAtendida == regiao && _painel != null)
            {
                return;
            }

            _painel?.Descartar();
            _painel = new PainelBarraAlvoTopo(regiao);
            _regiaoAtendida = regiao;
        }

        internal static void Descartar()
        {
            _painel?.Descartar();
            _painel = null;
            _regiaoAtendida = null;
            AudioDoJogo.DefinirLutaContraChefe(false);
        }

        internal static void Atualizar(
            IReadOnlyCollection<InimigoNoClient> inimigos,
            RastreadorDeAlvoDoHud rastreador,
            Camera camera,
            Vector3 posicaoDoJogador,
            long personagemId
        )
        {
            if (!EntradaNoJogo.NoJogo)
            {
                Descartar();
                return;
            }

            GarantirMontado();
            if (_painel == null)
            {
                return;
            }

            if (inimigos == null || inimigos.Count == 0)
            {
                _painel.Mostrar(null, null);
                return;
            }

            var candidatos = MontarCandidatos(inimigos, camera, posicaoDoJogador);
            var sobMira = MiraDeCombate.InimigoSobMira(camera, inimigos, posicaoDoJogador);
            var contatoComum = rastreador.UltimoContatoComum(inimigos);
            var segundosComum =
                contatoComum == null ? -1.0 : rastreador.SegundosDesdeContato(contatoComum.Value);

            var contatoBoss = rastreador.UltimoContatoComBoss(inimigos);
            var segundosBoss =
                contatoBoss == null ? -1.0 : rastreador.SegundosDesdeContato(contatoBoss.Value);

            var bossId = RegrasAlvoDoHud.EscolherBoss(
                candidatos,
                personagemId,
                contatoBoss,
                segundosBoss
            );
            InimigoNoClient boss = null;
            if (bossId != null)
            {
                boss = Achar(inimigos, bossId.Value);
            }

            long? alvoId;
            if (bossId != null)
            {
                alvoId = RegrasAlvoDoHud.EscolherAlvoSecundario(
                    candidatos,
                    bossId.Value,
                    sobMira,
                    contatoComum,
                    segundosComum
                );
            }
            else
            {
                alvoId = RegrasAlvoDoHud.EscolherAlvoComum(
                    candidatos,
                    sobMira,
                    contatoComum,
                    segundosComum
                );
            }

            InimigoNoClient alvo = null;
            if (alvoId != null)
            {
                alvo = Achar(inimigos, alvoId.Value);
            }

            _painel.Mostrar(boss, alvo);
            AudioDoJogo.DefinirLutaContraChefe(boss != null);
        }

        private static RegrasAlvoDoHud.Candidato[] MontarCandidatos(
            IReadOnlyCollection<InimigoNoClient> inimigos,
            Camera camera,
            Vector3 jogador
        )
        {
            var lista = new List<RegrasAlvoDoHud.Candidato>(inimigos.Count);
            foreach (var inimigo in inimigos)
            {
                if (inimigo == null)
                {
                    continue;
                }

                var distancia = DistanciaHorizontal(jogador, inimigo.X, inimigo.Z);
                var distanciaMira = DistanciaAoCentroViewport(camera, inimigo);
                lista.Add(
                    new RegrasAlvoDoHud.Candidato(
                        inimigo.InimigoId,
                        inimigo.CriaturaId,
                        inimigo.Vida,
                        inimigo.VidaMaxima,
                        distancia,
                        distanciaMira,
                        inimigo.AlvoId
                    )
                );
            }

            return lista.ToArray();
        }

        private static float DistanciaAoCentroViewport(Camera camera, InimigoNoClient inimigo)
        {
            if (camera == null)
            {
                return float.MaxValue;
            }

            var mundo = new Vector3(inimigo.X, inimigo.Y + 1.4f * inimigo.Escala, inimigo.Z);
            var viewport = camera.WorldToViewportPoint(mundo);
            if (viewport.z <= 0f)
            {
                return float.MaxValue;
            }

            var dx = viewport.x - 0.5f;
            var dy = viewport.y - 0.5f;
            return dx * dx + dy * dy;
        }

        private static float DistanciaHorizontal(Vector3 jogador, float x, float z)
        {
            var dx = x - jogador.x;
            var dz = z - jogador.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static InimigoNoClient Achar(IReadOnlyCollection<InimigoNoClient> inimigos, long id)
        {
            foreach (var inimigo in inimigos)
            {
                if (inimigo != null && inimigo.InimigoId == id)
                {
                    return inimigo;
                }
            }

            return null;
        }
    }
}
