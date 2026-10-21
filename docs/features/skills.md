# Skills (magias)

**Responsável:**
**Status:** skills ativas de arma implementadas ([#167](https://github.com/GabrielNied/jogo_do_grupo/issues/167)); tela de consulta de skills por conjunto/arma implementada ([#921](https://github.com/GabrielNied/jogo_do_grupo/issues/921)); o resto em definição
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
- **Skills nas armas ([#167](https://github.com/GabrielNied/jogo_do_grupo/issues/167)), decidido em 2026-10-14** (valores de teste em constantes, fácil de mudar):
  - **SKILL-001:** as **ativas** da barra vêm da **arma principal do conjunto ativo** (`Arma1`/`Arma2` quando existir troca — [#580](https://github.com/GabrielNied/jogo_do_grupo/issues/580)). Desequipar ou trocar de conjunto altera a barra na hora. A **recarga fica no exemplar da arma**, não no personagem.
  - **SKILL-002:** **3 skills ativas** por arma, teclas **1, 2 e 3** ([interface](interface.md#teclas-e-atalhos)). Duas armas com recargas separadas ficam para a #580; a fatia joga com uma espada.
  - **SKILL-003:** só a **arma** coloca ativas na barra. **Armaduras e acessórios** não somam ativa extra: modificam as skills da arma com **poderes lendários** e **bônus de set** ([#629](https://github.com/GabrielNied/jogo_do_grupo/issues/629), [#630](https://github.com/GabrielNied/jogo_do_grupo/issues/630)).
  - **SKILL-004:** as **3 opções** vêm do **modelo-base** (tipo de arma): o servidor sorteia **sem repetição** no pool, grava por **exemplar** (`SkillsDoExemplar`; o id do exemplar não muda na fusão, DEC-003). Catálogo inicial em `ConfigSkills` / `CatalogoDeSkills`.
  - **SKILL-005:** uso **autoritativo** no servidor: custo em **mana** ou **stamina** ([fatia-jogavel](fatia-jogavel.md#1-atributos-e-recursos)); recarga por slot da arma. Recusa com `SkillRecusada` (4000–4999) se faltar recurso, estiver em recarga, não houver arma ou o personagem estiver caído.
  - **SKILL-006:** cada skill declara **`PropriedadesDaSkill`** para a pipeline de [combate](combate.md#atributos-secundários-e-pipeline-624), poderes (#629) e [talentos](talentos.md).

## Skills de arma (implementado, #167)

Decisões de design da implementação (valores de teste, em `ConfigSkills` e `CatalogoDeSkills` no shared; mudam sem mexer nas regras):

- **Catálogo inicial:** 5 skills por tipo de arma (4 no escudo), com nomes do catálogo de proposta acima e custo, recarga (8 a 30 s) e multiplicador de dano de teste. Armas físicas gastam **stamina**; cajado e foco, **mana** (responde "Recurso usado"). Uma skill de cada tipo de dano (física, mágica e híbrida) já declara as propriedades.
- **Sorteio:** na primeira vez que a arma é equipada, sorteia 3 do pool do tipo, sem repetir, e grava por **exemplar** (tabela `SkillsDoExemplar`, fora do `EquipamentoJson` de Itens). A chave é o id do exemplar, que a fusão mantém: por isso a fusão preserva as skills. Se o modelo do exemplar ou o catálogo mudarem, sorteia de novo.
- **Qual é a arma:** a principal do conjunto ativo (`Arma1` no conjunto 1, `Arma2` no 2). A mão secundária (escudo) não dá barra por enquanto.
- **Uso:** `UsarSkill` com o espaço (0 a 2) e a direção da mira. O servidor confere arma, espaço, recarga e custo, cobra **uma vez só** (pedidos simultâneos do mesmo personagem rodam em fila) e aplica o dano **na hora** (sem preparo; skill forte tem preparo, abaixo) nos alvos do cone do golpe básico. Skill que não acerta ninguém também gasta e recarrega.
- **Dano:** `(dano da arma + dano flat dos itens, físico ou mágico conforme a skill) x multiplicador da skill`, arredondado, mínimo 1. Ainda **não** usa o bônus em % de Força ou Intelecto, crítico, penetração e mitigação final ([combate](combate.md#atributos-secundários-e-pipeline-624), números #627 aprovados no quiz 5); as propriedades da skill já os declaram.
- **Preparo e cancelamento de skill forte (#345):** skill com `PreparoMs > 0` (multiplicador de 2,5 para cima; as demais seguem instantâneas) não sai na hora. Fluxo: `UsarSkill` confere arma, recarga e se o recurso cobre o custo (sem cobrar) e responde `SkillPreparoIniciado` (4007, com `PreparoMs`); ao fim do tempo o servidor cobra o custo inteiro, aplica o efeito e manda `SkillUsada`/`SkillAcertou` como antes (ou `SkillRecusada` sem cobrar se faltar recurso ou estiver caído; arma trocada cancela sem cobrar). `CancelarSkill` (4008) no meio cobra 50% do custo (`RegrasSkills.CustoAoCancelar`) e responde `SkillCancelada` (4009, com o custo cobrado) mais `RecursosMudaram`; sem dano e sem recarga. Cancelar e concluir ao mesmo tempo: só um vale (a trava do personagem), então o custo nunca é cobrado duas vezes. A barra traz o `PreparoMs` de cada skill (`SkillDaBarra.PreparoMs`). Regra de custo em [combate](combate.md#definido). No client a mesma tecla cancela.
- **Recarga:** em memória, por (exemplar, espaço): trocar de arma não mexe na recarga da outra. Reiniciar o servidor zera as recargas.
- **Dano e apoio de grupo:** quase todas as skills causam dano; três de **apoio** (Manto Protetor no cajado, Fonte Arcana no foco, Brado do Guardião no martelo, #170) dão escudo ou buff ao grupo em vez de dano ([combate.md](combate.md#escudo-e-buffs-de-grupo-170)). Cura, controle e mobilidade esperam os estados do Combate ([#171](https://github.com/GabrielNied/jogo_do_grupo/issues/171)). Os poderes ([#629](https://github.com/GabrielNied/jogo_do_grupo/issues/629)) e sets ([#630](https://github.com/GabrielNied/jogo_do_grupo/issues/630)) já mudam o dano e a recarga (abaixo).
- **Poderes e sets aplicados ao uso (#629, #630):** decidido na implementação; as regras e os valores estão em [itens, Entregue nas #629 e #630](itens.md#entregue-nas-629-e-630-poderes-e-sets).
  - **Etiquetas:** `DefinicaoDeSkill.Etiquetas` declara o que a skill é para os poderes (PODER-003): `fisica`/`magica` saem da categoria de dano (a híbrida leva as duas) e `investida` é marcada no catálogo (Avanço Devastador e Avanço de Muralha). Poder só afeta skill cujo id ou etiqueta ele cita.
  - **Ordem do uso:** cobra o custo; `MotorDePoderes.AoUsarSkillAsync` gasta as cargas e devolve o multiplicador do dano (`RegrasSkills.ComPoderes`); aplica o dano; com acerto, marca alvos, dá cargas e reduz recarga (`AoAcertarComSkillAsync`). Reduzir recarga tira das **outras** skills da barra (a recém-usada começa a recarga inteira) e o dono recebe a barra nova.
  - **Aviso:** `PoderDisparou` (4006) vai ao dono a cada efeito aplicado: nome do poder ou do set, ação e valor.
  - **Estado:** cargas, marcas e folga ficam em memória por personagem, somem ao sair e são apagados quando a origem (peça, marco) deixa de valer.
- **Barra no client:** no centro de baixo da tela, três espaços com tecla, nome, custo e a recarga que falta (cobertura e segundos). Sem arma, a barra some e aparece o aviso para equipar uma. Um uso recusado mostra o motivo na tela. O dano aparece como número sobre o alvo.
- **Tela de consulta de skills (K, [#921](https://github.com/GabrielNied/jogo_do_grupo/issues/921)):** janela somente leitura registrada nas `JanelasDoJogo` (tecla K). Exibe o catálogo completo de skills por tipo de arma, destacando a arma ativa no conjunto atual e as 3 skills presentes na barra de combate. Trocar de conjunto (R) atualiza o destaque na hora.

## Proposta do board: preparação, raciais e catálogo (2026-10-11)

Do "Livro de ideias de Asterra" ([seções 04 e 13](../brainstorm/figjam.md#04--skills-e-combinações)). **Proposta do board (2026-10-11), não validada pelo grupo**: nomes, números e custos são referências para ajustar, e nada está implementado. O que já está em "Definido" não muda.

### Preparação de skills (proposta do board, 2026-10-11)

> **Não será implementada** (Gabriel, 2026-10-16): vale o Definido, com as 3 skills da arma ativa direto nas teclas 1, 2 e 3, sem orçamento nem biblioteca (#339 fechada).

- A skill continua **fixa no item** (sorteada na geração; ~~ou ajustada por fusão~~ desde 2026-10-12 a fusão preserva as skills do item principal, [itens](itens.md#invariantes)); o personagem não aprende a skill ao usar a arma. Sem o item equipado (inclusive no inventário, banco ou baú), a skill fica indisponível.
- **Biblioteca** com todas as skills dos itens equipados (filtro por origem, elemento e função); o jogador arrasta para a barra.
- **Orçamento de preparação:** 12 pontos iniciais, **+1 a cada 5 níveis** ([personagem.md](personagem.md#nível-atributos-e-rotina-proposta-do-board-2026-10-11)). Cada skill custa de **1 a 4 pontos**; reservado ao pôr na barra, devolvido ao tirar. É um custo diferente do vigor ou da energia gasto ao usar.
- **Três abas de 8 posições**, todas dividindo o mesmo orçamento. Repetir a mesma skill em duas posições não duplica custo e **compartilha a recarga**.
- Desequipar a origem libera o orçamento e apaga o atalho (proposta: manter esmaecido para restaurar ao reequipar, se houver orçamento). Trocar de equipamento durante uma ação espera a ação acabar.
- **Sorteio:** o equipamento sorteia skills compatíveis com **tipo, região e família**; armaduras e acessórios também dão skills, não só armas. Funções (tanque, dano físico, suporte, cajado/foco, arqueiro) são rótulos que orientam os pools de loot, **não classes jogáveis**.
- As skills usam **mira manual** com áreas generosas; a área visível deve ser a área real de acerto (veja [combate.md](combate.md)).
- Recurso por skill: **vigor** (físico, movimento) ou **energia** (magia). Isso responde a "Recurso usado" em aberto, como proposta.

### Skills raciais (proposta do board, 2026-10-11)

> **Não serão implementadas** (Gabriel, 2026-10-16): raça é só visual ([personagem](personagem.md#definido)); #341 fechada. O texto abaixo fica como registro.

Cada raça dá uma skill própria; descrição curta de cada uma em [personagem.md](personagem.md#raças-propostas-no-board-2026-10-11). Interações previstas (detalhes no [livro](../brainstorm/figjam.md#01--raças-de-asterra)):

- **Estandarte dos Errantes** (Valdren, 90 s): protege a preparação de uma magia coletiva (aliados resistem a interrupção).
- **Aurora Errante** (Aurídeos, 70 s): a luz é vista por amigos e inimigos; ajuda arqueiros a mirar à noite.
- **Ressonância Ancestral** (Graníticos, 75 s): desequilibra para a Martelada Sísmica e faz minério cintilar.
- **Sopro de Cinzas** (Cinéreos, 65 s): as cinzas **inflamam** com fogo (Linha Incendiária) e fazem uma frente de chamas, como o Óleo de Cerco.
- **Chamado da Clareira** (Silvanos, 60 s): atrai ou desvia manadas; bosses percebem mas não obedecem.

### Catálogo por tipo de arma (referência do board, 2026-10-11)

> **Adotado em 2026-10-16** (Gabriel): nomes do [livro, seção 13](../brainstorm/figjam.md#13--25-skills-para-montar-funções) viram referência do **catálogo por tipo de arma** em `CatalogoDeSkills` ([#167](https://github.com/GabrielNied/jogo_do_grupo/issues/167)); **não há pool extra por região** ([#340](https://github.com/GabrielNied/jogo_do_grupo/issues/340) not planned, quiz 3). Sai o custo de preparação; ficam stamina ou mana (SKILL-005) e recarga. **Sem reações elementais** entre skills nesta fatia.

> **Revogado em 2026-10-18** (Gabriel): a [#342](https://github.com/GabrielNied/jogo_do_grupo/issues/342) (Pequeno Sol ritual coletivo, Tempestade do Horizonte como evento, Ponte de Geada ligada a região) **não será implementada**. Não há **classe de mago** — só **tipo de arma** (cajado, foco, etc.). Rituais de área, magias gated por evento/região e travessia especial ficam fora do escopo; o prólogo de [missões](missoes.md) pode mostrar efeito parecido **só como cena de NPC**.

O jogo sorteia **3 skills ativas** do pool do **tipo da arma equipada** (5 entradas por tipo, 4 no escudo). A tabela abaixo agrupa nomes do board por **função de combate** (rótulo de loot, não classe). O código canônico é `CatalogoDeSkills`.

| Função (rótulo) | Exemplos no pool (stamina ou mana, recarga em s) |
|---|---|
| Tanque / escudo | Guarda Ressonante; Postura de Ferro; Avanço de Muralha; Desafio do Guardião; Pancada de Escudo |
| Dano físico corpo a corpo | Martelada Sísmica; Ruptura Sangrenta; Lâmina Giratória; Golpe do Carrasco; Avanço Devastador; Corte Veloz |
| Suporte | Vínculo Vital; Hino do Recomeço; Manto Protetor; Pulso de Vida; Fonte Arcana *(ainda não no catálogo de armas)* |
| Cajado / foco (mana) | Estilhaço Glacial; Orbes da Aurora; Raio Arcano; Nova de Fogo; Surto Solar; Pulso Arcano; Vórtice; … |
| Arqueiro | Flecha-Sinal; Disparo Perfurante; Chuva de Brasas; Recuo do Caçador; Linha Incendiária |

Skills de utilidade citadas no board (Trompa dos Mil Passos, Óleo de Cerco, Névoa) e **skills raciais** não entram no catálogo de armas ([#341](https://github.com/GabrielNied/jogo_do_grupo/issues/341) fechada).

**Combinações e reações** (proposta do board, **não implementadas**; pedem estados em [combate.md](combate.md)): frio + frio congela; Martelada contra congelado estilhaça; óleo + fogo inflama; água apaga fogo; chuva + elétrica. Sem magia coletiva nem atribuição proporcional de dano por ritual.

### Perguntas que o board abre

- [x] Hino do Recomeço (levantar caído por skill) e Vínculo Vital: confirmar com [personagem.md](personagem.md#morte-e-caído-issue-97) que o levantar manual continua valendo para todos. Respondido: o levantar manual vale para todos ([Levantar um aliado](personagem.md#levantar-um-aliado)); a skill é uma forma a mais.
- [x] ~~Conflito livro x docs: todas as peças com skill~~ Decidido em 2026-10-12/2026-10-14: **armas** trazem as ativas da barra; **outras peças** modificam via poderes/sets, não ativas extras (SKILL-003). Preparação com orçamento (#339) continua proposta do board.
- [x] ~~Conflito livro x docs: skill fixa no item~~ Decidido: sorteio no exemplar da arma, preservado na fusão; some ao desequipar (SKILL-001, SKILL-004).
- [x] ~~Conflito livro x docs: poucas skills ativas x 24 posições na barra~~ Decidido para a fatia ([#167](https://github.com/GabrielNied/jogo_do_grupo/issues/167)): **3 ativas por arma** (SKILL-002); orçamento de 12 pontos e 3×8 slots do livro ficam na proposta de preparação ([#339](https://github.com/GabrielNied/jogo_do_grupo/issues/339)).

## Em aberto

- [x] ~~A skill pertence ao item equipado ou o jogador aprende?~~ Decidido em 2026-10-14 (SKILL-001): pertence à **arma equipada** no conjunto ativo.
- [x] ~~Quantas skills ativas ao mesmo tempo?~~ Decidido em 2026-10-14 (SKILL-002): **3 por arma** (teclas 1, 2, 3); duas armas na [#580](https://github.com/GabrielNied/jogo_do_grupo/issues/580).
- [x] ~~Modelo da skill no item: sorteada e fixa, peça por peça ou encaixável?~~ Respondido em 2026-10-10: ativa, passiva ou buff (veja "Definido").
- [x] ~~Quantas skills de cada tipo e quais peças?~~ Decidido em 2026-10-12/2026-10-14 (SKILL-003, SKILL-004): **3 ativas** na **arma**; armaduras/acessórios via **poderes** ([#629](https://github.com/GabrielNied/jogo_do_grupo/issues/629)). Item especial: exceção declarada nos dados quando existir.
- [x] ~~Como funciona o sorteio?~~ Decidido em 2026-10-14: pool por **tipo de arma**, sem repetição, fixo no exemplar (`SkillsDoExemplar`). ~~Por raridade?~~ saiu em 2026-10-12.
- [x] ~~A fusão mistura as skills dos itens?~~ Decidido em 2026-10-12: não ([itens](itens.md#fusão), DEC-003).
- [x] ~~Quantas magias por classe?~~ Não há classes (D2).
- [x] ~~Recurso usado?~~ Decidido em 2026-10-14 (SKILL-005): **mana** ou **stamina** por skill; não "energia" genérica.
- [x] ~~Pools de skills de armas de regiões avançadas além do Vale ([#340](https://github.com/GabrielNied/jogo_do_grupo/issues/340)).~~ Substituído em 2026-10-16 (quiz 3): **não existe** segundo pool por região; #340 **not planned**. Sorteio continua só pelo **tipo de arma** (Vale).
- [x] ~~Orçamento da aba Preparação ([#339](https://github.com/GabrielNied/jogo_do_grupo/issues/339)).~~ Decidido em 2026-10-16 (quiz 3): **sem orçamento**; só as **3 skills da arma equipada**. Tela somente leitura para ver skills **por conjunto/arma** implementada na [#921](https://github.com/GabrielNied/jogo_do_grupo/issues/921) (tecla **K**).
- [x] ~~Skills de área especial do livro ([#342](https://github.com/GabrielNied/jogo_do_grupo/issues/342)).~~ Revogado em 2026-10-18: sem classe mago; skills só da **arma**; sem ritual/região/evento dedicado.

## Regras

SKILL-001 a SKILL-006 em **Definido**; comportamento implementado em [Skills de arma](#skills-de-arma-implementado-167).

## Depende de

- **Itens:** `IArmaEquipada` (arma principal do conjunto ativo) e `EquipamentoMudou`; `IAtributosDosEquipamentos` (dano da arma e flat); `IEfeitosDoEquipamento` (poderes e marcos de set ligados).
- **Combate:** `GolpeComumAcertou` e `PersonagemEsquivou` em `Contratos/Combate/`, para os poderes de golpe comum e de esquiva (#875).
- **Combate:** `ICobrancaDeRecursos` (cobra stamina ou mana de forma atômica), `IAlvosDeCombate` (alvos no cone e dano). Barras de recurso via `RecursosMudaram` de Combate.
- **Mundo:** `IPosicoesDoMundo` (origem da skill e área).

## Expõe

Contratos em `shared/Package/Runtime/Features/Skills/`:

- Mensagens **4000–4009** (`SkillsMessageIds`): `UsarSkill`, `PedirBarraDeSkills`, `BarraDeSkillsMudou`, `SkillUsada`, `SkillAcertou`, `SkillRecusada`, `PoderDisparou`, `SkillPreparoIniciado`, `CancelarSkill`, `SkillCancelada`.
- `CatalogoDeSkills`, `DefinicaoDeSkill` (com `PropriedadesDaSkill`) e `RegrasSkills` (validações puras de custo e recarga).
