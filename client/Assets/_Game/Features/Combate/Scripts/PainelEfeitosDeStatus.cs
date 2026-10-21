using System.Collections.Generic;
using System.Globalization;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Combate;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// Efeitos de status ativos (e a imunidade a atordoar) como ícones abaixo do quadro do
    /// personagem, como os buffs e debuffs do World of Warcraft: borda verde para o que ajuda,
    /// vermelha para o que atrapalha, tempo embaixo, acúmulos no canto e o nome na dica.
    /// Some quando não há nada (#171, combate.md). Os ícones são reaproveitados a cada quadro.
    /// </summary>
    internal sealed class PainelEfeitosDeStatus
    {
        private readonly VisualElement _raiz;
        private readonly List<Icone> _icones = new List<Icone>();

        public VisualElement Raiz => _raiz;

        private sealed class Icone
        {
            public VisualElement Coluna;
            public VisualElement Caixa;
            public VisualElement Imagem;
            public Label Tempo;
            public Label Acumulos;
            public string Titulo;
            public string Descricao;
        }

        public PainelEfeitosDeStatus(VisualElement pai)
        {
            var estilo = Resources.Load<StyleSheet>(PainelRecursos.CaminhoEstilo);
            if (estilo != null && !pai.styleSheets.Contains(estilo))
            {
                pai.styleSheets.Add(estilo);
            }

            _raiz = new VisualElement { name = "combate.status" };
            _raiz.pickingMode = PickingMode.Ignore;
            _raiz.AddToClassList("painel-status");
            _raiz.style.display = DisplayStyle.None;

            pai.Add(_raiz);
        }

        public void Atualizar(
            IReadOnlyList<ItemDeStatus> efeitos,
            float imunidadeRestante,
            ItemDeEscudo escudo = default
        )
        {
            var usados = 0;
            if (escudo.Ativo)
            {
                Preencher(
                    usados++,
                    "protegido",
                    "Escudo",
                    escudo.Texto + ": absorve dano antes da vida.",
                    escudo.DuracaoRestante,
                    1,
                    true
                );
            }

            if (efeitos != null)
            {
                for (var i = 0; i < efeitos.Count; i++)
                {
                    var e = efeitos[i];
                    Preencher(
                        usados++,
                        NomeDoIcone(e.Tipo),
                        e.Nome,
                        Descricao(e.Tipo),
                        e.DuracaoRestante,
                        e.Acumulos,
                        EhBenefico(e.Tipo)
                    );
                }
            }

            if (imunidadeRestante > 0f)
            {
                Preencher(
                    usados++,
                    "atordoado",
                    "Imune a Atordoado",
                    "Não pode ser atordoado de novo por enquanto.",
                    imunidadeRestante,
                    1,
                    true
                );
            }

            for (var i = usados; i < _icones.Count; i++)
            {
                _icones[i].Coluna.style.display = DisplayStyle.None;
            }

            _raiz.style.display = usados > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Descartar()
        {
            _raiz.RemoveFromHierarchy();
        }

        private void Preencher(
            int indice,
            string icone,
            string titulo,
            string descricao,
            float segundos,
            int acumulos,
            bool benefico
        )
        {
            while (_icones.Count <= indice)
            {
                _icones.Add(Criar());
            }

            var item = _icones[indice];
            item.Coluna.style.display = DisplayStyle.Flex;
            item.Caixa.EnableInClassList("item-status-benefico", benefico);
            item.Caixa.EnableInClassList("item-status-nocivo", !benefico);
            var textura = ComponentesHud.Icone("Efeitos", icone);
            item.Imagem.style.backgroundImage =
                textura != null ? new StyleBackground(textura) : null;
            item.Tempo.text = TextoDoTempo(segundos);
            item.Acumulos.text = acumulos > 1 ? acumulos.ToString() : string.Empty;
            item.Titulo = titulo;
            item.Descricao = descricao;
        }

        private Icone Criar()
        {
            var item = new Icone();
            item.Caixa = new VisualElement();
            item.Caixa.AddToClassList("item-status");
            item.Imagem = new VisualElement { pickingMode = PickingMode.Ignore };
            item.Imagem.AddToClassList("item-status-icone");
            item.Acumulos = ComponentesHud.Texto(string.Empty, "item-status-acumulos");
            item.Tempo = ComponentesHud.Texto(string.Empty, "status-tempo");
            item.Caixa.Add(item.Imagem);
            item.Caixa.Add(item.Acumulos);
            var coluna = new VisualElement { pickingMode = PickingMode.Ignore };
            coluna.AddToClassList("item-status-coluna");
            coluna.Add(item.Caixa);
            coluna.Add(item.Tempo);
            _raiz.Add(coluna);
            ComponentesHud.ComDica(
                item.Caixa,
                () =>
                    new ConteudoDaDica
                    {
                        Titulo = item.Titulo,
                        Texto = item.Descricao,
                        Rodape = item.Tempo.text + " restantes",
                    }
            );
            item.Coluna = coluna;
            item.Coluna.style.display = DisplayStyle.None;
            return item;
        }

        private static string TextoDoTempo(float segundos)
        {
            if (segundos >= 60f)
                return Mathf.CeilToInt(segundos / 60f) + "m";
            if (segundos >= 10f)
                return Mathf.CeilToInt(segundos) + "s";
            return segundos.ToString("0.0", CultureInfo.InvariantCulture);
        }

        private static string NomeDoIcone(TipoEfeitoDeStatus tipo)
        {
            switch (tipo)
            {
                case TipoEfeitoDeStatus.Lentidao:
                    return "lentidao";
                case TipoEfeitoDeStatus.Atordoado:
                    return "atordoado";
                case TipoEfeitoDeStatus.Sangramento:
                    return "sangramento";
                case TipoEfeitoDeStatus.Queimadura:
                    return "queimadura";
                case TipoEfeitoDeStatus.Vulneravel:
                    return "vulneravel";
                case TipoEfeitoDeStatus.Fortalecido:
                    return "fortalecido";
                case TipoEfeitoDeStatus.Protegido:
                    return "protegido";
                default:
                    return null;
            }
        }

        private static bool EhBenefico(TipoEfeitoDeStatus tipo) =>
            tipo == TipoEfeitoDeStatus.Fortalecido || tipo == TipoEfeitoDeStatus.Protegido;

        private static string Descricao(TipoEfeitoDeStatus tipo)
        {
            switch (tipo)
            {
                case TipoEfeitoDeStatus.Lentidao:
                    return "Movimento 30% mais lento.";
                case TipoEfeitoDeStatus.Atordoado:
                    return "Não pode agir.";
                case TipoEfeitoDeStatus.Sangramento:
                    return "Perde vida a cada segundo. Acumula até 3 vezes.";
                case TipoEfeitoDeStatus.Queimadura:
                    return "Perde vida a cada segundo. Acumula até 3 vezes.";
                case TipoEfeitoDeStatus.Vulneravel:
                    return "Recebe 15% mais dano.";
                case TipoEfeitoDeStatus.Fortalecido:
                    return "Causa 15% mais dano.";
                case TipoEfeitoDeStatus.Protegido:
                    return "Recebe 15% menos dano.";
                default:
                    return string.Empty;
            }
        }
    }
}
