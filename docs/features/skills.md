# Skills (magias)

**Responsável:**
**Status:** skills ativas de arma implementadas ([#167](https://github.com/GabrielNied/jogo_do_grupo/issues/167)); o resto em definição
**Faixa de IDs de mensagem:** 4000–4999

## Resumo

Skills que vêm dos itens (sorteadas) e da raça, com custo e recarga.

## Definido

- Skills são as magias e habilidades ativas do personagem
- **As skills estão nos itens, geradas aleatoriamente** (D3 da [validação de 2026-10-10](../brainstorm/validacao.md))
- **A arma define as skills-base; as outras peças modificam essas skills** com poderes lendários e sets (decidido por Gabriel em 2026-10-12). Regras dos poderes e sets em [itens](itens.md#poderes-e-sets); a skill precisa ter categorias e propriedades explícitas para os poderes e a árvore de talentos se ligarem a ela.
- A raça dá uma skill base
- **A skill do item é a magia que o jogador usa** enquanto está com o item (decidido em 2026-10-10). Três tipos:
  - **ativa** (ativável): o jogador aperta para usar, tem custo e recarga;
  - **passiva**: sempre ligada enquanto o item está equipado;
  - **buff**: fortalece o próprio jogador ou o grupo (base para os buffs que complementam o time)
- **Poucas skills ativas, com recarga que pesa: sem spam** (ritmo do New World, preferência do grupo em 2026-10-10)
- Skills de itens incluem buffs de grupo que complementam o time (papéis tank, healer e DPS; veja [combate.md](combate.md))
- ~~Cada classe tem suas magias, com custo e recarga~~ substituído em 2026-10-10: não há classes (D2), as skills vêm dos itens (D3)
- **Decisões consolidadas na [#167](https://github.com/GabrielNied/jogo_do_grupo/issues/167) (SKILL-001 a SKILL-006):**
  - **SKILL-001:** a skill ativa pertence à arma equipada no conjunto ativo; desequipar ou trocar de conjunto troca as skills ativas. A recarga fica na arma.
  - **SKILL-002:** a barra tem **3** ativas por arma. **Teclas 1, 2 e 3** (decidido na implementação; Q, E e R eram a alternativa).
  - **SKILL-003:** armas trazem as skills ativas; armaduras e acessórios trazem poderes lendários (modificadores) e bônus de set, **sem ativas extras** na barra ([itens](itens.md#poderes-e-sets)).
  - **SKILL-004:** a quantidade (3) e as opções vêm do **modelo-base** da arma (o tipo: espada, martelo, arco, cajado, foco, escudo). O servidor sorteia e grava no exemplar; a fusão preserva (DEC-003).
  - **SKILL-005:** o servidor valida tudo: arma equipada, recarga e custo (mana ou stamina). Recusa com `SkillRecusada`; nada é gasto numa recusa.
  - **SKILL-006:** cada skill declara `PropriedadesDaSkill` (categoria de dano, tipo de efeito e as flags de crítico, penetração e velocidade), para os poderes e a árvore se ligarem a ela ([combate](combate.md#atributos-secundários-e-pipeline-624)).

## Skills de arma (implementado, #167)

Decisões de design da implementação (valores de teste, em `ConfigSkills` e `CatalogoDeSkills` no shared; mudam sem mexer nas regras):

- **Catálogo inicial:** 5 skills por tipo de arma (4 no escudo), com nomes do catálogo de proposta acima e custo, recarga (8 a 30 s) e multiplicador de dano de teste. Armas físicas gastam **stamina**; cajado e foco, **mana** (responde "Recurso usado"). Uma skill de cada tipo de dano (física, mágica e híbrida) já declara as propriedades.
- **Sorteio:** na primeira vez que a arma é equipada, sorteia 3 do pool do tipo, sem repetir, e grava por **exemplar** (tabela `SkillsDoExemplar`, fora do `EquipamentoJson` de Itens). A chave é o id do exemplar, que a fusão mantém: por isso a fusão preserva as skills. Se o modelo do exemplar ou o catálogo mudarem, sorteia de novo.
- **Qual é a arma:** a principal do conjunto ativo (`Arma1` no conjunto 1, `Arma2` no 2). A mão secundária (escudo) não dá barra por enquanto.
- **Uso:** `UsarSkill` com o espaço (0 a 2) e a direção da mira. O servidor confere arma, espaço, recarga e custo, cobra **uma vez só** (pedidos simultâneos do mesmo personagem rodam em fila) e aplica o dano **na hora** (sem preparo) nos alvos do cone do golpe básico. Skill que não acerta ninguém também gasta e recarrega.
- **Dano:** `(dano da arma + dano flat dos itens, físico ou mágico conforme a skill) x multiplicador da skill`, arredondado, mínimo 1. Ainda **não** usa o bônus em % de Força ou Intelecto, crítico e penetração (pedem a pipeline do Combate, #627); as propriedades da skill já os declaram.
- **Recarga:** em memória, por (exemplar, espaço): trocar de arma não mexe na recarga da outra. Reiniciar o servidor zera as recargas.
- **Só dano nesta fatia:** cura, escudo, controle e mobilidade esperam os estados do Combate ([#171](https://github.com/GabrielNied/jogo_do_grupo/issues/171)) e os poderes ([#629](https://github.com/GabrielNied/jogo_do_grupo/issues/629)).
- **Barra no client:** no centro de baixo da tela, três espaços com tecla, nome, custo e a recarga que falta (cobertura e segundos). Sem arma, a barra some e aparece o aviso para equipar uma. Um uso recusado mostra o motivo na tela. O dano aparece como número sobre o alvo.

## Proposta do board: preparação, raciais e catálogo (2026-10-11)

Do "Livro de ideias de Asterra" ([seções 04 e 13](../brainstorm/figjam.md#04--skills-e-combinações)). **Proposta do board (2026-10-11), não validada pelo grupo**: nomes, números e custos são referências para ajustar, e nada está implementado. O que já está em "Definido" não muda.

### Preparação de skills (proposta do board, 2026-10-11)

- A skill continua **fixa no item** (sorteada na geração; ~~ou ajustada por fusão~~ desde 2026-10-12 a fusão preserva as skills do item principal, [itens](itens.md#invariantes)); o personagem não aprende a skill ao usar a arma. Sem o item equipado (inclusive no inventário, banco ou baú), a skill fica indisponível.
- **Biblioteca** com todas as skills dos itens equipados (filtro por origem, elemento e função); o jogador arrasta para a barra.
- **Orçamento de preparação:** 12 pontos iniciais, **+1 a cada 5 níveis** ([personagem.md](personagem.md#nível-atributos-e-rotina-proposta-do-board-2026-10-11)). Cada skill custa de **1 a 4 pontos**; reservado ao pôr na barra, devolvido ao tirar. É um custo diferente do vigor ou da energia gasto ao usar.
- **Três abas de 8 posições**, todas dividindo o mesmo orçamento. Repetir a mesma skill em duas posições não duplica custo e **compartilha a recarga**.
- Desequipar a origem libera o orçamento e apaga o atalho (proposta: manter esmaecido para restaurar ao reequipar, se houver orçamento). Trocar de equipamento durante uma ação espera a ação acabar.
- **Sorteio:** o equipamento sorteia skills compatíveis com **tipo, região e família**; armaduras e acessórios também dão skills, não só armas. Funções (tanque, dano físico, suporte, mago, arqueiro) são rótulos que orientam os pools de loot, não classes.
- As skills usam **mira manual** com áreas generosas; a área visível deve ser a área real de acerto (veja [combate.md](combate.md)).
- Recurso por skill: **vigor** (físico, movimento) ou **energia** (magia). Isso responde a "Recurso usado" em aberto, como proposta.

### Skills raciais (proposta do board, 2026-10-11)

Cada raça dá uma skill própria; descrição curta de cada uma em [personagem.md](personagem.md#raças-propostas-no-board-2026-10-11). Interações previstas (detalhes no [livro](../brainstorm/figjam.md#01--raças-de-asterra)):

- **Estandarte dos Errantes** (Valdren, 90 s): protege a preparação de uma magia coletiva (aliados resistem a interrupção).
- **Aurora Errante** (Aurídeos, 70 s): a luz é vista por amigos e inimigos; ajuda arqueiros a mirar à noite.
- **Ressonância Ancestral** (Graníticos, 75 s): desequilibra para a Martelada Sísmica e faz minério cintilar.
- **Sopro de Cinzas** (Cinéreos, 65 s): as cinzas **inflamam** com fogo (Linha Incendiária) e fazem uma frente de chamas, como o Óleo de Cerco.
- **Chamado da Clareira** (Silvanos, 60 s): atrai ou desvia manadas; bosses percebem mas não obedecem.

### Catálogo inicial: 25 skills para montar funções (proposta do board, 2026-10-11)

Cinco por função. Custo de preparação em pontos, uso e recarga; os efeitos completos estão no [livro, seção 13](../brainstorm/figjam.md#13--25-skills-para-montar-funções).

| Função | Skills (pontos, uso, recarga) |
|---|---|
| Tanque | Guarda Ressonante (2, 20 vigor, 12 s); Postura de Ferro (2, 20 vigor, 20 s); Avanço de Muralha (2, 30 vigor, 16 s); Desafio do Guardião (2, 25 vigor, 18 s); Muralha Viva (3, 35 energia, 30 s) |
| Dano físico | Martelada Sísmica (2, 25 vigor, 12 s); Ruptura Sangrenta (2, 20 vigor, 12 s); Lâmina Giratória (2, 30 vigor, 14 s); Golpe do Carrasco (3, 40 vigor, 22 s); Avanço Devastador (2, 25 vigor, 15 s) |
| Suporte | Vínculo Vital (2, 25 energia, 18 s); Hino do Recomeço (3, 40 energia, 35 s); Manto Protetor (3, 35 energia, 25 s); Pulso de Vida (2, 30 energia, 15 s); Fonte Arcana (2, 30 energia, 30 s) |
| Mago | Orbes da Aurora (2, 35 energia, 30 s); Estilhaço Glacial (2, 20 energia, 10 s); Pequeno Sol (4, canalização, 45 s); Ponte de Geada (2, 50 energia, 45 s); Tempestade do Horizonte (4, energia contínua, 60 s) |
| Arqueiro | Flecha-Sinal (1, 10 vigor, 8 s); Disparo Perfurante (2, 25 vigor, 14 s); Chuva de Brasas (3, 40 vigor, 25 s); Recuo do Caçador (1, 20 vigor, 12 s); Linha Incendiária (2, 30 vigor, 18 s) |

Skills de utilidade que continuam no catálogo anterior (não fazem parte das 25): Trompa dos Mil Passos, Óleo de Cerco, Névoa e as raciais.

**Combinações e reações** (pedem o sistema de estados de [combate.md](combate.md)): frio + frio congela (mobs comuns, 3 s) e a Martelada contra congelado estilhaça (12 m); óleo + fogo inflama a poça toda; água apaga fogo e remove óleo; chuva molha e permite reação elétrica; o Pequeno Sol soma energia de vários jogadores (a cada 100, +2 m de raio e +10% de dano-base) e o iniciador lança; dano e efeitos pertencem a quem executou (conta para o loot).

### Perguntas que o board abre

- [ ] Hino do Recomeço (levantar caído por skill) e Vínculo Vital: confirmar com [personagem.md](personagem.md#morte-e-caído-issue-97) que o levantar manual continua valendo para todos.
- [ ] Conflito livro x docs: o board propõe **todas as peças** com skill (armadura e acessório), enquanto "Em aberto" ainda pergunta se toda peça pode ter skill. Manter a pergunta até o grupo validar.
- [ ] Conflito livro x docs: o board propõe **uma skill por item, fixa até a fusão** e que ela some se o item sai; isso responde a "A skill pertence ao item equipado" como proposta, mas a pergunta continua em aberto até o grupo validar.
- [ ] Conflito livro x docs: "poucas skills ativas, sem spam" (definido) x **24 posições na barra** (3 abas de 8). O orçamento de 12 pontos é o que limita; confirmar com o grupo.

## Em aberto

- [x] ~~A skill pertence ao item equipado ou o jogador aprende a skill do item?~~ Respondido em 2026-10-14 (SKILL-001): pertence à **arma equipada** no conjunto ativo; trocou a arma, trocou a barra.
- [x] ~~Quantas skills ativas ao mesmo tempo?~~ Respondido em 2026-10-14 (SKILL-002): 3 por arma (teclas 1, 2 e 3); com duas armas ([#580](https://github.com/GabrielNied/jogo_do_grupo/issues/580)) serão 3 por arma, uma barra de cada vez.
- [x] ~~Modelo da skill no item: sorteada e fixa, peça por peça ou encaixável?~~ Respondido em 2026-10-10: a skill do item é a magia que o jogador usa, e pode ser ativa, passiva ou buff (veja "Definido")
- [ ] Quantas skills de cada tipo um item pode ter? Toda peça de equipamento pode ter skill, ou só algumas (arma, acessórios)? **Em parte decidido por Gabriel em 2026-10-12:** a quantidade vem do **modelo-base** do item, não da raridade. Armas têm seus conjuntos de skills; armaduras e acessórios **modificam** essas skills com poderes próprios; item especial pode ter exceção declarada nos dados ([itens](itens.md#poderes-e-sets)). **Respondido em 2026-10-14 (SKILL-003, SKILL-004):** só a arma dá skills ativas, 3 por arma, do pool do tipo; armadura e acessório só modificam, por poderes (#629 e #630). Exceção de item especial continua para quando houver item especial.
- [x] Como funciona o sorteio: **lista de skills por tipo de arma** (modelo-base), sem repetir e fixa no exemplar; por região fica para depois (respondido em 2026-10-14). ~~Por raridade?~~ A raridade saiu em 2026-10-12 (tier pelos atributos, que não depende das skills; [itens](itens.md#tier)).
- [x] ~~A fusão mistura as skills dos itens?~~ Decidido em 2026-10-12: não. A fusão muda um atributo e preserva as skills do item principal ([itens](itens.md#fusão)).
- [ ] ~~Quantas magias por classe?~~ não há classes (D2)
- [x] Recurso usado: **stamina** nas skills físicas e **mana** nas mágicas; energia saiu (respondido em 2026-10-14).

## Regras

Veja [Skills de arma](#skills-de-arma-implementado-167) em Definido.

## Depende de

- **Itens:** `IArmaEquipada` (a arma principal do conjunto ativo) e `EquipamentoMudou`; `IAtributosDosEquipamentos` (dano da arma e flat).
- **Combate:** `ICobrancaDeRecursos` (cobra stamina ou mana de forma atômica), `IAlvosDeCombate` (alvos no cone e dano). As barras de recursos mostram o gasto pelo `RecursosMudaram` de Combate.
- **Mundo:** `IPosicoesDoMundo` (de onde sai a skill e quem está na área).

## Expõe

- Mensagens 4000 a 4005 (`SkillsMessageIds`): `UsarSkill`, `PedirBarraDeSkills`, `BarraDeSkillsMudou`, `SkillUsada`, `SkillAcertou`, `SkillRecusada`.
- `CatalogoDeSkills` e `DefinicaoDeSkill` (com `PropriedadesDaSkill`) no shared, para os poderes (#629) e a árvore lerem a skill.
