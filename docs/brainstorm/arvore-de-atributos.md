# Árvore de atributos (entrada de 2026-10-12)

Especificação trazida por Gabriel em 2026-10-12, junto com um resumo dos atributos (objetivo, cinco principais, nove secundários, árvore, relação com equipamentos, decisões e pendências). Fica aqui como **entrada**, no texto original da versão 1.1, para rastrear de onde vieram as regras. **Não é a fonte das regras:** cada parte foi levada para o lugar dela, e o que estiver lá vale sobre este texto.

| Seções | Onde vale | Status |
|---|---|---|
| 2 (princípios), 5 (atributos) e o resumo dos atributos | [personagem.md, Atributos do personagem](../features/personagem.md#atributos-do-personagem) | **Definido** (Gabriel, 2026-10-12); números pendentes ([#627](https://github.com/GabrielNied/jogo_do_grupo/issues/627)) |
| 6, 7, 9, 10, 11, 13 a 15 (árvore, efeitos por skill, perfis, redistribuição, tela, requisitos, testes) | [talentos.md](../features/talentos.md) | **Definido** na estrutura; números pendentes |
| 8 e 18 (separação da árvore e dos equipamentos, fronteiras) | [talentos.md](../features/talentos.md#separação-da-árvore-e-dos-equipamentos) e [itens.md, DEC-001](../features/itens.md#invariantes) | **Definido**; atributos próprios dos itens na [#623](https://github.com/GabrielNied/jogo_do_grupo/issues/623) |
| 7.3 (pipeline de modificadores) e fórmulas de combate | [combate.md](../features/combate.md) e [#624](https://github.com/GabrielNied/jogo_do_grupo/issues/624) | Estrutura definida; fórmulas pendentes |
| 16 e 17 (plano e backlog ATTR-001 a ATTR-015) | Épico [#621](https://github.com/GabrielNied/jogo_do_grupo/issues/621): [#622](https://github.com/GabrielNied/jogo_do_grupo/issues/622) a [#627](https://github.com/GabrielNied/jogo_do_grupo/issues/627) | A auditoria (ATTR-001) foi feita nesta entrada; as tarefas seguem o processo do repo ([ADR 0030](../decisoes/0030-processo-v2-dono-da-funcionalidade.md): um dono por fatia, do contrato à cena) |
| 19 e 24 (decisões pendentes) | [#627](https://github.com/GabrielNied/jogo_do_grupo/issues/627) | Pendente |
| 1, 12, 20 a 23, 25 e 26 (governança, modelo de dados sugerido, instruções e prompt para IA) | [AGENTS.md](../../AGENTS.md) e [agentes.md](../agentes.md) | O processo do repo continua valendo; o modelo de dados é sugestão, e os nomes finais saem do código |

Termos: "estamina" e "stamina" são o mesmo recurso, que o código ainda chama de vigor; a mana é a energia do código ([#622](https://github.com/GabrielNied/jogo_do_grupo/issues/622)). "Recursos do jogo" na redistribuição são as **coroas** ([#626](https://github.com/GabrielNied/jogo_do_grupo/issues/626)).

---

# MMORPG — Especificação de Sistema: Árvore de Atributos

**Versão:** 1.1  
**Status do documento:** Em desenvolvimento  
**Status do sistema:** Especificação funcional inicial  
**Tipo:** Documento especializado de sistema  
**Documento-pai:** MMORPG — Documento Mestre de Projeto, Implementação e Planejamento por IA  
**Público-alvo:** IA de desenvolvimento, game design, programação, arquitetura, QA e planejamento  
**Última revisão:** 12/10/2026

---

> **Regra de governança:** este documento detalha exclusivamente o sistema de árvore de atributos. As regras globais de documentação, planejamento, implementação, testes, controle de mudanças e classificação de decisões seguem o Documento Mestre. Nenhuma proposta ou exemplo numérico deste documento deve ser tratado como aprovado sem status explícito.

# 1. Objetivo, autoridade e uso do documento

## 1.1 Relação com o Documento Mestre

Este é um documento especializado, subordinado ao Documento Mestre do MMORPG. Ele aprofunda as regras funcionais e técnicas da árvore de atributos, mas não substitui as regras globais de governança, arquitetura, planejamento, segurança, qualidade ou controle de mudanças.

Em caso de conflito:
1. Verificar se existe decisão aprovada mais recente no registro de decisões do projeto.
2. Consultar o Documento Mestre e as especificações dos sistemas integrados.
3. Registrar a inconsistência; não escolher silenciosamente uma das versões.
4. Solicitar decisão ou registrar uma proposta pendente.

## 1.2 Classificação obrigatória das informações

- **APROVADO/DEFINIDO:** regra confirmada pelo projeto.
- **PROPOSTA:** solução sugerida, ainda sem aprovação.
- **PENDENTE/A DEFINIR:** decisão necessária que ainda não foi tomada.
- **BLOQUEADO:** depende de informação, sistema ou decisão externa.
- **IMPLEMENTADO:** existe implementação de software, sem implicar validação.
- **VALIDADO:** implementação testada e aprovada conforme critérios definidos.
- **DEPRECIADO:** regra substituída; manter histórico e referência para migração.

Quando o documento usar “definido” para descrever uma intenção conceitual, isso não significa que fórmulas, números ou detalhes técnicos ausentes estejam aprovados.

## 1.3 Objetivo e uso do documento

Este documento define a arquitetura funcional e técnica inicial do sistema de árvore de atributos do MMORPG. Deve servir como fonte de verdade para agentes de IA que produzam documentação complementar, decomposição de tarefas, planejamento de sprint, implementação, testes e revisão de código.

A IA deve preservar as decisões marcadas como DEFINIDO. Itens marcados como A DEFINIR não podem ser tratados como regras aprovadas; devem gerar perguntas, propostas identificadas ou decisões pendentes.

Escopo deste documento: árvore de talentos/atributos do personagem, regras de progressão, categorias de skills para aplicação de bônus, integração com equipamentos sem compartilhar escalas, perfis de build, interface, persistência, validações e plano de implementação.


# 2. Princípios e decisões consolidadas

- Não existem classes fixas. A identidade do personagem resulta da combinação de atributos, nós escolhidos, equipamentos e skills equipadas.
- A árvore é única e circular, com cinco caminhos principais e conexões híbridas.
- Os cinco atributos principais são Força, Vigor, Agilidade, Intelecto e Espírito.
- A progressão da árvore utiliza pontos próprios investidos nos nós. Os pontos da árvore não são concedidos por itens nem compartilhados com o sistema de equipamento.
- Atributos de equipamentos são independentes dos atributos e pontos da árvore. Não devem ser somados como pontos de talento, desbloquear nós automaticamente nem alterar o orçamento de pontos da árvore.
- Equipamentos podem conceder atributos próprios, skills, tier e progressão de aprimoramento por fusão até a faixa S+, conforme o sistema de itens.
- Trocar equipamento é gratuito. A troca recalcula os atributos e efeitos ativos do personagem, sem alterar a distribuição de pontos da árvore.
- Redistribuir pontos da árvore tem custo em recursos do jogo. Valor, fórmula e condições exatas ainda precisam ser definidos.
- O jogador pode salvar e reutilizar perfis de build. A associação automática entre perfil e equipamento é opcional e não deve ser presumida como obrigatória.
- Nós grandes aplicam efeitos condicionais conforme propriedades/categorias da skill executada, não simplesmente pela categoria da arma.
- Não serão criadas ramificações exclusivas de árvore para equipamentos especiais na arquitetura inicial.
- Novos nós e skills devem ser adicionáveis por dados/configuração sempre que possível, evitando lógica individual codificada para cada item.

# 3. Objetivos e não objetivos

## Objetivos

- Permitir especialização livre sem classes predefinidas.
- Separar a economia de progressão da árvore da economia de poder dos equipamentos.
- Aplicar bônus de nós com regras explícitas, consistentes e testáveis.
- Permitir que novas skills sejam classificadas e recebam os bônus compatíveis sem reescrever a árvore.
- Dar ao jogador visibilidade sobre custo, requisitos, efeito atual e motivo de um efeito não estar ativo.
## Fora do escopo inicial

- Balanceamento final de números, fórmulas definitivas de dano e economia.
- Árvore exclusiva por arma, classe ou equipamento.
- Ramificações de talento desbloqueadas por itens.
- Definição completa do sistema de loot, tier, fusão e geração aleatória de skills; este documento apenas define fronteiras de integração.
- Implementação de PvP, raids ou dungeons, exceto quando forem necessários para testar os atributos.

# 4. Vocabulário do domínio

| Termo | Definição |
| --- | --- |
| Árvore | Grafo de nós conectados que representa a progressão do personagem. |
| Nó | Unidade selecionável da árvore com custo, requisitos, níveis e efeito. |
| Ponto de árvore | Unidade exclusiva usada para comprar/evoluir nós. |
| Atributo principal | Força, Vigor, Agilidade, Intelecto ou Espírito. |
| Atributo secundário | Estatística derivada ou de combate, como crítico, regeneração, velocidade ou penetração. |
| Skill | Habilidade executável, disponibilizada pelo conjunto de equipamentos ou por outro sistema autorizado. |
| Categoria de skill | Propriedade usada para decidir compatibilidade de bônus, como dano físico, dano mágico, cura ou utilidade. |
| Perfil de build | Distribuição salva de pontos da árvore, com metadados opcionais de equipamento. |
| Efeito ativo | Bônus cujos requisitos estão satisfeitos no estado atual do personagem. |
| Tier de item | Classificação de qualidade/progressão do equipamento; independente da árvore. |


# 5. Atributos e responsabilidades

| Atributo principal | Responsabilidade conceitual | Observações |
| --- | --- | --- |
| Força | Contribuição ao dano físico | Fórmula final e relação com skills a definir. |
| Vigor | Vida máxima/sobrevivência | Definir se afeta apenas vida ou outros parâmetros. |
| Agilidade | Estamina máxima | Não presumir bônus automáticos de esquiva ou velocidade. |
| Intelecto | Contribuição ao dano mágico | Fórmula final e relação com skills a definir. |
| Espírito | Mana máxima | Definir efeitos adicionais somente por decisão explícita. |

Atributos secundários previstos: regeneração de vida, estamina e mana; chance crítica; dano crítico; velocidade de ataque físico; velocidade de execução/ataque mágico; penetração física; penetração mágica. É necessário definir unidades, limites, fórmulas, stacking e condições antes de lançar valores finais.

Regra contra efeitos implícitos: um atributo principal não deve conceder automaticamente vários bônus secundários sem uma decisão documentada. Cada efeito deve ter fonte e fórmula identificáveis.


# 6. Estrutura da árvore

## 6.1 Topologia

- Representação visual circular/radial, com cinco caminhos principais.
- Caminhos podem se conectar por nós híbridos para permitir builds mistas.
- A estrutura lógica deve ser um grafo de nós e arestas; a posição visual não deve determinar as regras de gameplay.
- A árvore é compartilhada entre personagens. Regras de desbloqueio podem variar por estado/progressão, mas não deve haver uma cópia específica para cada arma.
## 6.2 Tipos de nós

| Tipo | Finalidade | Regras |
| --- | --- | --- |
| Principal | Concede ou aumenta atributo principal | Tem custo, nível máximo e pré-requisitos. |
| Secundário | Concede atributo secundário | Define unidade e stacking explicitamente. |
| Especialização (nó grande) | Aplica bônus condicional por propriedade de skill | Deve declarar critérios de compatibilidade e efeito por categoria. |
| Híbrido/conexão | Permite acesso entre caminhos | Pode ser nó de atributo ou requisito de rota; função precisa ser explicitada por nó. |
| Marco avançado | Representa investimento profundo ou sinergia | Custos e requisitos ainda a definir. |

## 6.3 Custos e progressão

O custo, nível máximo por nó, pontos concedidos por nível do personagem e total de pontos no endgame ainda não foram aprovados. Não hardcodar números de exemplo como regras finais. Criar configuração para custo, maxRank, requisitos e efeitos.

A compra de um nó deve validar: pontos suficientes; pré-requisitos; nível máximo; estado do personagem; e dependências de rota. A operação deve ser atômica: ou todas as alterações são salvas, ou nenhuma é aplicada.


# 7. Sistema de efeitos condicionais por skill

Princípio: o nó consulta as propriedades declaradas pela skill executada e aplica somente os modificadores compatíveis. O equipamento pode conceder a skill, mas a categoria do item não determina por si só o bônus.

## 7.1 Propriedades mínimas de skill

- skillId estável.
- Categorias de dano: física, mágica ou ambas, quando aplicável.
- Tipos de efeito: dano, cura, escudo, controle, mobilidade, utilidade, recuperação de recurso.
- Atributos de escala, se aplicável.
- Flags explícitas: pode causar crítico; recebe penetração; pode ser acelerada; pode ser modificada por bônus de cura.
- Regras de divisão de dano para skills híbridas, evitando aplicação duplicada do mesmo modificador.
## 7.2 Exemplo ilustrativo

| Nó | Categoria/propriedade reconhecida | Efeito ilustrativo |
| --- | --- | --- |
| Maestria Ofensiva | Dano físico | +10% de dano físico |
| Maestria Ofensiva | Dano mágico | +10% de dano mágico |
| Ritmo de Combate | Ataque físico acelerável | +5% de velocidade de ataque |
| Ritmo de Combate | Skill mágica acelerável | +5% de velocidade de execução |
| Sustentação | Recuperação de vida/estamina/mana | +10% de recuperação do recurso correspondente |
| Precisão Letal | Skill elegível a crítico | +5 pontos percentuais de chance crítica |

Todos os números acima são exemplos para testar o formato. Não representam valores aprovados. O balanceamento final deve configurar limites, regras de acumulação, arredondamento e interação com imunidades/resistências.

## 7.3 Resolução dos modificadores

- Usar uma pipeline única de cálculo, com fontes identificáveis: base da skill, atributos do personagem, modificadores de árvore, modificadores do equipamento, defesas do alvo e efeitos situacionais.
- Definir explicitamente se cada modificador é aditivo ou multiplicativo e a ordem de aplicação.
- Evitar que o mesmo bônus seja aplicado duas vezes por ter sido encontrado em mais de uma categoria.
- Se uma skill não possuir a propriedade exigida pelo nó, o efeito não se aplica e a interface deve explicar a incompatibilidade quando relevante.

# 8. Separação da árvore e dos equipamentos

## 8.1 Fonte de verdade

- Árvore: guarda pontos disponíveis, compras de nós, ranks e perfis de build.
- Equipamento: guarda atributos próprios, tier, aprimoramento, skills e propriedades do item.
- Personagem/combat stats: agrega fontes de acordo com uma fórmula definida, mas não converte atributos de item em pontos da árvore.
## 8.2 Regras obrigatórias

- Atributos de equipamentos não compram, removem nem aumentam ranks de nós.
- Atributos de equipamentos não alteram o orçamento de pontos da árvore.
- Trocar equipamento não exige redistribuir pontos e não tem custo de recurso.
- Trocar equipamento pode mudar skills disponíveis e atributos finais, o que pode mudar quais bônus condicionais de skill são aplicáveis.
- Tier e aprimoramento de item pertencem ao sistema de itens; não devem alterar a estrutura nem a escala de pontos da árvore.
- O sistema pode combinar efeitos em combate, mas deve preservar a origem de cada modificador para depuração e UI.

# 9. Perfis de build

O jogador pode salvar e carregar perfis de distribuição da árvore. A implementação inicial deve separar a distribuição salva da seleção de equipamentos, permitindo associar equipamento opcionalmente sem exigir que cada perfil possua itens específicos.

- Cada perfil possui ID, nome, distribuição de ranks, data de atualização e versão da configuração da árvore.
- Ao carregar um perfil, validar os nós contra a versão atual da árvore e os pontos disponíveis.
- Se um nó foi removido ou alterado por atualização, aplicar migração explícita ou apresentar uma mensagem de incompatibilidade; não descartar pontos silenciosamente.
- Definir posteriormente limite de perfis, custo para trocar perfil e restrições de uso em combate.
- Troca de equipamento não deve trocar automaticamente o perfil, a menos que o jogador ative essa opção.

# 10. Redistribuição de pontos

A redistribuição custa recursos do jogo. O tipo de recurso, fórmula de custo, custo mínimo/máximo, descontos e condições de execução ainda são decisões pendentes.

Fluxo recomendado: jogador solicita redistribuição; servidor calcula custo; UI apresenta resumo; jogador confirma; servidor valida saldo e estado; árvore é recalculada; saldo e distribuição são persistidos atomicamente.

- A redistribuição não pode produzir estado inválido ou pontos negativos.
- O servidor deve validar custo e requisitos; o cliente não é autoridade.
- Definir se a redistribuição é total, parcial ou ambas. Não assumir regras ainda não aprovadas.
- Definir se redistribuição é proibida em combate. Recomendação para o primeiro protótipo: fora de combate.

# 11. Requisitos de interface/UX

- Mostrar os cinco caminhos principais e as conexões híbridas.
- Diferenciar visualmente nó bloqueado, disponível, comprado, maximizado e condicionalmente ativo.
- Ao selecionar um nó, mostrar nome, descrição, rank atual/máximo, custo, efeito por rank, requisitos e motivo de bloqueio.
- Mostrar pontos disponíveis e custo total antes da confirmação.
- Exibir separadamente atributos provenientes da árvore e atributos provenientes de equipamentos.
- Ao trocar equipamento, atualizar atributos e skills sem alterar a árvore.
- Em nós condicionais, explicar quais categorias de skill recebem o bônus.
- Fornecer comparação antes/depois ao comprar nó, redistribuir ou carregar perfil.
- Dar suporte a navegação por mouse e teclado/controle conforme os padrões gerais do jogo.
- Não depender exclusivamente de cor para comunicar estado; combinar ícone, texto e tooltip.

# 12. Modelo de dados conceitual

A estrutura abaixo é uma proposta lógica independente de engine, linguagem ou banco. Adaptar à stack do projeto após inspeção do repositório.

## 12.1 NodeDefinition

```text
nodeId: string estável\nnameKey: string\ndescriptionKey: string\nnodeType: enum\npathId: enum ou lista\nmaxRank: integer\ncostByRank: lista/configuração\nprerequisites: lista de regras\neffects: lista de efeitos tipados\nuiPosition: x/y ou coordenadas normalizadas\nconfigVersion: integer
```

## 12.2 CharacterTreeState

```text
characterId\ntreeConfigVersion\navailablePoints\npurchasedRanks: map<nodeId, rank>\nupdatedAt
```

## 12.3 SkillDefinition

```text
skillId\ndamageCategories: [physical, magical] opcional\neffectTypes: [damage, heal, shield, utility, ...]\nscalingAttributes: lista\nflags: canCrit, affectedByPenetration, canBeAccelerated, ...\nhybridDamageRules: configuração explícita
```

## 12.4 BuildProfile

```text
profileId\ncharacterId\nname\ntreeConfigVersion\npurchasedRanks\noptionalEquipmentPresetId\ncreatedAt\nupdatedAt
```

## 12.5 Modifier

```text
modifierId\nsourceType: tree | equipment | skill | status | target\nsourceId\nstatId\noperation: add | multiply | override | special\nvalue\nconditions\npriority/stackingGroup
```

Os nomes e formatos finais dependem da linguagem, engine e arquitetura já existentes. A IA deve inspecionar o repositório antes de criar classes ou migrar dados.


# 13. Requisitos funcionais

| ID | Requisito |
| --- | --- |
| FR-001 | O jogador consegue visualizar os cinco caminhos principais da árvore. |
| FR-002 | O jogador consegue inspecionar qualquer nó e seus requisitos. |
| FR-003 | O jogador consegue comprar ranks se cumprir requisitos e possuir pontos suficientes. |
| FR-004 | O sistema impede ranks acima do máximo configurado. |
| FR-005 | O sistema aplica somente efeitos compatíveis com propriedades da skill. |
| FR-006 | O sistema mantém atributos de equipamento separados dos pontos e ranks da árvore. |
| FR-007 | Trocar equipamento não altera pontos ou ranks da árvore. |
| FR-008 | O jogador consegue salvar e carregar perfis de build válidos. |
| FR-009 | A redistribuição cobra o custo configurado e valida saldo no servidor. |
| FR-010 | A UI diferencia a origem dos atributos apresentados. |
| FR-011 | O sistema registra motivo de efeitos condicionais inativos para UI e depuração. |
| FR-012 | A configuração da árvore pode ser versionada e validada. |

## 13.1 Requisitos não funcionais

- Determinismo: a mesma configuração e estado devem produzir os mesmos modificadores.
- Segurança: operações que alteram pontos, custo e perfil são validadas no servidor em ambiente multiplayer.
- Observabilidade: registrar erros de configuração, nó inválido e modificador incompatível.
- Extensibilidade: adicionar nó/skill por configuração sempre que possível.
- Desempenho: evitar recalcular a árvore completa em cada frame; recalcular em eventos de mudança relevantes e/ou cachear resultados.
- Testabilidade: lógica de progressão e cálculo de modificadores deve ser testável sem UI.
- Compatibilidade: alterações de configuração precisam de versionamento/migração.

# 14. Regras de validação e casos-limite

- Não aceitar compra de nó com pontos insuficientes.
- Não aceitar compra de nó sem pré-requisitos.
- Não permitir ciclo de pré-requisitos que torne nós inalcançáveis ou validação infinita.
- Não permitir rank menor que zero nem acima do máximo.
- Ao trocar equipamento, não apagar ranks ou perfis.
- Ao carregar perfil antigo, validar nós inexistentes e versão de configuração.
- Skills híbridas devem declarar divisão de dano ou regras de aplicação sem duplicação.
- Se vários modificadores se aplicam, respeitar regras de stacking declaradas; nunca assumir soma/multiplicação implicitamente.
- Se um efeito é incompatível, não aplicá-lo silenciosamente sem permitir diagnóstico.
- Falha ao persistir uma compra ou redistribuição deve reverter a operação inteira.

# 15. Estratégia de testes

## Testes unitários

- Compra de nó válido, inválido, bloqueado e maximizado.
- Cálculo de pontos gastos e disponíveis.
- Aplicação de bônus físico, mágico, cura e recuperação.
- Skills híbridas sem duplicação de modificadores.
- Stacking aditivo/multiplicativo conforme configuração.
- Cálculo de custo e falha por saldo insuficiente na redistribuição.
## Testes de integração

- Troca de equipamento não altera a árvore.
- Troca de skill muda somente os bônus condicionais pertinentes.
- Carregar perfil restaura ranks válidos.
- Salvar/carregar estado persiste após reconexão.
- Versão nova da árvore migra ou sinaliza incompatibilidades corretamente.
## Testes de interface

- Nós bloqueados explicam o motivo.
- Pontos, custos e preview refletem o estado real.
- Atributos da árvore e do equipamento são visualmente separados.
- A interface atualiza após equipar/desequipar item.
## Critério de qualidade

A lógica central deve ter testes automatizados cobrindo os caminhos válidos e inválidos. Os percentuais de cobertura devem seguir o padrão já utilizado pelo projeto, ou ser definidos após inspeção do repositório.


# 16. Planejamento de implementação

O plano abaixo organiza dependências; não estima duração. A IA deve primeiro inspecionar stack, repositório, padrões de arquitetura e testes para estimar esforço real.

## Fase 0 — Descoberta técnica

- Identificar engine, linguagem, arquitetura cliente/servidor, sistema de atributos atual, sistema de itens/skills e persistência.
- Localizar convenções de dados, eventos, UI, testes e versionamento.
- Produzir relatório de lacunas e proposta de integração sem alterar código.
## Fase 1 — Domínio e configuração

- Definir modelos de nó, pré-requisitos, efeitos, skills e estado do personagem.
- Criar configuração inicial mínima da árvore.
- Criar validador de configuração para IDs duplicados, referências inexistentes, custos inválidos e ciclos.
## Fase 2 — Motor de progressão

- Implementar compra/evolução de nó, orçamento de pontos, requisitos e estado válido.
- Implementar redistribuição com custo configurável e operação atômica.
- Criar testes unitários do domínio.
## Fase 3 — Motor de modificadores

- Implementar categorias/propriedades de skill.
- Implementar pipeline determinística de modificadores e stacking.
- Integrar fontes de árvore, equipamento, skill e alvo sem misturar suas escalas.
- Adicionar testes de skills físicas, mágicas, híbridas e cura.
## Fase 4 — Persistência e perfis

- Persistir estado da árvore e perfis.
- Adicionar versionamento e estratégia de migração.
- Validar operações no servidor quando aplicável.
## Fase 5 — Interface

- Construir visualização radial e painel de detalhes do nó.
- Adicionar compra, preview, redistribuição e perfis.
- Exibir origem dos atributos e efeitos condicionais.
## Fase 6 — Integração e balanceamento inicial

- Integrar com equipamento e skills reais.
- Validar troca de equipamento sem alteração da árvore.
- Configurar valores de teste e executar testes de regressão.
- Revisar legibilidade, desempenho, acessibilidade e telemetria.

# 17. Backlog inicial de tarefas

| ID | Tarefa | Dependência | Entrega verificável |
| --- | --- | --- | --- |
| ATTR-001 | Auditar repositório e arquitetura atual | Nenhuma | Relatório técnico com arquivos e padrões relevantes |
| ATTR-002 | Definir schema de configuração da árvore | ATTR-001 | Schema documentado e validado |
| ATTR-003 | Criar validador de configuração | ATTR-002 | Testes para IDs, requisitos, custos e ciclos |
| ATTR-004 | Implementar estado e compra de nós | ATTR-002 | API/domínio com testes unitários |
| ATTR-005 | Implementar regras de pré-requisito | ATTR-004 | Casos válidos e inválidos testados |
| ATTR-006 | Definir schema de propriedades das skills | ATTR-001 | Categorias e flags documentadas |
| ATTR-007 | Implementar pipeline de modificadores | ATTR-006 | Resultados determinísticos e testes de stacking |
| ATTR-008 | Integrar árvore e equipamento sem compartilhar pontos | ATTR-004, ATTR-007 | Teste de troca de item sem mutação da árvore |
| ATTR-009 | Implementar redistribuição paga | ATTR-004 | Custo validado e operação atômica |
| ATTR-010 | Implementar persistência/versionamento | ATTR-004 | Salvar, carregar e migrar estado |
| ATTR-011 | Implementar perfis de build | ATTR-010 | Salvar/carregar perfil válido |
| ATTR-012 | Criar interface da árvore | ATTR-004, ATTR-007 | Navegação, detalhe e estados dos nós |
| ATTR-013 | Adicionar preview e mensagens de requisitos | ATTR-012 | UI explica efeitos e bloqueios |
| ATTR-014 | Executar testes de integração e regressão | ATTR-008 a ATTR-013 | Relatório de resultados e defeitos |
| ATTR-015 | Definir valores de balanceamento inicial | ATTR-014 | Tabela aprovada de custos, ranks e efeitos |

Cada tarefa deve ser convertida em ticket com: contexto, objetivo, escopo, fora de escopo, dependências, arquivos prováveis após auditoria, critérios de aceitação, testes, riscos e definição de pronto. Não inventar caminhos de arquivo antes de inspecionar o repositório.


# 18. Critérios de aceitação do sistema

- É possível construir e validar a árvore por configuração, sem codificar posições visuais como regras de gameplay.
- O personagem só compra nós para os quais possui pontos e cumpre pré-requisitos.
- Os pontos e ranks da árvore permanecem iguais ao trocar qualquer equipamento.
- Os atributos próprios dos equipamentos não alteram pontos disponíveis, ranks ou desbloqueios da árvore.
- Bônus condicionais dependem das propriedades declaradas da skill executada.
- Skills híbridas têm regra explícita e não recebem o mesmo bônus duas vezes.
- A redistribuição cobra o custo configurado e não pode deixar o estado parcialmente atualizado.
- Perfis válidos podem ser salvos e carregados; perfis incompatíveis são tratados de forma explícita.
- A UI separa origem dos atributos e apresenta requisitos/custos corretamente.
- Testes automatizados cobrem regras críticas de progressão, cálculo e integração.

# 19. Decisões pendentes

| Decisão | Estado | Orientação para IA |
| --- | --- | --- |
| Pontos ganhos por nível/marco | A definir | Propor opções e solicitar aprovação; não fixar silenciosamente. |
| Total máximo de pontos | A definir | Avaliar progressão inicial e endgame. |
| Custo e ranks de cada nó | A definir | Usar valores de teste configuráveis. |
| Fórmulas exatas dos atributos | A definir | Não presumir escalas finais. |
| Custo da redistribuição | A definir | Criar interface/configuração; não inventar economia final. |
| Restrições da redistribuição | A definir | Recomendar fora de combate para protótipo, sujeito a aprovação. |
| Quantidade de perfis | A definir | Tornar configurável. |
| Regras de stacking | A definir por tipo de modificador | Documentar aditivo, multiplicativo, prioridade e limites. |
| Categorias finais de skills | A definir com combate | Criar enumeração extensível e exemplos. |
| Fórmula de dano e mitigação | A definir no sistema de combate | A árvore deve integrar por contrato, não duplicar fórmulas. |
| Versão e migração de árvore | A definir tecnicamente | Usar versionamento desde o primeiro schema. |


# 20. Instruções para agentes de IA

## 20.1 Antes de implementar

- Inspecione o repositório, stack, arquitetura e convenções existentes antes de propor arquivos ou código.
- Leia a documentação relacionada a combate, itens, skills, personagem, persistência e UI.
- Identifique conflitos entre este documento e o código existente; reporte-os antes de mudanças estruturais.
- Converta as decisões pendentes em perguntas ou propostas numeradas.
## 20.2 Durante a implementação

- Implemente uma fase por vez e mantenha mudanças pequenas e revisáveis.
- Não misture pontos da árvore com atributos de equipamentos.
- Não codifique valores ilustrativos como balanceamento final.
- Não crie ramificações de talento exclusivas por equipamento sem aprovação explícita.
- Adicione testes para cada regra de negócio nova.
- Atualize a documentação quando uma decisão aprovada mudar o comportamento.
- Não substitua sistemas existentes sem explicar impacto, migração e alternativa.
## 20.3 Ao gerar documentação e tickets

- Use IDs estáveis para requisitos e tarefas (FR-xxx, ATTR-xxx).
- Toda tarefa deve ter objetivo, escopo, dependências, critérios de aceitação e testes.
- Separe fato aprovado, hipótese, proposta e decisão pendente.
- Não invente duração de sprint, nomes de arquivos, APIs, schemas de banco ou capacidades da engine sem evidência.
- Ao planejar, respeite dependências e apresente entregas verificáveis.
## 20.4 Definição de pronto

- Requisitos atendidos e critérios de aceitação verificados.
- Testes relevantes executados e resultados reportados.
- Sem regressões conhecidas nos sistemas de equipamento, skill e combate.
- Persistência e migração verificadas quando aplicáveis.
- Documentação e tarefas atualizadas.
- Decisões pendentes e riscos explicitados.

# 21. Prompt reutilizável para planejamento por IA

Use o prompt abaixo ao solicitar a uma IA que transforme esta especificação em documentação, tarefas ou um plano de implementação.

```text
Você é um engenheiro de software sênior responsável por implementar o sistema de Árvore de Atributos de um MMORPG. Leia a especificação integral e inspecione o repositório antes de propor mudanças.\n\nPreserve as decisões marcadas como definidas. Não invente decisões pendentes; apresente opções e solicite aprovação quando a escolha afetar arquitetura, balanceamento, economia ou experiência do jogador.\n\nMantenha árvore de talentos e equipamentos em escalas independentes: atributos de itens nunca alteram pontos, ranks ou orçamento da árvore. A árvore aplica bônus condicionais a partir das propriedades declaradas das skills, não apenas da categoria da arma. Trocar equipamento é gratuito e não altera a árvore. Redistribuir pontos tem custo configurável.\n\nPrimeiro entregue: (1) resumo da arquitetura atual do repositório; (2) lacunas e conflitos; (3) proposta de arquitetura compatível com os padrões existentes; (4) decisões pendentes; (5) backlog ordenado por dependência; (6) critérios de aceitação e estratégia de testes. Não implemente antes de separar e aprovar decisões de alto impacto.\n\nPara cada tarefa, informe ID, objetivo, contexto, escopo, fora de escopo, dependências, critérios de aceitação, testes, riscos e definição de pronto. Evite tarefas vagas. Não estime prazos sem evidências sobre a stack e a capacidade da equipe. Atualize a documentação afetada ao implementar mudanças.
```


# 22. Histórico e governança

- Versão 1.0: consolida a árvore circular com cinco atributos principais, nós condicionais por skill, separação de escalas de equipamento e árvore, troca de equipamento gratuita, redistribuição paga e perfis de build.
- Toda alteração de regra deve registrar: decisão, motivo, sistemas afetados, migração necessária, data/versão e responsável pela aprovação.
- Este documento é uma especificação funcional inicial. O design técnico final deve ser adaptado à engine, linguagem, arquitetura de rede e padrões do repositório após a fase de descoberta.


# 18. Integrações e fronteiras entre sistemas

## 18.1 Sistemas produtores e consumidores

| Sistema | Relação com a árvore | Contrato mínimo |
|---|---|---|
| Personagem/progressão | Fornece personagem, nível e pontos disponíveis | Identidade estável, saldo e eventos de progressão |
| Equipamentos | Fornece atributos próprios e skills disponíveis | Itens não concedem pontos de árvore nem desbloqueiam nós automaticamente |
| Skills/combate | Consome atributos e bônus condicionais | Skills possuem categorias/propriedades consultáveis |
| Persistência | Armazena distribuição e perfis | Salvamento consistente e validação no servidor |
| Interface | Exibe estado e envia intenções do jogador | Não é autoridade para validar compras ou redistribuições |
| Economia | Pode cobrar recursos por redistribuição | Custo e transação definidos pelo sistema econômico |

## 18.2 Regras de fronteira

- A árvore não deve conter cópias de fórmulas pertencentes ao sistema de combate; deve consumir contratos definidos por ele.
- O sistema de equipamentos não deve modificar diretamente pontos, nós comprados ou saldo de pontos da árvore.
- Alterações de equipamento devem provocar recálculo dos efeitos dependentes do estado atual, sem alterar a distribuição salva.
- O servidor é a autoridade para compras de nós, redistribuição, carregamento de perfil e efeitos persistentes.
- Se uma dependência ainda não possuir contrato técnico aprovado, marcar a integração como **BLOQUEADA** ou **PENDENTE**, em vez de inventar uma API definitiva.

# 19. Requisitos de observabilidade e diagnóstico

A implementação deverá permitir diagnosticar por que um nó ou efeito está ativo/inativo. A solução técnica exata depende da arquitetura final, mas deve ser possível identificar:

- Personagem e versão da distribuição consultada.
- Nó e nível do nó.
- Requisitos avaliados e resultado de cada requisito.
- Categorias/propriedades da skill que participaram da avaliação.
- Efeitos aplicados e origem do modificador.
- Motivo de rejeição de uma compra ou redistribuição.
- Erros de persistência, conflitos de versão e tentativas repetidas.

Não registrar segredos, credenciais ou dados pessoais desnecessários. Os logs devem respeitar a política de privacidade e retenção definida pelo projeto.

# 20. Migração, versionamento e compatibilidade

- A distribuição salva deve incluir uma versão de esquema.
- Mudanças em identificadores de nós, custos, pré-requisitos ou efeitos precisam de plano de migração.
- Remover ou renomear um nó não pode apagar silenciosamente pontos investidos.
- Quando uma mudança tornar uma distribuição inválida, definir explicitamente uma estratégia aprovada: migração automática, reembolso, redefinição ou bloqueio para revisão.
- Perfis salvos devem ser validados novamente ao serem carregados; dados antigos ou incompatíveis não podem ser aplicados sem validação.
- A política de migração e reembolso permanece **PENDENTE** até decisão formal.

# 21. Segurança, integridade e concorrência

- Compras, remoções e redistribuições devem ser validadas no servidor.
- A operação deve verificar saldo de pontos, custo, requisitos, pré-requisitos, limites de nível e versão atual da distribuição.
- Operações repetidas por retry não podem cobrar ou conceder pontos duas vezes.
- Atualizações concorrentes devem usar controle de versão, transação ou mecanismo equivalente da arquitetura escolhida.
- O cliente não pode informar livremente o efeito final de um nó; deve enviar a intenção e receber o resultado validado.
- As regras concretas de autenticação, banco, transação e rede dependem das decisões técnicas globais do projeto.

# 22. Matriz de rastreabilidade

Cada requisito deve ligar regra, tarefa, teste e estado de implementação. Os identificadores já definidos neste documento devem ser mantidos estáveis.

| Elemento | Convenção |
|---|---|
| Requisito | `ATTR-REQ-###` ou identificador já existente no backlog |
| Tarefa | ID do backlog global do projeto, referenciando o requisito |
| Teste | `ATTR-TST-###` |
| Decisão | ID do registro de decisão do projeto |
| Bug/incidente | ID do rastreador global do projeto |

Não renumerar IDs já usados em tarefas, commits, testes ou documentação. Se a convenção global do Documento Mestre for diferente, adotar a convenção global e manter mapeamento explícito.

# 23. Checklist para implementação por IA

Antes de implementar:
- [ ] Ler o Documento Mestre e as especificações dos sistemas integrados.
- [ ] Confirmar o status de cada regra necessária.
- [ ] Listar decisões pendentes que bloqueiam a tarefa.
- [ ] Confirmar contratos de dados e limites de autoridade cliente/servidor.
- [ ] Vincular a tarefa aos requisitos e critérios de aceitação.

Durante a implementação:
- [ ] Implementar apenas regras aprovadas ou explicitamente autorizadas.
- [ ] Não inventar fórmulas, custos, limites ou efeitos finais.
- [ ] Manter lógica de nós orientada a dados quando viável.
- [ ] Validar operações persistentes no servidor.
- [ ] Criar testes para fluxo normal, limites, falhas e repetição de requisições.

Antes de concluir:
- [ ] Executar os testes relevantes.
- [ ] Atualizar status para IMPLEMENTADO e somente para VALIDADO quando houver evidência.
- [ ] Registrar arquivos, APIs, migrações e decisões alteradas.
- [ ] Atualizar backlog, dependências e riscos.
- [ ] Documentar pendências remanescentes sem declará-las resolvidas.

# 24. Decisões pendentes prioritárias

Os itens abaixo não devem ser definidos implicitamente pela IA:

1. Fórmula final de conversão dos cinco atributos principais em parâmetros de combate.
2. Como e quando o personagem recebe pontos da árvore.
3. Limite total de pontos e relação com nível do personagem.
4. Quantidade inicial de nós, custos por nível e regras exatas de desbloqueio.
5. Valores, unidades, limites e stacking dos atributos secundários.
6. Lista final de categorias/propriedades de skills e compatibilidade com efeitos.
7. Fórmulas de efeitos condicionais dos nós grandes.
8. Custo, restrições, cooldown e condições para redistribuição.
9. Limites, custo e política de armazenamento dos perfis de build.
10. Contratos técnicos definitivos entre árvore, personagem, skills, combate e persistência.
11. Regras de migração quando nós, custos ou efeitos forem alterados.
12. Critérios de balanceamento e telemetria após testes de jogo.

Cada decisão deve receber um ID, responsável, estado, justificativa, sistemas impactados e data de revisão no registro global do projeto.

# 25. Histórico de versões

| Versão | Data | Alteração |
|---|---|---|
| 1.0 | 12/10/2026 | Primeira especificação detalhada do sistema de árvore de atributos. |
| 1.1 | 12/10/2026 | Alinhamento com a governança do Documento Mestre; inclusão de fronteiras de integração, observabilidade, migração, segurança, rastreabilidade e checklist de implementação por IA. |

# 26. Referência cruzada

Este documento deve ser mantido junto ao **MMORPG — Documento Mestre de Projeto, Implementação e Planejamento por IA**. O documento mestre é responsável pela governança global, metodologia de trabalho, organização da documentação, planejamento e critérios transversais. Esta especificação é responsável pelas regras detalhadas da árvore de atributos.

Em caso de divergência, registrar a inconsistência e solicitar resolução explícita; não corrigir uma fonte silenciosamente.
