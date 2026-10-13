# Documento mestre (entrada de 2026-10-12)

Documento trazido por Gabriel em 2026-10-12 como base para o sistema de itens. Fica aqui como **entrada**, no texto original (só as listas longas foram juntadas em linha), para rastrear de onde vieram as regras. **Não é a fonte das regras:** cada parte foi levada para o lugar dela, e o que estiver lá vale sobre este texto.

| Seções | Onde vale | Status |
|---|---|---|
| 7 (equipamentos), 8 (tier), 9 (fusão), 18.2 (DEC-001 a DEC-005), 17.2 (testes da fusão) | [itens.md, Modelo de itens v2](../features/itens.md#modelo-de-itens-v2-documento-mestre-2026-10-12) | **Definido** (Gabriel, 2026-10-12), prioridade alta, épico [#619](https://github.com/GabrielNied/jogo_do_grupo/issues/619) |
| 10 e 11.4 (loot e materiais de dungeon e raid) | [itens.md](../features/itens.md#fusão) e [instancias.md](../features/instancias.md) | Estrutura de custo definida; números pendentes |
| 6 (personagem e árvore) e DEC-001 | [talentos.md](../features/talentos.md) e [personagem.md](../features/personagem.md) | **Pendente:** conflita com a [fatia, seção 1](../features/fatia-jogavel.md#1-atributos-e-recursos); decisão em [#618](https://github.com/GabrielNied/jogo_do_grupo/issues/618) |
| 11 (dungeons e raids, resto) e 12 (arquitetura) | [instancias.md](../features/instancias.md), [arquitetura.md](../arquitetura.md) e ADRs | Já coberto pelo que o repo decidiu (Unity, .NET, servidor autoritativo, Postgres) |
| 3, 4, 5, 13 a 21, 24 e 25 (processo da IA, organização das docs, planejamento) | [AGENTS.md](../../AGENTS.md), [agentes.md](../agentes.md) e [docs/README.md](../README.md) | O processo do repo continua valendo. A exceção adotada: as [invariantes de itens](../features/itens.md#invariantes) não mudam por decisão de quem implementa |

Termos: o "ouro" do texto são as **coroas** do jogo; a "família de arma" é o `TipoEquipamento` (veja [Fusão](../features/itens.md#fusão)); os prefixos de requisito usados no repo são `ITEM-`, `TIER-` e `FUSAO-`.

---

# MMORPG — Documento Mestre de Projeto, Implementação e Planejamento por IA

**Versão:** 1.0
**Status:** Em desenvolvimento
**Tipo:** Documento central de referência
**Público-alvo:** IA de desenvolvimento, programação, game design, arquitetura, QA e planejamento
**Projeto:** MMORPG com progressão de personagem, combate baseado em armas, itens evolutivos, dungeons e raids.

---

## 1. Objetivo do documento

Este documento define como uma IA deve compreender, documentar, planejar, implementar e validar os sistemas do MMORPG.

Ele funciona como a principal referência para:

- Documentação de game design (GDD).
- Documentação técnica (TDD).
- Arquitetura de software.
- Especificação de sistemas e funcionalidades.
- Planejamento de desenvolvimento.
- Decomposição de funcionalidades em tarefas.
- Criação de histórias de usuário e critérios de aceitação.
- Planejamento de testes.
- Registro de decisões arquiteturais.
- Identificação de dependências, riscos e bloqueios.
- Revisão de implementações existentes.
- Controle de mudanças e prevenção de inconsistências.

O objetivo não é apenas registrar ideias, mas transformar decisões de design em especificações suficientemente claras para que uma IA possa implementá-las sem precisar interpretar regras fundamentais por conta própria.

### 1.1 Princípio fundamental

**Nenhum sistema deve ser implementado com base em suposições tratadas como decisões aprovadas.**

Quando faltar uma definição importante, a IA deverá:

1. Identificar a informação ausente.
2. Verificar se ela já foi definida em outra documentação.
3. Identificar os sistemas afetados.
4. Propor uma solução quando necessário.
5. Registrar a proposta como pendente de aprovação.
6. Implementar apenas o que estiver suficientemente especificado e autorizado.

---

## 2. Visão geral do MMORPG

### 2.1 Conceito

O projeto é um MMORPG com foco em:

- Combate baseado em armas e habilidades.
- Construção livre de personagens.
- Especialização por árvore de atributos e talentos.
- Equipamentos com atributos variáveis.
- Itens de alto valor que podem receber investimentos prolongados.
- Evolução previsível de equipamentos.
- Loot com potencial de gerar descobertas excepcionais.
- Dungeons e raids como conteúdos de progressão.
- Economia baseada em obtenção, negociação e consumo de itens.
- Progressão de longo prazo sem tornar o azar um bloqueio permanente.

### 2.2 Jogos de referência

As referências devem orientar sistemas específicos, sem obrigar o projeto a reproduzir integralmente nenhum dos jogos.

| Referência | Área de inspiração |
|---|---|
| New World | Combate, armas e interação com habilidades |
| Diablo 3 | Variedade de itens, atributos e possibilidades de loot |
| Risk Your Life | Investimento, evolução e fusão de equipamentos |
| World of Warcraft | Dungeons, raids e conteúdo PvE de grupo |

A IA não deverá assumir que uma mecânica existe no projeto apenas porque existe em um desses jogos.

Cada mecânica precisa ser definida pelas regras próprias deste MMORPG.

### 2.3 Pilares de design

**Pilar A — Liberdade de construção.** O personagem não depende obrigatoriamente de uma classe fixa. Sua especialização é definida pelos pontos investidos na árvore de atributos e talentos.

**Pilar B — Equipamentos com identidade.** Os equipamentos possuem seus próprios atributos, habilidades, histórico e progressão. Um item pode se tornar valioso pelo seu conjunto de características e pelo investimento realizado nele.

**Pilar C — Progressão previsível.** Os investimentos permanentes devem oferecer resultados previsíveis. O jogador não deve perder seu progresso por uma falha aleatória de aprimoramento.

**Pilar D — Loot relevante.** Drops excepcionais devem continuar interessantes ao longo da progressão, em vez de serem descartados automaticamente assim que o jogador encontra um item de nível superior.

**Pilar E — Sistemas independentes e integráveis.** Árvore de talentos, atributos do personagem, atributos dos itens, habilidades e progressão de equipamentos precisam ter responsabilidades bem definidas.

**Pilar F — Conteúdo cooperativo.** Dungeons e raids devem oferecer desafios, recompensas e materiais que façam parte do ciclo de progressão do personagem e dos equipamentos.

---

## 3. Regras para a IA de desenvolvimento

Esta seção define o comportamento obrigatório da IA durante o projeto.

### 3.1 Fonte de verdade

A IA deverá utilizar as fontes nesta ordem de prioridade:

1. Decisões explicitamente aprovadas pelo responsável pelo projeto.
2. Documento mestre e especificações atualizadas.
3. Decisões arquiteturais registradas.
4. Documentação dos sistemas relacionados.
5. Código implementado e testes, quando estiverem de acordo com as especificações aprovadas.
6. Propostas técnicas ainda não aprovadas.
7. Convenções e suposições técnicas.

O código existente não deve ser considerado automaticamente correto se contradisser uma decisão aprovada.

Se duas fontes aprovadas entrarem em conflito, a IA deverá registrar a inconsistência e solicitar resolução, em vez de escolher silenciosamente uma delas.

### 3.2 Classificação obrigatória das informações

Toda informação relevante deverá pertencer a uma das categorias:

- **APROVADO:** decisão confirmada e válida.
- **PROPOSTA:** solução sugerida, ainda não confirmada.
- **PENDENTE:** informação necessária que ainda não foi decidida.
- **BLOQUEADO:** trabalho que não pode avançar sem uma dependência.
- **IMPLEMENTADO:** funcionalidade desenvolvida.
- **VALIDADO:** funcionalidade testada contra seus critérios de aceitação.
- **DEPRECIADO:** decisão ou funcionalidade substituída, mantida para histórico.

Uma funcionalidade não pode ser classificada como validada apenas porque seu código foi escrito.

### 3.3 Proibições

A IA não deverá:

- Criar regras de balanceamento sem identificá-las como propostas.
- Inventar valores definitivos para dano, custos, limites ou probabilidades.
- Alterar silenciosamente decisões aprovadas.
- Misturar atributos de equipamentos com pontos da árvore de talentos.
- Introduzir chances de falha em sistemas definidos como previsíveis.
- Criar sistemas de desmontagem de itens sem aprovação.
- Alterar a regra de fusão de equipamentos sem aprovação.
- Implementar progressão que dependa de uma funcionalidade inexistente sem registrar a dependência.
- Marcar tarefas como concluídas sem evidências.
- Duplicar sistemas existentes por falta de consulta à documentação.
- Fazer mudanças abrangentes de arquitetura para resolver um problema local sem avaliar impactos.

### 3.4 Autonomia permitida

A IA pode decidir autonomamente detalhes técnicos reversíveis, desde que:

- Não contradigam o design aprovado.
- Não alterem regras de jogo.
- Não introduzam dependências desnecessárias.
- Sejam documentados quando afetarem a arquitetura.
- Possam ser testados e revisados.

Exemplos: organização de arquivos, nomes internos de funções, divisão de módulos e escolha de estruturas de dados adequadas à arquitetura aprovada.

Decisões que alterem economia, progressão, combate, recompensas ou experiência do jogador devem ser tratadas com mais rigor.

---

## 4. Organização da documentação

A documentação deverá ser modular, evitando um único arquivo gigantesco que precise ser reescrito a cada mudança.

### 4.1 Estrutura recomendada

```text
/docs
  /00-project
    project-overview.md
    design-pillars.md
    glossary.md
    project-roadmap.md
    scope-and-milestones.md

  /01-game-design
    character-progression.md
    attribute-and-talent-tree.md
    combat-system.md
    weapon-system.md
    skill-system.md
    item-system.md
    item-fusion.md
    item-tier-system.md
    loot-and-rewards.md
    economy.md
    dungeon-system.md
    raid-system.md
    party-system.md
    world-and-content.md

  /02-technical-design
    architecture-overview.md
    system-boundaries.md
    data-models.md
    persistence.md
    server-authority.md
    networking.md
    security.md
    observability.md
    performance.md

  /03-implementation
    implementation-plan.md
    dependency-map.md
    technical-decisions.md
    migration-plan.md
    integration-plan.md

  /04-planning
    roadmap.md
    milestones.md
    backlog.md
    sprint-plans.md
    risks-and-blockers.md

  /05-quality
    test-strategy.md
    test-cases.md
    acceptance-checklists.md
    regression-plan.md
    balance-validation.md

  /06-operations
    build-and-release.md
    environments.md
    deployment.md
    monitoring.md
    incident-response.md

  /07-project-status
    current-status.md
    completed-work.md
    known-issues.md
    pending-decisions.md

  /templates
    system-spec-template.md
    task-template.md
    test-template.md
    decision-template.md
    change-request-template.md
```

Essa estrutura é uma proposta de organização. Ela pode ser adaptada à engine, às ferramentas e ao tamanho real do projeto.

### 4.2 Documento mestre

O documento mestre deve conter:

- Objetivo do projeto.
- Pilares de design.
- Sistemas principais.
- Decisões globais.
- Regras de consistência.
- Estado geral do desenvolvimento.
- Referências para documentos detalhados.
- Principais pendências e riscos.

Ele não deverá duplicar todas as regras técnicas de cada sistema. Os detalhes devem permanecer nos documentos especializados.

### 4.3 Regra de atualização

Quando uma decisão mudar, a IA deverá:

1. Identificar o documento proprietário da regra.
2. Atualizar a especificação principal.
3. Localizar documentos, tarefas, testes e diagramas afetados.
4. Atualizar as referências relacionadas.
5. Registrar a mudança no histórico de decisões.
6. Indicar se o código existente precisa ser alterado.
7. Criar tarefas de migração ou correção quando necessário.

---

## 5. Modelo de especificação de sistema

Toda funcionalidade relevante deverá ter uma especificação própria.

### 5.1 Estrutura obrigatória

Cada documento de sistema deverá conter:

1. Identificação e versão.
2. Status da especificação.
3. Objetivo.
4. Escopo incluído.
5. Escopo excluído.
6. Conceitos e terminologia.
7. Regras de negócio.
8. Dados de entrada e saída.
9. Modelo de dados.
10. Fluxos principais.
11. Fluxos alternativos.
12. Casos de erro.
13. Dependências.
14. Integrações com outros sistemas.
15. Requisitos funcionais.
16. Requisitos não funcionais.
17. Critérios de aceitação.
18. Estratégia de testes.
19. Riscos conhecidos.
20. Decisões pendentes.
21. Tarefas de implementação.
22. Histórico de alterações.

### 5.2 Identificação de requisitos

Cada requisito deverá possuir um identificador único.

Exemplos:

- `GAME-001`: requisito geral do jogo.
- `CHAR-001`: personagem.
- `TREE-001`: árvore de talentos.
- `ITEM-001`: itens.
- `FUSION-001`: fusão de equipamentos.
- `COMBAT-001`: combate.
- `DUNGEON-001`: dungeons.
- `RAID-001`: raids.
- `TECH-001`: arquitetura técnica.
- `QA-001`: qualidade e testes.

Os prefixos são convencionais e poderão ser adaptados. Depois de publicados, os identificadores não deverão ser reutilizados para requisitos diferentes.

### 5.3 Regras de qualidade

Um requisito precisa ser:

- Claro.
- Específico.
- Testável.
- Rastreável.
- Compatível com as decisões aprovadas.
- Independente de interpretações subjetivas sempre que possível.

Evitar frases como "o sistema deve funcionar bem" ou "a fusão deve ser balanceada".

Preferir regras observáveis, como: "A fusão deve alterar somente o atributo selecionado, mantendo os demais atributos e as habilidades do item principal inalterados".

---

## 6. Sistema de personagem e árvore de atributos

**Status:** estrutura conceitual aprovada; números de balanceamento e detalhes técnicos ainda precisam ser definidos.

### 6.1 Objetivo

Permitir que o jogador construa seu personagem por meio de pontos investidos em atributos e de uma árvore de talentos, sem depender obrigatoriamente de classes fixas.

### 6.2 Atributos principais

| Atributo | Responsabilidade definida |
|---|---|
| Força | Dano físico |
| Vigor | Vida |
| Agilidade | Vigor físico ou stamina |
| Intelecto | Dano mágico |
| Espírito | Mana |

A conversão numérica dos atributos em estatísticas finais permanece pendente.

### 6.3 Atributos secundários previstos

- Regeneração de vida.
- Regeneração de stamina.
- Regeneração de mana.
- Chance crítica.
- Dano crítico.
- Velocidade de ataque físico.
- Velocidade de ataque mágico.
- Penetração física.
- Penetração mágica.

A fórmula, os limites e as interações desses atributos precisam ser definidos na especificação de combate.

### 6.4 Estrutura da árvore

A árvore possui cinco caminhos principais: Força, Vigor, Agilidade, Intelecto e Espírito. Os caminhos podem possuir conexões híbridas.

**Nós pequenos:** fornecem atributos primários ou secundários.

**Nós grandes:** representam especializações e bônus condicionais associados às categorias ou propriedades das habilidades utilizadas.

**Nós avançados:** representam sinergias entre atributos ou combinações de caminhos.

### 6.5 Progressão

- A árvore utiliza pontos próprios de progressão.
- Os pontos investidos desbloqueiam ou evoluem nós.
- A redistribuição de pontos pode ocorrer mediante custo de recurso.
- O jogador pode salvar perfis reutilizáveis de builds.
- A troca de equipamentos não deve redistribuir os pontos da árvore.

O nível do personagem, a quantidade de pontos por nível e os custos de redistribuição ainda precisam ser especificados.

### 6.6 Separação obrigatória dos atributos

Os pontos da árvore e os atributos próprios dos equipamentos são sistemas independentes.

Atributos de armas não devem:

- Conceder pontos de árvore.
- Ser convertidos automaticamente em Força, Vigor, Agilidade, Intelecto ou Espírito do personagem.
- Desbloquear nós da árvore.
- Alterar diretamente a quantidade de pontos de progressão.

A interação permitida ocorre por meio das fórmulas de combate e das propriedades das habilidades, conforme especificação própria.

---

## 7. Sistema de equipamentos

**Status:** regras estruturais aprovadas; catálogo, faixas numéricas e implementação técnica pendentes.

### 7.1 Objetivo

Criar equipamentos com atributos próprios, habilidades variáveis e progressão prolongada, de forma que o jogador possa investir em um item por bastante tempo.

Um equipamento excepcional não deve perder seu valor apenas porque o personagem avançou de nível. Um novo item poderá substituí-lo quando oferecer uma vantagem suficientemente relevante.

### 7.2 Identidade do item

Cada item deverá possuir, no mínimo, os seguintes conceitos de dados:

- Identificador único da instância.
- Tipo de equipamento.
- Família de arma, quando aplicável.
- Atributos próprios.
- Habilidades vinculadas.
- Nível de fusão.
- Tier atual calculado.
- Tier de origem ou histórico.
- Identificação do item-base ou definição de catálogo.
- Estado de posse e equipamento.
- Metadados necessários à persistência.

Campos adicionais, como nível mínimo de uso, vinculação ao personagem, procedência do drop e histórico completo de transações, devem ser definidos conforme a arquitetura e as necessidades de segurança.

### 7.3 Regras de atributos

Os equipamentos possuem atributos independentes dos atributos da árvore.

Os valores permitidos precisam ser definidos por: família de arma; tipo de atributo; faixa de nível do item, caso exista; limite mínimo e máximo válido; regras de geração; regras de evolução.

A IA não deve inventar valores finais para essas faixas sem aprovação.

### 7.4 Habilidades dos equipamentos

As habilidades são selecionadas aleatoriamente a partir de catálogos compatíveis com a família da arma e com os espaços ou regras definidos para aquele item.

As habilidades permanecem no item principal durante a fusão.

O número de habilidades, as probabilidades de obtenção, as restrições de combinação e a possibilidade de duplicatas ainda precisam ser definidos.

A árvore de talentos deve avaliar as categorias ou propriedades da habilidade utilizada para aplicar seus bônus. Ela não deve depender exclusivamente do tipo de arma equipada.

### 7.5 Estados permitidos

Os itens podem ser: equipados; vendidos; consumidos como materiais de fusão.

**Desmontagem de equipamentos não faz parte do sistema definido.**

Qualquer novo destino para um item exige uma decisão de design explícita.

---

## 8. Sistema de tier dos equipamentos

### 8.1 Tiers definidos

A classificação prevista é `D < C < B < A < S`. Existe ainda a condição especial `S+`.

### 8.2 Regra de classificação

O tier atual deve ser calculado com base exclusivamente nos valores dos atributos do item, conforme o modelo de cálculo que ainda será definido.

Não deve depender diretamente de: uma raridade atribuída aleatoriamente; uma etiqueta fixa do drop; uma categoria visual que não represente os valores reais; o nível de fusão isoladamente; a qualidade ou raridade das habilidades, se a regra de atributos exclusivos for mantida.

As habilidades podem afetar a utilidade e o valor de mercado do item, mas não devem alterar seu tier se isso contrariar a regra aprovada de classificação por atributos.

### 8.3 Tier de origem e tier atual

O sistema deverá distinguir:

- **Tier de origem:** registro histórico do tier inicial do item.
- **Tier atual:** classificação recalculada conforme os atributos atuais.

O tier de origem não deve limitar a evolução do item. Um item de tier D poderá alcançar um tier superior mediante fusões e investimento.

### 8.4 Condição de S+

S+ não deve ser obtido como um tier aleatório em drops do mundo.

A condição S+ representa um item S no qual todos os atributos elegíveis atingiram seus respectivos valores máximos permitidos.

A IA deverá tratar S+ como uma condição verificável do estado do item, e não como uma rolagem independente.

### 8.5 Definições pendentes

Antes de implementar o cálculo definitivo, é necessário aprovar: faixas válidas por atributo; fórmula de normalização dos atributos; forma de agregação dos valores; limiares entre D, C, B, A e S; tratamento de atributos ausentes; tratamento de atributos opcionais; definição exata de quais atributos são elegíveis para S+; regras para precisão e arredondamento.

Nenhuma fórmula ilustrativa deverá ser tratada como definitiva sem aprovação.

---

## 9. Sistema de fusão de equipamentos

### 9.1 Objetivo

Permitir que o jogador evolua equipamentos utilizando outras armas compatíveis como materiais, preservando o investimento feito no item principal.

A fusão deve ser previsível e não possuir chance aleatória de falha.

### 9.2 Regras aprovadas

1. O item principal permanece como item resultante.
2. O material deve ser compatível e pertencer à mesma família de arma.
3. O jogador escolhe apenas um atributo por operação.
4. Somente o atributo escolhido é alterado.
5. As demais propriedades do item principal são preservadas.
6. As habilidades do item principal são preservadas.
7. O material é consumido.
8. Não existe chance de falha.
9. O nível de fusão máximo é `+10`.
10. A operação deve apresentar uma prévia antes da confirmação.
11. O tier atual é recalculado após a alteração dos atributos.
12. O tier de origem é preservado como informação histórica.

### 9.3 Fórmula de fusão

A fórmula definida é:

`novo_valor = valor_atual + (valor_material - valor_atual) / 2`

Exemplo: atributo no item principal 20; mesmo atributo no material 40; novo valor 30; variação +10.

A operação move o atributo do item principal em direção ao valor do material, percorrendo metade da diferença.

A fórmula também se aplica quando o material possui valor inferior ao item principal. Nesse caso, o atributo poderá diminuir.

### 9.4 Arredondamento e progresso mínimo

Quando os atributos forem representados por valores inteiros, a implementação deverá definir uma regra determinística de arredondamento.

Também deverá ser respeitada a regra de progresso mínimo: quando os valores forem diferentes, a fusão deverá produzir pelo menos uma unidade de mudança em direção ao valor do material, dentro dos limites válidos do atributo.

Exemplos esperados, considerando atributos inteiros:

| Principal | Material | Resultado esperado |
|---:|---:|---:|
| 20 | 40 | 30 |
| 40 | 20 | 30 |
| 20 | 21 | 21 |
| 21 | 20 | 20 |
| 20 | 20 | 20 |

A tabela ilustra o comportamento esperado, não substitui a especificação formal de precisão para atributos decimais.

### 9.5 Prévia de fusão

Antes da confirmação, a interface deverá mostrar: item principal; item material; família e compatibilidade; nível atual e nível resultante; atributo selecionado; valor atual; valor do material; valor resultante; variação numérica; custos em ouro; materiais de dungeon ou raid exigidos; aviso de consumo permanente do material; alteração prevista do tier.

A operação só deve prosseguir após a confirmação explícita do jogador.

### 9.6 Níveis e custos

O limite é `+10`.

| Evolução | Custo estrutural |
|---|---|
| +0 → +1 até +4 → +5 | Ouro |
| +5 → +6 até +7 → +8 | Ouro + materiais de dungeon |
| +8 → +9 e +9 → +10 | Ouro + materiais de raid |

Os valores de ouro, as quantidades de materiais e os identificadores dos materiais ainda precisam ser definidos.

A implementação deve tratar o nível de fusão e os custos como dados configuráveis, e não espalhar valores fixos pelo código.

### 9.7 Integridade da operação

A fusão deverá ser atômica: ou todos os efeitos da operação são concluídos, ou nenhum deles é aplicado.

A validação e a confirmação final precisam ocorrer em uma autoridade confiável do servidor, conforme a arquitetura técnica aprovada.

O sistema deve impedir: consumir um item que não pertence ao jogador; utilizar uma família de arma incompatível; ultrapassar o nível máximo; consumir materiais insuficientes; cobrar recursos sem aplicar a fusão; aplicar a fusão sem consumir o material; duplicar itens ou recursos em operações concorrentes; reutilizar a mesma instância de material em mais de uma fusão.

A operação precisa possuir tratamento para falhas de rede, repetição de requisições e confirmação duplicada.

---

## 10. Loot, economia e progressão

### 10.1 Objetivo

Criar um ciclo de recompensas que combine descoberta aleatória com investimento previsível.

O jogador poderá encontrar itens com características excepcionais, mas não deverá depender exclusivamente de sorte para desenvolver permanentemente um item.

### 10.2 Princípios

- O loot oferece variedade e descoberta.
- A fusão oferece progressão controlada.
- Os materiais de dungeon sustentam a progressão intermediária.
- Os materiais de raid sustentam a progressão avançada.
- O ouro funciona como um recurso econômico de aprimoramento.
- Itens compatíveis podem ser consumidos como materiais de fusão.
- Itens de alto investimento devem ter valor de uso potencialmente prolongado.

### 10.3 Pontos ainda pendentes

Fontes de ouro e sumidouros econômicos; tabelas de drop; probabilidades por fonte de recompensa; frequência esperada de itens por tier; regras de negociação e vinculação; quantidade de materiais por fusão; fontes de materiais de dungeon e raid; limites de transação e proteção contra exploração; regras de recompensa individual ou compartilhada; prevenção contra inflação econômica.

A IA deve documentar os impactos econômicos antes de introduzir novas fontes de recursos ou custos recorrentes.

---

## 11. Dungeons e raids

### 11.1 Objetivo

Fornecer conteúdo cooperativo de PvE integrado à progressão de equipamentos e personagens.

Dungeons e raids são referências estruturais para o conteúdo de grupo, mas seus detalhes ainda precisam ser definidos.

### 11.2 Dungeons

As dungeons deverão ser especificadas com: requisitos de entrada; tamanho do grupo; encontros e chefes; mecânicas de combate; duração esperada; recompensas; materiais utilizados na fusão; regras de repetição; critérios de conclusão; regras de dificuldade.

### 11.3 Raids

As raids deverão ser especificadas com: tamanho do grupo; requisitos de entrada; encontros e chefes; mecânicas que exigem coordenação; regras de progressão; recompensas; materiais necessários para as últimas etapas da fusão; regras de repetição e bloqueio; critérios de conclusão.

### 11.4 Dependência da progressão de itens

A distribuição dos materiais deve respeitar a estrutura aprovada:

- Evoluções iniciais: ouro.
- Progressão intermediária até +8: materiais de dungeon e ouro.
- Progressão de +9 a +10: materiais de raid e ouro.

As quantidades, fontes específicas e taxas de obtenção precisam ser balanceadas.

A IA não deverá implementar recompensas de raid como requisito de etapas anteriores sem justificativa e aprovação.

---

## 12. Arquitetura técnica

**Status:** princípios de separação definidos; tecnologias, engine e infraestrutura pendentes de decisão.

### 12.1 Objetivo

Construir sistemas modulares, testáveis e integráveis, que possam evoluir sem exigir reescritas generalizadas.

### 12.2 Princípios arquiteturais

- Separar regras de negócio da interface.
- Separar definições de catálogo das instâncias de itens.
- Centralizar regras críticas em serviços ou módulos responsáveis.
- Evitar duplicação de fórmulas.
- Utilizar dados configuráveis para balanceamento.
- Manter identificadores estáveis para entidades persistentes.
- Criar testes para regras de negócio.
- Definir contratos claros entre sistemas.
- Registrar decisões técnicas relevantes.
- Preservar a integridade de operações econômicas.
- Evitar dependências circulares entre módulos.

### 12.3 Separação lógica sugerida

A arquitetura deverá contemplar responsabilidades equivalentes a: personagem e progressão; árvore de talentos; combate; armas e habilidades; inventário e equipamentos; atributos de itens; tiers de itens; fusão de equipamentos; economia e recursos; loot e recompensas; dungeons e raids; persistência de dados; autenticação e autorização; comunicação cliente-servidor; telemetria e diagnóstico.

Esses módulos são uma divisão conceitual, não uma determinação de microserviços. A separação física deverá depender da engine, da infraestrutura e das necessidades reais do projeto.

### 12.4 Autoridade do servidor

Operações que alteram estado persistente ou possuem valor econômico deverão ser validadas por uma autoridade confiável. Exemplos: alteração de atributos; equipar e desequipar itens; consumo de materiais; cobrança de ouro; fusão; distribuição de recompensas; negociação entre jogadores, caso exista.

O cliente pode solicitar uma operação e exibir uma prévia, mas não deve ser a autoridade final para decidir se a operação é válida.

### 12.5 Persistência e integridade

O modelo técnico deverá especificar: quais entidades são persistidas; como itens únicos são identificados; como transações são registradas; como recursos são debitados e creditados; como são tratadas requisições repetidas; como são recuperadas falhas parciais; como são realizadas migrações de dados; como são feitas cópias de segurança e restaurações.

A tecnologia de banco de dados ainda não está definida.

---

## 13. Planejamento de implementação

### 13.1 Objetivo

Transformar os sistemas de jogo em entregas incrementais, priorizadas por dependências, risco e valor de validação.

O planejamento não deverá ser baseado apenas em uma lista de funcionalidades. Ele precisa demonstrar o que pode ser implementado, em qual ordem e com quais condições de conclusão.

### 13.2 Fases recomendadas

- **Fase 0 — Definição e preparação:** engine, linguagem e plataformas; requisitos técnicos e operacionais; consolidar decisões de game design; documentação base; mapa de dependências; estratégia de testes; riscos e pendências. Saída: projeto suficientemente especificado para iniciar uma implementação vertical.
- **Fase 1 — Fundamentos técnicos:** repositório e pipeline de build; estrutura modular; persistência e identificadores; contratos entre módulos; logging e tratamento de erros; testes automatizados; autoridade do servidor para operações críticas. Saída: base técnica funcional e verificável.
- **Fase 2 — Protótipo de personagem e combate:** criar personagem; atributos base; árvore de talentos inicial; armas e habilidades básicas; cálculo de combate; cenários de teste reproduzíveis. Saída: ciclo básico de combate demonstrável.
- **Fase 3 — Inventário e equipamentos:** catálogo de itens; instâncias de equipamentos; inventário; equipar e desequipar armas; gerar atributos válidos; vincular habilidades compatíveis; persistir o estado dos itens. Saída: o jogador pode obter, guardar, equipar e utilizar armas.
- **Fase 4 — Tier e fusão:** cálculo de tier; tier de origem; seleção de atributo; fórmula de fusão; prévia de custos e resultados; custos progressivos; consumo seguro do material; testes de integridade e concorrência. Saída: ciclo de evolução de equipamentos completo, sem falhas aleatórias.
- **Fase 5 — Economia e conteúdo PvE:** fontes e custos de recursos; loot; dungeon inicial; materiais de dungeon; raid inicial; materiais de raid; validar o ciclo econômico. Saída: a progressão de itens possui fontes de recursos e objetivos de longo prazo.
- **Fase 6 — Testes integrados e balanceamento:** interação entre árvore e equipamentos; limites dos atributos; progressão de tier; economia e loot; desempenho; exploits; inconsistências; regressão. Saída: versão integrada, testada e pronta para o próximo estágio de produção.
- **Fase 7 — Preparação para lançamento:** segurança e estabilidade; persistência e recuperação; monitoramento; estratégia de atualização; testes de carga; procedimentos operacionais; critérios de lançamento. Saída: build aprovada segundo os critérios de lançamento definidos.

**Observação:** as fases são uma proposta de planejamento. A ordem e o escopo final dependem da engine, do tamanho da equipe, dos riscos técnicos e do objetivo do primeiro protótipo.

---

## 14. Método de decomposição de tarefas

### 14.1 Hierarquia

**Épico → Funcionalidade → História de usuário ou tarefa técnica → Subtarefas → Testes.**

Exemplo: Épico Progressão de equipamentos → Funcionalidade Fusão → Tarefas: cálculo de novo atributo; validar compatibilidade de armas; calcular custo; consumo do material; prévia de fusão; testes automatizados.

Uma tarefa não deve misturar várias funcionalidades independentes apenas para reduzir a quantidade de cartões.

### 14.2 Campos obrigatórios de tarefa

ID único; título objetivo; contexto; objetivo; escopo incluído; escopo excluído; referências documentais; pré-requisitos; dependências; arquivos ou módulos afetados, quando conhecidos; requisitos relacionados; critérios de aceitação; testes necessários; riscos; prioridade; estimativa ou faixa de esforço, quando possível; status; evidências de conclusão.

### 14.3 Critérios de aceitação

Os critérios deverão ser verificáveis e observáveis. Exemplo para uma tarefa de fusão:

- A operação rejeita materiais de famílias incompatíveis.
- A operação rejeita um item principal no nível +10.
- O atributo selecionado é calculado pela fórmula aprovada.
- Os demais atributos permanecem inalterados.
- As habilidades do item principal permanecem inalteradas.
- O custo é validado antes da confirmação.
- O material é consumido uma única vez.
- O tier atual é recalculado após a fusão.
- Uma falha de persistência não pode resultar em cobrança sem atualização do item.
- Existem testes para valores maiores, menores e iguais entre item principal e material.

### 14.4 Definição de pronto

Uma tarefa só poderá ser marcada como concluída quando: o escopo foi implementado; os critérios de aceitação foram verificados; os testes relevantes foram executados; não há regressões conhecidas sem registro; a documentação necessária foi atualizada; as dependências e integrações foram verificadas; as limitações restantes foram registradas; há evidência suficiente para justificar a conclusão.

Se a IA não puder executar os testes, deverá registrar essa limitação. Não poderá afirmar que os testes passaram.

---

## 15. Priorização do backlog

A IA deverá priorizar tarefas levando em conta: dependências bloqueantes; valor para o protótipo; risco técnico; impacto em outros sistemas; necessidade de validar uma hipótese importante; custo de mudança futura; criticidade para segurança e integridade; esforço estimado.

### 15.1 Classes de prioridade

- **P0 — Bloqueador:** impede o funcionamento de uma entrega ou envolve risco crítico de integridade.
- **P1 — Essencial:** necessário para o marco atual ou para o funcionamento básico do sistema.
- **P2 — Importante:** agrega valor relevante, mas não bloqueia a entrega atual.
- **P3 — Futuro:** melhoria, refinamento ou funcionalidade fora do escopo imediato.

Essas prioridades não substituem a análise de dependências. Uma tarefa P2 pode precisar ser concluída antes de uma tarefa P1 se for um pré-requisito técnico.

### 15.2 Regras de estimativa

A IA não deverá inventar estimativas exatas quando o escopo estiver incerto. Quando houver incerteza, deverá: indicar uma faixa de esforço; explicar as premissas; separar implementação de pesquisa técnica; identificar dependências externas; propor um protótipo para reduzir a incerteza; atualizar a estimativa após obter evidências.

---

## 16. Planejamento de sprints e marcos

Cada sprint ou ciclo de trabalho deverá possuir: objetivo verificável; escopo selecionado; tarefas incluídas; dependências resolvidas; critérios de sucesso; riscos e bloqueios; estratégia de validação; resultado esperado; revisão do que foi concluído; registro do que ficou pendente.

A IA não deverá preencher um calendário com prazos arbitrários apenas para produzir um roadmap visualmente completo. As datas devem ser derivadas de capacidade real, estimativas, dependências e prioridades. Quando essas informações não existirem, o planejamento deverá usar sequências e marcos relativos, identificando as datas como pendentes.

### 16.1 Critério para iniciar uma tarefa

Uma tarefa está pronta para começar quando: seu objetivo está claro; o escopo é suficientemente delimitado; as regras de negócio estão disponíveis; as dependências necessárias estão resolvidas; os critérios de aceitação podem ser definidos; não existe uma decisão pendente que impeça sua implementação segura.

Caso contrário, a IA deverá criar uma tarefa de descoberta, especificação ou resolução de decisão.

---

## 17. Estratégia de testes

### 17.1 Níveis de teste

- **Unitários:** cálculo de atributo; cálculo de tier; fórmula de fusão; validação de limites; cálculo de custos.
- **Integração:** inventário e fusão; economia e consumo de ouro; progressão de equipamentos e tier; árvore de talentos e habilidades; loot e armazenamento de itens.
- **Sistema:** fluxos completos do ponto de vista do jogador.
- **Regressão:** mudanças em uma funcionalidade não quebram regras anteriormente validadas.
- **Segurança e exploração:** duplicação de itens, repetição de transações, consumo incorreto de recursos, abuso de requisições e alterações indevidas de estado.

### 17.2 Testes obrigatórios para fusão

Atributo do material maior que o principal; menor; iguais; diferença de uma unidade; limites mínimo e máximo; material incompatível; ausência de ouro; ausência de materiais especiais; item no nível máximo; item inexistente ou pertencente a outro jogador; requisição repetida; falha de persistência; duas tentativas concorrentes de consumir o mesmo material; recálculo de tier; preservação das habilidades; preservação dos atributos não selecionados.

### 17.3 Balanceamento

O balanceamento deverá ser documentado e testado com dados e cenários reproduzíveis. A IA deverá separar: regra funcional; valor configurável; hipótese de balanceamento; resultado medido; decisão aprovada.

Não deverá apresentar uma escolha de balanceamento como verdade comprovada sem dados suficientes.

---

## 18. Registro de decisões técnicas e de design

Decisões que afetem vários sistemas deverão ser registradas em um documento de decisões.

### 18.1 Modelo

ID; título; data; status; contexto; problema a resolver; alternativas consideradas; decisão selecionada; justificativa; consequências positivas; consequências negativas; sistemas afetados; documentos e tarefas relacionados.

### 18.2 Exemplos de decisões já estabelecidas

- **DEC-001 — Separação entre atributos de personagem e equipamento.** Os atributos de equipamentos não concedem pontos de árvore e não são convertidos automaticamente nos atributos da árvore. Motivo: evitar inflação de atributos e conflitos entre progressão do personagem e progressão dos equipamentos. Consequência: combate e habilidades precisam definir claramente como os atributos próprios dos equipamentos interagem com as estatísticas do personagem.
- **DEC-002 — Fusão sem falha aleatória.** A fusão não possui chance de falha. Consequência: o balanceamento deve ocorrer por custos, disponibilidade de materiais e limites de progressão, não por destruição aleatória do progresso.
- **DEC-003 — Alteração de um atributo por fusão.** O jogador escolhe apenas um atributo para a operação. Consequência: a prévia deve mostrar o resultado específico desse atributo, e os demais atributos do item principal devem ser preservados.
- **DEC-004 — Tier calculado pelos atributos.** O tier atual depende dos valores de atributos do item. Consequência: a classificação precisa ser recalculada quando esses valores mudarem. Os limiares e a fórmula final ainda precisam de aprovação.
- **DEC-005 — Sem desmontagem de equipamentos.** Itens podem ser equipados, vendidos ou consumidos em fusões, mas não desmontados. Consequência: a economia e as fontes de materiais não devem pressupor receitas de desmontagem.

---

## 19. Controle de mudanças

### 19.1 Fluxo de mudança

1. Registrar a solicitação.
2. Identificar a regra atual.
3. Descrever a regra proposta.
4. Identificar o motivo.
5. Mapear sistemas afetados.
6. Avaliar impacto técnico e de balanceamento.
7. Avaliar compatibilidade com dados existentes.
8. Identificar testes necessários.
9. Obter aprovação quando a mudança alterar decisões de design.
10. Atualizar documentos e tarefas.
11. Implementar.
12. Validar e registrar o resultado.

### 19.2 Mudanças que exigem análise ampliada

Fórmulas de combate; atributos principais; interação entre árvore e equipamento; fórmula de fusão; limite +10; cálculo de tier; fontes de materiais; custos econômicos; regras de drop; estrutura de persistência; autoridade do servidor; compatibilidade de itens existentes.

A IA deverá identificar impactos indiretos, não apenas atualizar o documento solicitado.

---

## 20. Fluxo de trabalho da IA

1. **Entender:** resultado desejado, sistemas envolvidos, documentação relacionada, decisões aprovadas, estado atual da implementação, restrições e dependências.
2. **Investigar:** consultar documentos e código existentes. Não criar um documento novo sem verificar se a informação já possui uma fonte oficial.
3. **Planejar:** dividir o trabalho em unidades verificáveis; identificar tarefas independentes, dependências, riscos e decisões pendentes.
4. **Especificar:** regras, entradas, saídas, estados, erros e critérios de aceitação.
5. **Propor:** soluções para lacunas importantes, distinguindo proposta de aprovado.
6. **Implementar:** somente o escopo autorizado, respeitando os contratos entre sistemas.
7. **Validar:** executar testes, comparar com os critérios de aceitação, verificar regressões.
8. **Documentar:** atualizar especificações, tarefas, decisões e estado do projeto.
9. **Reportar:** o que foi feito; o que foi validado; o que não pôde ser validado; documentos atualizados; riscos; próximas tarefas.

---

## 21. Relatório de conclusão da IA

Ao terminar uma tarefa relevante: **Resumo; Implementado; Documentação atualizada; Testes** (executados, resultados e limitações); **Critérios de aceitação** (aprovado, reprovado ou não verificado); **Riscos e pendências; Próximas tarefas** (priorizadas, com dependências).

A IA não deverá afirmar que algo foi implementado se apenas produziu uma especificação, nem afirmar que algo foi testado se apenas revisou o código.

---

## 22. Definições prioritárias ainda pendentes

- **Prioridade A — Fundamentos técnicos:** engine e versão; linguagem; plataformas-alvo; arquitetura de rede; modelo de autoridade do servidor; banco de dados e persistência; ferramentas de build, versionamento e testes; hospedagem e ambientes.
- **Prioridade B — Regras centrais do jogo:** fórmulas de atributos do personagem; fórmulas de dano e mitigação; progressão por nível; quantidade e obtenção de pontos da árvore; regras detalhadas de habilidades; slots e restrições de habilidades nas armas; limites e faixas de atributos dos equipamentos.
- **Prioridade C — Itens e economia:** fórmula definitiva de tier; limiares D, C, B, A e S; tratamento de atributos ausentes e opcionais; precisão e arredondamento da fusão; valores de ouro por nível; materiais e quantidades por nível; tabelas de loot e probabilidades; regras de venda, negociação e vinculação.
- **Prioridade D — Conteúdo e operação:** estrutura de dungeons e raids; tamanho dos grupos; regras de recompensa; escalonamento de dificuldade; persistência de progresso; monitoramento e telemetria; critérios de lançamento; plano de testes de carga.

A IA deverá priorizar a resolução dos itens que bloqueiam decisões de arquitetura ou impedem a criação de critérios de aceitação objetivos.

---

## 23. Critérios de sucesso do processo de desenvolvimento

O processo será considerado saudável quando: cada sistema relevante possuir uma especificação clara; decisões aprovadas estiverem separadas de propostas; cada requisito puder ser rastreado até tarefas e testes; as tarefas possuírem critérios de aceitação verificáveis; as dependências entre sistemas estiverem documentadas; as mudanças não criarem contradições silenciosas; a implementação puder ser validada por testes; o estado real do projeto estiver atualizado; a IA conseguir retomar o trabalho sem depender de contexto implícito; o planejamento refletir o estado real do desenvolvimento.

O objetivo final é que o projeto possa ser desenvolvido incrementalmente por uma IA ou equipe humana, preservando consistência de design, integridade técnica e rastreabilidade.

---

## 24. Instrução permanente para a IA

> Você é a IA responsável por auxiliar no desenvolvimento deste MMORPG. Sua função é transformar as decisões aprovadas em documentação, arquitetura, tarefas, planejamento, implementação e testes verificáveis.
>
> Antes de propor ou implementar uma funcionalidade, consulte as regras existentes e identifique os sistemas afetados. Não invente regras definitivas para preencher lacunas. Diferencie decisões aprovadas, propostas, pendências e limitações técnicas.
>
> Preserve as decisões fundamentais do projeto, especialmente a separação entre atributos da árvore e atributos dos equipamentos, a fusão determinística sem chance de falha, a alteração de um único atributo por operação, a preservação das habilidades do item principal, o limite de fusão +10 e o cálculo de tier baseado nos atributos.
>
> Não altere silenciosamente regras de combate, economia, progressão, loot ou persistência. Se uma mudança for necessária, documente o problema, apresente alternativas e identifique os impactos.
>
> Produza tarefas pequenas, rastreáveis e testáveis. Inclua dependências, critérios de aceitação e testes. Não declare conclusão sem evidências e não declare testes aprovados quando eles não tiverem sido executados.
>
> Mantenha a documentação alinhada à implementação real. Ao concluir um trabalho, informe o que mudou, o que foi validado, quais riscos permanecem e quais são as próximas ações recomendadas.
>
> Sua prioridade é preservar a consistência global do projeto e reduzir retrabalho, não apenas concluir a solicitação imediata.

---

## 25. Próximo passo recomendado

1. Definir engine, linguagem, plataformas e arquitetura de rede.
2. Criar o mapa de sistemas e suas dependências.
3. Fechar as regras de atributos, habilidades e combate necessárias para o protótipo.
4. Formalizar as fórmulas de tier e fusão.
5. Criar um protótipo vertical com personagem, arma, inventário e fusão.
6. Validar a arquitetura com testes.
7. Expandir o backlog com base no que foi aprendido.

**Regra de encerramento:** nenhuma funcionalidade será considerada pronta apenas porque está documentada, planejada ou codificada. Ela só será considerada validada quando seu comportamento real atender aos critérios de aceitação definidos.
