using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.UI
{
    /// <summary>
    /// Peças do HUD compartilhadas pelas funcionalidades (barras, slots com recarga, retrato,
    /// ícones e dica flutuante), para o HUD inteiro ter a mesma cara sem uma funcionalidade
    /// conhecer a outra. Visual em <c>EstiloCore.uss</c> (seção "HUD: componentes").
    /// </summary>
    public static class ComponentesHud
    {
        private const string PastaIcones = "Interface/";
        private static readonly Dictionary<string, Texture2D> _icones =
            new Dictionary<string, Texture2D>();

        /// <summary>
        /// Ícone branco da interface (<c>Resources/Interface/&lt;grupo&gt;/&lt;nome&gt;</c>), ou
        /// <c>null</c>. Grupos: <c>Icones</c>, <c>Skills</c> e <c>Efeitos</c>.
        /// </summary>
        public static Texture2D Icone(string grupo, string nome)
        {
            if (string.IsNullOrEmpty(nome))
                return null;
            var caminho = PastaIcones + grupo + "/" + nome;
            if (_icones.TryGetValue(caminho, out var textura) && textura != null)
                return textura;
            textura = Resources.Load<Texture2D>(caminho);
            _icones[caminho] = textura;
            return textura;
        }

        /// <summary>Elemento que só mostra um ícone tingido.</summary>
        public static VisualElement Imagem(Texture2D icone, string classe = null)
        {
            var imagem = new VisualElement { pickingMode = PickingMode.Ignore };
            imagem.AddToClassList("hud-icone");
            if (classe != null)
                imagem.AddToClassList(classe);
            if (icone != null)
                imagem.style.backgroundImage = new StyleBackground(icone);
            return imagem;
        }

        /// <summary>Texto do HUD que não recebe clique.</summary>
        public static Label Texto(string texto, string classe)
        {
            var rotulo = new Label(texto) { pickingMode = PickingMode.Ignore };
            rotulo.AddToClassList(classe);
            return rotulo;
        }

        /// <summary>
        /// Mostra a dica flutuante (título, linhas e cor do título) ao passar o mouse sobre o
        /// elemento. O conteúdo é lido na hora, então pode mudar com o jogo.
        /// </summary>
        public static void ComDica(VisualElement alvo, System.Func<ConteudoDaDica> conteudo)
        {
            DicaHud.Registrar(alvo, conteudo);
        }
    }

    /// <summary>O que a dica flutuante mostra.</summary>
    public sealed class ConteudoDaDica
    {
        public string Titulo;
        public string Subtitulo;
        public string Texto;
        public string Rodape;

        /// <summary>Classe extra do título (ex.: cor da raridade).</summary>
        public string ClasseTitulo;
    }

    /// <summary>
    /// Barra de recurso do HUD: fundo escuro, preenchimento tingido, rastro claro que alcança o
    /// valor devagar quando ele cai (o "pedaço" perdido fica visível) e texto no meio.
    /// </summary>
    public sealed class BarraHud
    {
        private readonly VisualElement _preenchimento;
        private readonly VisualElement _rastro;
        private readonly Label _texto;
        private float _fracao = -1f;
        private float _fracaoRastro;

        /// <param name="classeCor">
        /// Classe da cor: <c>hud-barra-vida</c>, <c>hud-barra-mana</c>, <c>hud-barra-stamina</c>,
        /// <c>hud-barra-xp</c>, <c>hud-barra-inimigo</c> ou outra do USS.
        /// </param>
        public BarraHud(string classeCor, bool fina = false)
        {
            Raiz = new VisualElement { pickingMode = PickingMode.Ignore };
            Raiz.AddToClassList("hud-barra");
            Raiz.EnableInClassList("hud-barra-fina", fina);
            Raiz.AddToClassList(classeCor);

            _rastro = new VisualElement { pickingMode = PickingMode.Ignore };
            _rastro.AddToClassList("hud-barra-rastro");
            _preenchimento = new VisualElement { pickingMode = PickingMode.Ignore };
            _preenchimento.AddToClassList("hud-barra-preenchimento");
            _texto = new Label { pickingMode = PickingMode.Ignore };
            _texto.AddToClassList("hud-barra-texto");

            Raiz.Add(_rastro);
            Raiz.Add(_preenchimento);
            Raiz.Add(_texto);
            Raiz.schedule.Execute(AtualizarRastro).Every(33);
        }

        public VisualElement Raiz { get; }

        /// <summary>Fração atual (0 a 1).</summary>
        public float Fracao => Mathf.Max(0f, _fracao);

        /// <summary>Define o valor; <paramref name="texto"/> nulo esconde o texto.</summary>
        public void Definir(float atual, float maximo, string texto)
        {
            var fracao = maximo > 0f ? Mathf.Clamp01(atual / maximo) : 0f;
            if (_fracao < 0f || fracao > _fracaoRastro)
                _fracaoRastro = fracao;
            _fracao = fracao;
            _preenchimento.style.width = Length.Percent(fracao * 100f);
            _rastro.style.width = Length.Percent(_fracaoRastro * 100f);
            _texto.text = texto ?? string.Empty;
            _texto.style.display = string.IsNullOrEmpty(texto)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }

        /// <summary>Atalho para "atual / máximo" no texto.</summary>
        public void Definir(int atual, int maximo) =>
            Definir(atual, maximo, atual + " / " + maximo);

        private void AtualizarRastro()
        {
            if (_fracaoRastro <= _fracao)
                return;
            _fracaoRastro = Mathf.Max(_fracao, _fracaoRastro - 0.012f);
            _rastro.style.width = Length.Percent(_fracaoRastro * 100f);
        }
    }

    /// <summary>
    /// Slot quadrado do HUD (barra de ação, efeitos): moldura, ícone, tecla no canto, custo
    /// embaixo e a recarga como um relógio escurecendo o ícone, com os segundos no meio.
    /// </summary>
    public sealed class SlotHud
    {
        private readonly VisualElement _icone;
        private readonly Label _tecla;
        private readonly Label _rodape;
        private readonly Label _recarga;
        private readonly RelogioDeRecarga _relogio;
        private readonly VisualElement _brilho;

        public SlotHud(bool pequeno = false)
        {
            Raiz = new VisualElement { pickingMode = PickingMode.Position };
            Raiz.AddToClassList("hud-slot");
            Raiz.EnableInClassList("hud-slot-pequeno", pequeno);

            _icone = new VisualElement { pickingMode = PickingMode.Ignore };
            _icone.AddToClassList("hud-slot-icone");
            _relogio = new RelogioDeRecarga();
            _brilho = new VisualElement { pickingMode = PickingMode.Ignore };
            _brilho.AddToClassList("hud-slot-brilho");
            _tecla = new Label { pickingMode = PickingMode.Ignore };
            _tecla.AddToClassList("hud-slot-tecla");
            _rodape = new Label { pickingMode = PickingMode.Ignore };
            _rodape.AddToClassList("hud-slot-rodape");
            _recarga = new Label { pickingMode = PickingMode.Ignore };
            _recarga.AddToClassList("hud-slot-recarga");

            Raiz.Add(_icone);
            Raiz.Add(_relogio);
            Raiz.Add(_brilho);
            Raiz.Add(_recarga);
            Raiz.Add(_tecla);
            Raiz.Add(_rodape);
            DefinirVazio(true);
        }

        public VisualElement Raiz { get; }

        public void DefinirIcone(Texture2D icone)
        {
            _icone.style.backgroundImage = icone != null ? new StyleBackground(icone) : null;
            _icone.style.display = icone != null ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>Classe de cor do ícone (ex.: <c>hud-icone-fisico</c>, <c>hud-icone-magico</c>).</summary>
        public void DefinirCorDoIcone(string classe)
        {
            foreach (
                var c in new[]
                {
                    "hud-icone-fisico",
                    "hud-icone-magico",
                    "hud-icone-apoio",
                    "hud-icone-neutro",
                }
            )
                _icone.EnableInClassList(c, c == classe);
        }

        public void DefinirTecla(string tecla)
        {
            _tecla.text = tecla ?? string.Empty;
            _tecla.style.display = string.IsNullOrEmpty(tecla)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }

        public void DefinirRodape(string texto)
        {
            _rodape.text = texto ?? string.Empty;
            _rodape.style.display = string.IsNullOrEmpty(texto)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }

        /// <summary>Slot sem conteúdo: moldura apagada.</summary>
        public void DefinirVazio(bool vazio)
        {
            Raiz.EnableInClassList("hud-slot-vazio", vazio);
            if (vazio)
            {
                _icone.style.display = DisplayStyle.None;
                DefinirRecarga(0f, null);
            }
        }

        /// <summary>
        /// Recarga: <paramref name="fracao"/> é quanto ainda falta (1 = acabou de usar, 0 = pronta);
        /// <paramref name="texto"/> são os segundos que faltam (ou nulo).
        /// </summary>
        public void DefinirRecarga(float fracao, string texto)
        {
            _relogio.Fracao = Mathf.Clamp01(fracao);
            _recarga.text = texto ?? string.Empty;
            _recarga.style.display = string.IsNullOrEmpty(texto)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            Raiz.EnableInClassList("hud-slot-em-recarga", fracao > 0f);
        }

        /// <summary>Sem recurso para usar: ícone avermelhado/apagado.</summary>
        public void DefinirSemRecurso(bool semRecurso) =>
            Raiz.EnableInClassList("hud-slot-sem-recurso", semRecurso);

        /// <summary>Destaque de "usando agora" (preparo ou ativo).</summary>
        public void DefinirAtivo(bool ativo) => Raiz.EnableInClassList("hud-slot-ativo", ativo);

        /// <summary>Pisca o brilho de "pronto" (quando a recarga acaba).</summary>
        public void Piscar()
        {
            _brilho.RemoveFromClassList("hud-slot-brilho-visivel");
            _brilho.schedule.Execute(() => _brilho.AddToClassList("hud-slot-brilho-visivel"));
            _brilho
                .schedule.Execute(() => _brilho.RemoveFromClassList("hud-slot-brilho-visivel"))
                .ExecuteLater(350);
        }
    }

    /// <summary>
    /// Escurecimento em forma de relógio (setor circular) para a recarga, desenhado com
    /// <see cref="Painter2D"/>: começa cheio e vai abrindo no sentido horário.
    /// </summary>
    internal sealed class RelogioDeRecarga : VisualElement
    {
        private float _fracao;

        public RelogioDeRecarga()
        {
            pickingMode = PickingMode.Ignore;
            AddToClassList("hud-slot-relogio");
            generateVisualContent += Desenhar;
        }

        public float Fracao
        {
            get => _fracao;
            set
            {
                if (Mathf.Approximately(_fracao, value))
                    return;
                _fracao = value;
                MarkDirtyRepaint();
            }
        }

        private void Desenhar(MeshGenerationContext contexto)
        {
            if (_fracao <= 0.001f)
                return;
            var r = contentRect;
            if (r.width <= 0 || r.height <= 0)
                return;
            var centro = r.center;
            // Raio até o canto: o setor cobre o quadrado inteiro (o slot corta o excesso).
            var raio = Mathf.Sqrt(r.width * r.width + r.height * r.height) * 0.5f;
            var pintor = contexto.painter2D;
            pintor.fillColor = new Color(0f, 0f, 0f, 0.68f);
            pintor.BeginPath();
            if (_fracao >= 0.999f)
            {
                pintor.MoveTo(new Vector2(r.xMin, r.yMin));
                pintor.LineTo(new Vector2(r.xMax, r.yMin));
                pintor.LineTo(new Vector2(r.xMax, r.yMax));
                pintor.LineTo(new Vector2(r.xMin, r.yMax));
                pintor.ClosePath();
                pintor.Fill();
                return;
            }

            // O que falta fica escuro: do ponto de "agora" até o topo, no sentido horário.
            var inicio = -90f + (1f - _fracao) * 360f;
            pintor.MoveTo(centro);
            pintor.Arc(
                centro,
                raio,
                Angle.Degrees(inicio),
                Angle.Degrees(270f),
                ArcDirection.Clockwise
            );
            pintor.ClosePath();
            pintor.Fill();
        }
    }

    /// <summary>Retrato redondo com a inicial (até termos arte de rosto) e anel dourado.</summary>
    public sealed class RetratoHud
    {
        private readonly Label _inicial;
        private readonly Label _nivel;
        private readonly VisualElement _fundo;

        public RetratoHud(bool pequeno = false)
        {
            Raiz = new VisualElement { pickingMode = PickingMode.Ignore };
            Raiz.AddToClassList("hud-retrato");
            Raiz.EnableInClassList("hud-retrato-pequeno", pequeno);
            _fundo = new VisualElement { pickingMode = PickingMode.Ignore };
            _fundo.AddToClassList("hud-retrato-fundo");
            _inicial = new Label { pickingMode = PickingMode.Ignore };
            _inicial.AddToClassList("hud-retrato-inicial");
            var anel = new VisualElement { pickingMode = PickingMode.Ignore };
            anel.AddToClassList("hud-retrato-anel");
            _nivel = new Label { pickingMode = PickingMode.Ignore };
            _nivel.AddToClassList("hud-retrato-nivel");
            _fundo.Add(_inicial);
            Raiz.Add(_fundo);
            Raiz.Add(anel);
            Raiz.Add(_nivel);
        }

        public VisualElement Raiz { get; }

        public void Definir(string nome, int nivel, Color cor)
        {
            _inicial.text = string.IsNullOrEmpty(nome)
                ? "?"
                : nome.Substring(0, 1).ToUpperInvariant();
            _nivel.text = nivel > 0 ? nivel.ToString() : string.Empty;
            _nivel.style.display = nivel > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _fundo.style.backgroundColor = cor;
        }
    }
}
