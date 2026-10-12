using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Client.Features.Itens
{
    /// <summary>Onde o ponteiro soltou o item.</summary>
    public enum TipoAlvo
    {
        /// <summary>Dentro da janela, fora de qualquer casa ou encaixe.</summary>
        Nenhum,
        Casa,
        Encaixe,

        /// <summary>Fora da janela.</summary>
        Fora,
    }

    public struct AlvoSoltura
    {
        public TipoAlvo Tipo;
        public int Aba;
        public int Casa;
        public PosicaoEquipamento Posicao;

        public static AlvoSoltura EmCasa(int aba, int casa) =>
            new AlvoSoltura
            {
                Tipo = TipoAlvo.Casa,
                Aba = aba,
                Casa = casa,
            };

        public static AlvoSoltura EmEncaixe(PosicaoEquipamento posicao) =>
            new AlvoSoltura { Tipo = TipoAlvo.Encaixe, Posicao = posicao };

        public static AlvoSoltura Em(TipoAlvo tipo) => new AlvoSoltura { Tipo = tipo };
    }

    /// <summary>De onde o item sai: uma casa da bolsa ou um encaixe do paperdoll.</summary>
    public struct OrigemArrasto
    {
        public bool DoEncaixe;
        public PosicaoEquipamento Posicao;
        public int Aba;
        public int Casa;
        public long InstanciaId;

        /// <summary>Tipo da peça; <c>null</c> para item que não é equipamento (poção, moeda).</summary>
        public TipoEquipamento? Tipo;
    }

    public enum TipoAcao
    {
        Nada,
        Mover,
        Equipar,
        Desequipar,
        Descartar,

        /// <summary>Não envia nada: mostra <see cref="AcaoDoInventario.Aviso"/>.</summary>
        Recusada,
    }

    /// <summary>A intenção que a interação do jogador vira. O servidor decide o resultado.</summary>
    public struct AcaoDoInventario
    {
        public TipoAcao Tipo;
        public long InstanciaId;
        public PosicaoEquipamento Posicao;
        public int Aba;
        public int Casa;

        /// <summary>Em <see cref="TipoAcao.Desequipar"/>: se vai para <see cref="Aba"/>/<see cref="Casa"/>.</summary>
        public bool TemDestino;

        public string Aviso;

        public static AcaoDoInventario Nenhuma => new AcaoDoInventario { Tipo = TipoAcao.Nada };

        public static AcaoDoInventario Recusa(string aviso) =>
            new AcaoDoInventario { Tipo = TipoAcao.Recusada, Aviso = aviso };
    }

    /// <summary>
    /// Regras de interação do inventário, separadas da UI para teste: o que cada arraste e cada
    /// clique direito viram. Só monta a intenção; quem confere nível, duas mãos e espaço é o
    /// servidor.
    /// </summary>
    public static class LogicaInventario
    {
        public const string AvisoNaoCabe = "Essa peça não cabe nesse encaixe.";
        public const string AvisoNaoEquipavel = "Esse item não pode ser equipado.";
        public const string AvisoDescartarEquipado = "Desequipe a peça antes de descartá-la.";

        /// <summary>Tipo da peça, vindo do exemplar ou, nos itens antigos, do catálogo.</summary>
        public static TipoEquipamento? TipoDe(EntradaInventario entrada)
        {
            var dados = entrada.Equipamento ?? CatalogoEquipamentos.Obter(entrada.ItemId);
            if (dados == null)
                return null;
            return dados.Tipo;
        }

        public static OrigemArrasto OrigemDe(EntradaInventario entrada) =>
            new OrigemArrasto
            {
                DoEncaixe = entrada.Posicao != PosicaoEquipamento.Inventario,
                Posicao = entrada.Posicao,
                Aba = entrada.Aba,
                Casa = entrada.Casa,
                InstanciaId = entrada.InstanciaId,
                Tipo = TipoDe(entrada),
            };

        /// <summary>Encaixes que aceitam o tipo (acendem durante o arraste).</summary>
        public static List<PosicaoEquipamento> EncaixesValidos(TipoEquipamento tipo)
        {
            var validos = new List<PosicaoEquipamento>();
            foreach (var posicao in Encaixes)
            {
                if (RegrasEquipamentos.Aceita(tipo, posicao))
                    validos.Add(posicao);
            }
            return validos;
        }

        /// <summary>Os 15 encaixes do paperdoll (tudo menos <c>Inventario</c>).</summary>
        public static readonly PosicaoEquipamento[] Encaixes =
        {
            PosicaoEquipamento.Capacete,
            PosicaoEquipamento.Peito,
            PosicaoEquipamento.Luvas,
            PosicaoEquipamento.Calca,
            PosicaoEquipamento.Botas,
            PosicaoEquipamento.Cinto,
            PosicaoEquipamento.Anel1,
            PosicaoEquipamento.Anel2,
            PosicaoEquipamento.Brinco1,
            PosicaoEquipamento.Brinco2,
            PosicaoEquipamento.Adorno,
            PosicaoEquipamento.Arma1,
            PosicaoEquipamento.Secundaria1,
            PosicaoEquipamento.Arma2,
            PosicaoEquipamento.Secundaria2,
        };

        /// <summary>O que soltar o item em <paramref name="alvo"/> significa.</summary>
        public static AcaoDoInventario ResolverSoltura(OrigemArrasto origem, AlvoSoltura alvo)
        {
            switch (alvo.Tipo)
            {
                case TipoAlvo.Casa:
                    if (!origem.DoEncaixe)
                    {
                        if (origem.Aba == alvo.Aba && origem.Casa == alvo.Casa)
                            return AcaoDoInventario.Nenhuma;
                        return new AcaoDoInventario
                        {
                            Tipo = TipoAcao.Mover,
                            InstanciaId = origem.InstanciaId,
                            Aba = alvo.Aba,
                            Casa = alvo.Casa,
                        };
                    }
                    return new AcaoDoInventario
                    {
                        Tipo = TipoAcao.Desequipar,
                        InstanciaId = origem.InstanciaId,
                        Posicao = origem.Posicao,
                        TemDestino = true,
                        Aba = alvo.Aba,
                        Casa = alvo.Casa,
                    };
                case TipoAlvo.Encaixe:
                    if (origem.DoEncaixe && origem.Posicao == alvo.Posicao)
                        return AcaoDoInventario.Nenhuma;
                    if (origem.Tipo == null)
                        return AcaoDoInventario.Recusa(AvisoNaoEquipavel);
                    if (!RegrasEquipamentos.Aceita(origem.Tipo.Value, alvo.Posicao))
                        return AcaoDoInventario.Recusa(AvisoNaoCabe);
                    return new AcaoDoInventario
                    {
                        Tipo = TipoAcao.Equipar,
                        InstanciaId = origem.InstanciaId,
                        Posicao = alvo.Posicao,
                    };
                case TipoAlvo.Fora:
                    if (origem.DoEncaixe)
                        return AcaoDoInventario.Recusa(AvisoDescartarEquipado);
                    return new AcaoDoInventario
                    {
                        Tipo = TipoAcao.Descartar,
                        InstanciaId = origem.InstanciaId,
                    };
                default:
                    return AcaoDoInventario.Nenhuma;
            }
        }

        /// <summary>
        /// Clique direito: na bolsa equipa no encaixe automático; no paperdoll desequipa para a
        /// primeira casa livre.
        /// </summary>
        public static AcaoDoInventario CliqueDireito(
            EntradaInventario entrada,
            EquipamentosResponse estado
        )
        {
            if (entrada.Posicao != PosicaoEquipamento.Inventario)
            {
                return new AcaoDoInventario
                {
                    Tipo = TipoAcao.Desequipar,
                    InstanciaId = entrada.InstanciaId,
                    Posicao = entrada.Posicao,
                };
            }

            var tipo = TipoDe(entrada);
            if (tipo == null)
                return AcaoDoInventario.Recusa(AvisoNaoEquipavel);
            return new AcaoDoInventario
            {
                Tipo = TipoAcao.Equipar,
                InstanciaId = entrada.InstanciaId,
                Posicao = EncaixeAutomatico(tipo.Value, estado),
            };
        }

        public static PosicaoEquipamento EncaixeAutomatico(
            TipoEquipamento tipo,
            EquipamentosResponse estado
        )
        {
            var ocupados = new List<PosicaoEquipamento>();
            foreach (var equipado in estado.Equipados)
                ocupados.Add(equipado.Posicao);
            return RegrasEquipamentos.EncaixeAutomatico(tipo, ocupados, estado.ConjuntoAtivo);
        }

        public static EntradaInventario NoEncaixe(
            EquipamentosResponse estado,
            PosicaoEquipamento posicao
        )
        {
            foreach (var equipado in estado.Equipados)
            {
                if (equipado.Posicao == posicao)
                    return equipado;
            }
            return null;
        }

        public static EntradaInventario NaCasa(EquipamentosResponse estado, int aba, int casa)
        {
            foreach (var item in estado.Inventario)
            {
                if (item.Aba == aba && item.Casa == casa)
                    return item;
            }
            return null;
        }

        /// <summary>
        /// Exemplares que ficam travados (semitransparentes) enquanto o servidor responde: o
        /// que se move e quem está no destino.
        /// </summary>
        public static List<long> InstanciasEnvolvidas(
            AcaoDoInventario acao,
            EquipamentosResponse estado
        )
        {
            var ids = new List<long> { acao.InstanciaId };
            switch (acao.Tipo)
            {
                case TipoAcao.Mover:
                    AdicionarSe(ids, NaCasa(estado, acao.Aba, acao.Casa));
                    break;
                case TipoAcao.Equipar:
                    AdicionarSe(ids, NoEncaixe(estado, acao.Posicao));
                    break;
                case TipoAcao.Desequipar:
                    if (acao.TemDestino)
                        AdicionarSe(ids, NaCasa(estado, acao.Aba, acao.Casa));
                    break;
            }
            return ids;
        }

        private static void AdicionarSe(List<long> ids, EntradaInventario entrada)
        {
            if (entrada != null && !ids.Contains(entrada.InstanciaId))
                ids.Add(entrada.InstanciaId);
        }
    }
}
