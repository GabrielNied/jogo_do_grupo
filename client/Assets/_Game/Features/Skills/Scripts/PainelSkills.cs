using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Skills;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Skills
{
    /// <summary>
    /// A barra de ação embaixo, no centro (como a do World of Warcraft, com a leitura do combate
    /// de ação do New World): o golpe da arma no botão esquerdo, as três skills da arma com
    /// ícone, tecla, custo e recarga em relógio, e os atalhos fixos de combate (troca de arma e
    /// esquiva). Acima dela, a barra de preparo da skill forte e o aviso de recusa. Os números
    /// de dano das skills ficam na camada própria, sobre o mundo. Só mostra o que recebe; o
    /// visual está em <c>EstiloSkills.uss</c> e nos componentes do HUD do Core.
    /// </summary>
    internal sealed class PainelSkills
    {
        public const string CaminhoEstilo = "UI/EstiloSkills";

        private readonly VisualElement _raiz;
        private readonly VisualElement _barra;
        private readonly Label _vazio;
        private readonly Label _aviso;
        private readonly Label _poder;
        private readonly VisualElement _preparo;
        private readonly VisualElement _preparoBarra;
        private readonly Label _preparoTexto;
        private readonly VisualElement _numeros;
        private readonly SlotHud _golpe;
        private readonly List<Espaco> _espacos = new List<Espaco>();
        private readonly List<Numero> _vivos = new List<Numero>();
        private readonly List<Label> _livres = new List<Label>();

        private sealed class Espaco
        {
            public SlotHud Slot;
            public SkillDaBarra Skill;
            public bool EmRecarga;
        }

        private sealed class Numero
        {
            public Label Rotulo;
            public Vector3 Origem;
            public float Idade;
        }

        /// <param name="raiz">Camada sobre o mundo (números de dano).</param>
        /// <param name="regiaoDaBarra">
        /// Onde a barra de ação fica (a região <see cref="RegiaoDoHud.BarraDeAcao"/>); nula usa
        /// a própria <paramref name="raiz"/>.
        /// </param>
        public PainelSkills(VisualElement raiz, VisualElement regiaoDaBarra = null)
        {
            _raiz = raiz;
            var estilo = Resources.Load<StyleSheet>(CaminhoEstilo);
            var regiao = regiaoDaBarra ?? raiz;
            if (estilo != null)
            {
                raiz.styleSheets.Add(estilo);
                if (regiao != raiz && !regiao.styleSheets.Contains(estilo))
                    regiao.styleSheets.Add(estilo);
            }

            var coluna = new VisualElement
            {
                name = "skills.coluna",
                pickingMode = PickingMode.Ignore,
            };
            coluna.AddToClassList("skills-coluna");

            _aviso = ComponentesHud.Texto(string.Empty, "skills-aviso");
            _aviso.style.display = DisplayStyle.None;
            _poder = ComponentesHud.Texto(string.Empty, "skills-poder");
            _poder.style.display = DisplayStyle.None;

            _preparo = new VisualElement
            {
                name = "skills.preparo",
                pickingMode = PickingMode.Ignore,
            };
            _preparo.AddToClassList("skills-preparo");
            _preparo.style.display = DisplayStyle.None;
            _preparoBarra = new VisualElement { pickingMode = PickingMode.Ignore };
            _preparoBarra.AddToClassList("skills-preparo-barra");
            _preparoTexto = ComponentesHud.Texto(string.Empty, "skills-preparo-texto");
            _preparo.Add(_preparoBarra);
            _preparo.Add(_preparoTexto);

            _barra = new VisualElement { name = "skills.barra", pickingMode = PickingMode.Ignore };
            _barra.AddToClassList("skills-barra");

            _golpe = new SlotHud();
            _golpe.Raiz.AddToClassList("skills-slot-golpe");
            _golpe.DefinirTecla("M1");
            _golpe.DefinirVazio(false);
            ComponentesHud.ComDica(
                _golpe.Raiz,
                () =>
                    new ConteudoDaDica
                    {
                        Titulo = "Golpe da arma",
                        Texto =
                            "Clique: golpe leve (encadeia até 3).\nSegure: golpe pesado.\nBotão direito: guarda.",
                        Rodape = "Mouse 1 / Mouse 2",
                    }
            );
            _barra.Add(_golpe.Raiz);
            _barra.Add(Separador());

            for (var i = 0; i < ConfigSkills.SkillsPorArma; i++)
            {
                _espacos.Add(CriarEspaco(i));
            }

            _barra.Add(Separador());
            _barra.Add(
                SlotFixo(
                    "trocar-arma",
                    "R",
                    "Trocar de arma",
                    "Alterna entre os dois conjuntos de armas equipados (e as skills deles)."
                )
            );
            _barra.Add(
                SlotFixo(
                    "esquiva",
                    "Shift",
                    "Esquiva",
                    "Rolamento rápido na direção do movimento. Gasta stamina."
                )
            );

            _vazio = ComponentesHud.Texto(ApresentacaoSkills.TextoSemArma, "skills-vazio");

            _numeros = new VisualElement
            {
                name = "skills.numeros",
                pickingMode = PickingMode.Ignore,
            };
            _numeros.AddToClassList("skills-numeros");
            raiz.Add(_numeros);

            coluna.Add(_poder);
            coluna.Add(_aviso);
            coluna.Add(_preparo);
            coluna.Add(_vazio);
            coluna.Add(_barra);
            // Primeiro na região: o que outras funcionalidades põem ao lado (montaria) fica à direita.
            regiao.Insert(0, coluna);
        }

        private static VisualElement Separador()
        {
            var separador = new VisualElement { pickingMode = PickingMode.Ignore };
            separador.AddToClassList("skills-separador");
            return separador;
        }

        private static VisualElement SlotFixo(
            string icone,
            string tecla,
            string titulo,
            string texto
        )
        {
            var slot = new SlotHud(pequeno: true);
            slot.Raiz.AddToClassList("skills-slot-fixo");
            slot.DefinirVazio(false);
            slot.DefinirIcone(ComponentesHud.Icone("Icones", icone));
            slot.DefinirCorDoIcone("hud-icone-neutro");
            slot.DefinirTecla(tecla);
            ComponentesHud.ComDica(
                slot.Raiz,
                () =>
                    new ConteudoDaDica
                    {
                        Titulo = titulo,
                        Texto = texto,
                        Rodape = "Atalho: " + tecla,
                    }
            );
            return slot.Raiz;
        }

        private Espaco CriarEspaco(int indice)
        {
            var espaco = new Espaco { Slot = new SlotHud() };
            espaco.Slot.Raiz.name = "skills.espaco." + indice;
            espaco.Slot.Raiz.AddToClassList("skill-espaco");
            espaco.Slot.DefinirTecla(ApresentacaoSkills.Tecla(indice));
            ComponentesHud.ComDica(espaco.Slot.Raiz, () => Dica(espaco.Skill));
            _barra.Add(espaco.Slot.Raiz);
            return espaco;
        }

        private static ConteudoDaDica Dica(SkillDaBarra skill)
        {
            if (skill == null)
                return new ConteudoDaDica
                {
                    Titulo = "Espaço vazio",
                    Texto = ApresentacaoSkills.TextoSemArma,
                };
            var definicao = CatalogoDeSkills.Obter(skill.Id);
            var texto =
                definicao != null
                    ? ApresentacaoSkills.DescricaoCategoria(definicao)
                        + " · "
                        + ApresentacaoSkills.DescricaoDano(definicao)
                    : string.Empty;
            return new ConteudoDaDica
            {
                Titulo = skill.Nome,
                Subtitulo =
                    ApresentacaoSkills.Custo(skill)
                    + " · recarga "
                    + ApresentacaoSkills.RecargaEmSegundos(skill.RecargaMs),
                Texto = texto,
                Rodape = "Atalho: " + ApresentacaoSkills.Tecla(skill.Espaco),
            };
        }

        /// <summary>Desenha a barra: ícone, custo e a recarga que falta de cada skill.</summary>
        public void Mostrar(EstadoDaBarra estado, double agora, int preparandoEspaco = -1)
        {
            var tem = estado.TemSkills;
            _vazio.style.display = tem || !estado.Recebida ? DisplayStyle.None : DisplayStyle.Flex;

            var arma = estado.TipoDaArmaAtiva();
            _golpe.DefinirIcone(
                arma.HasValue
                    ? Resources.Load<Texture2D>("Itens/Icones/" + NomeDoIconeDaArma(arma.Value))
                    : null
            );
            _golpe.DefinirCorDoIcone("hud-icone-fisico");

            for (var i = 0; i < _espacos.Count; i++)
            {
                var skill = estado.Skill(i);
                var espaco = _espacos[i];
                espaco.Skill = skill;
                if (skill == null)
                {
                    espaco.Slot.DefinirVazio(true);
                    espaco.Slot.DefinirRodape(null);
                    espaco.EmRecarga = false;
                    continue;
                }

                espaco.Slot.DefinirVazio(false);
                espaco.Slot.DefinirIcone(ComponentesHud.Icone("Skills", skill.Id));
                espaco.Slot.DefinirCorDoIcone(ClasseDaCor(skill));
                espaco.Slot.DefinirRodape(skill.Custo.ToString());
                var restante = estado.RestanteMs(i, agora);
                espaco.Slot.DefinirRecarga(
                    ApresentacaoSkills.FracaoDaRecarga(restante, skill.RecargaMs),
                    ApresentacaoSkills.Recarga(restante)
                );
                var emRecarga = restante > 0;
                if (espaco.EmRecarga && !emRecarga)
                    espaco.Slot.Piscar();
                espaco.EmRecarga = emRecarga;
                espaco.Slot.DefinirAtivo(preparandoEspaco == i);
            }
        }

        private static string ClasseDaCor(SkillDaBarra skill)
        {
            var definicao = CatalogoDeSkills.Obter(skill.Id);
            if (definicao?.Propriedades != null)
            {
                if (
                    definicao.Propriedades.Tipo == TipoDeEfeito.Escudo
                    || definicao.Propriedades.Tipo == TipoDeEfeito.Cura
                    || definicao.Propriedades.Tipo == TipoDeEfeito.Utilidade
                )
                    return "hud-icone-apoio";
                if (definicao.Propriedades.Categoria == CategoriaDeDano.Magica)
                    return "hud-icone-magico";
            }

            return skill.Recurso == RecursoDeSkill.Mana ? "hud-icone-magico" : "hud-icone-fisico";
        }

        private static string NomeDoIconeDaArma(TipoEquipamento tipo)
        {
            switch (tipo)
            {
                case TipoEquipamento.Espada:
                    return "Espada";
                case TipoEquipamento.Martelo:
                    return "Martelo";
                case TipoEquipamento.Arco:
                    return "Arco";
                case TipoEquipamento.Cajado:
                    return "Cajado";
                case TipoEquipamento.Foco:
                    return "Foco";
                case TipoEquipamento.Escudo:
                    return "Escudo";
                default:
                    return tipo.ToString();
            }
        }

        /// <summary>A barra de preparo da skill forte: texto e quanto já encheu (0 a 1).</summary>
        public void MostrarPreparo(string texto, float fracao)
        {
            _preparoTexto.text = texto;
            _preparoBarra.style.width = Length.Percent(Mathf.Clamp01(fracao) * 100f);
            _preparo.style.display = DisplayStyle.Flex;
        }

        public void EsconderPreparo() => _preparo.style.display = DisplayStyle.None;

        public void MostrarAviso(string texto)
        {
            _aviso.text = texto;
            _aviso.style.display = DisplayStyle.Flex;
        }

        public void EsconderAviso() => _aviso.style.display = DisplayStyle.None;

        /// <summary>O aviso de que um poder de equipamento ou bônus de set disparou.</summary>
        public void MostrarPoder(string texto)
        {
            _poder.text = texto;
            _poder.style.display = DisplayStyle.Flex;
        }

        public void EsconderPoder() => _poder.style.display = DisplayStyle.None;

        /// <summary>Nasce um número de dano da skill sobre <paramref name="origem"/> (mundo).</summary>
        public void AdicionarNumero(string texto, Vector3 origem)
        {
            Label rotulo;
            if (_livres.Count > 0)
            {
                rotulo = _livres[_livres.Count - 1];
                _livres.RemoveAt(_livres.Count - 1);
            }
            else
            {
                rotulo = new Label();
                rotulo.pickingMode = PickingMode.Ignore;
                rotulo.AddToClassList("skill-dano");
                _numeros.Add(rotulo);
            }

            rotulo.text = texto;
            rotulo.style.display = DisplayStyle.Flex;
            _vivos.Add(
                new Numero
                {
                    Rotulo = rotulo,
                    Origem = origem,
                    Idade = 0f,
                }
            );
        }

        /// <summary>Faz os números subirem e sumirem; chame todo quadro.</summary>
        public void AtualizarNumeros(float segundos)
        {
            var camera = Camera.main;
            var painel = _numeros.panel;
            for (var i = _vivos.Count - 1; i >= 0; i--)
            {
                var numero = _vivos[i];
                numero.Idade += segundos;
                var progresso = numero.Idade / ConfigSkills.DuracaoDoNumeroSegundos;
                if (progresso >= 1f || camera == null || painel == null)
                {
                    numero.Rotulo.style.display = DisplayStyle.None;
                    _livres.Add(numero.Rotulo);
                    _vivos.RemoveAt(i);
                    continue;
                }

                var mundo = numero.Origem + Vector3.up * progresso;
                if (camera.WorldToViewportPoint(mundo).z <= 0f)
                {
                    numero.Rotulo.style.display = DisplayStyle.None;
                    continue;
                }

                var ponto = RuntimePanelUtils.CameraTransformWorldToPanel(painel, mundo, camera);
                numero.Rotulo.style.display = DisplayStyle.Flex;
                numero.Rotulo.style.left = ponto.x;
                numero.Rotulo.style.top = ponto.y;
                numero.Rotulo.style.opacity = 1f - progresso * progresso;
                // Começa grande e assenta, como o "pop" do número de dano.
                var escala = 1f + Mathf.Max(0f, 0.35f - progresso) * 1.4f;
                numero.Rotulo.style.scale = new Scale(new Vector3(escala, escala, 1f));
            }
        }
    }
}
