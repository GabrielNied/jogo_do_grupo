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
        private Label _posicao;
        private Label _preparando;
        private Label _lugarEmFoco;
        private Vector2? _sobMouse;
        private IVisualElementScheduledItem _agenda;
        private IVisualElementScheduledItem _agendaGeracao;
        private readonly List<MarcadorDoMapa> _outros = new List<MarcadorDoMapa>();

        public override bool TelaInteira => true;

        protected override void Montar(VisualElement conteudo)
        {
            var estilo = Resources.Load<StyleSheet>("Mundo/EstiloMapa");
            if (estilo != null)
            {
                conteudo.styleSheets.Add(estilo);
            }

            _mapa = new VisualElement { name = "mundo.mapa-mundi" };
            _mapa.AddToClassList("mundo-mapa-tela");
            _mapa.RegisterCallback<PointerMoveEvent>(e =>
                _sobMouse = PontoDoMouse(e.localPosition)
            );
            _mapa.RegisterCallback<PointerLeaveEvent>(e => _sobMouse = null);
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

            foreach (var lugar in EstruturasDoMapa.Lugares)
            {
                AdicionarLugar(lugar);
            }

            _lugarEmFoco = new Label();
            _lugarEmFoco.AddToClassList(EstiloCore.Texto);
            _lugarEmFoco.AddToClassList("mundo-mapa-lugar-em-foco");
            _lugarEmFoco.style.display = DisplayStyle.None;
            _mapa.Add(_lugarEmFoco);

            var norte = new Label("N ↑");
            norte.AddToClassList(EstiloCore.Texto);
            norte.AddToClassList("mundo-mapa-norte");
            _mapa.Add(norte);

            var legenda = new Label("◆ Lugares   ◆ Caverna   ▲ Você  ·  Sem viagem rápida");
            legenda.AddToClassList(EstiloCore.Texto);
            legenda.AddToClassList("mundo-mapa-legenda");
            _mapa.Add(legenda);

            _posicao = new Label();
            _posicao.AddToClassList(EstiloCore.Texto);
            _posicao.AddToClassList("mundo-mapa-posicao");
            _mapa.Add(_posicao);

            _preparando = new Label("Preparando mapa...");
            _preparando.AddToClassList(EstiloCore.Texto);
            _preparando.AddToClassList("mundo-mapa-preparando");
            _mapa.Add(_preparando);
            AplicarTextura();
        }

        protected override void AoAbrir()
        {
            Atualizar();
            _agenda = Conteudo.schedule.Execute(Atualizar).Every(200);
            if (TexturaDoMapa.Pronta == null)
            {
                _agendaGeracao = Conteudo.schedule.Execute(AvancarTextura).Every(16);
            }
        }

        protected override void AoFechar()
        {
            _agenda?.Pause();
            _agenda = null;
            _agendaGeracao?.Pause();
            _agendaGeracao = null;
        }

        private void AvancarTextura()
        {
            if (!TexturaDoMapa.Avancar(8))
            {
                return;
            }

            AplicarTextura();
            _agendaGeracao?.Pause();
            _agendaGeracao = null;
        }

        private void AplicarTextura()
        {
            var textura = TexturaDoMapa.Pronta;
            _preparando.style.display = textura == null ? DisplayStyle.Flex : DisplayStyle.None;
            if (textura != null)
            {
                _mapa.style.backgroundImage = Background.FromTexture2D(textura);
            }
        }

        private void AdicionarLugar(LugarDoMapa lugar)
        {
            var lado = ProjecaoDoMapa.LadoDasAmostras;
            ProjecaoDoMapa.PixelDoLugar(lugar, lado, lado, out var pixelX, out var pixelY);
            var marcador = new VisualElement();
            marcador.AddToClassList("mundo-mapa-lugar");
            marcador.EnableInClassList(
                "mundo-mapa-lugar-passagem",
                lugar.Tipo == TipoDeEstrutura.Passagem
            );
            marcador.tooltip = lugar.Nome;
            marcador.pickingMode = PickingMode.Position;
            marcador.focusable = true;
            marcador.style.left = Length.Percent(pixelX * 100f / lado);
            marcador.style.top = Length.Percent(pixelY * 100f / lado);
            marcador.RegisterCallback<PointerEnterEvent>(_ => MostrarLugar(lugar.Nome));
            marcador.RegisterCallback<PointerLeaveEvent>(_ => EsconderLugar());
            marcador.RegisterCallback<FocusInEvent>(_ => MostrarLugar(lugar.Nome));
            marcador.RegisterCallback<FocusOutEvent>(_ => EsconderLugar());

            var simbolo = new Label("◆");
            simbolo.AddToClassList(EstiloCore.Texto);
            simbolo.AddToClassList("mundo-mapa-lugar-simbolo");
            simbolo.pickingMode = PickingMode.Ignore;
            marcador.Add(simbolo);

            _mapa.Add(marcador);
        }

        private void MostrarLugar(string nome)
        {
            _lugarEmFoco.text = nome;
            _lugarEmFoco.style.display = DisplayStyle.Flex;
        }

        private void EsconderLugar()
        {
            _lugarEmFoco.style.display = DisplayStyle.None;
        }

        private void Atualizar()
        {
            if (!Aberta)
            {
                return;
            }

            PresencaNoMundo.Atual?.CopiarOutros(_outros);
            _marcadores?.MarkDirtyRepaint();
            AtualizarPosicao();
        }

        private Vector2? PontoDoMouse(Vector2 local)
        {
            var ret = _mapa.contentRect;
            if (ret.width < 1f || ret.height < 1f)
            {
                return null;
            }

            ProjecaoDoMapa.DoMapa(
                local.x / ret.width,
                1f - local.y / ret.height,
                out var x,
                out var z
            );
            return new Vector2(x, z);
        }

        private void AtualizarPosicao()
        {
            var texto = "";
            var jogador = EntradaNoJogo.Jogador;
            if (jogador != null)
            {
                var p = jogador.transform.position;
                texto = "Você: " + ProjecaoDoMapa.Coordenadas(p.x, p.z);
            }

            if (_sobMouse.HasValue)
            {
                texto +=
                    (texto.Length > 0 ? "   |   " : "")
                    + "Mouse: "
                    + ProjecaoDoMapa.Coordenadas(_sobMouse.Value.x, _sobMouse.Value.y);
            }

            _posicao.text = texto;
        }

        private void DesenharMarcadores(MeshGenerationContext contexto)
        {
            var ret = _marcadores.contentRect;
            if (ret.width < 1f)
            {
                return;
            }

            var pincel = contexto.painter2D;
            ProjecaoDoMapa.NoMapa(
                0f,
                MapaDoMundo.MetadeDaCidade,
                out var estradaU,
                out var estradaV
            );
            ProjecaoDoMapa.NoMapa(0f, MapaDoMundo.RaioTransicao2, out _, out var fimEstradaV);
            pincel.strokeColor = CoresDosBiomas.Estrada;
            pincel.lineWidth = Mathf.Max(2f, ret.width * 8f / MapaDoMundo.Lado);
            pincel.BeginPath();
            pincel.MoveTo(new Vector2(estradaU * ret.width, (1f - estradaV) * ret.height));
            pincel.LineTo(new Vector2(estradaU * ret.width, (1f - fimEstradaV) * ret.height));
            pincel.Stroke();

            var jogador = EntradaNoJogo.Jogador;
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
