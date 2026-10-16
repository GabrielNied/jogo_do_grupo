using System;
using System.Collections.Generic;

namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Raridade sorteada do modelo anterior. <b>Legado</b> desde a #615: ninguém decide por ela
    /// (a fusão da #616 também não); o tier vem dos atributos (<see cref="TierItem"/>). Fica no
    /// exemplar só para o histórico.
    /// </summary>
    public enum RaridadeItem
    {
        Comum,
        Incomum,
        Raro,
        Epico,
        Lendario,
    }

    public enum GrauEquipamento
    {
        Inicial,
        Medio,
        Forte,
    }

    public enum TipoEquipamento
    {
        Capacete,
        Peito,
        Luvas,
        Calca,
        Botas,
        Cinto,
        Anel,
        Brinco,
        Adorno,
        Espada,
        Martelo,
        Arco,
        Cajado,
        Escudo,
        Foco,
    }

    public enum PosicaoEquipamento
    {
        Inventario,
        Capacete,
        Peito,
        Luvas,
        Calca,
        Botas,
        Cinto,
        Anel1,
        Anel2,
        Brinco1,
        Brinco2,
        Adorno,
        Arma1,
        Secundaria1,
        Arma2,
        Secundaria2,
    }

    /// <summary>
    /// Atributos do item (ATRIB-001). Força, Vigor, Agilidade, Intelecto e Espírito são <b>do
    /// item</b>: nunca somam nos atributos do personagem (DEC-001). Os números de ordem não mudam
    /// (o exemplar grava o número no JSON): os valores marcados como legado são os do modelo
    /// anterior, que a migração para a versão 2 (<see cref="MigracaoDeAtributos"/>) troca pelos
    /// novos, e <see cref="Vigor"/> mudou de sentido (era o recurso, agora é o atributo).
    /// </summary>
    public enum StatusItem
    {
        Forca,

        /// <summary>Legado: vira <see cref="Agilidade"/> na migração.</summary>
        Destreza,
        Intelecto,

        /// <summary>Legado: vira <see cref="Vigor"/> na migração.</summary>
        Vitalidade,
        Espirito,

        /// <summary>Legado: vira <see cref="Vigor"/> na migração.</summary>
        Vida,

        /// <summary>Atributo Vigor do item. No legado era o recurso, que virou <see cref="Agilidade"/>.</summary>
        Vigor,

        /// <summary>Legado: vira <see cref="Espirito"/> na migração.</summary>
        Energia,

        /// <summary>Legado: vira <see cref="DefesaFisica"/> na migração.</summary>
        Defesa,

        /// <summary>Legado sem efeito até existirem elementos.</summary>
        PotenciaFogo,

        /// <summary>Legado sem efeito até existirem elementos.</summary>
        PotenciaGelo,

        /// <summary>Legado sem efeito até existir alcance.</summary>
        Alcance,

        /// <summary>Legado: vira <see cref="RegeneracaoStamina"/> na migração.</summary>
        RecuperacaoVigor,
        Agilidade,

        /// <summary>Dano da arma, físico ou mágico pela categoria (cajado é mágico). Só em arma.</summary>
        DanoDaArma,
        DefesaFisica,
        DefesaMagica,
        RegeneracaoVida,
        RegeneracaoStamina,
        RegeneracaoMana,
        ChanceCritica,
        DanoCritico,
        VelocidadeAtaqueFisico,
        VelocidadeAtaqueMagico,
        PenetracaoFisica,
        PenetracaoMagica,

        /// <summary>Dano físico flat somado ao golpe (v3; substitui <see cref="Forca"/> do item).</summary>
        DanoFisico,

        /// <summary>Dano mágico flat somado ao golpe (v3; substitui <see cref="Intelecto"/> do item).</summary>
        DanoMagico,

        /// <summary>Vida máxima concedida pela peça (v3; substitui <see cref="Vigor"/> do item).</summary>
        VidaMaxima,
    }

    [Serializable]
    public class AtributoItem
    {
        public StatusItem Status;
        public double Valor;
        public bool Fixo;
    }

    /// <summary>Exemplar gerado uma vez pelo servidor. Nível da peça não é nível do personagem.</summary>
    [Serializable]
    public class Equipamento
    {
        public int ModeloId;
        public string Nome = "";
        public string Regiao = "";
        public string Familia = "";
        public string Variacao = "";
        public TipoEquipamento Tipo;

        /// <summary>Legado: nada novo decide por ela (veja <see cref="RaridadeItem"/>).</summary>
        public RaridadeItem Raridade;
        public GrauEquipamento Grau;
        public int Nivel = 1;
        public int NivelPersonagemMinimo = 1;
        public bool DuasMaos;

        /// <summary>
        /// Valores inteiros de 0 a 100 (<see cref="ConfigTier"/>) desde a versão 1; no legado,
        /// fracionários na escala do catálogo. Qualquer mudança aqui muda o tier atual
        /// (<see cref="RegrasTier.Calcular(Equipamento)"/>).
        /// </summary>
        public AtributoItem[] Atributos = Array.Empty<AtributoItem>();

        /// <summary>
        /// Tier na geração (ou na migração), nunca mais muda e não limita a evolução (TIER-004).
        /// Só vale com <see cref="VersaoDosAtributos"/> 1 ou mais.
        /// </summary>
        public TierItem TierOrigem;

        /// <summary>Escala dos atributos: 0 é o legado, <see cref="RegrasTier.VersaoAtual"/> a atual.</summary>
        public int VersaoDosAtributos;

        /// <summary>Nível de fusão, de +0 a +10 (<see cref="ConfigFusao.NivelMaximo"/>).</summary>
        public int NivelFusao;

        /// <summary>
        /// Legado da infusão (#174): só é lido na migração, que o converte em
        /// <see cref="NivelFusao"/> e zera (<see cref="MigracaoDeTier"/>). Nada novo grava aqui.
        /// </summary>
        public int InfusoesUsadas;

        /// <summary>
        /// O poder lendário sorteado na geração (PODER-006): vazio é "sem poder". Fixo no exemplar,
        /// a fusão preserva (DEC-003) e não entra no tier (DEC-004). Exemplar antigo lê vazio.
        /// </summary>
        public string PoderId = "";
        public bool Favorito;
        public bool Lixo;
    }

    [Serializable]
    public class PedidoEquipar
    {
        public long InstanciaId;
        public PosicaoEquipamento Posicao;
    }

    /// <summary>Descarta (destrói) um exemplar da bolsa. Rota: <see cref="RotasItens.Descartar"/>.</summary>
    [Serializable]
    public class PedidoDescartar
    {
        public long InstanciaId;
    }

    /// <summary>Descarta múltiplos itens em lote (ou todos marcados como lixo). Rota: <see cref="RotasItens.DescartarLote"/>.</summary>
    [Serializable]
    public class PedidoDescartarLote
    {
        public long[] InstanciaIds = System.Array.Empty<long>();
        public bool ApenasLixo;
    }

    /// <summary>Marca ou desmarca itens como lixo. Rota: <see cref="RotasItens.Lixo"/>.</summary>
    [Serializable]
    public class PedidoLixo
    {
        public long InstanciaId;
        public bool Lixo;
        public long[] InstanciaIds = System.Array.Empty<long>();
    }

    [Serializable]
    public class PedidoDesequipar
    {
        public PosicaoEquipamento Posicao;

        /// <summary>
        /// Quando verdadeiro, <see cref="Aba"/> e <see cref="Casa"/> dizem onde guardar a peça;
        /// falso: a primeira casa livre.
        /// </summary>
        public bool TemDestino;

        public int Aba;
        public int Casa;
    }

    /// <summary>
    /// Move um item da bolsa para uma casa. Casa ocupada: os dois trocam de lugar. Rota:
    /// <see cref="RotasItens.Mover"/>.
    /// </summary>
    [Serializable]
    public class PedidoMover
    {
        public long InstanciaId;
        public int Aba;
        public int Casa;
    }

    /// <summary>
    /// Ordena uma aba da bolsa no servidor (#562): tipo, tier (do maior para o menor), nível.
    /// Rota: <see cref="RotasItens.Ordenar"/>.
    /// </summary>
    [Serializable]
    public class PedidoOrdenar
    {
        public int Aba;
    }

    [Serializable]
    public class PedidoConjunto
    {
        public int Conjunto;
    }

    [Serializable]
    public class EquipamentosResponse
    {
        public EntradaInventario[] Inventario = Array.Empty<EntradaInventario>();
        public EntradaInventario[] Equipados = Array.Empty<EntradaInventario>();
        public CapacidadeBolsa Capacidade = RegrasItens.CapacidadeInicial();
        public int ConjuntoAtivo = 1;

        /// <summary>
        /// Nível do personagem, para a tela mostrar antes do clique o que o servidor recusaria
        /// por nível (<see cref="RegrasEquipar"/>). 0 é "não informado" (resposta antiga ou
        /// inventário limpo): a tela não recusa por nível.
        /// </summary>
        public int NivelDoPersonagem;

        /// <summary>
        /// O que as peças ativas dão ao combate, já convertido (ATRIB-003) e separado do
        /// personagem: nada aqui soma em Força, Vigor, Agilidade, Intelecto ou Espírito.
        /// </summary>
        public AtributosDosEquipamentos Atributos = new AtributosDosEquipamentos();
        public long Coroas;
        public int MateriaisDungeon;
        public int MateriaisRaid;
        public bool ModoFusao;
        public PreviaFusao Previa;
        public string Aviso = "";
    }

    public static class RegrasEquipamentos
    {
        public const string Rota = "/api/personagens/{personagemId}/equipamentos";

        public static string RotaDe(long id) => "/api/personagens/" + id + "/equipamentos";

        public static int NivelMaximo(RaridadeItem raridade) => 10 + 5 * (int)raridade;

        public static int SlotsSkills(RaridadeItem raridade) => 1 + (int)raridade;

        public static int NivelSkill(int slot) => slot == 0 ? 1 : slot * 5;

        public static int Fator(GrauEquipamento grau) => 1 << (int)grau;

        public static string NomeDaRaridade(RaridadeItem raridade)
        {
            switch (raridade)
            {
                case RaridadeItem.Incomum:
                    return "Incomum";
                case RaridadeItem.Raro:
                    return "Raro";
                case RaridadeItem.Epico:
                    return "Épico";
                case RaridadeItem.Lendario:
                    return "Lendário";
                default:
                    return "Comum";
            }
        }

        public static int Conjunto(PosicaoEquipamento posicao) =>
            posicao == PosicaoEquipamento.Arma1 || posicao == PosicaoEquipamento.Secundaria1 ? 1
            : posicao == PosicaoEquipamento.Arma2 || posicao == PosicaoEquipamento.Secundaria2 ? 2
            : 0;

        public static PosicaoEquipamento Principal(int conjunto) =>
            conjunto == 1 ? PosicaoEquipamento.Arma1 : PosicaoEquipamento.Arma2;

        /// <summary>
        /// Se a peça conta no combate: vestida (fora da bolsa) e, no caso de arma e mão
        /// secundária, do <paramref name="conjunto"/> ativo.
        /// </summary>
        public static bool EstaAtiva(EntradaInventario entrada, int conjunto)
        {
            if (entrada?.Equipamento == null || entrada.Posicao == PosicaoEquipamento.Inventario)
                return false;
            var origem = Conjunto(entrada.Posicao);
            return origem == 0 || origem == conjunto;
        }

        public static PosicaoEquipamento Secundaria(int conjunto) =>
            conjunto == 1 ? PosicaoEquipamento.Secundaria1 : PosicaoEquipamento.Secundaria2;

        /// <summary>
        /// Os dados de equipamento da entrada, ou <c>null</c> se o item não tem. O client lê JSON
        /// com <c>JsonUtility</c>, que troca <c>"Equipamento": null</c> por um objeto vazio (tipo
        /// <c>Capacete</c>, modelo 0); só um modelo real conta como equipamento.
        /// </summary>
        public static Equipamento DadosDe(EntradaInventario entrada)
        {
            var dados = entrada?.Equipamento;
            return dados != null && dados.ModeloId != 0 ? dados : null;
        }

        public static bool Aceita(TipoEquipamento tipo, PosicaoEquipamento posicao)
        {
            if (posicao >= PosicaoEquipamento.Capacete && posicao <= PosicaoEquipamento.Cinto)
                return (int)tipo == (int)posicao - 1;
            if (posicao == PosicaoEquipamento.Anel1 || posicao == PosicaoEquipamento.Anel2)
                return tipo == TipoEquipamento.Anel;
            if (posicao == PosicaoEquipamento.Brinco1 || posicao == PosicaoEquipamento.Brinco2)
                return tipo == TipoEquipamento.Brinco;
            if (posicao == PosicaoEquipamento.Adorno)
                return tipo == TipoEquipamento.Adorno;
            if (posicao == PosicaoEquipamento.Arma1 || posicao == PosicaoEquipamento.Arma2)
                return tipo >= TipoEquipamento.Espada && tipo <= TipoEquipamento.Cajado;
            if (
                posicao == PosicaoEquipamento.Secundaria1
                || posicao == PosicaoEquipamento.Secundaria2
            )
                return tipo == TipoEquipamento.Escudo || tipo == TipoEquipamento.Foco;
            return false;
        }

        /// <summary>
        /// Encaixe onde o clique direito equipa uma peça do <paramref name="tipo"/>: anel e brinco
        /// vão para o primeiro encaixe livre (os dois cheios: o primeiro, que troca); arma vai
        /// para o conjunto ativo; escudo e foco, para a mão secundária do conjunto ativo. O
        /// servidor continua conferindo tudo (nível, duas mãos, espaço).
        /// </summary>
        /// <param name="ocupados">Encaixes que já têm peça.</param>
        public static PosicaoEquipamento EncaixeAutomatico(
            TipoEquipamento tipo,
            IEnumerable<PosicaoEquipamento> ocupados,
            int conjuntoAtivo
        )
        {
            var usados = new HashSet<PosicaoEquipamento>(ocupados ?? new PosicaoEquipamento[0]);
            switch (tipo)
            {
                case TipoEquipamento.Anel:
                    return
                        usados.Contains(PosicaoEquipamento.Anel1)
                        && !usados.Contains(PosicaoEquipamento.Anel2)
                        ? PosicaoEquipamento.Anel2
                        : PosicaoEquipamento.Anel1;
                case TipoEquipamento.Brinco:
                    return
                        usados.Contains(PosicaoEquipamento.Brinco1)
                        && !usados.Contains(PosicaoEquipamento.Brinco2)
                        ? PosicaoEquipamento.Brinco2
                        : PosicaoEquipamento.Brinco1;
                case TipoEquipamento.Espada:
                case TipoEquipamento.Martelo:
                case TipoEquipamento.Arco:
                case TipoEquipamento.Cajado:
                    return Principal(conjuntoAtivo == 2 ? 2 : 1);
                case TipoEquipamento.Escudo:
                case TipoEquipamento.Foco:
                    return Secundaria(conjuntoAtivo == 2 ? 2 : 1);
                case TipoEquipamento.Adorno:
                    return PosicaoEquipamento.Adorno;
                default:
                    // Capacete..Cinto seguem a ordem do enum (Aceita usa a mesma conta).
                    return (PosicaoEquipamento)((int)tipo + 1);
            }
        }
    }
}
