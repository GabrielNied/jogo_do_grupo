# Especificação de poderes e sets (entrada de 2026-10-12)

Especificação trazida por Gabriel em 2026-10-12. Fica aqui como **entrada**, no texto original (só as listas longas foram juntadas em linha), para rastrear a origem. **A regra vale em [itens.md, Poderes e sets](../features/itens.md#poderes-e-sets)**: o que estiver lá prevalece. Tarefas: [#629](https://github.com/GabrielNied/jogo_do_grupo/issues/629) (poderes) e [#630](https://github.com/GabrielNied/jogo_do_grupo/issues/630) (sets). Diferenças de nome: o "amuleto" daqui é proposto como o Adorno do jogo; Cinto e Brinco não aparecem aqui.

---

# Especificação para IA — Equipamentos, Poderes Lendários e Sinergias de Build

## 1. Objetivo

Definir como os equipamentos além das armas influenciam o combate, modificam habilidades e criam builds por meio de poderes lendários, sinergias e bônus de conjunto.

O sistema deve permitir que diferentes combinações de equipamentos transformem a maneira como o jogador utiliza suas habilidades, sem depender exclusivamente de aumentos numéricos de atributos.

## 2. Princípio fundamental

Os equipamentos são modificadores das habilidades e dos estilos de combate.

- A arma define as habilidades-base disponíveis.
- Os demais equipamentos podem fortalecer, modificar ou conectar essas habilidades.
- Os poderes lendários criam mecânicas especiais e possibilidades de build.
- Os sets oferecem sinergias adicionais por meio da combinação de peças.
- A árvore de talentos especializa o personagem e interage com as categorias e propriedades das habilidades.

Duas builds que utilizam a mesma arma devem poder apresentar estilos de jogo significativamente diferentes devido aos equipamentos escolhidos.

## 3. Categorias de equipamento

- **3.1 Capacete** — foco sugerido: especialização de habilidades. Pode: fortalecer habilidades específicas; modificar alcance, duração ou quantidade de alvos; alterar interações com estados aplicados aos inimigos; aprimorar efeitos de habilidades existentes.
- **3.2 Peitoral** — foco sugerido: transformação de habilidades e combos. Pode: conectar duas ou mais habilidades; fazer uma habilidade preparar um efeito consumido por outra; alterar a distribuição do dano; criar mecânicas centrais para uma build.
- **3.3 Luvas** — foco sugerido: ofensiva e sequências de ataques. Pode: fortalecer combos; modificar ataques consecutivos ou carregados; interagir com acertos críticos; conceder efeitos condicionais após sequências de golpes.
- **3.4 Calças** — foco sugerido: sobrevivência e sustentação. Pode: interagir com mitigação de dano e escudos; modificar efeitos defensivos; conectar sobrevivência e contra-ataques; favorecer combates prolongados.
- **3.5 Botas** — foco sugerido: mobilidade e posicionamento. Pode: modificar esquivas e investidas; fortalecer habilidades utilizadas após movimentação; criar efeitos ao atravessar inimigos; conectar deslocamento e dano.
- **3.6 Amuleto** — foco sugerido: modificadores amplos de build. Pode: fortalecer categorias de habilidades ou elementos; alterar geração e consumo de recursos; criar sinergias entre diferentes poderes; modificar mecânicas compartilhadas por várias habilidades.
- **3.7 Anéis** — foco sugerido: gatilhos e sinergias condicionais. Podem: ativar efeitos mediante condições específicas; interagir com inimigos marcados ou debilitados; modificar recargas sob condições determinadas; complementar poderes de outros equipamentos.

Essas funções são diretrizes de design, não restrições absolutas. Um equipamento pode apresentar um poder fora de sua tendência principal quando isso produzir uma mecânica coerente.

## 4. Sistema de poderes lendários

Cada equipamento lendário poderá possuir um poder que modifica o comportamento de uma ou mais habilidades da arma.

Os poderes podem pertencer às seguintes categorias:

- **Amplificação:** aumenta o potencial de uma habilidade.
- **Transformação:** altera o funcionamento de uma habilidade.
- **Gatilho:** ativa um efeito quando determinada condição ocorre.
- **Preparação e consumo:** aplica estados que outras habilidades podem consumir.
- **Conversão:** modifica o tipo ou a distribuição de um efeito.
- **Recarga e recursos:** altera a disponibilidade de habilidades ou o consumo de recursos.
- **Mobilidade:** conecta deslocamento e combate.
- **Defesa reativa:** conecta sobrevivência e ofensiva.
- **Sinergia de grupo:** cria benefícios condicionais para aliados.

Priorizar poderes que alterem decisões de combate e criem novas combinações, evitando depender exclusivamente de bônus numéricos.

## 5. Compatibilidade entre equipamentos e habilidades

Cada poder deve declarar explicitamente quais habilidades, categorias ou propriedades ele afeta.

A especificação de um poder deverá identificar: habilidades compatíveis; condições de ativação; efeito produzido; alvos afetados; duração e limites, quando aplicáveis; interação com outros poderes; regras de acumulação; condições de encerramento; possíveis restrições de ativação.

Não assumir que um poder afeta todas as habilidades apenas por pertencer à mesma arma ou categoria.

A compatibilidade deve ser definida por regras explícitas, evitando dependências implícitas entre itens.

## 6. Exemplo de sinergia

Considere uma arma com três habilidades:

- Investida Trovejante: aproxima-se do inimigo e causa dano.
- Corte Relâmpago: causa dano e aplica uma carga elétrica.
- Explosão Elétrica: causa dano em área.

Um conjunto de equipamentos poderia produzir as seguintes interações:

- **Peitoral:** a investida marca os inimigos atingidos.
- **Luvas:** ataques consecutivos fortalecem a próxima habilidade.
- **Botas:** uma esquiva bem-sucedida fortalece a próxima investida.
- **Anel:** atingir um inimigo marcado reduz a recarga de uma habilidade.

A combinação incentiva o jogador a preparar os inimigos, executar uma sequência de ataques e finalizar com uma habilidade em área.

Este exemplo é ilustrativo. Seus valores, condições e efeitos não constituem regras definitivas de balanceamento.

## 7. Sets de equipamento

Os sets são conjuntos de peças relacionadas que concedem bônus adicionais conforme a quantidade equipada.

A estrutura poderá utilizar marcos como: duas peças, aprimoramento de uma mecânica central; quatro peças, sinergia entre habilidades; seis peças, transformação avançada de build.

Esses marcos são exemplos, não uma exigência para todos os conjuntos.

Regras:

- Cada peça mantém seus atributos e poderes individuais.
- Os bônus do set são independentes do tier dos equipamentos.
- O jogador pode combinar peças de sets diferentes.
- Os sets não devem ser obrigatórios para todas as builds.
- Os bônus devem oferecer alternativas de gameplay, evitando superioridade universal.
- O sistema deve identificar corretamente as peças equipadas e recalcular os bônus quando houver alterações no equipamento.

## 8. Integração com os sistemas existentes

**Atributos e tiers.** Os atributos numéricos dos equipamentos determinam seu tier atual conforme a fórmula de qualidade aprovada. Poderes lendários e bônus de set não entram no cálculo do tier.

**Árvore de talentos.** A árvore especializa o personagem por meio de nós, efeitos e sinergias com categorias ou propriedades das habilidades. Equipamentos não concedem pontos de árvore nem convertem seus atributos em pontos de progressão.

**Fusão.** A fusão altera somente o atributo selecionado, conforme a fórmula e os limites definidos no sistema de itens. O item principal preserva seus poderes lendários, suas habilidades e os demais atributos.

**Compatibilidade.** A categoria de equipamento define quais peças podem ser utilizadas como materiais de fusão, respeitando as regras de compatibilidade aprovadas.

## 9. Requisitos técnicos

A implementação deverá: separar definições de poderes das instâncias de equipamentos; representar poderes por configurações ou estruturas de dados adequadas; evitar regras específicas espalhadas pelo código; definir eventos, condições e efeitos de forma explícita; prevenir ciclos infinitos de ativação; controlar acumulação, duração e remoção de efeitos; garantir que poderes incompatíveis não sejam ativados indevidamente; atualizar corretamente os efeitos quando equipamentos forem equipados ou removidos; validar os efeitos no servidor, conforme a arquitetura de autoridade aprovada; permitir testes unitários e de integração dos poderes.

Não introduzir uma arquitetura de plugins, scripts ou sistemas de eventos complexos sem avaliar as necessidades reais do projeto.

## 10. Critérios de aceitação

O sistema será considerado implementado quando:

1. Equipamentos puderem modificar habilidades conforme suas regras de compatibilidade.
2. Os poderes tiverem condições de ativação explícitas.
3. As sinergias entre habilidades e equipamentos funcionarem de forma consistente.
4. Os bônus de set dependerem corretamente das peças equipadas.
5. Trocar ou remover equipamentos atualizar os efeitos ativos.
6. Poderes e bônus de set não alterarem indevidamente o cálculo de tier.
7. A fusão preservar os poderes do item principal.
8. A interação com a árvore de talentos respeitar a separação entre atributos e especialização.
9. Existirem testes para ativação, acumulação, remoção e interação entre efeitos.
10. A documentação refletir o comportamento efetivamente implementado.

## 11. Diretrizes para a IA

Antes de implementar qualquer poder ou set:

1. Consultar as especificações de armas, habilidades, combate, equipamentos e árvore de talentos.
2. Identificar dependências e regras existentes.
3. Definir o comportamento do poder e suas condições de ativação.
4. Identificar possíveis conflitos com outros efeitos.
5. Criar tarefas e critérios de aceitação.
6. Implementar somente as regras aprovadas.
7. Criar e executar os testes pertinentes.
8. Atualizar a documentação e registrar decisões pendentes.

Não inventar valores de balanceamento, probabilidades, limites ou efeitos definitivos sem aprovação.

Distinguir decisões aprovadas, propostas e pendências. Não alterar silenciosamente regras existentes.

## 12. Resultado esperado

Criar um sistema de equipamentos no qual as armas fornecem as habilidades-base, os poderes lendários modificam seu comportamento, os acessórios e armaduras criam sinergias e os sets oferecem especializações adicionais.

O objetivo é fazer com que encontrar um equipamento possa motivar o jogador a reorganizar sua build, sem tornar uma única combinação universalmente obrigatória.
