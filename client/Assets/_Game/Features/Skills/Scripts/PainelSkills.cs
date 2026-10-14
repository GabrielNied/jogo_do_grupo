using System.Collections.Generic;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Skills;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Skills
{
    /// <summary>
    /// A barra de skills no centro de baixo da tela, os números de dano das skills e o aviso de
    /// recusa (UI Toolkit montada em código). Só mostra o que recebe; o visual está em
    /// <c>EstiloSkills.uss</c> e usa os tokens Lumora.
    /// </summary>
    internal sealed class PainelSkills
    {
        public const string CaminhoEstilo = "UI/EstiloSkills";

        private readonly VisualElement _raiz;
        private readonly VisualElement _barra;
        private readonly Label _vazio;
        private readonly Label _aviso;
        private readonly VisualElement _numeros;
        private readonly List<Espaco> _espacos = new List<Espaco>();
        private readonly List<Numero> _vivos = new List<Numero>();
        private readonly List<Label> _livres = new List<Label>();

        private sealed class Espaco
        {
            public VisualElement Caixa;
            public Label Nome;
            public Label Custo;
            public VisualElement Cobertura;
            public Label Recarga;
        }

        private sealed class Numero
        {
            public Label Rotulo;
            public Vector3 Origem;
            public float Idade;
        }

        public PainelSkills(VisualElement raiz)
        {
            _raiz = raiz;
            var estilo = Resources.Load<StyleSheet>(CaminhoEstilo);
            if (estilo != null)
            {
                raiz.styleSheets.Add(estilo);
            }

            _barra = new VisualElement { name = "skills.barra" };
            _barra.pickingMode = PickingMode.Ignore;
            _barra.AddToClassList("skills-barra");
            for (var i = 0; i < ConfigSkills.SkillsPorArma; i++)
            {
                _espacos.Add(CriarEspaco(i));
            }

            _vazio = new Label(ApresentacaoSkills.TextoSemArma);
            _vazio.pickingMode = PickingMode.Ignore;
            _vazio.AddToClassList("skills-vazio");

            _aviso = new Label();
            _aviso.pickingMode = PickingMode.Ignore;
            _aviso.AddToClassList("skills-aviso");
            _aviso.style.display = DisplayStyle.None;

            _numeros = new VisualElement { name = "skills.numeros" };
            _numeros.pickingMode = PickingMode.Ignore;
            _numeros.AddToClassList("skills-numeros");

            raiz.Add(_numeros);
            raiz.Add(_aviso);
            raiz.Add(_vazio);
            raiz.Add(_barra);
        }

        private Espaco CriarEspaco(int indice)
        {
            var espaco = new Espaco();
            espaco.Caixa = new VisualElement { name = "skills.espaco." + indice };
            espaco.Caixa.pickingMode = PickingMode.Ignore;
            espaco.Caixa.AddToClassList("skill-espaco");

            var tecla = new Label(ApresentacaoSkills.Tecla(indice));
            tecla.pickingMode = PickingMode.Ignore;
            tecla.AddToClassList("skill-tecla");

            espaco.Nome = new Label();
            espaco.Nome.pickingMode = PickingMode.Ignore;
            espaco.Nome.AddToClassList("skill-nome");

            espaco.Custo = new Label();
            espaco.Custo.pickingMode = PickingMode.Ignore;
            espaco.Custo.AddToClassList("skill-custo");

            espaco.Cobertura = new VisualElement();
            espaco.Cobertura.pickingMode = PickingMode.Ignore;
            espaco.Cobertura.AddToClassList("skill-cobertura");

            espaco.Recarga = new Label();
            espaco.Recarga.pickingMode = PickingMode.Ignore;
            espaco.Recarga.AddToClassList("skill-recarga");

            espaco.Caixa.Add(espaco.Cobertura);
            espaco.Caixa.Add(tecla);
            espaco.Caixa.Add(espaco.Nome);
            espaco.Caixa.Add(espaco.Custo);
            espaco.Caixa.Add(espaco.Recarga);
            _barra.Add(espaco.Caixa);
            return espaco;
        }

        /// <summary>Desenha a barra: nome, custo, e a recarga que falta de cada skill.</summary>
        public void Mostrar(EstadoDaBarra estado, double agora)
        {
            var tem = estado.TemSkills;
            _barra.style.display = tem ? DisplayStyle.Flex : DisplayStyle.None;
            _vazio.style.display = tem || !estado.Recebida ? DisplayStyle.None : DisplayStyle.Flex;
            for (var i = 0; i < _espacos.Count; i++)
            {
                var skill = estado.Skill(i);
                var espaco = _espacos[i];
                espaco.Caixa.style.display = skill == null ? DisplayStyle.None : DisplayStyle.Flex;
                if (skill == null)
                {
                    continue;
                }

                var restante = estado.RestanteMs(i, agora);
                espaco.Nome.text = skill.Nome;
                espaco.Custo.text = ApresentacaoSkills.Custo(skill);
                espaco.Recarga.text = ApresentacaoSkills.Recarga(restante);
                espaco.Cobertura.style.height = Length.Percent(
                    ApresentacaoSkills.FracaoDaRecarga(restante, skill.RecargaMs) * 100f
                );
                espaco.Caixa.EnableInClassList("skill-em-recarga", restante > 0);
            }
        }

        public void MostrarAviso(string texto)
        {
            _aviso.text = texto;
            _aviso.style.display = DisplayStyle.Flex;
        }

        public void EsconderAviso() => _aviso.style.display = DisplayStyle.None;

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
            }
        }
    }
}
