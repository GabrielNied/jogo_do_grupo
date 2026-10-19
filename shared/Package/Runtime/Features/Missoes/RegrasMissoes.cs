using System;
using System.Collections.Generic;
using System.Linq;

namespace JogoDoGrupo.Shared.Features.Missoes
{
    /// <summary>
    /// Regras puras e validações do motor de missões (#179, #1061).
    /// </summary>
    public static class RegrasMissoes
    {
        public static bool PodeAceitar(
            DefinicaoDeMissao definicao,
            int nivelPersonagem,
            IReadOnlyCollection<int> missoesConcluidas,
            IReadOnlyCollection<int> missoesAtivas,
            out string? erro
        )
        {
            if (definicao == null)
            {
                erro = CodigosErroMissoes.MissaoNaoEncontrada;
                return false;
            }

            if (missoesAtivas.Contains(definicao.Id))
            {
                erro = CodigosErroMissoes.MissaoJaAceita;
                return false;
            }

            if (definicao.Tipo != TipoDeMissao.Diaria && definicao.Tipo != TipoDeMissao.Semanal)
            {
                if (missoesConcluidas.Contains(definicao.Id))
                {
                    erro = CodigosErroMissoes.MissaoJaConcluida;
                    return false;
                }
            }

            if (nivelPersonagem < definicao.NivelMinimo)
            {
                erro = CodigosErroMissoes.NivelInsuficiente;
                return false;
            }

            if (definicao.RequisitosMissaoIds != null)
            {
                foreach (var req in definicao.RequisitosMissaoIds)
                {
                    if (!missoesConcluidas.Contains(req))
                    {
                        erro = CodigosErroMissoes.PreRequisitosNaoCumpridos;
                        return false;
                    }
                }
            }

            if (
                definicao.Tipo != TipoDeMissao.Principal
                && missoesAtivas.Count >= ConfigMissoes.LimiteMissoesEmAndamento
            )
            {
                erro = CodigosErroMissoes.LimiteMissoesAtingido;
                return false;
            }

            erro = null;
            return true;
        }

        public static bool PodeAbandonar(DefinicaoDeMissao definicao, out string? erro)
        {
            if (definicao == null)
            {
                erro = CodigosErroMissoes.MissaoNaoEncontrada;
                return false;
            }

            if (definicao.Tipo == TipoDeMissao.Principal)
            {
                erro = CodigosErroMissoes.MissaoPrincipalNaoAbandona;
                return false;
            }

            erro = null;
            return true;
        }

        public static bool PodeEntregar(
            DefinicaoDeMissao definicao,
            ProgressoDeMissaoDto progresso,
            int? itemIdEscolhido,
            out string? erro
        )
        {
            if (definicao == null || progresso == null)
            {
                erro = CodigosErroMissoes.MissaoNaoEncontrada;
                return false;
            }

            if (progresso.Estado == EstadoDaMissao.Concluida)
            {
                erro = CodigosErroMissoes.MissaoJaConcluida;
                return false;
            }

            if (!TodosObjetivosCumpridos(progresso))
            {
                erro = CodigosErroMissoes.ObjetivosNaoCumpridos;
                return false;
            }

            if (definicao.ItensParaEscolha != null && definicao.ItensParaEscolha.Length > 0)
            {
                if (
                    !itemIdEscolhido.HasValue
                    || Array.IndexOf(definicao.ItensParaEscolha, itemIdEscolhido.Value) < 0
                )
                {
                    erro = CodigosErroMissoes.ItemEscolhaInvalido;
                    return false;
                }
            }

            erro = null;
            return true;
        }

        public static bool TodosObjetivosCumpridos(ProgressoDeMissaoDto progresso)
        {
            if (progresso.Objetivos == null || progresso.Objetivos.Length == 0)
                return true;

            for (int i = 0; i < progresso.Objetivos.Length; i++)
            {
                if (!progresso.Objetivos[i].Concluido)
                    return false;
            }

            return true;
        }

        public static bool PodeCompartilhar(
            DefinicaoDeMissao definicao,
            bool ehLiderGrupo,
            float distanciaDoLiderMetros,
            out string? erro
        )
        {
            if (definicao == null)
            {
                erro = CodigosErroMissoes.MissaoNaoEncontrada;
                return false;
            }

            if (!ehLiderGrupo)
            {
                erro = CodigosErroMissoes.ApenasLiderCompartilha;
                return false;
            }

            if (distanciaDoLiderMetros > ConfigMissoes.RaioCompartilhamentoMetros)
            {
                erro = CodigosErroMissoes.LongeDoLider;
                return false;
            }

            erro = null;
            return true;
        }

        public static bool EstaPertoDoNpc(
            float px,
            float pz,
            float npcx,
            float npcz,
            float raio = ConfigMissoes.DistanciaInteracaoNpcMetros
        )
        {
            var dx = px - npcx;
            var dz = pz - npcz;
            return (dx * dx + dz * dz) <= (raio * raio);
        }

        /// <summary>
        /// Determina a cor do indicador (! ou ?) com base no nível do personagem e nível recomendado (#1061).
        /// </summary>
        public static string ObterCorDoIndicador(int nivelPersonagem, int nivelRecomendado)
        {
            if (nivelRecomendado - nivelPersonagem >= ConfigMissoes.DiferencaNivelAlta)
                return "vermelho";

            if (nivelPersonagem - nivelRecomendado >= ConfigMissoes.DiferencaNivelBaixa)
                return "cinza";

            return "amarelo";
        }
    }
}
