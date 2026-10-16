using System;
using System.Collections.Generic;

namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>As categorias de poder (PODER-002). Cada modelo-base declara quais pode receber.</summary>
    public enum CategoriaDePoder
    {
        Amplificacao,
        Transformacao,
        Gatilho,
        PreparacaoEConsumo,
        Conversao,
        RecargaERecursos,
        Mobilidade,
        DefesaReativa,
        SinergiaDeGrupo,
    }

    /// <summary>O que dispara uma regra. Os dois últimos dependem de Combate publicar o evento.</summary>
    public enum GatilhoDePoder
    {
        /// <summary>Uma skill compatível foi usada (custo cobrado, antes do dano).</summary>
        AoUsarSkill,

        /// <summary>Uma skill compatível acertou ao menos um alvo.</summary>
        AoAcertarComSkill,

        /// <summary>Um golpe comum do personagem acertou.</summary>
        AoAcertarAtaqueBasico,

        /// <summary>O personagem esquivou.</summary>
        AoEsquivar,
    }

    /// <summary>A condição, avaliada no estado de antes do evento, para a regra valer.</summary>
    public enum CondicaoDePoder
    {
        Nenhuma,

        /// <summary>Algum alvo atingido tem a marca <see cref="RegraDePoder.Estado"/>.</summary>
        AlvoMarcado,

        /// <summary>O personagem tem ao menos uma carga de <see cref="RegraDePoder.Estado"/>.</summary>
        TemCarga,
    }

    /// <summary>O que a regra faz quando dispara. Nenhuma ação dispara outro gatilho.</summary>
    public enum AcaoDePoder
    {
        /// <summary>Marca os alvos atingidos com <see cref="RegraDePoder.Estado"/> por <see cref="RegraDePoder.DuracaoMs"/>.</summary>
        MarcarAlvos,

        /// <summary>Dá uma carga de <see cref="RegraDePoder.Estado"/> ao personagem, até o máximo.</summary>
        GanharCarga,

        /// <summary>
        /// Com <see cref="CondicaoDePoder.TemCarga"/>: gasta todas as cargas e soma
        /// <see cref="RegraDePoder.Valor"/> por carga ao dano da skill. Sem condição: soma o valor
        /// uma vez.
        /// </summary>
        AmplificarDano,

        /// <summary>Tira <see cref="RegraDePoder.Valor"/> ms da recarga das skills de <see cref="RegraDePoder.AlvoDaAcao"/>.</summary>
        ReduzirRecarga,
    }

    /// <summary>
    /// Escolhe skills de forma explícita (PODER-003): pelo id ou por uma etiqueta que a skill declara.
    /// Seletor sem nenhum dos dois é inválido, então nada vale "para tudo da arma".
    /// </summary>
    public sealed class SeletorDeSkill
    {
        public string[] Ids = Array.Empty<string>();
        public string Etiqueta = "";

        public static SeletorDeSkill PorEtiqueta(string etiqueta) =>
            new SeletorDeSkill { Etiqueta = etiqueta };

        public bool Valido => Ids.Length > 0 || !string.IsNullOrEmpty(Etiqueta);

        public bool Corresponde(string skillId, IReadOnlyCollection<string> etiquetas)
        {
            if (Array.IndexOf(Ids, skillId) >= 0)
                return true;
            if (string.IsNullOrEmpty(Etiqueta) || etiquetas == null)
                return false;
            foreach (var etiqueta in etiquetas)
            {
                if (etiqueta == Etiqueta)
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Uma regra de poder, em dados (PODER-004): quando (gatilho e skill), sob qual condição, o
    /// que faz, com que valor, por quanto tempo e com que limites. O servidor interpreta; nada é
    /// código por poder.
    /// </summary>
    public sealed class RegraDePoder
    {
        public string Id = "";
        public GatilhoDePoder Gatilho;

        /// <summary>A skill que dispara a regra (e que recebe o dano, na amplificação). Ignorada nos gatilhos de Combate.</summary>
        public SeletorDeSkill Skill = new SeletorDeSkill();

        public CondicaoDePoder Condicao;

        /// <summary>O nome do estado (marca ou carga) que a condição e a ação usam. Estados são explícitos e globais.</summary>
        public string Estado = "";

        public AcaoDePoder Acao;
        public double Valor;
        public int DuracaoMs;

        /// <summary>Teto de cargas empilhadas (acumulação); 1 para marcas.</summary>
        public int MaximoDeAcumulos = 1;

        /// <summary>Folga mínima entre duas ativações da regra; protege contra repetição.</summary>
        public int IntervaloMinimoMs;

        /// <summary>Só em <see cref="AcaoDePoder.ReduzirRecarga"/>: as skills que ficam mais rápidas.</summary>
        public SeletorDeSkill AlvoDaAcao = new SeletorDeSkill();

        /// <summary>Se a regra está bem formada: o servidor ignora a que não está.</summary>
        public bool Valida
        {
            get
            {
                if (string.IsNullOrEmpty(Id) || MaximoDeAcumulos < 1 || IntervaloMinimoMs < 0)
                    return false;
                var usaSkill =
                    Gatilho == GatilhoDePoder.AoUsarSkill
                    || Gatilho == GatilhoDePoder.AoAcertarComSkill;
                if (usaSkill && !Skill.Valido)
                    return false;
                if (Condicao != CondicaoDePoder.Nenhuma && string.IsNullOrEmpty(Estado))
                    return false;
                if (
                    Condicao == CondicaoDePoder.AlvoMarcado
                    && Gatilho != GatilhoDePoder.AoAcertarComSkill
                )
                    return false;
                switch (Acao)
                {
                    case AcaoDePoder.MarcarAlvos:
                        return Gatilho == GatilhoDePoder.AoAcertarComSkill
                            && !string.IsNullOrEmpty(Estado)
                            && DuracaoMs > 0;
                    case AcaoDePoder.GanharCarga:
                        return !string.IsNullOrEmpty(Estado) && DuracaoMs > 0;
                    case AcaoDePoder.AmplificarDano:
                        return Gatilho == GatilhoDePoder.AoUsarSkill && Valor > 0;
                    case AcaoDePoder.ReduzirRecarga:
                        return AlvoDaAcao.Valido && Valor > 0;
                    default:
                        return false;
                }
            }
        }
    }

    /// <summary>Um poder lendário: definição separada do exemplar (PODER-004).</summary>
    public sealed class DefinicaoDePoder
    {
        public string Id = "";
        public string Nome = "";
        public CategoriaDePoder Categoria;

        /// <summary>O que o poder faz, em uma frase para o tooltip.</summary>
        public string Descricao = "";

        /// <summary>Quais skills ele afeta, em palavras para o tooltip (PODER-003).</summary>
        public string Afeta = "";

        /// <summary>Os tipos de peça em que o poder pode existir.</summary>
        public TipoEquipamento[] Encaixes = Array.Empty<TipoEquipamento>();

        public RegraDePoder[] Regras = Array.Empty<RegraDePoder>();

        public bool Aceita(TipoEquipamento tipo) => Array.IndexOf(Encaixes, tipo) >= 0;
    }

    /// <summary>
    /// Uma regra ligada na hora, com a origem (poder ou marco de set) que a liga. É o que Itens
    /// entrega a Skills (recalculado ao equipar, desequipar e trocar de conjunto).
    /// </summary>
    public sealed class EfeitoAtivo
    {
        /// <summary>"poder:&lt;id&gt;" ou "set:&lt;id&gt;:&lt;peças&gt;"; muda quando a origem sai.</summary>
        public string Origem = "";

        public string Nome = "";
        public RegraDePoder Regra = new RegraDePoder();

        /// <summary>A chave do estado de execução (intervalo mínimo): origem mais regra.</summary>
        public string Chave => Origem + "/" + Regra.Id;
    }
}
