# Validação de ideias

Pauta para o grupo validar as ideias do [brainstorm](figjam.md). Cada item tem a pergunta, o que o board diz, as opções e o impacto de cada uma.

As **sugestões** marcadas com 💡 são só um ponto de partida para a conversa, escrito com ajuda de IA. **Quem decide é o grupo.**

## Como usar

1. Discutam um item por vez, começando pelas [decisões de base](#decisões-de-base), que bloqueiam todo o resto.
2. Decidido? Troque o status do item para **✅ decidido** e escreva a decisão em uma linha logo abaixo da pergunta. Não apague as opções nem o histórico; elas explicam por que a escolha foi feita.
3. Leve a decisão para o `docs/features/<x>.md` das funcionalidades afetadas ("Definido" ou "Em aberto"). Se for técnica, crie um ADR.
4. Ideia rejeitada também fica registrada: status **❌ descartada**, com o motivo.

Status possíveis: 🟡 aberta · ✅ decidida · ❌ descartada · ⏸️ adiada (para depois da fatia jogável).

> **2026-10-10:** o grupo decidiu as cinco decisões de base (D1 a D5). As decisões de sistemas (S1 a S9) serão tomadas conforme o jogo for sendo construído, usando [jogos de referência](../referencias.md) como base e modificando a partir deles.
>
> **2026-10-11:** a D1 foi revisada. ~~Sessões de grupo pequeno (opção B)~~ substituídas pelo mundo persistente (opção A).

## O que o board propõe, em resumo

Juntando as ideias, aparece um jogo **medieval, de ação em terceira pessoa e com foco em loot**:

- **Sem classes:** a raça dá uma skill base e as outras skills vêm dos **itens**, que são temáticos da região onde caem. A **fusão** combina itens e skills, criando builds quase infinitas.
- **Todo item tem utilidade:** um item de nível baixo serve de receita em qualquer nível (estilo Runescape). Muito drop, inventário grande, loot rápido com uma tecla.
- **Combate contra hordas:** grupos grandes de inimigos que não escalam de nível, conhecimento acumulado sobre cada inimigo (bestiário) e talvez captura de pets.
- **PvPvE**, com eventos globais (fendas, meteoros, invasões de cidade), construção e cidades criadas pelos jogadores.
- **Identidade audiovisual forte:** skills e efeitos enormes, som que se ouve de longe, noite escura de verdade, skills que iluminam.

Essa visão combina bem com várias decisões que já estão nas docs: economia movida pelos jogadores, fusão, itens negociáveis. Mas **conflita** com outras (classes, skills por classe, mundo aberto MMO). Os conflitos estão nas decisões de base abaixo.

Jogos de referência que parecem próximos dessa visão, para o grupo comparar: **Albion Online** (sem classe, "você é o que veste", economia de jogadores, zonas PvPvE), **Path of Exile** (skills vindas de itens e builds por combinação), **Valheim** (sobrevivência e construção em grupo pequeno). A lista completa, por sistema, está em [referencias.md](../referencias.md).

## Decisões de base

Estas bloqueiam a definição da fatia jogável e da arquitetura do servidor.

### D1. Formato: mundo persistente (MMO) ou sessões com grupo limitado?

**Status:** ✅ decidida em 2026-10-10 · revisada em 2026-10-11

**Decisão:** ~~por hora, **sessões de grupo pequeno** (opção B). O objetivo é jogar em poucas pessoas.~~ Substituído em 2026-10-11 por: **mundo persistente** (opção A). Um mundo único e contínuo, compartilhado por quem está nele. A cidade faz parte desse mundo.

**O que o board diz:**
- Fluxo de ideias: "Mapa (mundo aberto)", raids, guerra de guildas, mercado entre jogadores (tudo de MMO).
- Estilo de jogo: **"Lobby com grupo limitado"**, "Players criam cidades", "PVPVE".

**Opções:**

| Opção | Como é | Prós | Contras e impacto técnico |
|---|---|---|---|
| A. MMO persistente | Um mundo único e contínuo, com centenas de jogadores ao mesmo tempo | Fiel à ideia original de MMORPG; economia e cidades de jogadores fazem mais sentido | É o formato mais difícil de construir: servidor que aguenta muitos jogadores, sincronização por área de interesse, persistência contínua, divisão do mundo entre servidores |
| B. Sessões (lobby) | Grupos pequenos entram em partidas ou mapas instanciados | Muito mais simples de construir e testar; combate de ação fica mais fácil de sincronizar | Perde o mundo compartilhado; economia e cidades de jogadores ficam limitadas |
| C. Híbrido | Áreas sociais persistentes (cidades, mercado) e conteúdo de combate em instâncias com grupo limitado | Mantém o social e a economia, e o combate roda em instâncias pequenas | Dois tipos de servidor; mais complexo que B, bem menos que A |

💡 **Sugestão:** C (híbrido). Ele aproveita o "lobby com grupo limitado" para o combate, mantém economia, mercado e guildas e reduz bastante o risco técnico para um grupo pequeno. Dá para evoluir para áreas abertas maiores depois.

**Afeta:** Mundo, Instâncias, Social, Economia, PvP, arquitetura do servidor (ADR).

### D2. Classes ou sem classes?

**Status:** ✅ decidida em 2026-10-10

**Decisão:** **sem classes** (opção B). A combinação de itens decide o estilo de jogabilidade.

**O que o board diz:**
- Fluxo de ideias: "Classes: quais são e como se diferenciam?" e "Skills (magias): magias por classe".
- Personagens: **"Não ter Classe definida"** e "Raça: personagens começam com raças base que vão dar uma base de skill única".

**Opções:**

| Opção | Prós | Contras |
|---|---|---|
| A. Classes fixas | Mais simples de balancear; papéis claros no grupo (tanque, cura, dano) | Contradiz a ideia de builds livres e de skills vindas de itens |
| B. Sem classe: a build vem de raça + itens | Liberdade total; casa com "itens possuem skills" e com a fusão | Balanceamento bem mais difícil; o grupo precisa garantir papéis (tanque, cura) de outra forma |
| C. Sem classe, com "arquétipos" leves | Itens definem as skills, mas pertencem a famílias (ex.: armas pesadas, arcano, sagrado) que dão bônus ao combinar | Meio-termo; exige definir as famílias |

💡 **Sugestão:** B ou C. Tudo o que o board propõe para itens, skills e fusão pressupõe sem classe, e esse pode ser o diferencial do jogo (a seção "O que é novo?" do board ainda está vazia).

**Afeta:** Personagem, Skills, Talentos, Itens, Combate. Se for sem classe, a doc [personagem.md](../features/personagem.md) e o "Skills por classe" do mapa precisam ser revistos.

### D3. De onde vêm as skills?

**Status:** ✅ decidida em 2026-10-10

**Decisão:** **as skills estão nos itens, sorteadas aleatoriamente.** As perguntas abaixo continuam em aberto e foram para [skills.md](../features/skills.md).

**Complemento (2026-10-10):** a skill do item é a magia que o jogador usa, e pode ser **ativa** (ativável), **passiva** ou **buff**. O jogo começa **sem árvore de talentos**; o foco é o sistema de itens.

**O que o board diz:**
- Fluxo de ideias: "Skills (magias): magias por classe, custo e recarga".
- Itens: **"Itens possuem skills"**. As skills vêm dos itens, são temáticas da área onde o item cai e podem ser combinadas pelo sistema de fusão.
- Raça: dá uma skill base única.

**Perguntas para fechar:**
- A skill pertence ao **item equipado** (trocou o item, perdeu a skill) ou o jogador **aprende** a skill do item e fica com ela?
- Quantas skills ativas ao mesmo tempo (barra de skills)?
- A fusão combina **itens**, **skills** ou os dois? Fundir dois itens mistura as skills deles?
- Onde ficam os "Talentos" (árvore de pontos) nesse modelo? Continuam existindo?

💡 **Sugestão:** skill presa ao item equipado, com poucos espaços ativos (ex.: 6). É mais simples de implementar e de balancear, e dá valor real a cada drop, o que combina com o estilo looter.

**Afeta:** Skills, Itens, Talentos, Economia (itens com skills raras valem mais no mercado).

### D4. Estilo de combate: ação ou alvo selecionado?

**Status:** ✅ decidida em 2026-10-10

**Decisão:** **ação** (opção A), no estilo de New World e jogos parecidos.

**Complemento (2026-10-10):** o grupo gosta do **equilíbrio do New World**: ação, mas **não frenética e sem spam de skills**. A troca de arma ficou para pensar (S10).

**O que o board diz:**
- Estilo de jogo: **"Action 3 pessoa"**.
- Inimigos: hordas grandes, "juntar tudo e matar 20 de uma vez".
- Skills: longo alcance, efeitos enormes.

**Opções:**

| Opção | Como é | Impacto técnico |
|---|---|---|
| A. Ação | Mira livre, esquiva, skills que acertam onde você mira | Servidor autoritativo precisa compensar a latência (lag compensation) para acertos justos; mais trabalho de rede |
| B. Alvo selecionado (tab-target) | Escolhe o alvo e a skill acerta | Bem mais simples de sincronizar; menos "ação" |
| C. Ação com mira assistida | Skills em área e cones com mira leve, sem precisão de tiro | Meio-termo: sensação de ação contra hordas sem exigir precisão de rede |

💡 **Sugestão:** C. Contra hordas, skills em área e cones dão a sensação de ação e toleram melhor a latência que tiros precisos. Também casa com "efeitos giga".

**Afeta:** Combate, Skills, Mobs, arquitetura de rede (ADR).

### D5. Qual é o loop principal do jogo?

**Status:** ✅ decidida em 2026-10-10

**Decisão:** **leveling, loot e eventos, com foco em derrotar bosses cada vez mais fortes, em dungeons e raids.**

**Complemento (2026-10-10):** **progressão infinita no estilo Diablo**, com dungeons de dificuldade que escala sem fim (como o Mythic+ do WoW), raids em etapas e bosses com ataques telegrafados (avisam antes de bater, para o jogador esquivar).

**O que o board diz:**
- Estilo de jogo: **"Loop de jogo: Missões? Entregas? Locais seguros e perigosos?"**
- Itens e inimigos: muito loot, hordas, fusão.

**Proposta para discutir** (montada a partir das ideias do board):

```
Sair da área segura → enfrentar hordas numa região perigosa → coletar muito loot
   → voltar e fundir, craftar ou vender → ficar mais forte → ir para regiões mais perigosas
```

**Perguntas para fechar:** missões e entregas são o motor do loop ou um complemento? Morrer numa área perigosa faz perder o loot (risco e recompensa, como no PvPvE)?

**Afeta:** Missões, Mundo, Itens, Economia, PvP.

### D6. Papéis e tamanho dos grupos

**Status:** ✅ decidida em 2026-10-10 (pedido do grupo, fora do board)

**Decisão:**
- Papéis **tank, healer e DPS**, com buffs de cada papel que complementam o time, no estilo das dungeons e raids de World of Warcraft. Sem classes (D2): o papel vem da combinação de itens.
- **Dungeons de 5 jogadores e raids de 10.**
- **Farm de mapa em grupo** também faz parte do loop (D5).

**Afeta:** Combate, Skills, Itens, Dungeons e raids, Social, Mundo.

## Decisões de sistemas

Podem ser decididas depois das de base. Várias ficam para depois da fatia jogável (⏸️).

### S1. Loot e inventário

**Status:** 🟡 aberta

**O que o board diz:** estilo looter com muito drop; inventário grande, sem muita limitação de tamanho ou peso; "clicar F e juntar tudo próximo, segurar F para loot seletivo, configurar loot rápido por tipo e nível".

**Perguntas:** inventário ilimitado ou grande com expansão? O loot é individual (cada jogador vê o seu) ou disputado no grupo? O que impede o mercado de ser inundado por itens (saídas de recursos)?

**Afeta:** Itens, Economia, Social (divisão de loot na party).

### S2. Fusão: todo item útil

**Status:** 🟡 aberta

**O que o board diz:** progressão de itens dependente de consumir outros itens ("estilo gacha"); itens de nível baixo úteis em todos os níveis, como receitas de poções e de equipamentos de nível alto (estilo Runescape).

Isso **complementa** o que já está definido em [itens.md](../features/itens.md) (limite por raridade, item sacrificado destruído, retorno decrescente, custo crescente). A ideia de receitas entre níveis liga Itens a Profissões.

**Perguntas:** "estilo gacha" quer dizer resultado aleatório na fusão? O jogador sabe o resultado antes de fundir?

**Afeta:** Itens, Profissões, Economia.

### S3. Inimigos: nível fixo, hordas e conhecimento

**Status:** 🟡 aberta

**O que o board diz:** inimigos não escalam (o nível é fixo por região); aparecem em grupos grandes, com agro longo como no RYL (Risk Your Life: Path of the Emperor, MMORPG antigo); conhecimento acumulado ("quanto mais mata, mais fácil fica", pontos fracos); coleção de conhecimentos que pode liberar captura de pets.

**Perguntas:** o conhecimento é por jogador ou da guilda? Quantos inimigos na tela ao mesmo tempo? (Isso mexe com o desempenho do servidor e do cliente.) ~~Pets lutam junto ou são só cosméticos?~~ Decidido em 2026-10-12: o primeiro pet segue e não luta ([pets](../features/pets.md)).

**Afeta:** Mobs, Combate, Personagem (pets).

### S4. PvPvE

**Status:** 🟡 aberta · depende de D5

**O que o board diz:** "PVPVE". O fluxo de ideias já perguntava: "PvP no mundo aberto: áreas seguras, penalidades, consentimento ou livre?".

Com a D1 opção A (2026-10-11), o PvP no mundo aberto volta ao formato do jogo. As perguntas abaixo continuam sem resposta.

**Perguntas:** existem zonas onde jogadores podem atacar uns aos outros enquanto enfrentam monstros? Morrer para outro jogador faz perder itens? Como evitar que jogadores fortes só ataquem os novatos?

**Afeta:** PvP, Mundo, Itens, Economia.

### S5. Construção e cidades de jogadores

**Status:** ⏸️ sugestão: adiar para depois da fatia jogável

**O que o board diz:** sistema de construção; jogadores criam cidades do zero com grupo ou guilda, com mapas procedurais que se adaptam ao crescimento da cidade.

**Perguntas:** construção livre (peça por peça) ou lotes predefinidos? É para guildas ou para qualquer jogador?

**Afeta:** Mundo, Social (guilda), Economia, Profissões. Seria uma funcionalidade nova ("Construção").

### S6. Eventos globais

**Status:** ⏸️ sugestão: adiar para depois da fatia jogável

**O que o board diz:** eventos globais estilo Rift; meteoro que inicia mineração de metais únicos; invasão de cidades, com alerta e defesa em massa.

**Afeta:** Mundo, Mobs, Profissões. Pode virar uma funcionalidade nova ("Eventos").

### S7. Sobrevivência

**Status:** 🟡 aberta, precisa de explicação

**O que o board diz:** o post-it "Elementos de Sobrevivencia:" está sem conteúdo depois dos dois-pontos.

**Perguntas:** quais elementos? Fome e sede, frio, durabilidade de equipamentos, acampamento? Quem escreveu o post-it pode completar.

### S8. Luz e som como mecânica

**Status:** 🟡 aberta

**O que o board diz:** sons de skills, monstros e eventos ouvidos de longe, com eco; grandes skills alertam quem está perto ou disparam eventos. Noite escura de verdade, escondendo inimigos. Skills que iluminam áreas (chuva de orbes, flechas de fogo), úteis em dungeons e eventos de escuridão. Skills que fazem barulho para atrair inimigos.

Combina com "skills e efeitos giga". É uma **identidade forte e um bom diferencial** para a seção "O que é novo?".

**Perguntas:** o som e a luz têm efeito de jogo (inimigos reagem, escuridão esconde inimigos de verdade) ou são só visuais? Se tiverem efeito, o servidor precisa simular (ex.: raio de som que atrai monstros).

**Afeta:** Skills, Mobs, Mundo (ciclo dia e noite), Instâncias.

### S9. Aparência (transmog)

**Status:** ⏸️ sugestão: adiar para depois da fatia jogável

**O que o board diz:** "TRANSMOG".

**Pergunta:** a aparência do equipamento pode ser trocada pela de outro item já obtido? Pode virar fonte de monetização no futuro.

**Afeta:** Itens.

### S10. Troca de arma

**Status:** decidida em 2026-10-12, opção B. A regra está em [combate](../features/combate.md#duas-armas-depois-desta-fatia). A fatia da espada não inclui a troca.

**De onde veio:** conversa do grupo em 2026-10-10, a partir das [referências](../referencias.md) (New World, Throne and Liberty, Guild Wars 2).

**Opções:**

| Opção | Como é | Impacto |
|---|---|---|
| A. Uma arma | Uma arma equipada, com as skills dela | Mais simples; menos variedade por luta |
| B. Duas armas com troca (New World) | Duas armas, cada uma com suas skills e recargas separadas; troca no meio da luta | Mais profundidade sem spam: enquanto as skills de uma arma recarregam, usa-se a outra |
| C. Par de armas define o papel (Throne and Liberty) | As duas armas juntas definem o papel (ex.: espada e escudo + varinha = tank que se cura) | Casa com "a combinação de itens decide o estilo" (D2) e com os papéis de D6 |

**Perguntas:** a troca tem recarga? As skills sorteadas ficam em cada arma? Armaduras e acessórios também dão skills (como no Albion)?

**Afeta:** Combate, Skills, Itens.

## Propostas escritas à espera do grupo

Em 2026-10-11 várias funcionalidades ganharam regras em **proposta**, escritas com ajuda de IA para a implementação poder começar. Nenhuma é decisão. Cada linha aponta para o bloco "Em aberto" da página, onde estão as perguntas; as respostas são marcadas **lá**, não aqui.

A ordem é a sugerida para a conversa: primeiro o que trava código.

| # | Assunto | Onde estão as perguntas | O que trava |
|---|---|---|---|
| 1 | Onde moram os tipos que só o servidor usa (eventos e consultas entre funcionalidades). **Decidido pelo Gabriel em 2026-10-11:** em `server/src/JogoDoGrupo.Server/Contratos/<Dono>/`; o ADR 0016 já entrou | [ADR 0016](../decisoes/0016-eventos-entre-funcionalidades-do-servidor.md), "Em aberto" (o ADR entra com o PR #126) | Ataque (#61), esquiva (#62), chat (#121), caído (#125) |
| 2 | Teclas: o pulo fica? Esquiva no Alt esquerdo? Ataque pesado no botão direito? **Decidido pelo Gabriel em 2026-10-11:** só a esquiva, no **Alt esquerdo**; o pulo e o ataque pesado seguem em aberto | [interface.md](../features/interface.md#em-aberto), "Teclas e atalhos" | Esquiva (#62), trocar teclas (#124) |
| 3 | Ataque: tamanho do cone, sem compensação de lag na fatia, boneco de treino na cidade. **Decidido pelo Gabriel em 2026-10-11:** cone de **2,5 m e 90°**, **sem** compensação de lag na fatia e boneco de treino na cidade | ficha da issue #61 e [combate.md](../features/combate.md#em-aberto) | Ataque (#61) e tudo o que vem depois |
| 4 | Inimigos no campo aberto (com respawn) ou só na dungeon? **Decidido pelo Gabriel em 2026-10-11:** o primeiro grupo fica no **campo aberto**, a uns 150 m do portão norte, com respawn de **120 s** | ficha da issue #63 | Inimigo (#63), loot (#64), XP (#65) |
| 5 | Grupo: herança da liderança, expulsão, tempos de convite e de vaga | [social.md](../features/social.md#em-aberto) | Grupo (#120), dungeon (#66) |
| 6 | Morte e caído: a contagem para enquanto alguém levanta? Pode voltar à cidade antes? Quem é o dono no código? **Decidido pelo Gabriel em 2026-10-11:** só o dono: o estado caído é de **Combate**, e Personagem só mostra a tela; as outras duas perguntas seguem em aberto | [personagem.md](../features/personagem.md#em-aberto), "Morte e caído" | Caído (#125) |
| 7 | XP: inteiro para cada participante? Valores por inimigo e pelo boss | ficha da issue #65 e [fatia-jogavel.md](../features/fatia-jogavel.md), seção 6 | XP (#65) |
| 8 | Loot: chances de drop e o que quer dizer "raio de 12 m" | ficha da issue #64 e [fatia-jogavel.md](../features/fatia-jogavel.md), seção 5 | Loot (#64) |
| 9 | Chat: canal Cidade só na cidade? Ignorar no computador ou na conta? Guardar mensagens? | [social.md](../features/social.md#em-aberto), "Chat" | Chat (#121) |
| 10 | HUD: alvo sem Tab, números de dano, aviso só do boss | [interface.md](../features/interface.md#em-aberto), "HUD do jogo" | HUD (#122) |
| 11 | Menu de opções: pausa? Espera para sair em combate? Idiomas | [interface.md](../features/interface.md#em-aberto) | Menu (#89) |
| 12 | Minimapa e mapa-múndi: raio, inimigos visíveis, aliados em outra área, viagem rápida | [mundo.md](../features/mundo.md#em-aberto-dúvidas-ao-grupo) | Minimapa (#91), mapa (#92) |
| 13 | Áudio: áreas com música e quem escolhe as faixas CC0 | [interface.md](../features/interface.md#em-aberto), "Áudio" | Áudio (#123) |
| 14 | Dungeon: divisão em quatro tarefas, entrada nas colinas, entrar de novo na mesma corrida | ficha da issue #66 | Dungeon (#66) |

Como decidir rápido: quem concorda com a proposta marca o item na página; quem discorda escreve a alternativa ao lado. Os números (tempos, distâncias, chances) entram no código como constantes, então mudar depois é barato; o que custa caro mudar depois são os itens 1 a 4.

## Precisa de explicação

Ideias do board que não ficaram claras. Quem escreveu pode explicar e o item volta para a lista acima.

- ~~**"Todos os personagens serem o gueek"**: o que é "o gueek"?~~ ❌ descartada em 2026-10-10: o grupo pediu para ignorar.
- ~~**"Agro ser longo assim como no RYU"**: qual jogo é o "RYU"?~~ ✅ respondida em 2026-10-10: é o **RYL: Path of the Emperor** (Risk Your Life), um MMORPG antigo. Entrou em S3 e nas [referências](../referencias.md).

## Impacto na fatia jogável

A fatia jogável em [visao-geral.md](../visao-geral.md#primeiro-marco-fatia-jogável) foi escrita antes destas ideias ("uma raça e uma classe, algumas magias, um inimigo"). ✅ Com D1 a D5 decididas, ela foi revisada lá em 2026-10-10. Em 2026-10-11 a D1 passou a ser mundo persistente: a fatia continua uma área e uma dungeon, agora como pedaço desse mundo. O rascunho que serviu de base:

- Uma área segura (hub) e uma região de combate instanciada para até 4 jogadores (atualizado por D6: dungeon de 5)
- Uma raça, com a sua skill base
- 3 ou 4 itens que dão skills, para testar a ideia de "build pelos itens"
- Um tipo de inimigo, aparecendo em horda
- Loot no chão, coletado com uma tecla
- Uma fusão simples entre dois itens
