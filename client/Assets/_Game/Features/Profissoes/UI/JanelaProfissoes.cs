using System.Text;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.Visual;
using JogoDoGrupo.Shared.Features.Profissoes;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Profissoes.UI
{
    /// <summary>
    /// A forja da cidade (#192), aberta com E no marcador: aprender profissões, ver os
    /// materiais e produzir as receitas do Ferreiro. O servidor decide; a janela só pede e
    /// mostra o que ele mandou (botão desabilitado mostra o motivo, nunca esconde).
    /// </summary>
    public sealed class JanelaProfissoes : JanelaDoJogo
    {
        public const string Chave = "profissoes.forja";
        public const int Ordem = 103;

        private const string CaminhoEstilo = "Profissoes/EstiloProfissoes";

        private VisualElement _materiais;
        private VisualElement _profissoes;
        private VisualElement _receitas;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar() => JanelasDoJogo.Registrar(CriarDefinicao());

        public static DefinicaoDeJanela CriarDefinicao() =>
            new DefinicaoDeJanela(
                Chave,
                TextosProfissoes.Titulo,
                null,
                Ordem,
                () => new JanelaProfissoes()
            );

        protected override void Montar(VisualElement conteudo)
        {
            var estilo = Resources.Load<StyleSheet>(CaminhoEstilo);
            if (estilo != null)
                conteudo.styleSheets.Add(estilo);
            conteudo.AddToClassList("profissoes-janela");

            var ajuda = ComponentesVisuais.Texto(TextosProfissoes.AjudaDaForja);
            ajuda.AddToClassList("profissoes-ajuda");
            conteudo.Add(ajuda);

            _materiais = Secao(conteudo, "Materiais");
            _profissoes = Secao(conteudo, "Profissões");
            _receitas = Secao(conteudo, "Forja do Ferreiro");
            Redesenhar();
        }

        protected override void AoAbrir()
        {
            ControleDeProfissoes.EstadoMudou += Redesenhar;
            ControleDeProfissoes.ConsultarTudo();
            Redesenhar();
        }

        protected override void AoFechar() => ControleDeProfissoes.EstadoMudou -= Redesenhar;

        private static VisualElement Secao(VisualElement pai, string titulo)
        {
            var secao = new VisualElement();
            secao.AddToClassList("profissoes-secao");
            secao.Add(ComponentesVisuais.Texto(titulo, subtitulo: true));
            var lista = new VisualElement();
            secao.Add(lista);
            pai.Add(secao);
            return lista;
        }

        private void Redesenhar()
        {
            if (_materiais == null || !Aberta)
                return;

            DesenharMateriais();
            DesenharProfissoes();
            DesenharReceitas();
        }

        private void DesenharMateriais()
        {
            _materiais.Clear();
            foreach (var material in ConfigProfissoes.Materiais)
            {
                var linha = Linha();
                linha.Add(Texto(material.Nome));
                linha.Add(Texto("x" + ControleDeProfissoes.Saldo(material.Id)));
                _materiais.Add(linha);
            }
        }

        private void DesenharProfissoes()
        {
            _profissoes.Clear();
            for (var numero = 1; numero <= 6; numero++)
            {
                var profissao = (Profissao)numero;
                var linha = Linha();
                linha.Add(Texto(ConfigProfissoes.NomeDaProfissao(profissao)));

                if (ControleDeProfissoes.Aprendeu(profissao))
                {
                    linha.Add(Detalhe("Aprendida"));
                }
                else if (ConfigProfissoes.PodeAprender(profissao))
                {
                    var alvo = profissao;
                    var botao = ComponentesVisuais.Botao(
                        "Aprender",
                        () => ControleDeProfissoes.Aprender(alvo),
                        principal: true
                    );
                    botao.AddToClassList("profissoes-botao");
                    linha.Add(botao);
                }
                else
                {
                    linha.Add(Detalhe("Em breve"));
                }

                _profissoes.Add(linha);
            }
        }

        private void DesenharReceitas()
        {
            _receitas.Clear();
            var ferreiro = ControleDeProfissoes.Aprendeu(Profissao.Ferreiro);
            if (!ferreiro)
            {
                _receitas.Add(Detalhe("Aprenda Ferreiro para produzir equipamentos."));
            }

            foreach (var receita in ConfigProfissoes.Receitas)
            {
                var linha = Linha();
                var texto = new VisualElement();
                texto.AddToClassList("profissoes-texto");
                texto.Add(ComponentesVisuais.Texto(TextosProfissoes.NomeDoProduto(receita)));
                texto.Add(Detalhe(Custo(receita, out var faltaMaterial), faltaMaterial));
                linha.Add(texto);

                var alvo = receita.Id;
                var botao = ComponentesVisuais.Botao(
                    "Produzir",
                    () => ControleDeProfissoes.Produzir(alvo),
                    principal: true
                );
                botao.AddToClassList("profissoes-botao");
                botao.SetEnabled(ferreiro && !faltaMaterial);
                linha.Add(botao);
                _receitas.Add(linha);
            }
        }

        private static string Custo(ReceitaDeProducao receita, out bool falta)
        {
            falta = false;
            var texto = new StringBuilder();
            for (var i = 0; i < receita.MateriaisIds.Length; i++)
            {
                var tem = ControleDeProfissoes.Saldo(receita.MateriaisIds[i]);
                var precisa = receita.MateriaisQuantidades[i];
                if (tem < precisa)
                    falta = true;
                if (i > 0)
                    texto.Append(" · ");
                texto.Append(ConfigProfissoes.NomeDoMaterial(receita.MateriaisIds[i]));
                texto.Append(' ').Append(tem).Append('/').Append(precisa);
            }

            if (falta)
                texto.Append(" (faltam materiais)");
            return texto.ToString();
        }

        private static VisualElement Linha()
        {
            var linha = new VisualElement();
            linha.AddToClassList("profissoes-linha");
            return linha;
        }

        private static Label Texto(string texto)
        {
            var rotulo = ComponentesVisuais.Texto(texto);
            rotulo.AddToClassList("profissoes-texto");
            return rotulo;
        }

        private static Label Detalhe(string texto, bool destaque = false)
        {
            var rotulo = ComponentesVisuais.Texto(texto);
            rotulo.AddToClassList("profissoes-detalhe");
            if (destaque)
                rotulo.AddToClassList("profissoes-falta");
            return rotulo;
        }
    }
}
