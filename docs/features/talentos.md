# Árvore de atributos (talentos)

**Responsável:**
**Status:** em definição. Estrutura decidida por Gabriel em 2026-10-12, números pendentes. **Prioridade alta**, épico [#621](https://github.com/GabrielNied/jogo_do_grupo/issues/621)
**Faixa de IDs de mensagem:** 5000–5999

## Resumo

A árvore de atributos é a progressão do personagem. É uma árvore circular com cinco caminhos (Força, Vigor, Agilidade, Intelecto e Espírito), ligados por conexões híbridas. O jogador investe pontos próprios da árvore em nós. O que cada atributo faz está em [personagem.md, Atributos do personagem](personagem.md#atributos-do-personagem); esta página cuida da árvore: nós, pontos, compra, redistribuição e perfis.

Vem da especificação trazida por Gabriel em 2026-10-12 (entrada em [arvore-de-atributos.md](../brainstorm/arvore-de-atributos.md)).

## Definido

Decidido por Gabriel em 2026-10-12:

- **Sem classes fixas.** A especialização vem da combinação de atributos investidos, nós de especialização, equipamentos e skills usadas.
- **Uma árvore só, circular**, com cinco caminhos principais e conexões híbridas para builds mistas. Não há árvore por arma, por classe nem por equipamento, e nenhum item abre ramo exclusivo.
- **Pontos próprios, só por nível.** A árvore usa pontos da árvore, obtidos exclusivamente ao subir de nível do personagem. Nenhum item concede ponto, e melhorar equipamento aumenta só os atributos do próprio item.
- **Três tipos de nó:** pequeno (aumenta um atributo principal ou secundário), grande (especialização com bônus condicional pelas propriedades da skill usada) e avançado (sinergia entre atributos, especializações ou caminhos).
- **Bônus pela skill, não pela arma.** O nó grande olha as propriedades declaradas da skill executada (dano físico, dano mágico, cura...), não o tipo da arma equipada.
- **Trocar equipamento é grátis** e não mexe na árvore: recalcula os atributos finais e as skills, sem redistribuir pontos.
- **Redistribuir pontos custa** recursos do jogo. O valor e as condições estão pendentes.
- **Perfis de build:** o jogador salva e carrega distribuições. Ligar um perfil a um conjunto de equipamentos é opcional e não deve ser presumido.
- **Novos nós e skills entram por dados** (configuração), sem código próprio para cada item.
- ~~Adiada (2026-10-10): o jogo começa sem árvore de talentos~~ Substituído em 2026-10-12: a árvore volta, com prioridade alta.
- ~~Habilidades são pontos de talento~~ Substituído em 2026-10-12: a árvore distribui atributos e especializações. Skills continuam vindo dos itens ([skills.md](skills.md)) e a árvore não ensina skill.

## Em aberto

- [x] ~~A árvore de talentos continua existindo, com o jogo sem classes e com skills nos itens?~~ Decidido em 2026-10-12: sim, como árvore de atributos. Ela não ensina skill; dá atributos e especializações.
- [x] ~~Progressão por tipo de equipamento (Destiny Board do Albion)?~~ Decidido em 2026-10-12: não. Uma árvore só, e o bônus olha a skill, não a arma.
- [x] ~~Dá para resetar a árvore? Qual o custo?~~ Decidido em 2026-10-12: dá, com custo em recursos. Os números estão em [Pendente](#pendente).
- [x] ~~Documento mestre x adiada x fatia (#618)~~ Decidido em 2026-10-12 por esta especificação: a árvore volta e DEC-001 vale ([itens](itens.md#invariantes)).
- [x] ~~Proposta da IA na #194 (1 ponto por nível a partir do 10, reset grátis)~~ Substituída em 2026-10-12: redistribuir custa. Pontos por nível seguem em [Pendente](#pendente).
- [ ] O livro de Asterra ([seções 04, 09 e 10](../brainstorm/figjam.md#09--personagem-cooperação-e-rotina)) pede que a evolução do personagem não ensine skills nem mude o orçamento de preparação. A árvore respeita: não ensina skill. Se algum nó for mexer no orçamento de preparação ([skills.md](skills.md#preparação-de-skills-proposta-do-board-2026-10-11)), é decisão explícita.

### Pendente

Números e políticas que precisam de aprovação ([#627](https://github.com/GabrielNied/jogo_do_grupo/issues/627)). Até lá, quem implementa usa **valores de teste** em constantes nomeadas e não os trata como balanceamento:

- Quantos pontos da árvore cada nível dá e o total no nível 100 (a fonte já está decidida: só subir de nível).
- Quantidade de nós, custo por rank, rank máximo, requisitos e limites.
- Efeitos e valores dos nós grandes e avançados; lista final de categorias de skill.
- Regras de acúmulo de cada tipo de modificador (soma, multiplicação, prioridade, limite).
- Custo, restrições e recarga da redistribuição; se é total, parcial ou as duas.
- Limite, custo e restrições de uso dos perfis.
- O que fazer quando a árvore mudar e uma distribuição salva deixar de valer: migrar, reembolsar, zerar ou bloquear para revisão.

- [ ] **Documento mestre (2026-10-12):** o [documento mestre](../brainstorm/documento-mestre.md#6-sistema-de-personagem-e-árvore-de-atributos) traz uma árvore com cinco caminhos (Força = dano físico, Vigor = vida, Agilidade = stamina, Intelecto = dano mágico, Espírito = mana), nós pequenos, grandes e avançados, redistribuição com custo e perfis de build, e separa esses atributos dos atributos dos equipamentos (DEC-001). Conflita com a adiada acima e com a [fatia, seção 1](fatia-jogavel.md#1-atributos-e-recursos). A adiada continua até Gabriel decidir na [#618](https://github.com/GabrielNied/jogo_do_grupo/issues/618) (e na [#194](https://github.com/GabrielNied/jogo_do_grupo/issues/194), se a árvore voltar).

## Regras

Quem decide: **servidor**. O client envia a intenção (comprar, redistribuir, carregar perfil) e mostra o resultado.

### Estrutura

- A árvore é um grafo de nós e arestas. A posição na tela serve só à interface e não decide regra de jogo.
- Cada nó tem id estável, tipo, caminho (ou caminhos, no híbrido), rank máximo, custo por rank, pré-requisitos e efeitos tipados.
- A configuração tem **versão**. Ao subir, o servidor valida: id repetido, referência a nó inexistente, custo inválido e ciclo de pré-requisito. Configuração inválida não sobe.

| Tipo de nó | Para que serve | Regra |
|---|---|---|
| Pequeno (principal) | Aumenta um atributo principal | Custo, rank máximo e pré-requisitos |
| Pequeno (secundário) | Aumenta um atributo secundário | Declara unidade e acúmulo |
| Grande (especialização) | Bônus condicional pela propriedade da skill | Declara as categorias compatíveis e o efeito por categoria |
| Híbrido (conexão) | Liga dois caminhos | Diz se é nó de atributo ou só requisito de rota |
| Avançado (marco) | Investimento profundo ou sinergia | Custos e requisitos pendentes |

### Compra de nó

- Valida pontos suficientes, pré-requisitos, rank máximo, estado do personagem e versão atual da distribuição.
- É atômica: ou tudo é salvo, ou nada muda. Pedido repetido (retry) não cobra nem concede duas vezes.
- Rank nunca fica negativo nem passa do máximo.

### Efeitos condicionais por skill

- O nó grande consulta as propriedades declaradas da skill executada e aplica só os modificadores compatíveis. O equipamento pode dar a skill, mas a categoria do item não decide o bônus.
- Propriedades mínimas da skill: id estável; categoria de dano (física, mágica ou ambas); tipo de efeito (dano, cura, escudo, controle, mobilidade, utilidade, recuperação de recurso); atributos de escala; flags "pode critar", "recebe penetração", "pode ser acelerada" e "recebe bônus de cura". Skill híbrida declara como divide o dano, para não receber o mesmo bônus duas vezes.
- Uma **pipeline única** calcula o resultado, com a origem de cada parcela: base da skill, atributos do personagem, árvore, equipamento, defesas do alvo e efeitos. Cada modificador declara se soma ou multiplica e em que ordem entra; nada é somado ou multiplicado por suposição. A fórmula de dano e de mitigação é do [Combate](combate.md); a árvore usa o contrato, sem copiar fórmula.
- Se a skill não tem a propriedade que o nó pede, o efeito não se aplica, e o motivo fica disponível para a interface e para diagnóstico.
- Exemplo só para testar o formato, **sem valor aprovado**: "Maestria ofensiva" dá +10% de dano físico a skill de dano físico e +10% de dano mágico a skill de dano mágico.

### Separação da árvore e dos equipamentos

- **Árvore:** guarda pontos disponíveis, nós comprados, ranks e perfis.
- **Equipamento:** guarda atributos próprios, tier, fusão, skills e propriedades do item ([itens](itens.md)).
- **Combate:** junta as fontes por fórmula, sem converter atributo de item em ponto da árvore.
- Atributo de equipamento não compra, não remove e não sobe rank de nó, não muda o orçamento de pontos e não abre nó. Tier e fusão não mudam a estrutura nem a escala da árvore.
- Trocar equipamento pode mudar as skills disponíveis e, com isso, quais bônus condicionais estão ativos.
- Poderes lendários e sets ([itens](itens.md#poderes-e-sets), [#629](https://github.com/GabrielNied/jogo_do_grupo/issues/629) e [#630](https://github.com/GabrielNied/jogo_do_grupo/issues/630)) também mudam as skills, mas são do equipamento: não dão ponto da árvore, não abrem nó e não viram atributo do personagem. Os nós da árvore leem as categorias e propriedades da skill já modificada, do mesmo jeito que leem as de uma skill-base.

### Redistribuição e perfis

- **Redistribuição:** o jogador pede; o servidor calcula o custo; a tela mostra o resumo e o antes e depois; o jogador confirma; o servidor valida saldo e estado e grava a cobrança e a nova distribuição juntas. Nunca fica estado parcial nem ponto negativo. Proposta para o protótipo: só fora de combate, com custo em coroas pelo contrato de [Economia](economia.md) ([#626](https://github.com/GabrielNied/jogo_do_grupo/issues/626)).
- **Perfil:** id, nome, ranks, versão da configuração e data. Ao carregar, valida contra a versão atual e os pontos disponíveis. Nó que mudou ou sumiu gera migração explícita ou aviso de incompatibilidade; ponto nunca some em silêncio. Trocar equipamento não troca de perfil, a não ser que o jogador ligue essa opção.

### Na tela

- Os cinco caminhos e as conexões híbridas, em visão radial.
- Estados do nó: bloqueado, disponível, comprado, no máximo e condicionalmente ativo. Ícone, texto e tooltip junto com a cor, nunca só a cor.
- Ao escolher um nó: nome, descrição, rank atual e máximo, custo, efeito por rank, requisitos e motivo do bloqueio.
- Pontos disponíveis e custo total antes de confirmar; antes e depois ao comprar, redistribuir ou carregar perfil.
- Atributos da árvore e do equipamento aparecem separados. No nó condicional, quais categorias de skill recebem o bônus.
- Mouse, teclado e controle, no padrão da [interface](interface.md) e da [Lumora UI](../../design/interface/GUIA-DE-ESTILO.md).

### Requisitos

IDs estáveis da especificação; tarefas e testes apontam para eles.

| ID | Requisito |
|---|---|
| FR-001 | O jogador vê os cinco caminhos da árvore |
| FR-002 | O jogador inspeciona qualquer nó e seus requisitos |
| FR-003 | Compra rank se cumpre os requisitos e tem pontos |
| FR-004 | Nenhum rank passa do máximo configurado |
| FR-005 | Só se aplicam efeitos compatíveis com as propriedades da skill |
| FR-006 | Atributos de equipamento ficam separados dos pontos e ranks |
| FR-007 | Trocar equipamento não muda pontos nem ranks |
| FR-008 | Salvar e carregar perfis válidos |
| FR-009 | Redistribuição cobra o custo configurado e valida o saldo no servidor |
| FR-010 | A interface mostra a origem dos atributos |
| FR-011 | Efeito condicional inativo tem o motivo registrado |
| FR-012 | A configuração da árvore tem versão e é validada |

### Testes esperados

- Compra válida, sem pontos, sem pré-requisito e no máximo; pontos gastos e disponíveis.
- Validador: ciclo, id repetido, referência quebrada, custo inválido.
- Bônus físico, mágico, de cura e de recuperação; skill híbrida sem duplicar; acúmulo na ordem declarada.
- Redistribuição sem saldo (nada muda), com saldo (cobra uma vez) e em combate.
- Trocar equipamento não muda a árvore; relogar mantém a árvore; perfil antigo com nó removido é tratado.

### Tarefas

Épico [#621](https://github.com/GabrielNied/jogo_do_grupo/issues/621): [#622](https://github.com/GabrielNied/jogo_do_grupo/issues/622) nomes novos, [#623](https://github.com/GabrielNied/jogo_do_grupo/issues/623) atributos próprios dos equipamentos, [#624](https://github.com/GabrielNied/jogo_do_grupo/issues/624) secundários e pipeline, [#625](https://github.com/GabrielNied/jogo_do_grupo/issues/625) árvore e tela, [#626](https://github.com/GabrielNied/jogo_do_grupo/issues/626) redistribuição e perfis, [#627](https://github.com/GabrielNied/jogo_do_grupo/issues/627) números.

## Depende de

- **[Personagem](personagem.md):** identidade, nível e os pontos da árvore (`IPontosDistribuidos` hoje; vira o estado da árvore na #625).
- **[Skills](skills.md) e [Combate](combate.md):** propriedades das skills e a fórmula de dano e mitigação, por contrato.
- **[Economia](economia.md):** cobrança da redistribuição.

## Expõe

Ainda nada. A #625 define o contrato (configuração da árvore, estado do personagem e mensagens na faixa 5000–5999).
