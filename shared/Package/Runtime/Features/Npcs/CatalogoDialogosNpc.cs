using System;

namespace JogoDoGrupo.Shared.Features.Npcs
{
    /// <summary>
    /// Catálogo de diálogos e opções canônicas dos NPCs (#178).
    /// </summary>
    public static class CatalogoDialogosNpc
    {
        public static DialogoDoNpc ObterDialogo(DefinicaoNpc npc)
        {
            if (npc == null)
            {
                throw new ArgumentNullException(nameof(npc));
            }

            var fala = ObterFalaPadrao(npc);
            var opcoes = ObterOpcoesPadrao(npc);
            return new DialogoDoNpc(npc.Id, npc.Nome, npc.Papel, fala, opcoes);
        }

        public static string ObterFalaPadrao(DefinicaoNpc npc) =>
            npc.Papel switch
            {
                PapelNpc.Vendedor =>
                    "Saudações, viajante! Tenho provisões e itens de toda espécie para a sua jornada.",
                PapelNpc.Ferreiro =>
                    "O calor da forja nunca descansa. Precisa reforçar suas armas ou reparar o equipamento?",
                PapelNpc.Banqueiro =>
                    "Seus bens e coroas estão sempre em total segurança nos cofres da Brasa Mansa.",
                PapelNpc.Armeiro =>
                    "Lâminas afiadas, arcos equilibrados e cajados encantados. Em que posso ajudá-lo?",
                PapelNpc.Armadureiro =>
                    "Uma boa couraça é a diferença entre a glória e a cova. Escolha sua armadura.",
                PapelNpc.Taverneiro =>
                    "Puxe uma cadeira! Nada como uma refeição quente após enfrentar os perigos da estrada.",
                PapelNpc.Merceeiro =>
                    "Couros, pós, ervas e suprimentos gerais. Sempre repondo o estoque da cidade.",
                PapelNpc.Leiloeiro =>
                    "Compradores e vendedores de toda Asterra negociam aqui no mercado. Gostaria de ver o livro de ordens?",
                PapelNpc.Cavalarico =>
                    "Nenhum guerreiro precisa caminhar a pé pelas vastidões de Asterra. Conheça minhas montarias.",
                PapelNpc.CuidadorDePets =>
                    "Esses pequenos companheiros são fiéis e trazem ânimo nas horas sombrias.",
                PapelNpc.DadorDeMissao =>
                    "Contratos, avisos de caça e missões diárias para proteger e abastecer a Brasa Mansa.",
                PapelNpc.Guarda =>
                    "Mantenha a ordem na cidade e a espada embainhada. Os portões da Brasa Mansa estão sob vigília.",
                PapelNpc.Morador =>
                    "Bom dia, viajante! É bom ver novos aventureiros trazendo vida e segurança à nossa cidade.",
                PapelNpc.MestreLenhador =>
                    "As árvores do campo ao norte rendem madeira e ervas boas. Quer aprender o ofício de lenhador?",
                PapelNpc.MestreMinerador =>
                    "O ferro e os cristais do campo norte esperam por picaretas firmes. Quer aprender a minerar?",
                PapelNpc.MestreCurtidor =>
                    "Couro bem tratado vale mais que ouro na estrada. Quer aprender a curtir e a trabalhar o couro?",
                PapelNpc.MestreFerreiro =>
                    "Ferro, fogo e paciência fazem uma lâmina. Quer aprender a forjar armas e armaduras pesadas?",
                PapelNpc.MestreAlquimista =>
                    "Cristais, ervas e uma boa fervura: é disso que se faz a magia de bolso. Quer aprender alquimia?",
                PapelNpc.MestreCozinheiro =>
                    "Barriga cheia faz herói forte. Quer aprender a preparar refeições de expedição?",
                _ => "Olá, viajante! Bem-vindo à Brasa Mansa.",
            };

