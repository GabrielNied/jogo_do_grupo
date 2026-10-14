using System;

namespace JogoDoGrupo.Shared.Features.Talentos
{
    /// <summary>Resultado de carregar um perfil de build na árvore de talentos.</summary>
    public sealed class CarregarPerfilResponse
    {
        public ArvoreResponse Arvore { get; set; }

        /// <summary>
        /// Avisos gerados na aplicação do perfil (ex.: nós que não existem mais na versão atual
        /// e foram ignorados, com seus pontos preservados).
        /// </summary>
        public string[] Avisos { get; set; } = Array.Empty<string>();
    }
}
