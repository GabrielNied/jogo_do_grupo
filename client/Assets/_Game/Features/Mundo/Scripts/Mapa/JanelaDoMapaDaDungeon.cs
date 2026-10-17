using System;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Client.Core.Visual;
using JogoDoGrupo.Client.Features.Mundo.Presenca;
using JogoDoGrupo.Shared.Features.Instancias;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Mundo
{
    /// <summary>
    /// Mapa da Mina do Reflexo (tecla M na instância): todos os salões desde a entrada (#903).
    /// </summary>
    internal sealed class JanelaDoMapaDaDungeon : JanelaDoJogo
    {
        private VisualElement _planta;
        private Label _posicao;

        public override bool TelaInteira => true;

        protected override void Montar(VisualElement conteudo)
        {
            var estilo = Resources.Load<StyleSheet>("Mundo/EstiloMapa");
            if (estilo != null)
            {
                conteudo.styleSheets.Add(estilo);
            }

            _planta = new VisualElement { name = "mundo.mapa-dungeon" };
            _planta.AddToClassList("mundo-mapa-tela");
            _planta.generateVisualContent += DesenharPlanta;
            conteudo.Add(_planta);

            var titulo = new Label("Mina do Reflexo");
            titulo.AddToClassList(EstiloCore.Texto);
            titulo.AddToClassList("mundo-mapa-legenda");
            _planta.Add(titulo);

            _posicao = new Label();
            _posicao.AddToClassList(EstiloCore.Texto);
            _posicao.AddToClassList("mundo-mapa-posicao");
            _planta.Add(_posicao);
        }

        protected override void AoAbrir()
        {
            AtualizarPosicao();
            Conteudo.schedule.Execute(AtualizarPosicao).Every(200);
        }

        private void AtualizarPosicao()
        {
            var jogador = EntradaNoJogo.Jogador;
            if (jogador == null)
            {
                _posicao.text = "";
                return;
            }

            var p = jogador.transform.position;
            _posicao.text = "Você: " + ProjecaoDoMapa.Coordenadas(p.x, p.z);
            _planta.MarkDirtyRepaint();
        }

        private void DesenharPlanta(MeshGenerationContext contexto)
        {
            var ret = _planta.contentRect;
            if (ret.width < 40f || ret.height < 40f)
            {
                return;
            }

            var pincel = contexto.painter2D;
            var ultimo = LayoutDaMinaDoReflexo.Trechos[^1];
            var comprimento = ultimo.Ate;
            var margem = 24f;
            var larguraUtil = ret.width - (2f * margem);
            var alturaUtil = ret.height - (2f * margem);
            var escala = Math.Min(larguraUtil / comprimento, alturaUtil / 30f);

            pincel.strokeColor = EstiloVisual.Cor(ConfigVisual.Cores.Trilho);
            pincel.lineWidth = 2f;
            foreach (var trecho in LayoutDaMinaDoReflexo.Trechos)
            {
                var x0 = margem + (trecho.De * escala);
                var x1 = margem + (trecho.Ate * escala);
                var y = ret.height * 0.5f;
                var meioLargura = trecho.MetadeDaLargura * escala;
                pincel.BeginPath();
                pincel.MoveTo(new Vector2(x0, y - meioLargura));
                pincel.LineTo(new Vector2(x1, y - meioLargura));
                pincel.LineTo(new Vector2(x1, y + meioLargura));
                pincel.LineTo(new Vector2(x0, y + meioLargura));
                pincel.ClosePath();
                pincel.Stroke();
            }

            var jogador = EntradaNoJogo.Jogador;
            if (jogador == null)
            {
                return;
            }

            var local = jogador.transform.position.x - LayoutDaMinaDoReflexo.OrigemX;
            local = Mathf.Clamp(local, 0f, comprimento);
            var px = margem + (local * escala);
            var py = ret.height * 0.5f;
            pincel.fillColor = EstiloVisual.Cor(ConfigVisual.Cores.Essencia);
            pincel.BeginPath();
            pincel.MoveTo(new Vector2(px, py - 6f));
            pincel.LineTo(new Vector2(px + 5f, py + 4f));
            pincel.LineTo(new Vector2(px - 5f, py + 4f));
            pincel.ClosePath();
            pincel.Fill();
        }
    }
}
