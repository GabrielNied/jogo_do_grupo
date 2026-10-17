# 0047. Contexto local de Editor conecta os controles reais no Playground

**Data:** 2026-10-17
**Status:** aceita

## Contexto

A v2 precisa validar input, movimento e câmera sem servidor. Os componentes reais funcionam isoladamente, mas AcoesJogador e os métodos Configurar são internos a Core. SessaoNoMundo também inicializa menus/janelas e bloqueia input sem conexão; reutilizá-la iniciaria um contexto alheio à ferramenta. Copiar os algoritmos ou abrir a API de Core aumentaria a superfície da mudança.

## Decisão

- Um adaptador somente Editor em Personagem valida e invoca essas assinaturas internas por reflexão ao criar o contexto, mantendo os componentes reais responsáveis por cada frame. Não alterar Core, iniciar sessão ou acrescentar InternalsVisibleTo.
- Reutilizar o mesmo catálogo e Spawn/Reset em Play; componentes e câmera de gameplay existem apenas na instância temporária. O chão recebe collider local pelo Unity.
- Preservar a inspeção em Edit Mode e dispensar animações de locomoção até haver clips Humanoid adequados e com proveniência estabelecida.
- Validar o encadeamento completo com Input System e frames reais no Editor aberto via MCP.

## Consequências

### Extensão local na TASK 06

O acesso direto abre a cena/ferramenta com confirmação normal de salvamento. O bootstrap global permanece intacto: no Playground, o adaptador de Editor troca a tela de abertura por uma tela vazia usando a API pública de Navegacao, fechando a anterior e impedindo sua continuação assíncrona de navegar. A interface normal fica inativa somente nesse contexto. Isso não impede a inicialização/requisição de saúde inicial do bootstrap; evita a sobreposição e não exige rede para movimentar.

Um playModeStartScene previamente definido é substituído apenas durante um Play explícito na cena do Playground e restaurado no Stop; não muda Build Settings nem cenas normais. O controller de locomoção e Apply Root Motion OFF são atribuídos somente à instância temporária com o Avatar do Human Male, preservando o prefab original. Não há novos contratos ou alterações em Core.

O adendo visual mantém a aprovação do retarget separada dos testes técnicos. Avatars de calibração candidatos existem como assets próprios do Playground; o Avatar/FBX original não é sobrescrito. Calibração de origem e amplitude de Shoulder são reproduzíveis por ferramenta Editor, com pendências visuais documentadas na [operação](../arte/character-playground.md#locomotion-animation-set-v1-task-06).

Sem diferenças funcionais no jogo principal e sem lógica de movimento duplicada. A reflexão depende de detalhes internos: se Core mudar, a validação de assinatura falha explicitamente e o adaptador deverá ser revisto. Não é um contrato de extensão para builds; fica restrito à ferramenta de desenvolvimento. Operação e testes: [Character Playground](../arte/character-playground.md).
