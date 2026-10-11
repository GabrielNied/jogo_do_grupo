using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Client.Core.Visual;
using JogoDoGrupo.Client.Features.Mundo.Presenca;
using JogoDoGrupo.Client.Features.Mundo.Terreno;
using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Mundo
{
    /// <summary>
    /// Mapa do mundo de 3 km (tecla M): biomas, a cidade destacada se o jogador está nela,
    /// a seta dele e os outros da mesma área. A dungeon fica para a issue #66.
    /// </summary>
    internal sealed class JanelaDoMapa : JanelaDoJogo
    {
        private VisualElement _mapa;
        private VisualElement _marcadores;
        private IVisualElementScheduledItem _agenda;
        private readonly List<MarcadorDoMapa> _outros = new List<MarcadorDoMapa>();
        private readonly Color32[] _amostras = new Color32[
            ProjecaoDoMapa.LadoDasAmostras * ProjecaoDoMapa.LadoDasAmostras
        ];

        protected override void Montar(VisualElement conteudo)
        {
            PreencherAmostras();
            var legenda = new Label("Cidade e o terreno em volta. Sem viagem rápida.");
            legenda.AddToClassList(EstiloCore.Texto);
            conteudo.Add(legenda);

            _mapa = new VisualElement { name = "mundo.mapa-mundi" };
            _mapa.AddToClassList("mundo-mapa");
            _mapa.RegisterCallback<GeometryChangedEvent>(e =>
            {
                // Mantém a proporção ao estreitar a área disponível.
                if (e.newRect.width > 0 && e.newRect.height != e.newRect.width)
                    _mapa.style.height = e.newRect.width;
            });
            _mapa.generateVisualContent += DesenharFundo;
            conteudo.Add(_mapa);

            _marcadores = new VisualElement();
            _marcadores.pickingMode = PickingMode.Ignore;
            _marcadores.style.position = Position.Absolute;
            _marcadores.style.left = 0;
            _marcadores.style.right = 0;
            _marcadores.style.top = 0;
            _marcadores.style.bottom = 0;
            _marcadores.generateVisualContent += DesenharMarcadores;
            _mapa.Add(_marcadores);
        }

        protected override void AoAbrir()
        {
            _agenda = Conteudo.schedule.Execute(Atualizar).Every(200);
        }

        protected override void AoFechar()
        {
            _agenda?.Pause();
            _agenda = null;
        }

        private void PreencherAmostras()
        {
            var lado = ProjecaoDoMapa.LadoDasAmostras;
            for (var linha = 0; linha < lado; linha++)
            {
                for (var coluna = 0; coluna < lado; coluna++)
                {
                    ProjecaoDoMapa.MundoDaAmostra(coluna, linha, out var x, out var z);
                    var cor = CoresDosBiomas.Do(MapaDoMundo.BiomaEm(x, z));
                    _amostras[linha * lado + coluna] = cor;
                }
            }
        }

        private void Atualizar()
        {
            if (!Aberta)
            {
                return;
            }

            PresencaNoMundo.Atual?.CopiarOutros(_outros);
            _marcadores?.MarkDirtyRepaint();
        }

        private void DesenharFundo(MeshGenerationContext contexto)
        {
            var ret = _mapa.contentRect;
            if (ret.width < 1f)
            {
                return;
            }

            var lado = ProjecaoDoMapa.LadoDasAmostras;
            var largura = ret.width / lado;
            var altura = ret.height / lado;
            var pincel = contexto.painter2D;
            for (var linha = 0; linha < lado; linha++)
            {
                for (var coluna = 0; coluna < lado; coluna++)
                {
                    var cor = (Color)_amostras[linha * lado + coluna];
                    var x = coluna * largura;
                    var y = ret.height - (linha + 1) * altura;
                    Retangulo(pincel, x, y, largura + 0.5f, altura + 0.5f, cor);
                }
            }
        }

        private void DesenharMarcadores(MeshGenerationContext contexto)
        {
            var ret = _marcadores.contentRect;
            if (ret.width < 1f)
            {
                return;
            }

            var pincel = contexto.painter2D;
            var jogador = EntradaNoJogo.Jogador;
            if (
                jogador != null
                && ProjecaoDoMapa.DentroDaCidade(
                    jogador.transform.position.x,
                    jogador.transform.position.z
                )
            )
            {
                ProjecaoDoMapa.RetanguloDaCidade(out var u0, out var v0, out var u1, out var v1);
                var x = u0 * ret.width;
                var y = (1f - v1) * ret.height;
                var largura = (u1 - u0) * ret.width;
                var altura = (v1 - v0) * ret.height;
                pincel.strokeColor = EstiloVisual.Cor(ConfigVisual.Cores.Ouro);
                pincel.lineWidth = ConfigVisual.Bordas.Destaque;
                pincel.BeginPath();
                pincel.MoveTo(new Vector2(x, y));
                pincel.LineTo(new Vector2(x + largura, y));
                pincel.LineTo(new Vector2(x + largura, y + altura));
                pincel.LineTo(new Vector2(x, y + altura));
                pincel.ClosePath();
                pincel.Stroke();
            }

            foreach (var outro in _outros)
            {
                if (!ProjecaoDoMapa.NaAreaAtual(outro.Area, ProjecaoDoMapa.AreaDaCidade))
                {
                    continue;
                }

                Ponto(
                    pincel,
                    ret,
                    outro.X,
                    outro.Z,
                    ConfigVisual.Dimensoes.MarcadorAliado,
                    EstiloVisual.Cor(ConfigVisual.Cores.Marfim)
                );
            }

            if (jogador != null)
            {
                var posicao = jogador.transform.position;
                ProjecaoDoMapa.NoMapa(posicao.x, posicao.z, out var u, out var v);
                var centro = new Vector2(u * ret.width, (1f - v) * ret.height);
                Seta(
                    pincel,
                    centro,
                    ConfigVisual.Dimensoes.MarcadorJogador,
                    ProjecaoDoMapa.AnguloDaSeta(jogador.transform.eulerAngles.y),
                    EstiloVisual.Cor(ConfigVisual.Cores.Essencia)
                );
            }
        }

        private static void Ponto(
            Painter2D pincel,
            Rect ret,
            float mundoX,
            float mundoZ,
            float raio,
            Color cor
        )
        {
            ProjecaoDoMapa.NoMapa(mundoX, mundoZ, out var u, out var v);
            var centro = new Vector2(u * ret.width, (1f - v) * ret.height);
            pincel.fillColor = cor;
            pincel.BeginPath();
            pincel.MoveTo(new Vector2(centro.x, centro.y - raio));
            pincel.LineTo(new Vector2(centro.x + raio, centro.y));
            pincel.LineTo(new Vector2(centro.x, centro.y + raio));
            pincel.LineTo(new Vector2(centro.x - raio, centro.y));
            pincel.ClosePath();
            pincel.Fill();
        }

        private static void Retangulo(
            Painter2D pincel,
            float x,
            float y,
            float largura,
            float altura,
            Color cor
        )
        {
            pincel.fillColor = cor;
            pincel.BeginPath();
            pincel.MoveTo(new Vector2(x, y));
            pincel.LineTo(new Vector2(x + largura, y));
            pincel.LineTo(new Vector2(x + largura, y + altura));
            pincel.LineTo(new Vector2(x, y + altura));
            pincel.ClosePath();
            pincel.Fill();
        }

        private static void Seta(
            Painter2D pincel,
            Vector2 centro,
            float raio,
            float graus,
            Color cor
        )
        {
            var ponta = Girar(new Vector2(0f, -raio), graus);
            var esquerda = Girar(new Vector2(-raio * 0.55f, raio * 0.7f), graus);
            var direita = Girar(new Vector2(raio * 0.55f, raio * 0.7f), graus);
            pincel.fillColor = cor;
            pincel.BeginPath();
            pincel.MoveTo(centro + ponta);
            pincel.LineTo(centro + esquerda);
            pincel.LineTo(centro + direita);
            pincel.ClosePath();
            pincel.Fill();
        }

        private static Vector2 Girar(Vector2 ponto, float graus)
        {
            var radianos = graus * Mathf.Deg2Rad;
            var c = Mathf.Cos(radianos);
            var s = Mathf.Sin(radianos);
            return new Vector2(c * ponto.x - s * ponto.y, s * ponto.x + c * ponto.y);
        }
    }
}
