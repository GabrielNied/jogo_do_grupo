# Skills (magias)

**Responsável:**
**Status:** em definição
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
- [x] ~~Conflito livro x docs: o board propõe todas as peças com skill~~ Resolvido em 2026-10-12: apenas as armas trazem as skills ativas base; armaduras e acessórios trazem poderes lendários e bônus de set ([itens](itens.md#poderes-e-sets)).
- [x] ~~Conflito livro x docs: o board propõe uma skill por item fixa até a fusão~~ Resolvido em 2026-10-12: a fusão preserva as skills do principal (DEC-003); a skill pertence ao item equipado e fica indisponível ao desequipar.
- [x] ~~Conflito livro x docs: poucas skills ativas x 24 posições na barra~~ Resolvido em 2026-10-12: barra com 3 skills ativas por arma (estilo New World, #167, #580).

## Em aberto

- [x] ~~A skill pertence ao item equipado ou o jogador aprende a skill?~~ Decidido: pertence ao item equipado. Desequipar ou trocar remove as skills da barra ativa.
- [x] ~~Quantas skills ativas ao mesmo tempo?~~ Decidido: 3 skills ativas da arma ativa (teclas 1, 2, 3 / Q, E, R). Troca de armas (#580) dá acesso a outras 3 na segunda arma.
- [x] ~~Quantas skills por tipo de item?~~ Decidido em 2026-10-12: armas trazem as 3 skills-base. Armaduras e acessórios trazem modificadores (poderes lendários e sets), não skills ativas.
- [x] ~~Como funciona o sorteio?~~ Decidido: o modelo-base da arma declara as opções compatíveis; ao gerar o exemplar da arma, o servidor sorteia as 3 skills e grava no item.
- [x] ~~A fusão mistura as skills dos itens?~~ Decidido em 2026-10-12: não. A fusão preserva as skills do principal (DEC-003).
- [x] ~~Quantas magias por classe?~~ Não há classes (D2).
- [x] ~~Recurso usado?~~ Mana (base 100, +2 por Espírito; [fatia](fatia-jogavel.md#1-atributos-e-recursos)). Custo físico pode consumir stamina.
- [ ] Pools de skills de armas de regiões avançadas além do Vale.

## Regras

### Skills nos itens (#167)

Decidido em 2026-10-12 (Gabriel) e formalizado em 2026-10-14 (/decida).

- **SKILL-001 (Pertence à arma):** a skill ativa pertence à arma equipada no conjunto ativo. Desequipar a arma ou trocar de conjunto altera as 3 skills ativas da barra. A recarga fica associada à arma; trocar de arma não zera nem avança o tempo de recarga da outra arma (#580).
- **SKILL-002 (Barra de atalho):** 3 skills ativas na barra (teclas 1, 2, 3 ou Q, E, R). O client mostra ícones, recarga (sweep/tempo restante) e custo de recurso.
- **SKILL-003 (Papel das outras peças):** apenas as armas trazem o conjunto de skills ativas base. Armaduras e acessórios concedem poderes lendários (modificadores de comportamento de skills) e bônus de set ([itens](itens.md#poderes-e-sets)), sem slots extras de skills ativas na barra.
- **SKILL-004 (Sorteio e persistência):** o modelo-base da arma define o catálogo de skills elegíveis. Ao gerar o exemplar da arma, o servidor sorteia as 3 skills e grava no exemplar (`EquipamentoJson`). A fusão preserva as skills do item principal sem alterá-las (DEC-003).
- **SKILL-005 (Recurso e validação autoritativa):** o servidor valida uso, alcance, custo de mana (ou stamina) e recarga. Pedido com recurso insuficiente ou em recarga é recusado com mensagem na faixa 4000 (`SkillRecusada`).
- **SKILL-006 (Contrato com combate):** toda skill implementa `PropriedadesDaSkill` (`CategoriaDeDano`, `TipoDeEfeito`, `PodeCritar`, `RecebePenetracao`, `PodeSerAcelerada`), integrando com a pipeline de combate e atributos secundários ([combate](combate.md#atributos-secundários-e-pipeline-624)).

## Depende de

- **[Itens](itens.md):** armas equipadas, catálogo de modelos-base e exemplar com skills gravadas.
- **[Combate](combate.md):** recursos (mana/stamina), alvos (`IAlvosDeCombate`) e pipeline de modificadores (`PropriedadesDaSkill`).

## Expõe

Contratos em `shared/Package/Runtime/Features/Skills/`:
- Faixa de mensagens: 4000–4999 (`SkillsMessageIds`).
- `CatalogoSkills`: catálogo estático de skills com id, custo, recarga e `PropriedadesDaSkill`.
- `RegrasSkills`: validações puras de custo e recarga.