        public static OpcaoDeDialogo[] ObterOpcoesPadrao(DefinicaoNpc npc) =>
            npc.Papel switch
            {
                PapelNpc.Vendedor => new[]
                {
                    new OpcaoDeDialogo(
                        "negociar",
                        "Negociar mercadorias",
                        AcaoDeDialogo.AbrirServico,
                        "economia.vendedor"
                    ),
                    new OpcaoDeDialogo("sair", "Até logo", AcaoDeDialogo.Fechar),
                },
                PapelNpc.Ferreiro => new[]
                {
                    new OpcaoDeDialogo(
                        "forja",
                        "Serviços da Forja",
                        AcaoDeDialogo.AbrirServico,
                        "economia.ferreiro"
                    ),
                    new OpcaoDeDialogo("sair", "Até logo", AcaoDeDialogo.Fechar),
                },
                PapelNpc.Banqueiro => new[]
                {
                    new OpcaoDeDialogo(
                        "banco",
                        "Acessar Cofre",
                        AcaoDeDialogo.AbrirServico,
                        "itens.banco"
                    ),
                    new OpcaoDeDialogo("sair", "Até logo", AcaoDeDialogo.Fechar),
                },
                PapelNpc.Armeiro => new[]
                {
                    new OpcaoDeDialogo(
                        "armas",
                        "Ver Armas",
                        AcaoDeDialogo.AbrirServico,
                        "economia.armeiro"
                    ),
                    new OpcaoDeDialogo("sair", "Até logo", AcaoDeDialogo.Fechar),
                },
                PapelNpc.Armadureiro => new[]
                {
                    new OpcaoDeDialogo(
                        "armaduras",
                        "Ver Armaduras",
                        AcaoDeDialogo.AbrirServico,
                        "economia.armadureiro"
                    ),
                    new OpcaoDeDialogo("sair", "Até logo", AcaoDeDialogo.Fechar),
                },
                PapelNpc.Taverneiro => new[]
                {
                    new OpcaoDeDialogo(
                        "refeicao",
                        "Comprar Refeição",
                        AcaoDeDialogo.AbrirServico,
                        "economia.taverneiro"
                    ),
                    new OpcaoDeDialogo("sair", "Até logo", AcaoDeDialogo.Fechar),
                },
                PapelNpc.Merceeiro => new[]
                {
                    new OpcaoDeDialogo(
                        "suprimentos",
                        "Ver Suprimentos",
                        AcaoDeDialogo.AbrirServico,
                        "economia.merceeiro"
                    ),
                    new OpcaoDeDialogo("sair", "Até logo", AcaoDeDialogo.Fechar),
                },
                PapelNpc.Leiloeiro => new[]
                {
                    new OpcaoDeDialogo(
                        "leilao",
                        "Acessar Leilão",
                        AcaoDeDialogo.AbrirServico,
                        "economia.leilao"
                    ),
                    new OpcaoDeDialogo("sair", "Até logo", AcaoDeDialogo.Fechar),
                },
                PapelNpc.Cavalarico => new[]
                {
                    new OpcaoDeDialogo(
                        "estabulo",
                        "Ver Estábulo",
                        AcaoDeDialogo.AbrirServico,
                        "montarias.estabulo"
                    ),
                    new OpcaoDeDialogo("sair", "Até logo", AcaoDeDialogo.Fechar),
                },
                PapelNpc.CuidadorDePets => new[]
                {
                    new OpcaoDeDialogo(
                        "pets",
                        "Ver Mascotes",
                        AcaoDeDialogo.AbrirServico,
                        "pets.cuidador"
                    ),
                    new OpcaoDeDialogo("sair", "Até logo", AcaoDeDialogo.Fechar),
                },
                PapelNpc.DadorDeMissao => new[]
                {
                    new OpcaoDeDialogo(
                        "missoes",
                        "Ver Quadro de Missões",
                        AcaoDeDialogo.AbrirServico,
                        "missoes.conversa"
                    ),
                    new OpcaoDeDialogo("sair", "Até logo", AcaoDeDialogo.Fechar),
                },
                PapelNpc.Guarda => new[]
                {
                    new OpcaoDeDialogo("sair", "Até logo", AcaoDeDialogo.Fechar),
                },
                PapelNpc.Morador => new[]
                {
                    new OpcaoDeDialogo("sair", "Até logo", AcaoDeDialogo.Fechar),
                },
                PapelNpc.MestreLenhador => new[]
                {
                    new OpcaoDeDialogo(
                        "oficio",
                        "Ofício de Lenhador",
                        AcaoDeDialogo.AbrirServico,
                        "profissoes.lenhador"
                    ),
                    new OpcaoDeDialogo("sair", "Até logo", AcaoDeDialogo.Fechar),
                },
                PapelNpc.MestreMinerador => new[]
                {
                    new OpcaoDeDialogo(
                        "oficio",
                        "Ofício de Minerador",
                        AcaoDeDialogo.AbrirServico,
                        "profissoes.minerador"
                    ),
                    new OpcaoDeDialogo("sair", "Até logo", AcaoDeDialogo.Fechar),
                },
                PapelNpc.MestreCurtidor => new[]
                {
                    new OpcaoDeDialogo(
                        "oficio",
                        "Ofício de Curtidor",
                        AcaoDeDialogo.AbrirServico,
                        "profissoes.curtidor"
                    ),
                    new OpcaoDeDialogo("sair", "Até logo", AcaoDeDialogo.Fechar),
                },
                PapelNpc.MestreFerreiro => new[]
                {
                    new OpcaoDeDialogo(
                        "oficio",
                        "Ofício de Ferreiro",
                        AcaoDeDialogo.AbrirServico,
                        "profissoes.ferreiro"
                    ),
                    new OpcaoDeDialogo("sair", "Até logo", AcaoDeDialogo.Fechar),
                },
                PapelNpc.MestreAlquimista => new[]
                {
                    new OpcaoDeDialogo(
                        "oficio",
                        "Ofício de Alquimista",
                        AcaoDeDialogo.AbrirServico,
                        "profissoes.alquimista"
                    ),
                    new OpcaoDeDialogo("sair", "Até logo", AcaoDeDialogo.Fechar),
                },
                PapelNpc.MestreCozinheiro => new[]
                {
                    new OpcaoDeDialogo(
                        "oficio",
                        "Ofício de Cozinheiro",
                        AcaoDeDialogo.AbrirServico,
                        "profissoes.cozinheiro"
                    ),
                    new OpcaoDeDialogo("sair", "Até logo", AcaoDeDialogo.Fechar),
                },
                _ => new[] { new OpcaoDeDialogo("sair", "Até logo", AcaoDeDialogo.Fechar) },
            };
    }
}
