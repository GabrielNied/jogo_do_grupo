# Árvore de atributos (talentos)

**Responsável:**
**Status:** estrutura decidida por Gabriel em 2026-10-12; árvore, compra e tela implementadas (#625); redistribuição paga e perfis de build implementados (#626); números da simulação e custo de redistribuição por nível implementados (#627, #910). **Gabriel aprovou os números como base de jogo** em 2026-10-16 (quiz 5); calibrar depois na Sintonia (F10) sem reabrir design. Nós condicionais usam placeholder genérico de dano de skills até fechar categorias (quiz 6). **Prioridade alta**, épico [#621](https://github.com/GabrielNied/jogo_do_grupo/issues/621)
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
- **Redistribuir pontos custa** coroas, escalando com o nível do personagem (decidido em 2026-10-16; a curva está em [Implementação da #627](#implementação-da-627-números-da-árvore-e-dos-atributos)).
- **Perfis de build:** o jogador salva e carrega distribuições. Ligar um perfil a um conjunto de equipamentos é opcional e não deve ser presumido.
- **Novos nós e skills entram por dados** (configuração), sem código próprio para cada item.
- ~~Adiada (2026-10-10): o jogo começa sem árvore de talentos~~ Substituído em 2026-10-12: a árvore volta, com prioridade alta.
- ~~Habilidades são pontos de talento~~ Substituído em 2026-10-12: a árvore distribui atributos e especializações. Skills continuam vindo dos itens ([skills.md](skills.md)) e a árvore não ensina skill.
- **Decisões do Gabriel em 2026-10-16** (quiz de definições 6): nós **grandes** e **avançados** (condicionais) usam, até existir lista final de categorias de skill, efeito placeholder **"+X% de dano de skills"** genérico (sem filtrar por categoria); depois substituir pelos bônus por propriedade/categoria declarados na skill ([skills](skills.md)). Falta implementar o placeholder e, depois, a versão por categoria.

## Em aberto

- [x] ~~A árvore de talentos continua existindo, com o jogo sem classes e com skills nos itens?~~ Decidido em 2026-10-12: sim, como árvore de atributos. Ela não ensina skill; dá atributos e especializações.
- [x] ~~Progressão por tipo de equipamento (Destiny Board do Albion)?~~ Decidido em 2026-10-12: não. Uma árvore só, e o bônus olha a skill, não a arma.
- [x] ~~Dá para resetar a árvore? Qual o custo?~~ Decidido em 2026-10-12: dá, com custo em recursos. Os números estão em [Números propostos](#números-propostos-627).
- [x] ~~Documento mestre x adiada x fatia (#618)~~ Decidido em 2026-10-12 por esta especificação: a árvore volta e DEC-001 vale ([itens](itens.md#invariantes)).
- [x] ~~Proposta da IA na #194 (1 ponto por nível a partir do 10, reset grátis)~~ Substituída em 2026-10-12: redistribuir custa. Pontos por nível: 1 (decidido em 2026-10-16).
- [x] Respondido em 2026-10-16: não há orçamento de preparação (#339 fechada), então a árvore não tem o que mexer nele. O livro de Asterra ([seções 04, 09 e 10](../brainstorm/figjam.md#09--personagem-cooperação-e-rotina)) pede que a evolução do personagem não ensine skills nem mude o orçamento de preparação. A árvore respeita: não ensina skill. Se algum nó for mexer no orçamento de preparação ([skills.md](skills.md#preparação-de-skills-proposta-do-board-2026-10-11)), é decisão explícita.

### Números propostos ([#627](https://github.com/GabrielNied/jogo_do_grupo/issues/627))

**Decidido por Gabriel em 2026-10-16:** **1 ponto por nível** (99 no nível 100); redistribuir custa **coroas, escalando com o nível** ([economia](economia.md#definido)). **Quiz 4:** o resto veio da simulação em C# ([ADR 0043](../decisoes/0043-simulacoes-em-csharp-e-scripts-em-node.md), `tools/JogoDoGrupo.Simulacoes`). **Quiz 5:** Gabriel **aprova as constantes atuais como base**; ajuste fino na Sintonia (F10), não bloqueia fechar [#627](https://github.com/GabrielNied/jogo_do_grupo/issues/627). Tudo está em constantes nomeadas (regra 17): trocar um número é mexer em `ArvoreDoJogo`, `ConfigRedistribuicao`, `RegrasRecursos`, `RegrasAtaque`, `ConfigAtributosSecundarios` ou `ConfigAtributosItem`. A tabela completa está em [Implementação da #627](#implementação-da-627-números-da-árvore-e-dos-atributos).

Continua em aberto (não trava o jogo, nenhum nó depende disso):

- [x] **Bônus por categoria de skill** nos nós grandes e avançados. Decidido em 2026-10-16 (quiz 6): placeholder **"+X% de dano de skills"** genérico até a lista final de categorias; ver Definido. **Falta implementar** placeholder e, depois, efeito por categoria.
- **Regras de acúmulo** além das que a pipeline já tem (soma na ordem da árvore; percentuais da árvore somam entre si).
- **Restrições de uso e recarga** da redistribuição além de "só fora de combate" (hoje não há recarga).

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

## Implementação da #625 (árvore, compra e tela)

Decisões de design tomadas pela IA ao implementar, sem esperar resposta (mudam por constante ou por dados, nada trava):

- **Configuração por dados.** `ConfiguracaoDaArvore` (shared) tem `Versao` e os nós: id, nome, descrição, tipo, caminhos, rank máximo, `CustoPorRank` (um custo por rank), requisitos, efeitos e posição `X`/`Y` (só para a tela). **As arestas são os requisitos** (todos valem juntos: "nó A com rank mínimo N"). Hoje a fonte é `ArvoreDoJogo.Criar()` em código; trocar por arquivo é mudar a origem da configuração em `TalentosModule`.
- **Validador** (`ValidadorDaArvore`, puro): versão menor que 1, árvore sem nós, id vazio ou repetido, nó sem nome, rank máximo menor que 1, custo por rank com tamanho diferente do rank máximo ou menor que 1, caminho inexistente (híbrido precisa de dois caminhos diferentes), nó sem efeito (menos o híbrido, que pode ser só rota), efeito com valor menor que 1 ou secundário sem chave, requisito que aponta para nó inexistente ou pede rank fora de 1..máximo, e **ciclo**. O servidor chama em `ConfigurarServicos`; configuração inválida lança `InvalidOperationException` com o motivo de cada problema e o servidor não sobe.
- ~~Árvore mínima de teste (20 nós, `ArvoreDeTeste`).~~ Substituída pela árvore final da #627 (`ArvoreDoJogo`, versão 2, 40 nós): veja [Implementação da #627](#implementação-da-627-números-da-árvore-e-dos-atributos).
- **Pontos.** O saldo é `PontosLivres` do personagem (só sobe ao subir de nível: `ConfigExperiencia.PontosPorNivel`, **1 por nível**, 99 no nível 100). Talentos gasta por `IPontosDeNivel` ([personagem.md](personagem.md)), na mesma transação da compra. `PontosGastos` guarda o que foi pago, para a redistribuição (#626).
- **Compra atômica e idempotente, sem tabela de pedidos.** O pedido (`ComprarNoRequest`) leva `RankAlvo` (o rank que o jogador viu mais um) e a versão da configuração. O servidor abre transação Serializable (no SQLite, `BEGIN IMMEDIATE`: pedidos do mesmo personagem rodam um depois do outro) e avalia com `RegrasDaArvore.AvaliarCompra`: `RankAlvo` menor que 1 ou mais de um acima do rank atual é `RankAlvoInvalido` (400); **rank atual já maior ou igual ao alvo responde sucesso sem cobrar** (pedido repetido ou concorrente); depois vêm rank máximo, requisitos e pontos (409). Só então baixa os pontos, grava o rank e confirma; qualquer recusa desfaz tudo.
- **Estado persistido:** uma linha por personagem (`TalentosArvores`: versão da configuração em que comprou e `PontosGastos`, criada na primeira compra) e uma por nó comprado (`TalentosNos`: nó e rank maior que zero). Se a versão salva for diferente da atual, a compra é recusada com `ArvoreDesatualizada` e nada se perde; migrar ou reembolsar é da #626/#627.
- **Rotas HTTP, sem mensagens de tempo real** (como Personagem; a faixa 5000–5999 segue reservada): `GET /api/personagens/{id}/talentos` devolve `ArvoreResponse` (a configuração inteira, `PontosLivres`, `PontosGastos`, `Ranks` e a soma `Atributos` da árvore) e `POST .../talentos/comprar` devolve o mesmo. Erros: `NoInvalido` e `RankAlvoInvalido` (400); `RankMaximo`, `RequisitoNaoCumprido`, `PontosInsuficientes` e `ArvoreDesatualizada` (409); 404 se o personagem não é da conta.
- **Atributos para o combate:** `IAtributosDaArvore` (Talentos publica, só no servidor) soma os efeitos de atributo principal dos ranks comprados; Combate soma isso em `AtributosCombate` no lugar do antigo `IPontosDistribuidos`. Trocar equipamento não toca em nada disto.
- **Secundários na pipeline:** o efeito de secundário (`Chave` = nome do `AtributoSecundario`, ex.: `ChanceCritica`; `Multiplica` falso soma, verdadeiro é bônus percentual) vira um modificador da fonte `Arvore` por `FonteDaArvore`, que implementa `IFonteDeModificadores` (Combate). Entra com o **total atual** (valor por rank vezes o rank), ordem `OrdemArvore`, grupo de acúmulo `arvore` (percentuais da árvore somam entre si) e origem "Árvore: nome do nó". Chave que não é um secundário recusa a configuração ao subir. A árvore de teste ainda **não tem nó de secundário** (os números são da #627); o teste usa uma árvore com um. Nós grandes e avançados (condicionais) existem só como tipo e estado na tela; nenhum nó de teste é condicional.
- **Tela** (tecla **N**, janela do Core, Lumora UI com `ComponentesVisuais`): visão radial com os cinco caminhos (nome do caminho depois do último nó) e os híbridos entre eles, arestas ligando requisito e nó (mais grossas e turquesa quando cumpridas), legenda dos estados e painel do nó. Estados com **forma, marca e texto, nunca só cor**: bloqueado `x`, disponível `+`, comprado `rank/máximo`, no máximo `MAX`, condicionalmente ativo `~` (nó pequeno é redondo; híbrido e maior são quadrados de canto suave). O painel mostra tipo, descrição, estado, rank, efeito por rank, custo do próximo rank, cada requisito (cumprido ou pendente), o **motivo do bloqueio** ("Compre antes: ...", "Você não tem pontos livres. Suba de nível para ganhar mais.", "Rank máximo."), a **prévia antes e depois** dos atributos da árvore e dos pontos livres, e o botão de comprar (desabilitado com "Comprando..." enquanto o servidor não responde). Teclado: Tab entre os nós, Enter ou Espaço compra o nó escolhido. O botão na janela do personagem fica para depois (a tecla N abre a árvore; a janela do personagem só mostra o saldo e a dica).

## Implementação da #626 (redistribuição paga e perfis de build)

- **Redistribuição Paga:**
  - Custo em coroas que **escala com o nível** do personagem (#910): `ConfigRedistribuicao.CustoEmCoroas(nivel)` = 50 + 0,5 × nível², arredondado a 10 (nível 1: 50; 10: 100; 20: 250; 50: 1.300; 100: 5.050). O servidor lê o nível por `IPontosDeNivel.ObterNivelAsync` e devolve o custo previsto em `ArvoreResponse.CustoRedistribuicaoCoroas`; o botão e a confirmação da tela só mostram esse número.
  - Consulta `IEstadoDeCombate` (ADR 0016): se o personagem estiver em combate, a ação é recusada com `EmCombate` (409 Conflict).
  - Valida se `PontosGastos > 0`: se nenhum ponto foi gasto, recusa com `NenhumPontoGasto` (400 Bad Request).
  - Valida saldo e cobra via `ICarteiraCoroas` na mesma transação atômica serializável; saldo insuficiente recusa com `SaldoInsuficiente` (409 Conflict).
  - Remove todos os `NoComprado` do personagem, zera `PontosGastos` na `ArvoreDoPersonagem` e devolve todos os pontos gastos de volta para `PontosLivres` via `IPontosDeNivel.DevolverAsync`.
  - Rota HTTP: `POST /api/personagens/{id}/talentos/redistribuir` (`RedistribuirRequest`), retornando a nova `ArvoreResponse`.
- **Perfis de Build:**
  - Salvar é grátis; **carregar um perfil que muda a distribuição custa o mesmo que redistribuir** (#627: senão o perfil seria um jeito grátis de trocar de build). Perfil igual ao que o personagem já tem não cobra. A resposta traz `CoroasCobradas`; sem saldo, recusa com `SaldoInsuficiente` (409) e nada muda.
  - Persistência na tabela `TalentosPerfis` (`Id`, `PersonagemId`, `Nome`, `Versao`, `CriadoEm`, `RanksJson`), até 5 perfis por personagem (`ConfigRedistribuicao.LimitePerfis`), com nome de até 32 caracteres (`ConfigRedistribuicao.TamanhoMaximoNomePerfil`).
  - Rotas HTTP:
    - `GET /api/personagens/{id}/talentos/perfis`: lista perfis salvos com pontos totais pré-calculados.
    - `POST /api/personagens/{id}/talentos/perfis`: salva a distribuição atual de talentos como um novo perfil (`SalvarPerfilRequest`).
    - `PUT /api/personagens/{id}/talentos/perfis/{perfilId}`: renomeia o perfil (`RenomearPerfilRequest`).
    - `DELETE /api/personagens/{id}/talentos/perfis/{perfilId}`: exclui o perfil salvo.
    - `POST /api/personagens/{id}/talentos/perfis/{perfilId}/carregar`: valida e aplica a distribuição do perfil (`CarregarPerfilResponse`).
  - **Validação e Aplicação de Perfil (`RegrasDaArvore.ValidarECalcularPerfil`):**
    - Bloqueia se o personagem estiver em combate (`EmCombate`, 409).
    - Valida o total de pontos disponíveis (`PontosLivres` atuais + `PontosGastos` atuais): se o perfil exigir mais pontos do que o total disponível, recusa com `PontosInsuficientes` (409).
    - Nós obsoletos ou excluídos da configuração são ignorados com aviso explicativo nos `Avisos`.
    - Nós com requisitos quebrados são removidos em cascata com aviso explicativo nos `Avisos`.
    - Ranks que excedam o limite máximo da configuração atual são limitados ao teto com aviso.
    - A aplicação substitui os nós em transação atômica e debita ou devolve a diferença de pontos via `IPontosDeNivel`.
- **Interface (JanelaArvore):**
  - Botão de redistribuição com custo visível e confirmação antes de disparar.
  - Painel de perfis integrado à tela de talentos: listagem com nome, pontos e data; salvar estado atual com nome; carregar perfil com exibição de avisos de migração; e exclusão de perfil com confirmação.

## Implementação da #627 (números da árvore e dos atributos)

Propostos pela simulação `tools/JogoDoGrupo.Simulacoes` ([ADR 0043](../decisoes/0043-simulacoes-em-csharp-e-scripts-em-node.md); `dotnet run --project tools/JogoDoGrupo.Simulacoes` refaz todas as tabelas) e regredidos por `NumerosDaArvoreTestes`. **Aprovados como base em 2026-10-16 (quiz 5).**

### Fórmulas dos cinco atributos (`RegrasRecursos`, `RegrasAtaque`)

Os atributos do personagem começam em 0 e só vêm da árvore (mais efeitos). O equipamento nunca soma neles (DEC-001).

| Atributo | Efeito por ponto | Valor no nível 100 de uma build focada (40 pontos) |
|---|---|---|
| Força | +1,5% no dano físico (sobre dano da arma + dano flat do equipamento) | +60% |
| Vigor | +12 de vida máxima (base 500) | +480 (quase dobra a vida-base) |
| Agilidade | +2 de stamina máxima (base 100) | +80 |
| Intelecto | +1,5% no dano mágico | +60% |
| Espírito | +2 de mana máxima (base 100) | +80 |

### Secundários

Valor final = base + árvore + equipamento, na pipeline do [Combate](combate.md#atributos-secundários-e-pipeline-624). Bases e tetos em `ConfigAtributosSecundarios`.

| Secundário | Base | Teto ou piso | Fontes (árvore · equipamento 0 a 100) |
|---|---|---|---|
| Regeneração de vida | 0 (não regenera sozinha) | sem teto | árvore +0,5/rank e +1,0/rank (grande) em Vigor · equipamento 0 a 1,5/s por peça |
| Regeneração de stamina | 10/s | sem teto | árvore +1,0/rank em Agilidade e +0,7/rank em Vigor · equipamento 0 a 1,2/s por peça |
| Regeneração de mana | 5/s | sem teto | árvore +0,7/rank em Espírito, e +15%/rank no nó grande dele · equipamento 0 a 0,8/s |
| Chance crítica | 5% | teto 75% | árvore +2%/rank (Espírito) · equipamento 0 a 3% (anel 6%) |
| Dano crítico | 1,5x | piso 1,0x | árvore +0,04/rank (Força) e +0,06/rank (grande de Força) · equipamento 0 a +0,15 (brinco +0,20) |
| Velocidade de ataque física e mágica | 1,0x | 0,25x a 3,0x | árvore +0,03/rank (Agilidade; Intelecto), +0,04 (grande de Agilidade) e +0,02 (grande de Intelecto) · equipamento 0 a +8% |
| Penetração física e mágica | 0% | teto 75% | árvore +2%/rank e +3%/rank (grande) · equipamento 0 a 6% |

A regeneração de recurso mantém o atraso de 2 s do [Combate](combate.md). Os atributos principais **não** aumentam regeneração.

### A árvore (40 nós, versão 2)

Cada caminho (Força, Vigor, Agilidade, Intelecto, Espírito) tem 7 nós, e há 5 híbridos. Constantes em `ArvoreDoJogo`.

| Nó | Por caminho | Ranks | Custo por rank | Efeito por rank | Requisito |
|---|---|---|---|---|---|
| Principal | 3 (encadeados) | 5 | 1, 1, 2, 2, 3 (9) | +2 no atributo do caminho | o anterior no rank 3 |
| Secundário | 2 | 3 | 2, 2, 3 (7) | um secundário da tabela acima | 1º/2º principal no rank 2 |
| Grande (especialização) | 1 | 3 | 4, 5, 6 (15) | +2 no atributo e 1 ou 2 secundários do tipo de dano do caminho | 3º principal no rank 3 |
| Avançado (marco) | 1 | 1 | 8 | +4 no atributo do caminho e +3 no do caminho seguinte (sinergia) | grande no rank 3 e 3º principal no rank 5 |
| Híbrido | 1 por par vizinho (5) | 4 | 2, 2, 3, 3 (10) | +1 em cada um dos dois atributos | 1º principal dos dois caminhos no rank 3 |

- **Um caminho completo custa 64 pontos; a árvore toda, 370.** Os 99 pontos do nível 100 compram 27%: fecham um caminho e meio, ou um híbrido, ou duas especializações parciais. Uma build focada chega a **40** pontos no atributo do caminho.
- Os híbridos são deliberadamente um pouco piores que o principal puro (0,8 atributo por ponto contra 1,1), mas dão dois atributos.
- Não há pontos extras de ninguém: o total de 99 é o teto de qualquer build.

### Builds no nível 100 (99 pontos)

Equipamento de referência: o melhor (valor 100 em todos os atributos elegíveis, uma peça por posição) de cada função. Ver o resto em `relatorio.md` da simulação.

| Build | Atributos da árvore | Vida | Stamina | Mana | Golpe leve sem equipamento | Golpe leve com o melhor equipamento |
|---|---|---|---|---|---|---|
| Guerreiro (Força, depois Vigor) | F 40, V 35 | 920 | 100 | 100 | 32 | 133 |
| Tanque (Vigor, depois Força) | V 40, F 32 | 980 (1.940 com equipamento de vida) | 106 | 100 | 30 | 86 |
| Ladino (Agilidade, depois Força) | A 40, F 32 | 500 | 180 | 100 | 30 | 123 (vel. de ataque 1,37x) |
| Mago (Intelecto, depois Espírito) | I 40, E 35 | 500 | 100 | 170 | 20 | 141 |
| Místico (Espírito, depois Intelecto) | E 40, I 32 | 500 | 100 | 180 | 21 | 130 (regen. de mana 13,8/s) |
| Híbrido Força e Vigor | F 34, V 34 | 908 | 100 | 100 | 30 | 125 |
| Espalhado (2 nós de cada caminho) | 20 de cada | 740 | 140 | 140 | 26 | 108 |

Leitura: a árvore dá o que o equipamento não dá (dobra a vida do Vigor, +80% de stamina ou mana, +60% de dano na build focada), e o equipamento de ponta multiplica o dano por ~4. Ninguém fica obrigado a uma build: o espalhado perde para o focado, mas não é inútil.

### Conversão dos atributos de item e dano da arma

Detalhe em [itens, Atributos dos equipamentos](itens.md#atributos-dos-equipamentos). Resumo do valor de combate no **0 e no 100** da escala do item:

| Atributo do item | No 0 | No 100 (padrão) | Exceções no 100 |
|---|---|---|---|
| Dano da arma | 20 (martelo 28, arco 16) | 50 | martelo 72, arco 46 |
| Dano físico / mágico (flat) | 0 | 5 | espada e arco 8, martelo 10, cajado 8 |
| Vida | 0 | 100 | peito 160; anel, brinco e adorno 50 |
| Defesa física / mágica | 0 | 12 | peito 18, capacete (física) 15, escudo 20 |
| Regeneração de vida / stamina / mana | 0 | 1,5 / 1,2 / 0,8 por s | |
| Chance crítica | 0 | 3% | anel 6% |
| Dano crítico | 0 | +0,15 | brinco +0,20 |
| Velocidade de ataque | 0 | +8% | |
| Penetração | 0 | 6% | |

O dano sem arma continua 20 (`ParcelasDeDano.DanoSemArma`): uma arma de qualidade 0 vale o punho; a de qualidade 100 vale 2,5x. A mitigação por defesa (`ConstanteDeMitigacao` 100) segue sendo do Combate; com esses valores o melhor conjunto físico dá 31% de redução e o de tanque, 47%.

### Custo de redistribuir e de carregar perfil (#910)

`ConfigRedistribuicao.CustoEmCoroas(nível)` = 50 + 0,5 × nível², arredondado a 10. O servidor debita esse valor, e `ArvoreResponse.CustoRedistribuicaoCoroas` é o que o botão mostra.

| Nível | 1 | 10 | 20 | 30 | 50 | 75 | 100 |
|---|---|---|---|---|---|---|---|
| Pontos (nível - 1) | 0 | 9 | 19 | 29 | 49 | 74 | 99 |
| Coroas | 50 | 100 | 250 | 500 | 1.300 | 2.860 | 5.050 |

Referência: a fusão de um item até +10 custa 15.500 coroas. Redistribuir no nível 100 custa um terço disso.

### Perfis e migração

- **Perfis:** até 5 por personagem, nome de 1 a 32 caracteres, salvar grátis, carregar cobra como redistribuir quando muda alguma coisa, só fora de combate.
- **Migração quando a árvore mudar** (`ArvoreDoJogo.Versao`): ao ler, comprar, redistribuir ou carregar perfil, uma árvore comprada em outra versão é **zerada e reembolsada por inteiro** (cada ponto pago volta aos pontos livres, sem cobrar coroas, porque a mudança foi do jogo). A versão 1 (árvore de teste) migra assim. Perfis antigos seguem o aviso de migração de [Perfis de Build](#implementação-da-626-redistribuição-paga-e-perfis-de-build): nó sumido gera aviso e o ponto não some.

## Depende de

- **[Personagem](personagem.md):** identidade, nível e o saldo de pontos da árvore (`IPontosDeNivel`, incluindo `DevolverAsync`).
- **[Skills](skills.md) e [Combate](combate.md):** propriedades das skills, fórmula de dano e mitigação por contrato, e consulta de estado de combate (`IEstadoDeCombate`).
- **[Economia](economia.md):** cobrança da redistribuição (`ICarteiraCoroas`).

## Expõe

- **Rotas HTTP** (todas em `RotasTalentos`):
  - `GET /api/personagens/{id}/talentos` e `POST .../comprar` (`ArvoreResponse`, `ComprarNoRequest`).
  - `POST /api/personagens/{id}/talentos/redistribuir` (`RedistribuirRequest`).
  - `GET /api/personagens/{id}/talentos/perfis` (`ListaPerfisResponse`).
  - `POST /api/personagens/{id}/talentos/perfis` (`SalvarPerfilRequest`).
  - `PUT /api/personagens/{id}/talentos/perfis/{perfilId}` (`RenomearPerfilRequest`).
  - `DELETE /api/personagens/{id}/talentos/perfis/{perfilId}`.
  - `POST /api/personagens/{id}/talentos/perfis/{perfilId}/carregar` (`CarregarPerfilResponse`).
- **Regras puras** no shared (`RegrasDaArvore`): avaliar compra, validação e cálculo de perfil de build, estado do nó, requisitos pendentes, soma dos atributos.
- **`IAtributosDaArvore`** (servidor, `Contratos/Talentos/`): atributos principais da árvore de um personagem, para Combate.
