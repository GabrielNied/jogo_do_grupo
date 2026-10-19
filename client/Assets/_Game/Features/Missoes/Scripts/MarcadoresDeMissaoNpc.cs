using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Missoes;

namespace JogoDoGrupo.Client.Features.Missoes
{
    public enum TipoMarcadorNpc
    {
        Nenhum,
        EntregarPronta, // "?" dourado/amarelo
        EntregarPendente, // "?" cinza
        DisponivelNormal, // "!" amarelo
        DisponivelDificil, // "!" vermelho
        DisponivelTrivial, // "!" cinza
    }

    public struct InfoMarcadorNpc
    {
        public TipoMarcadorNpc Tipo;
        public string Simbolo; // "!" ou "?"
        public string CorHex; // "#FFD700", "#FF4444", "#A0A0A0"
        public int MissaoId;
        public string TituloMissao;

        public bool Ativo => Tipo != TipoMarcadorNpc.Nenhum;
    }

    /// <summary>
    /// Avalia e calcula o estado dos marcadores de missão sobre os NPCs e no minimapa (#1061).
    /// </summary>
    public static class MarcadoresDeMissaoNpc
    {
        public const string CorAmarela = "#FFD700";
        public const string CorVermelha = "#FF4444";
        public const string CorCinza = "#A0A0A0";
        public const string CorVerde = "#44FF44";

        public static InfoMarcadorNpc Avaliar(
            int npcId,
            int nivelJogador,
            IReadOnlyCollection<ProgressoDeMissaoDto> ativas,
            IReadOnlyCollection<int> concluidas
        )
        {
            if (npcId <= 0)
                return default;

            // 1. Prioridade máxima: missão para entregar neste NPC
            if (ativas != null)
            {
                foreach (var prog in ativas)
                {
                    var def = CatalogoDeMissoes.Obter(prog.MissaoId);
                    if (def != null && def.NpcEntregadorId == npcId)
                    {
                        if (prog.Estado == EstadoDaMissao.ProntaParaEntregar)
                        {
                            return new InfoMarcadorNpc
                            {
                                Tipo = TipoMarcadorNpc.EntregarPronta,
                                Simbolo = "?",
                                CorHex = CorAmarela,
                                MissaoId = def.Id,
                                TituloMissao = def.Titulo,
                            };
                        }
                    }
                }

                // 2. Segunda prioridade: missão aceita para este NPC, mas ainda pendente
                foreach (var prog in ativas)
                {
                    var def = CatalogoDeMissoes.Obter(prog.MissaoId);
                    if (def != null && def.NpcEntregadorId == npcId)
                    {
                        return new InfoMarcadorNpc
                        {
                            Tipo = TipoMarcadorNpc.EntregarPendente,
                            Simbolo = "?",
                            CorHex = CorCinza,
                            MissaoId = def.Id,
                            TituloMissao = def.Titulo,
                        };
                    }
                }
            }

            // 3. Terceira prioridade: missões disponíveis para pegar neste NPC
            var missoesAtivasIds = new HashSet<int>();
            if (ativas != null)
            {
                foreach (var a in ativas)
                {
                    missoesAtivasIds.Add(a.MissaoId);
                }
            }

            var concluidasSet =
                concluidas != null ? new HashSet<int>(concluidas) : new HashSet<int>();

            foreach (var def in CatalogoDeMissoes.Todas)
            {
                if (def.NpcDoadorId != npcId)
                    continue;

                if (
                    RegrasMissoes.PodeAceitar(
                        def,
                        nivelJogador,
                        concluidasSet,
                        missoesAtivasIds,
                        out _
                    )
                )
                {
                    var corNome = RegrasMissoes.ObterCorDoIndicador(
                        nivelJogador,
                        def.NivelRecomendado
                    );
                    if (corNome == "vermelho")
                    {
                        return new InfoMarcadorNpc
                        {
                            Tipo = TipoMarcadorNpc.DisponivelDificil,
                            Simbolo = "!",
                            CorHex = CorVermelha,
                            MissaoId = def.Id,
                            TituloMissao = def.Titulo,
                        };
                    }

                    if (corNome == "cinza")
                    {
                        return new InfoMarcadorNpc
                        {
                            Tipo = TipoMarcadorNpc.DisponivelTrivial,
                            Simbolo = "!",
                            CorHex = CorCinza,
                            MissaoId = def.Id,
                            TituloMissao = def.Titulo,
                        };
                    }

                    return new InfoMarcadorNpc
                    {
                        Tipo = TipoMarcadorNpc.DisponivelNormal,
                        Simbolo = "!",
                        CorHex = CorAmarela,
                        MissaoId = def.Id,
                        TituloMissao = def.Titulo,
                    };
                }
            }

            return default;
        }
    }
}
