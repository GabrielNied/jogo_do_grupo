using System;

namespace JogoDoGrupo.Shared.Features.Talentos
{
    /// <summary>Resultado de carregar um perfil de build na árvore de talentos.</summary>
    [System.Serializable]
    public sealed class CarregarPerfilResponse
    {
        public ArvoreResponse Arvore = null!;

        /// <summary>
        /// Avisos gerados na aplicação do perfil (ex.: nós que não existem mais na versão atual
        /// e foram ignorados, com seus pontos preservados).
        /// </summary>
        public string[] Avisos = Array.Empty<string>();

        /// <summary>
        /// Coroas cobradas por trocar a distribuição (<see cref="ConfigRedistribuicao.CustoEmCoroas"/>);
        /// 0 se o perfil era igual ao que o personagem já tinha.
        /// </summary>
        public long CoroasCobradas;
    }
}
