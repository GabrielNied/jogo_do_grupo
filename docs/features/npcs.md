# NPCs

**Responsável:**
**Status:** em definição
**Faixa de IDs de mensagem:** 2000–2999

## Resumo

Papel dos NPCs no mundo, comerciantes e reputação.

## Definido

- Comerciantes
- **Reputação por facção de NPC da cidade** (decidido por Gabriel em 2026-10-16): por exemplo Forja, Guarda e Mercadores. Sobe com missões e diárias e libera receitas, descontos e aparências. Não tem relação com PvP. **Nunca cai** e não há facções rivais (quiz de definições, 2026-10-16).
- **Decisões do Gabriel em 2026-10-16** (quiz de definições 3): na primeira Brasa Mansa entram **vendedor**, **ferreiro**, **banqueiro**, **leiloeiro** e **dadores de missão** ([#178](https://github.com/GabrielNied/jogo_do_grupo/issues/178)). NPCs de cena do prólogo só com a [#327](https://github.com/GabrielNied/jogo_do_grupo/issues/327).
- **Decisões do Gabriel em 2026-10-18** (elenco completo; detalhe em [Elenco da Brasa Mansa](#elenco-da-brasa-mansa)):
  - **Um NPC para cada serviço da cidade**, com corpo, nome e lugar, como num MMO clássico.
  - **Uma loja por categoria:** armeiro, armadureiro, taverneiro e merceeiro, no lugar do vendedor geral. Todas compram qualquer item, como hoje ([economia](economia.md#definido)).
  - **Estábulo** vende montarias básicas, e o **cuidador de pets** vende pets comuns ([montarias](montarias.md#definido), [pets](pets.md#definido)).
  - **Guardas e moradores** dão vida à cidade sem combate: ninguém luta e ninguém pode ser atacado.

## Em aberto

- [x] Quais tipos de NPC existem? Decidido em 2026-10-16 (quiz 3): lista acima; prólogo adiado.
- [x] Como a reputação é perdida? (ganha por missões e diárias, decidido em 2026-10-16) Decidido em 2026-10-16: não é perdida.

## Regras

### Elenco da Brasa Mansa

Decidido por Gabriel em 2026-10-18. Todo NPC tem corpo, nome sobre a cabeça, ícone do papel (também no minimapa) e um lugar fixo ([#1063](https://github.com/GabrielNied/jogo_do_grupo/issues/1063)), além do visual do papel ([#1064](https://github.com/GabrielNied/jogo_do_grupo/issues/1064)). O lugar de cada serviço na cidade está na [#1065](https://github.com/GabrielNied/jogo_do_grupo/issues/1065). A conversa e o menu do serviço são da [#178](https://github.com/GabrielNied/jogo_do_grupo/issues/178). Nomes, personalidades e falas saem do roteiro ([#1058](https://github.com/GabrielNied/jogo_do_grupo/issues/1058)). NPC não pode ser atacado nem empurrado.

| NPC | O que faz | Dono do serviço | Situação |
|---|---|---|---|
| Banqueiro | Banco | [Itens](itens.md) | serviço pronto (#177) |
| Ferreiro | Forja, fusão e reparo | [Itens](itens.md), [Economia](economia.md) | serviço pronto |
| Leiloeiro | Mercado (leilão) | [Economia](economia.md) | [#176](https://github.com/GabrielNied/jogo_do_grupo/issues/176) |
| Armeiro | Vende armas | [Economia](economia.md) | [#1066](https://github.com/GabrielNied/jogo_do_grupo/issues/1066) |
| Armadureiro | Vende armaduras | [Economia](economia.md) | [#1066](https://github.com/GabrielNied/jogo_do_grupo/issues/1066) |
| Taverneiro | Vende comida e bebida; a cozinha fica na taverna | [Economia](economia.md), [Profissões](profissoes.md) | [#1066](https://github.com/GabrielNied/jogo_do_grupo/issues/1066) |
| Merceeiro | Vende consumíveis e materiais básicos | [Economia](economia.md) | [#1066](https://github.com/GabrielNied/jogo_do_grupo/issues/1066) |
| Cavalariço (estábulo) | Vende montarias básicas | [Montarias](montarias.md) | [#1067](https://github.com/GabrielNied/jogo_do_grupo/issues/1067) |
| Cuidador de pets | Vende pets comuns | [Pets](pets.md) | [#1068](https://github.com/GabrielNied/jogo_do_grupo/issues/1068) |
| Dadores de missão | Dão e recebem missões; quadro de diárias | [Missões](missoes.md) | [#179](https://github.com/GabrielNied/jogo_do_grupo/issues/179) |
| Guardas | Portões e patrulha, sem combate | NPCs | [#1069](https://github.com/GabrielNied/jogo_do_grupo/issues/1069) |
| Moradores | Andam, conversam, falas curtas ao passar | NPCs | [#1069](https://github.com/GabrielNied/jogo_do_grupo/issues/1069) |

**Futuro** (entra quando o serviço existir): intendente de facção (troca [reputação](#definido) por receitas, descontos e aparências), mestres de [profissão](profissoes.md), registrador de [guilda](social.md) e treinador da [árvore](talentos.md).

### Livro de Asterra: NPCs da cidade (proposta do board, 2026-10-11)

Do "Livro de ideias" do [FigJam](../brainstorm/figjam.md) (seções 07 e 11). **Proposta do board, não validada pelo grupo.**

- A cidade (nome proposto: Brasa Mansa) já vem com **vendedores**, cozinha, forja, banco e mercado; os serviços de forja, banco e mercado são de [itens](itens.md) e [economia](economia.md). Aqui entra o NPC que atende cada um.
- No prólogo, os **quatro magos** e o viajante são NPCs **de cena**, para a introdução funcionar sem outros jogadores conectados (veja [missões](missoes.md)).
- Moradores e comerciantes não podem ser atacados por criaturas no MVP: manadas não entram na cidade ([mobs](mobs.md)).
- **Catálogo autoritativo (`CatalogoNpcs`):** fonte canônica de coordenadas, identidade e alcance de interação dos NPCs por área (#1063). Economia e demais módulos alinham suas posições a ele.
- **Presença e rede:** servidor publica `NpcsDaArea` (ID 2001) ao jogador entrar na área ou mediante `SolicitarNpcs` (ID 2002).
- **Corpo e placa:** no client, cada NPC possui cápsula com colisor estático (imune a dano e empurrões), cor temática por papel, placa de nome/papel voltada para a câmera e prompt `[E] Falar` ao alcance.

## Depende de

- `Core` (rede tempo real)
- `Mundo` (área do jogador)

## Expõe

- `CatalogoNpcs`: consulta de posições, papéis e alcance dos NPCs no mundo.
- `CatalogoDialogosNpc`: catálogo de diálogos, falas e opções por papel de NPC.
- `NpcsDaArea`: pacote com a lista de NPCs da área.
- `FalarComNpc`, `DialogoDoNpc`, `ExecutarOpcaoDeDialogo`, `DialogoRecusado`: fluxo autoritativo de diálogo (IDs 2010 a 2013).


