# Missões

**Responsável:**
**Status:** em definição
**Faixa de IDs de mensagem:** 10000–10999

## Resumo

História principal, missões secundárias e diárias.

## Definido

- Principal, secundárias e diárias
- O loop principal é leveling, loot e eventos, com foco em bosses, dungeons e raids (D5 da [validação de 2026-10-10](../brainstorm/validacao.md)); missões entram como parte desse loop
- **Decisões do Gabriel em 2026-10-16** (quiz de lacunas de MMO):
  - **Tipos que entram primeiro:** história principal (ordem fixa **prólogo → primeira hora → secundárias**, veja abaixo), secundárias por região (dadas por NPCs da cidade e de pontos do mapa: matar, coletar, entregar) e diárias e semanais (renovam no reset e dão coroas e [reputação](npcs.md#definido)).
  - **Em grupo:** o objetivo conta para todos do grupo que estão por perto, têm a missão e participaram ([fatia, seção 5](fatia-jogavel.md#5-loot-no-chão)). O líder pode compartilhar a missão com o grupo.
- **Decisões do Gabriel em 2026-10-16** (quiz de definições 3):
  - **Prólogo (#327) e primeira hora (#328):** adiados até o [marco atual](fatia-jogavel.md#foco-atual) fechar; quando entrarem, a ordem é prólogo → primeira hora → secundárias.
  - **XP:** além de matar mobs, **missões dão XP relevante** assim que o motor de missões ([#179](https://github.com/GabrielNied/jogo_do_grupo/issues/179)) existir. Coleta e craft **não** dão XP ([profissões](profissoes.md#definido)).
- **História igual para todas as raças** (decidido por Gabriel em 2026-10-16): raça é só visual ([personagem](personagem.md#definido)), então não há início, fala nem missão por raça.
- **Decisões do Gabriel em 2026-10-18** (fluxo de missões; detalhe em [Fluxo de missões](#fluxo-de-missões)):
  - **Desenhar já, codar depois:** o fluxo fica fechado aqui, e a implementação ([#179](https://github.com/GabrielNied/jogo_do_grupo/issues/179), [#327](https://github.com/GabrielNied/jogo_do_grupo/issues/327), [#328](https://github.com/GabrielNied/jogo_do_grupo/issues/328)) continua depois do [marco atual](fatia-jogavel.md#foco-atual).
  - **O personagem nasce com a primeira missão principal aceita**, sem precisar achar NPC. O prólogo (#327) passa a ser **opcional e pulável**, e não é mais ele que entrega a missão.
  - **Secundárias liberadas por etapa da principal e por nível:** cada etapa da principal destrava as secundárias da região dela, e cada secundária pode pedir um nível mínimo.
  - **Recompensa:** toda missão dá **XP e coroas**; algumas dão também um **item à escolha** entre 2 e 4 opções. Reputação vem das diárias e das secundárias de facção.

## Em aberto

- [x] Missões em grupo? Decidido em 2026-10-16 (veja Definido).
- [x] Como a história se conecta às raças? Decidido em 2026-10-16: não se conecta; raça é só visual.
- [x] O prólogo e a primeira hora do livro (proposta do board, 2026-10-11) viram o tutorial do jogo? ~~Respondido em 2026-10-16 (Definido): a história principal começa pelo prólogo e pela primeira hora do livro.~~ Substituído em 2026-10-16 (quiz 3): entram **depois do marco atual**, nessa ordem, como história principal.

## Regras

### Fluxo de missões

Decidido em 2026-10-18 (veja [Definido](#definido)). É o fluxo clássico de MMO: a história principal puxa o jogador pelo mapa e, a cada etapa, abre as secundárias da região. Os números ficam em `ConfigMissoes` e podem ser calibrados.

**Como as missões se encadeiam**

- Cada missão é um **dado** (não é código): id, tipo, região, NPC que dá, NPC que recebe, **pré-requisitos**, objetivos e recompensas.
- **Pré-requisitos:** as missões que precisam estar concluídas e o **nível mínimo**. Uma missão com os pré-requisitos cumpridos fica **disponível**.
- **Principal:** uma linha só, dividida em **capítulos**. Concluir uma etapa entrega a próxima **já aceita**, e o jogador nunca fica sem a próxima missão principal. O capítulo 1 está em [Capítulo 1: a cidade](#capítulo-1-a-cidade) (#328).
- **Secundárias:** cada etapa da principal abre um conjunto de secundárias na região dela, e cada uma pode pedir um nível mínimo. Uma secundária também pode abrir outra (pequenas cadeias dentro da região).
- **Diárias e semanais:** pedem só nível e a região já aberta pela principal. Renovam no reset (horário de Brasília), que é o mesmo das [raids](instancias.md#definido).

**Começo do jogo**

- O personagem novo entra no mundo com a missão principal 1 **já aceita** e no rastreador: ir à Brasa Mansa e falar com o primeiro NPC.
- O prólogo (#327) é **opcional e pulável**. Quem joga e quem pula termina no mesmo ponto, com a mesma missão.

**Estados de uma missão (por personagem)**

| Estado | Como o jogador vê |
|---|---|
| Bloqueada | Não aparece. Faltam pré-requisitos ou nível. |
| Disponível | **!** amarelo sobre o NPC que dá a missão, também no minimapa. |
| Em andamento | Aparece no registro (L) e, se estiver acompanhada, no rastreador. |
| Pronta para entregar | **?** amarelo sobre o NPC que recebe. |
| Concluída | Some do registro. Diária e semanal voltam a ficar disponíveis no reset. |

- Uma missão disponível com **nível recomendado** muito acima do personagem mostra o **!** vermelho. Muito abaixo, o **!** fica cinza e a missão dá menos XP.
- **Abandonar:** secundária, diária e semanal podem ser abandonadas e voltam a ficar disponíveis, com o progresso zerado. A **principal não pode ser abandonada**.
- **Limite:** até 25 missões em andamento, sem contar a principal.

**Objetivos (v1)**

- **Matar** N inimigos de um tipo, numa área ou em qualquer lugar. Conta com a regra de [quem participou](fatia-jogavel.md#5-loot-no-chão).
- **Coletar** N itens: drop de missão (só cai para quem tem a missão e não ocupa o inventário) ou ponto de coleta.
- **Falar** com um NPC.
- **Ir** a um lugar (área marcada no mapa).
- **Concluir uma dungeon** (ou derrotar o chefe dela).
- Uma missão pode ter vários objetivos ao mesmo tempo ("mate 6 javalis **e** colete 4 presas").

**Recompensas**

- Toda missão dá **XP e coroas**, em valor fixo definido no dado da missão. A principal e as secundárias dão o maior XP. A coroa entra na [economia](economia.md#definido) como fonte de valor fixo.
- **Item à escolha:** algumas missões oferecem de 2 a 4 itens, e o jogador escolhe um ao entregar. O servidor confere se a escolha está na lista e cria o item.
- **Reputação** de facção ([NPCs](npcs.md#definido)) vem das diárias e das secundárias da facção.
- Concluir uma missão pode também **liberar** algo: a próxima missão, um NPC ou uma receita.

**Interface**

- **Registro (L):** lista por tipo (principal no topo), com objetivos, recompensas e os botões de acompanhar, abandonar e compartilhar.
- **Rastreador no HUD:** a principal e até 4 missões acompanhadas, com o progresso dos objetivos.
- **Mapa e minimapa:** **!** e **?** dos NPCs e a área do objetivo das missões acompanhadas ([mundo](mundo.md)).
- **Conversa com o NPC:** texto da missão, objetivos, recompensas e os botões aceitar ou entregar. Usa o diálogo de NPC ([#178](https://github.com/GabrielNied/jogo_do_grupo/issues/178)).

**Servidor autoritativo**

- O servidor decide os pré-requisitos, o progresso, a entrega, a escolha do item e o reset. O client só envia intenções (aceitar, abandonar, entregar com a escolha, compartilhar, acompanhar) e mostra o resultado.
- A entrega é **uma vez só**: entregar duas vezes ao mesmo tempo não pode dar a recompensa em dobro (teste de concorrência).

### Capítulo 1: a cidade

Decidido por Gabriel em 2026-10-18. É o primeiro capítulo da principal e funciona como o tutorial do jogo. Vem **depois das implementações pendentes** (o [marco atual](fatia-jogavel.md#foco-atual) e os serviços dos NPCs). As falas, os nomes e a ordem fina saem do roteiro ([#1058](https://github.com/GabrielNied/jogo_do_grupo/issues/1058)).

1. **Explorar a cidade:** a principal leva o jogador pela Brasa Mansa, apresenta o lugar e um **NPC por vez**. Cada NPC explica o que faz, e o jogador usa o serviço uma vez: os do [elenco da Brasa Mansa](npcs.md#elenco-da-brasa-mansa) (lojas, ferreiro, banqueiro, leiloeiro, estábulo, cuidador de pets, taverna com a cozinha e dadores de missão). Só entra o NPC cujo serviço já existe no jogo.
2. **Ensinar o básico:** junto com os NPCs, as etapas ensinam o combate (leve, pesado e esquiva), o inventário e o equipamento, a skill da arma, subir de nível e gastar pontos na [árvore](talentos.md).
3. **Mobs perto da cidade:** sair pela primeira vez e enfrentar os inimigos fracos da área em volta da Brasa Mansa (matar e coletar).
4. **Chefe:** fechar o capítulo derrotando um **chefe perto da cidade** ([#1059](https://github.com/GabrielNied/jogo_do_grupo/issues/1059)). A entrega dá o item à escolha e libera as secundárias da região ([#1060](https://github.com/GabrielNied/jogo_do_grupo/issues/1060)).

A lista do Livro abaixo serve de inspiração para as etapas, mas não é obrigatória.

### Livro de Asterra: chegada ao mundo (proposta do board, 2026-10-11)

Do "Livro de ideias" do [FigJam](../brainstorm/figjam.md) (seção 11). **Proposta do board, não validada pelo grupo**; o nome Asterra e o título do jogo continuam a escolher.

- **Prólogo "o sol que nasceu na estrada":** cena jogável curta; o personagem chega a uma parada perto da cidade, vê **NPCs** sustentarem uma esfera de luz coletiva e eliminarem uma manada (só encenação, **não** é skill do jogador). Funciona sem outros jogadores conectados. Pode ser **pulado**; não dá loot nem mata o recém-chegado. Termina com o controle devolvido, indo à cidade.
- **Primeira hora (guia de missões iniciais):** receber arma e aprender golpes leve e pesado; comer na cozinha e achar banco e teleporte; ir ao primeiro ponto de coleta e achar os primeiros javalis; obter uma peça com skill, abrir Preparação e usar a skill num grupo; subir de nível, distribuir pontos e infundir uma peça na forja; ajudar um aliado caído (a primeira expedição não perde nada por morte).
- Depende de [personagem](personagem.md), [skills](skills.md), [itens](itens.md), [combate](combate.md) e [npcs](npcs.md); as missões só encadeiam.

## Depende de

## Expõe
