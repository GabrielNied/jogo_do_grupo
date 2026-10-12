# Adaptação das interfaces existentes — tarefa #404

Aplicação do [padrão Lumora](GUIA-DE-ESTILO.md) ao fluxo real do client. O laboratório permanece separado. Sem mudanças nas regras, mensagens de rede, conteúdo do inventário ou ações dos controles.

| Área | Adaptação |
|---|---|
| Abertura e login | Logo aprovada, fundo petróleo, fontes reais; formulário com moldura social e rolagem |
| Seleção e criação de personagem | Moldura social, cartões de seleção turquesa, nomes com Nunito; criação empilha raças e descrição na raiz estreita |
| Menu e opções | Mesmos botões, painel rolável, véu sobre o mundo |
| Personagem, inventário e missões | Moldura comum, listas e dados com tokens, conteúdo acessível por rolagem |
| Grupo | Folhas/calçada, campo e ações compartilhados; nomes e ações quebram em áreas estreitas |
| Chat | Fonte mínima aprovada, canais com papéis de cor, campo de entrada compartilhado; esmaecimento original preservado |
| Combate e inimigos | Barras, nomes, dano e avisos com os tokens; fundo escuro nos valores para leitura sobre barras claras |
| Mapa e minimapa | Borda e marcadores da paleta, norte legível; mapa mantém proporção ao estreitar. Cores do terreno continuam as dos biomas |
| HUD e atalhos | Tipografia, seleção e superfícies compartilhadas; atalhos quebram e omitem nomes na raiz estreita, mantendo as teclas |

## Recursos e manutenção

Os documentos usam a mesma escala, pela resolução (referência 1920 × 1080), e são criados por `EstiloCore.CriarConfiguracaoDoPainel` ([ADR 0035](../../docs/decisoes/0035-fonte-sdf-e-escala-pela-resolucao.md)).

Fontes e arte de runtime são saídas do gerador indicado no guia. O comando de conferência compara seus bytes com as matrizes e verifica tokens, cores avulsas e referências de recursos dos estilos migrados. Não editar as cópias de runtime. Texturas históricas de `Core/UI/Resources/UI` foram preservadas, mas deixaram de ser usadas pelos estilos migrados.

## Validação

Testes automatizados cobrem integração idempotente, preservação dos campos de login e senha protegida, composição rolável, variante social sem dependência de feature, hierarquia de texto aumentada, cores para desenho e geração de fontes para conteúdo dinâmico. Os testes Node verificam sincronização, referências ausentes e contraste das superfícies sólidas.

**Validação visual pendente:** este ambiente não tem Unity. Não há capturas do jogo adaptado nem confirmação de layout renderizado. Registrar a conferência da [ficha de revisão](../laboratorio-unity/VALIDACAO.md) nas resoluções e tamanhos de texto exigidos pelo guia. Incluir estados de carregamento, erro, desabilitado, nomes longos, lista vazia, navegação por teclado, rolagem de opções/criação e retorno ao mundo sem bloqueio de cliques. A leitura aumentada continua exposta pelo helper do guia; esta tarefa não adiciona uma preferência persistente de acessibilidade.

As três imagens novas em `Resources/Lumora/Arte` precisam dos metadados gerados pelo Unity. Conferir transparência, nitidez da logo, recorte e margens reais da moldura após importação. O esmaecimento original do chat é uma exceção à leitura em contraste pleno; verificar o estado ativo em todas as resoluções. Revisão do Core e dos responsáveis pelas funcionalidades afetadas é necessária.
