# Missões

**Responsável:**
**Status:** em andamento
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
  - **Prólogo (#327) e primeira hora (#328):** na fila por prioridade; a ordem é prólogo → primeira hora → secundárias.
  - **XP:** além de matar mobs, **missões dão XP relevante** assim que o motor de missões ([#179](https://github.com/GabrielNied/jogo_do_grupo/issues/179)) existir. Coleta e craft **não** dão XP ([profissões](profissoes.md#definido)).
- **História igual para todas as raças** (decidido por Gabriel em 2026-10-16): raça é só visual ([personagem](personagem.md#definido)), então não há início, fala nem missão por raça.
- **Decisões do Gabriel em 2026-10-18** (fluxo de missões; detalhe em [Fluxo de missões](#fluxo-de-missões)):
  - **Fluxo fechado:** o fluxo está definido aqui, e a implementação ([#179](https://github.com/GabrielNied/jogo_do_grupo/issues/179), [#327](https://github.com/GabrielNied/jogo_do_grupo/issues/327), [#328](https://github.com/GabrielNied/jogo_do_grupo/issues/328)) segue a fila por prioridade.
  - **O personagem nasce com a primeira missão principal aceita**, sem precisar achar NPC. O prólogo (#327) passa a ser **opcional e pulável**, e não é mais ele que entrega a missão.
  - **Secundárias liberadas por etapa da principal e por nível:** cada etapa da principal destrava as secundárias da região dela, e cada secundária pode pedir um nível mínimo.
  - **Recompensa:** toda missão dá **XP e coroas**; algumas dão também um **item à escolha** entre 2 e 4 opções. Reputação vem das diárias e das secundárias de facção.

## Em aberto

- [x] Missões em grupo? Decidido em 2026-10-16 (veja Definido).
- [x] Como a história se conecta às raças? Decidido em 2026-10-16: não se conecta; raça é só visual.
- [x] O prólogo e a primeira hora do livro (proposta do board, 2026-10-11) viram o tutorial do jogo? ~~Respondido em 2026-10-16 (Definido): a história principal começa pelo prólogo e pela primeira hora do livro.~~ Substituído em 2026-10-16 (quiz 3): entram, nessa ordem, como história principal.

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
- O prólogo (#327) é **opcional e pulável**. Quem joga e quem pula termina no mesmo ponto, com a mesma missão. Implementado como cena do client em [NPCs](npcs.md#prólogo-npcs-de-cena): oferecido uma vez por personagem, sem estado no servidor.

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

### Capítulo 1: A Brasa Mansa e a Serra do Vidro Frio (Roteiro Completo, issue #1058)

Roteiro da história principal pronto para virar dado de missão no `CatalogoDeMissoes`. Encadeia a introdução ao mundo, aprendizado dos serviços da cidade, combate básico e o primeiro grande desafio.

#### 1. Gancho da História
- **Quem é o personagem:** Um viajante/aventureiro sobrevivente da travessia pelas encostas da Serra do Vidro Frio, após a caravana mercantil em que viajava ter sido dispersada por javalis ferais e tempestades de gelo. O personagem chega a pé e sem recursos diante dos portões fortificados da Brasa Mansa. É uma premissa universal para qualquer raça ou combinação de equipamentos.
- **Por que ele fica:** A Brasa Mansa é a única fortaleza aquecida pelas forjas arcanas na região e vive sob constante pressão de monstros que descem da serra. A guarnição da cidade precisa de braços fortes para assegurar as rotas de coleta e proteger os comboios. Em troca de abrigo, equipamento e coroas, a capitã da guarda e os artesãos locais acolhem o viajante na guilda da cidade.

#### 2. NPCs de Serviço da Brasa Mansa
Cada NPC possui papel específico, localização fixa registrada em [npcs.md](npcs.md#elenco-da-brasa-mansa) e diálogo de apresentação:

1. **Capitã Valéria** (Comandante da Guarda / Dadora da Principal, Portão Sul): Firme, vigilante e pragmática. Avalia os recém-chegados e comanda a defesa da muralha.
   - *Fala de apresentação:* *"A Brasa Mansa não tem espaço para turistas. Se você sobreviveu à estrada, prove que sabe empunhar uma lâmina antes de cruzar nossos portões."*
   - *O que ensina:* Introduz a cidade, a missão principal e direciona o fluxo entre os bairros.
2. **Mercador Alden** (Comerciante Geral, Praça Central, Id 1): Negociante sagaz e bem-humorado.
   - *Fala de apresentação:* *"Tudo tem seu valor sob este teto, viajante. Traga o que recolher nos campos e garanto que suas coroas valerão o peso em provisões."*
   - *O que ensina:* Uso do inventário, venda de espólios e compra de consumíveis básicos.
3. **Armeiro Gareth** (Vendedor de Armas, Beco das Forjas, Id 4): Especialista paciente em metais e balística.
   - *Fala de apresentação:* *"Uma espada precisa ser a extensão do seu braço; um arco, o foco dos seus olhos. Escolha sua primeira ferramenta com sabedoria."*
   - *O que ensina:* Diferentes tipos de armas (espada, arco, cajado), arcos de corte e alcances de ataque.
4. **Armadureira Bruna** (Vendedora de Armaduras, Beco das Forjas, Id 5): Minuciosa e focada em sobrevivência prática.
   - *Fala de apresentação:* *"Ataque sem proteção é apenas bravata com plateia. Deixe-me ajustar essas tiras de couro ao seu peito antes que um javali o atravesse."*
   - *O que ensina:* Tipos de armadura (tecido, couro, placas), mitigação e defesa física contra dano direto.
5. **Ferreiro Brom** (Forja, Fusão e Reparo, Forja Central, Id 2): Rude, vigoroso e coberto de fuligem.
   - *Fala de apresentação:* *"O ferro frio não tem compaixão de quem bate frouxo. Mantenha o fio das suas armas polido ou terminará servindo de almoço aos lobos."*
   - *O que ensina:* Reparo de durabilidade de equipamentos e introdução à forja e fusão de essências.
6. **Banqueiro Cassian** (Guarda-Volumes e Cofre, Salão Comercial, Id 3): Educado, metódico e incorruptível.
   - *Fala de apresentação:* *"Seus tesouros descansam muito mais seguros nos cofres de pedra da cidade do que numa mochila rasgada durante uma emboscada na serra."*
   - *O que ensina:* Banco pessoal, cofre de conta e preservação de itens excedentes.
7. **Taverneiro Barnabé** (Cozinha e Hospedagem, Taverna da Lareira, Id 6): Acolhedor e generoso.
   - *Fala de apresentação:* *"Puxe um banco junto ao fogo e aqueça essas mãos trêmulas. Um estômago vazio é a pior fraqueza quando se enfrenta o vento cortante."*
   - *O que ensina:* O valor das refeições preparadas (buffs de longa duração de 30–60 min) e poções de alívio rápido.
8. **Merceeiro Tomas** (Insumos e Ferramentas, Mercado, Id 7): Curioso e perspicaz.
   - *Fala de apresentação:* *"Um punhado de ervas certas ou uma picareta reforçada valem mais do que um saco de ouro na hora do aperto nas cavernas."*
   - *O que ensina:* Ferramentas de coleta (picareta, foice) e insumos para profissões de produção.
9. **Cavalariço Roderic** (Estábulo, Cercado Norte, Id 8): Direto e dedicado aos animais de carga.
   - *Fala de apresentação:* *"Um bom cavalo encurta o caminho entre você e a vitória. Mas lembre-se: nada de galopar enquanto lâminas estiverem tinindo!"*
   - *O que ensina:* Aquisição e uso de montarias para travessias rápidas em terrenos abertos fora de combate.
10. **Cuidadora Mirna** (Criadouro de Pets, Alameda Oeste, Id 9): Carinhosa e atenta aos pequenos seres.
    - *Fala de apresentação:* *"Esses bichinhos possuem uma lealdade que ouro nenhum compra. Trate-os bem e você nunca caminhará solitário sob a névoa."*
    - *O que ensina:* Companheiros e mascotes colecionáveis no mundo.

#### 3. Etapas da Missão Principal (Capítulo 1)
Todas as etapas seguem objetivos estritamente verificáveis pelo motor autoritativo (`TipoDeObjetivo` em `shared/Package/Runtime/Features/Missoes/`):

1. **Etapa 1: Portões da Brasa Mansa**
   - *NPC Dador:* Capitã Valéria (Portão Sul) | *NPC Entregador:* Mercador Alden (Praça Central)
   - *Texto Aceitar:* *"Você conseguiu atravessar a estrada da serra, mas está em farrapos. Entre na cidadela e procure Alden no mercado da praça; ele fornecerá suas primeiras informações."*
   - *Objetivo:* `IrA` Praça Central (`Regiao: cidade`, X: 12, Z: -8, Raio: 4m) e `Falar` com Mercador Alden.
   - *Texto Entregar:* *"Vejo que Valéria mandou mais um sobrevivente. Bem-vindo à Brasa Mansa; aqui todo trabalho honesto é recompensado."*
   - *Recompensa:* 100 XP, 25 Coroas. Próxima missão desbloqueada: Etapa 2.
2. **Etapa 2: A Primeira Ferramenta**
   - *NPC Dador:* Mercador Alden | *NPC Entregador:* Armeiro Gareth
   - *Texto Aceitar:* *"Você não durará cinco minutos além das paliçadas desarmado. Vá até Gareth no beco das forjas e escolha a arma que melhor se adapta às suas mãos."*
   - *Objetivo:* `Falar` com Armeiro Gareth.
   - *Texto Entregar:* *"Chegou na hora certa. Espada de lâmina larga, arco recurvo ou cajado esculpido? A escolha é sua, novato."*
   - *Recompensa:* 120 XP, 30 Coroas. Item à escolha: `Espada de Ferro Curta`, `Arco de Caça Simples` ou `Cajado de Madeira Polida`.
3. **Etapa 3: Casca de Ferro e Couro**
   - *NPC Dador:* Armeiro Gareth | *NPC Entregador:* Ferreiro Brom
   - *Texto Aceitar:* *"Uma boa arma exige um corpo protegido. Leve este lingote inicial até a forja de Brom e peça a ele para reforçar sua veste."*
   - *Objetivo:* `Falar` com Ferreiro Brom.
   - *Texto Entregar:* *"Gareth mandou você? Deixe-me ver esse metal. Nada mau. Aqui está uma peça reforçada para manter suas costelas inteiras."*
   - *Recompensa:* 140 XP, 35 Coroas, Peitoral de Couro Batido.
4. **Etapa 4: Provisões e Guarda-Volumes**
   - *NPC Dador:* Ferreiro Brom | *NPC Entregador:* Banqueiro Cassian
   - *Texto Aceitar:* *"Antes de marchar para o descampado, passe na taverna de Barnabé para encher o cantil e depois deixe suas sobras no banco com Cassian."*
   - *Objetivo:* `Falar` com Taverneiro Barnabé e `Falar` com Banqueiro Cassian.
   - *Texto Entregar:* *"Um aventureiro que sabe onde guardar seus pertences é um aventureiro que vive para gastá-los. Seu baú está pronto."*
   - *Recompensa:* 160 XP, 40 Coroas, 3x Poções Menores de Vida e 1x Caldo Quente da Taverna.
5. **Etapa 5: Limpando a Trilha Norte**
   - *NPC Dador:* Capitã Valéria | *NPC Entregador:* Capitã Valéria
   - *Texto Aceitar:* *"Chega de passeios pela praça. Uma vara de javalis selvagens está revirando os fardos dos comboios a norte da muralha. Vá lá e elimine a ameaça."*
   - *Objetivo:* `Matar` 6x Javali do Campo (`AlvoId: 1`, Região: `campo`).
   - *Texto Entregar:* *"O sangue na sua lâmina prova sua utilidade. Os carroceiros já conseguem avistar a estrada sem pavor."*
   - *Recompensa:* 280 XP, 65 Coroas, Luvas do Desbravador.
6. **Etapa 6: O Enxame na Mata Fria**
   - *NPC Dador:* Capitã Valéria | *NPC Entregador:* Capitã Valéria
   - *Texto Aceitar:* *"Os javalis eram apenas o começo. Os lobos cinzentos desceram das ravinas e estão encurralando nossos lenhadores. Derrube as feras e traga suas peles para nossos agasalhos."*
   - *Objetivo:* `Matar` 5x Lobo Cinzento (`AlvoId: 2`) e `Coletar` 4x Peles Espessas.
   - *Texto Entregar:* *"Excelente trabalho. Essas peles manterão nossos vigias aquecidos durante as noites de geada."*
   - *Recompensa:* 360 XP, 85 Coroas, Botas Resistentes da Guarda.
7. **Etapa 7: O Senhor da Colina do Vigia (Clímax do Capítulo 1)**
   - *NPC Dador:* Capitã Valéria | *NPC Entregador:* Capitã Valéria
   - *Texto Aceitar:* *"Descobrimos o que está empurrando as feras contra nossos muros. O Presa Velha, um javali colossal com presas de obsidiana, estabeleceu território no topo da Colina do Vigia. Ele já esmagou duas patrulhas. Suba a colina e acabe com o monstro!"*
   - *Objetivo:* `Matar` 1x Presa Velha (`AlvoId: 200`, Chefe do Campo/Colina do Vigia).
   - *Texto Entregar:* *"Você abateu a lenda das ravinas! O silêncio voltou à Colina do Vigia. A Brasa Mansa tem uma dívida eterna com você, aventureiro. Considere-se um membro de honra da nossa cidadela."*
   - *Recompensa:* 750 XP, 200 Coroas, Acesso às Missões Secundárias da Região (#1060) e Item Raro à Escolha: `Amuleto da Chama Viva`, `Anel de Presa Afiada` ou `Manto do Vigia da Serra`.

#### 4. O Chefe do Capítulo 1: Presa Velha (CriaturaId: 200)
- **Identidade e Visual:** Um javali alfa de porte colossal, com pelagem prateada encrostada em minérios da serra e um par de presas recurvadas de obsidiana.
- **Motivo de estar ali:** Desalojado das galerias superiores da Serra do Vidro Frio pela atividade sísmica na Mina do Reflexo, desceu para os planaltos da Colina do Vigia em busca de pasto fértil, atacando qualquer comboio que tente cruzar a garganta rochosa.
- **Mecânica em Combate:** Investidas lineares telegrafadas em área com rastro de poeira, batida no chão provocando atordoamento leve e aumento de ferocidade ao atingir 30% de vida.
- **Falas do NPC:**
  - *Antes do confronto (Valéria):* *"Não o enfrente de frente sem esquivar. O peso daquele bicho quebra escudos como gravetos secos."*
  - *Após a vitória (Valéria):* *"O troféu das presas dele já está fincado no portão. Nenhuma fera ousará desafiar este marco novamente."*

#### 5. Missões Secundárias da Brasa Mansa e Arredores (#1060)

O Capítulo 1 destrava progressivamente 8 missões secundárias na região da cidade e campos, com variedade de objetivos (Coletar, IrA, Falar, Matar), uma cadeia curta e reputação de facção:

1. **Caldeirão da Lareira** (ID 101, Taverneiro Barnabé, Nível 1, Requer Etapa 4): Coletar 4x Carnes Frescas de javali nos pastos para abastecer a estalagem. Recompensa: 180 XP, 35 Coroas, +25 Reputação Mercadores.
2. **Minério para a Bigorna** (ID 102, Ferreiro Brom, Nível 2, Requer Etapa 5): Coletar 6x Minérios de Ferro nos afloramentos rochosos para reforçar a forja. Inicia a cadeia curta de ferreiro. Recompensa: 220 XP, 45 Coroas, +30 Reputação Forja.
3. **Reforço da Paliçada** (ID 107, Ferreiro Brom -> Capitã Valéria, Nível 2, Requer Missão 102): Cadeia curta da missão 102. Entregar as chapas forjadas à Capitã Valéria no portão sul. Recompensa: 240 XP, 50 Coroas, +25 Reputação Forja.
4. **O Mascote Fugitivo** (ID 103, Cuidadora Mirna, Nível 2, Requer Etapa 5): Ir até o Moinho Decrépito no campo norte (área) e falar com Mirna na cidade. Recompensa: 200 XP, 40 Coroas, +25 Reputação Mercadores.
5. **Correspondência Extraviada** (ID 104, Banqueiro Cassian, Nível 1, Requer Etapa 4): Coletar a Pasta de Ordens Mercantis extraviada na trilha após emboscada. Recompensa: 190 XP, 40 Coroas, +35 Reputação Mercadores.
6. **Vigilância no Portão Leste** (ID 105, Capitã Valéria, Nível 2, Requer Etapa 6): Abater 5x Lobos Selvagens que rondam a paliçada oriental na névoa. Recompensa: 260 XP, 55 Coroas, +40 Reputação Guarda.
7. **Remédio das Sombras** (ID 106, Merceeiro Tomas, Nível 3, Requer Etapa 7): Ir até a Ravina Sombria na mata fria e coletar 5x Fungos Luminescentes nos troncos. Recompensa: 300 XP, 70 Coroas, +40 Reputação Mercadores.
8. **Guarda de Honra da Brasa** (ID 108, Capitã Valéria, Nível 3, Requer Etapa 7): Eliminar 8x Javalis Selvagens remanescentes no platô norte após a derrota do Presa Velha. Recompensa: 320 XP, 75 Coroas, +50 Reputação Guarda.

#### 6. Missões Diárias e Semanais (#1062)

Missões rotativas para engajamento contínuo e progressão de reputação de facções:

- **Rotação:** 3 missões diárias sorteadas por dia e 1 missão semanal por semana (`ConfigMissoes`), filtradas pelo nível do personagem e regiões já abertas pela história principal.
- **Semente determinística (`RegrasCicloReset`):** a rotação é calculada a partir do ciclo civil no horário de Brasília (UTC-3), garantindo que todos os jogadores com o mesmo nível e região vejam o mesmo conjunto diário/semanal sem necessidade de persistência ou crons no servidor.
- **Horários de Reset (Horário de Brasília, UTC-3):**
  - **Reset Diário:** Meia-noite (00:00 BRT / 03:00 UTC).
  - **Reset Semanal:** Terça-feira às 00:00 BRT (03:00 UTC), alinhado com o ciclo de instâncias e raids.
- **Regras de conclusão e concorrência:**
  - A mesma missão diária ou semanal não pode ser realizada mais de uma vez dentro do mesmo ciclo de reset (`missao_ja_concluida_no_ciclo`).
  - Ao virar o ciclo de Brasília, o histórico do ciclo anterior é desconsiderado e a missão volta a ficar disponível se entrar na rotação.
  - A entrega no servidor possui trava por `(PersonagemId, MissaoId)` e transação atômica, impedindo duplicidade de recompensas (XP e Coroas) em requisições simultâneas.
- **Indicadores no Client:**
  - O marcador visual (**!**) nos NPCs só aparece para missões diárias e semanais que estão ativas na rotação do ciclo atual e que ainda não foram concluídas no ciclo.

##### Catálogo de Missões Diárias (IDs 201 a 206)
1. **Patrulha dos Javalis** (ID 201, Capitã Valéria, Nível 1): Matar 4x Javali do Campo. Recompensa: 120 XP, 20 Coroas, +15 Reputação Guarda.
2. **Suprimentos de Carne** (ID 202, Taverneiro Barnabé, Nível 1): Coletar 3x Carne Fresca. Recompensa: 100 XP, 25 Coroas, +10 Reputação Mercadores.
3. **Ferro para as Lâminas** (ID 203, Ferreiro Brom, Nível 2): Coletar 4x Minério de Ferro. Recompensa: 150 XP, 30 Coroas, +20 Reputação Forja.
4. **Caçada aos Lobos** (ID 204, Capitã Valéria, Nível 2): Matar 3x Lobo Cinzento. Recompensa: 180 XP, 35 Coroas, +20 Reputação Guarda.
5. **Entrega de Provisões** (ID 205, Banqueiro Cassian -> Taverneiro Barnabé, Nível 1): Falar com Barnabé. Recompensa: 80 XP, 15 Coroas, +15 Reputação Mercadores.
6. **Manutenção das Bigornas** (ID 206, Ferreiro Brom, Nível 2): Ir até a Forja Central [28, 48]. Recompensa: 100 XP, 20 Coroas, +15 Reputação Forja.

##### Catálogo de Missões Semanais (IDs 301 a 303)
1. **Grande Expedição de Caça** (ID 301, Capitã Valéria, Nível 2): Matar 10x Javali do Campo e 8x Lobo Cinzento. Recompensa: 600 XP, 120 Coroas, +60 Reputação Guarda.
2. **Estoque de Emergência da Taverna** (ID 302, Taverneiro Barnabé, Nível 2): Coletar 10x Carne Fresca e 10x Minério de Ferro. Recompensa: 500 XP, 150 Coroas, +50 Reputação Mercadores.
3. **Mestre da Forja da Brasa** (ID 303, Ferreiro Brom, Nível 3): Coletar 15x Minério de Ferro. Recompensa: 700 XP, 180 Coroas, +70 Reputação Forja.


### Livro de Asterra: chegada ao mundo (proposta do board, 2026-10-11)

Do "Livro de ideias" do [FigJam](../brainstorm/figjam.md) (seção 11). **Proposta do board, não validada pelo grupo**; o nome Asterra e o título do jogo continuam a escolher.

- **Prólogo "o sol que nasceu na estrada":** cena jogável curta; o personagem chega a uma parada perto da cidade, vê **NPCs** sustentarem uma esfera de luz coletiva e eliminarem uma manada (só encenação, **não** é skill do jogador). Funciona sem outros jogadores conectados. Pode ser **pulado**; não dá loot nem mata o recém-chegado. Termina com o controle devolvido, indo à cidade.
- **Primeira hora (guia de missões iniciais):** receber arma e aprender golpes leve e pesado; comer na cozinha e achar banco e teleporte; ir ao primeiro ponto de coleta e achar os primeiros javalis; obter uma peça com skill, abrir Preparação e usar a skill num grupo; subir de nível, distribuir pontos e infundir uma peça na forja; ajudar um aliado caído (a primeira expedição não perde nada por morte).
- Depende de [personagem](personagem.md), [skills](skills.md), [itens](itens.md), [combate](combate.md) e [npcs](npcs.md); as missões só encadeiam.

## Depende de

- `Economia`: `ICarteiraCoroas` (recompensa de coroas)
- `Itens`: `IInventarioEconomia` (recompensa de itens por escolha)
- `Personagem`: `IPontosDeNivel`, `IAjusteDeExperienciaDeTeste` (nível mínimo e concessão de XP)
- `Mundo`: `IPosicoesDoMundo` (validação de alcance de interação)
- `Social`: `IConsultaDeGrupo` (compartilhamento com membros do grupo)
- `Mobs`: `InimigoDerrotado` (progresso de objetivos de derrotar criaturas)
- `Npcs`: `CatalogoNpcs` (posições e identidades de NPCs doadores e entregadores)

## Expõe

- `ControleDeMissoes` (client): registro e acompanhamento de missões ativas
- `ServicoDeMissoes` (servidor): autoridade de pré-requisitos, aceitação, abandono, progresso e entrega atômica
- `JanelaMissoes`: diário e registro de missões no atalho L
- `RastreadorDeMissoesHud`: visualização de objetivos acompanhados na tela
- `MarcadoresDeMissaoNpc`: lógica de indicadores visuais (! e ?) sobre NPCs e mapa
- `MissoesMessageIds`: faixa 10000–10999 e DTOs em `shared/Package/Runtime/Features/Missoes/`
