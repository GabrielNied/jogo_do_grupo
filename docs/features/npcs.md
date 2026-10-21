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

- [x] Quais tipos de NPC existem? Decidido em 2026-10-16 (quiz 3): lista acima.
- [x] Como a reputação é perdida? (ganha por missões e diárias, decidido em 2026-10-16) Decidido em 2026-10-16: não é perdida.

## Regras

### Elenco da Brasa Mansa

Decidido por Gabriel em 2026-10-18. Todo NPC tem corpo, nome sobre a cabeça, ícone do papel (também no minimapa) e um lugar fixo ([#1063](https://github.com/GabrielNied/jogo_do_grupo/issues/1063)), além do visual do papel ([#1064](https://github.com/GabrielNied/jogo_do_grupo/issues/1064)). O lugar de cada serviço na cidade está na [#1065](https://github.com/GabrielNied/jogo_do_grupo/issues/1065). A conversa e o menu do serviço são da [#178](https://github.com/GabrielNied/jogo_do_grupo/issues/178). Nomes, personalidades e falas saem do roteiro ([#1058](https://github.com/GabrielNied/jogo_do_grupo/issues/1058)). NPC não pode ser atacado nem empurrado.

| NPC | Nome Canônico | O que faz | Dono do serviço | Situação |
|---|---|---|---|---|
| Banqueiro | Cassian | Banco | [Itens](itens.md) | serviço pronto (#177) |
| Ferreiro | Brom | Forja, fusão e reparo | [Itens](itens.md), [Economia](economia.md) | serviço pronto |
| Leiloeiro | — | Mercado (leilão) | [Economia](economia.md) | [#176](https://github.com/GabrielNied/jogo_do_grupo/issues/176) |
| Armeiro | Gareth | Vende armas | [Economia](economia.md) | [#1066](https://github.com/GabrielNied/jogo_do_grupo/issues/1066) |
| Armadureiro | Bruna | Vende armaduras | [Economia](economia.md) | [#1066](https://github.com/GabrielNied/jogo_do_grupo/issues/1066) |
| Taverneiro | Barnabé | Vende comida e bebida; a cozinha fica na taverna | [Economia](economia.md), [Profissões](profissoes.md) | [#1066](https://github.com/GabrielNied/jogo_do_grupo/issues/1066) |
| Merceeiro | Tomas | Vende consumíveis e materiais básicos | [Economia](economia.md) | [#1066](https://github.com/GabrielNied/jogo_do_grupo/issues/1066) |
| Cavalariço (estábulo) | Roderic | Vende montarias básicas | [Montarias](montarias.md) | [#1067](https://github.com/GabrielNied/jogo_do_grupo/issues/1067) |
| Cuidador de pets | Mirna | Vende pets comuns | [Pets](pets.md) | [#1068](https://github.com/GabrielNied/jogo_do_grupo/issues/1068) |
| Dadores de missão | Capitã Valéria e Alden | Dão e recebem missões; roteiro em [missões](missoes.md#capítulo-1-a-brasa-mansa-e-a-serra-do-vidro-frio-roteiro-completo-issue-1058) | [Missões](missoes.md) | [#179](https://github.com/GabrielNied/jogo_do_grupo/issues/179), [#1058](https://github.com/GabrielNied/jogo_do_grupo/issues/1058) |
| Guardas | Guarnição | Portões e patrulha, sem combate | NPCs | pronto ([#1069](https://github.com/GabrielNied/jogo_do_grupo/issues/1069), [vida na cidade](#vida-na-cidade)) |
| Moradores | Cidadãos | Andam, conversam, falas curtas ao passar | NPCs | pronto ([#1069](https://github.com/GabrielNied/jogo_do_grupo/issues/1069), [vida na cidade](#vida-na-cidade)) |

**Futuro** (entra quando o serviço existir): intendente de facção (troca [reputação](#definido) por receitas, descontos e aparências), mestres de [profissão](profissoes.md), registrador de [guilda](social.md) e treinador da [árvore](talentos.md).

### Vida na cidade

Decidido por quem implementou a [#1069](https://github.com/GabrielNied/jogo_do_grupo/issues/1069), seguindo o definido em 2026-10-18 (sem combate, falas curtas em balão).

- **Quem:** 12 habitantes ambiente em `PopulacaoDaCidade` (shared), fora do `CatalogoNpcs`: 2 guardas parados nos portões (além da Capitã Valéria e do Guarda Kael, que continuam no catálogo), 4 patrulheiros (anel nos dois sentidos e um trecho de cada estrada do portão) e 6 moradores (dois pares que passeiam pela praça e param para conversar, e dois que vão e voltam pela rua em anel).
- **Movimento só no client, sem rede:** a posição é função pura do relógio UTC e da rota (`RotaDeNpc.Amostrar`), então todos veem o mesmo lugar, o servidor não gasta CPU nem banda e o mapa não precisa de mudança. Os NPCs de serviço do catálogo ficam fixos e autoritativos (o servidor valida o alcance do diálogo contra o ponto do catálogo). Quem passa a mais de 90 m não desenha nem anima o habitante.
- **Rotas em dados:** pontos `(x, z)`, pausa, olhar e velocidade (1,2 m/s moradores, 1,5 m/s guardas), seguindo os corredores livres do [Mundo](mundo.md) (estradas em cruz e anel em ±22) e a praça de 24 × 24 m sem cruzar a fonte. Mudar a cidade é mudar o dado, sem editar cena.
- **Conversa:** o par tem rotas espelhadas de mesmo ciclo, para parar junto a 2,4 m um do outro, olhando-se. O balão alterna entre os dois a cada 4 s.
- **Falas:** até 60 caracteres. Quem chega a menos de 8 m vê um balão de 4 s sobre o NPC, no máximo um a cada 25 s por NPC. Os textos são provisórios até o roteiro ([#1058](https://github.com/GabrielNied/jogo_do_grupo/issues/1058)) assumir.
- **Sem alvo:** sem colisor, sem vida e sem dano: não aparecem para o combate nem empurram o jogador.
- **Ajuste:** velocidades, raio e tempo do balão em `ConfigPopulacaoDaCidade`.

### Visual dos NPCs

Decidido por quem implementou a [#1064](https://github.com/GabrielNied/jogo_do_grupo/issues/1064).

- **Base única:** o Monk do Quaternius (CC0, [licenças](../arte/licencas.md)), de 1,8 m, com um prefab por papel em `Resources/Npcs/Npc_<Papel>`, gerado por `GeradorDeVisualDeNpcs` (menu Jogo do Grupo/Npcs, ou `npm run unity:executar`). Papel sem prefab (estalajadeiro) cai na cápsula colorida.
- **Reconhecível de longe:** túnica ou tabardo na cor do papel mais um acessório: ferreiro (avental de couro e martelo), guarda (elmo, peitoral e lança), armeiro (tabardo vermelho e espada), armadureiro (peitoral de aço e martelinho), taverneiro (avental e gorro brancos, panela), merceeiro e vendedor (chapéu de aba e bolsa), banqueiro (cartola verde e livro), leiloeiro (cartola azul e martelete), cavalariço (chapéu de palha e forcado), cuidador de pets (lenço verde e cesto), dador de missão (capa roxa e selo) e mago de cena do prólogo (chapéu pontudo, capa e cajado com orbe).
- **Animação:** parado, andar (guardas e moradores), falar (acende quando o jogador está ao alcance ou o balão aparece) e trabalhar (martelar para ferreiro e armadureiro, mexer para taverneiro). Falar e trabalhar são o Idle do pacote com os braços trocados por script, sem animação nova de terceiros.
- **Variação:** todo NPC tem altura de 93% a 107% pela semente; moradores sorteiam também cabelo (3 estilos ou nenhum), barba (2 ou nenhuma), cor da túnica e tom do corpo (`AparenciaDeNpc`). A mesma semente dá o mesmo rosto em qualquer client.
- **Limite conhecido:** cabelo e rosto são formas simples sobre o Monk; modelos próprios por papel ficam para quando a arte do grupo chegar.

### Prólogo: NPCs de cena

Decidido por quem implementou a [#327](https://github.com/GabrielNied/jogo_do_grupo/issues/327); a regra do jogador está em [missões](missoes.md#começo-do-jogo).

- **Onde mora:** os quatro magos são NPCs de cena e o prólogo é só encenação do client, então ficam em `Features/Npcs` (roteiro e linha do tempo em `PrologoDoSol`, no shared; palco e janela no client). Sem catálogo, sem mensagem e sem estado no servidor.
- **Oferta:** ao entrar na cidade, 2 s depois, abre uma janela "O sol que nasceu na estrada" com **Assistir** e **Pular** (Esc também pula). Vale **uma vez por personagem neste aparelho** (`PlayerPrefs`, chave `npcs.prologo.visto.<id>`): não gasta banco nem rede, e quem trocar de aparelho vê a oferta de novo, sem custo.
- **A cena:** 44 s, tela inteira. Uma clareira montada longe do mundo, vista por uma câmera própria: quatro magos (Veyra, Ilso, Oren e Tavi) sustentam uma esfera que cresce, uma manada de sombras vem pela estrada, o clarão cobre a tela e a manada se desfaz. O viajante fecha mandando ir à Brasa Mansa e falar com a Capitã Valéria. Textos do livro de ideias ([missões](missoes.md#livro-de-asterra-chegada-ao-mundo-proposta-do-board-2026-10-11)).
- **Sem risco:** o personagem fica parado (o controle é desligado e devolvido ao fechar), nada dá loot e nada o mata. Quem pula ou assiste volta ao mesmo lugar, com a principal 1 como estava: o prólogo não entrega missão.
- **Ajuste:** tempos e textos em `PrologoDoSol`; tamanhos e cores do palco em `PalcoDoPrologo`.

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
- Conversa aceita (NPC existe, mesma área e dentro do raio, pela posição do servidor) publica o evento de servidor `JogadorFalouComNpc` (`Contratos/Npcs`); é por ele que as [missões](missoes.md) contam o objetivo **Falar**. Os canais da cidade (`cidade:2`...) têm os mesmos NPCs da cidade.


