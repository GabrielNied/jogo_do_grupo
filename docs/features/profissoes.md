# Profissões

**Responsável:**
**Status:** todas as seis profissões jogáveis e integradas (coleta e produção) (#192, #335)
**Faixa de IDs de mensagem:** 8000–8999

## Resumo

Craft e coleta, conectados à economia.

## Definido

- Profissões separadas de talentos.
- Cada profissão se conecta à economia.
- **Pontos de coleta compartilhados** (decidido por Gabriel em 2026-10-16): quem coleta primeiro leva, e o ponto some para todos até voltar, como no New World.
- Decidido em 2026-10-12: seis profissões, no estilo do World of Warcraft (coleta separada de produção) e sem limite por personagem, como no New World. Coleta: Lenhador, Minerador e Curtidor. Produção: Ferreiro, Alquimista e Cozinheiro. Um personagem pode aprender as seis. Fora da [fatia jogável](fatia-jogavel.md#fora-desta-fatia).
- **Comida dá buff, sem fome** (decidido por Gabriel em 2026-10-16): comer dá um efeito temporário (por exemplo, regeneração maior); não comer não pune. Não há barra de saciedade. Desde 2026-10-16 (quiz 2): **uma comida ativa por vez**, com buff de **30 a 60 min** conforme a receita; comer outra troca o buff, e cair não o tira.
- **Decisões do Gabriel em 2026-10-16** (quiz de definições 3): a **tabela de materiais e receitas** do Livro vira **roteiro de conteúdo** (#192, #335). **Coleta e craft não dão XP**; XP de profissão vem só de missões quando aplicável ([missões](missoes.md#definido)).

## Em aberto

- [x] ~~Quais profissões existem, e quantas por personagem?~~ Decidido em 2026-10-12: as seis, todas no mesmo personagem.
- [x] ~~Saciedade~~, receitas e a forja da cidade continuam proposta do board (abaixo). Saciedade saiu em 2026-10-16: comida só dá buff. Receitas do board **adotadas** como roteiro em 2026-10-16 (quiz 3); detalhes na seção Proposta abaixo.

## Entregas jogáveis (#192, #335, 2026-10-19)

As seis profissões completas e integradas em `ConfigProfissoes` (shared), com regras no servidor e marcadores/janela no client:

- **Aprender:** de graça e na hora na oficina da cidade (rua oeste, polo de produção). O personagem pode aprender todas as 6 profissões sem limite (Lenhador, Minerador, Curtidor, Ferreiro, Alquimista, Cozinheiro). Sem nível, sem XP (coleta e craft não dão XP).
- **Coleta (14 pontos):** no campo ao norte do portão da cidade, tecla **E** a até 3 m (o servidor aceita 4 m). Exige a profissão correspondente. Cada coleta rende 2 do material e o ponto esgota por **60 s** com trava atômica no servidor.
  - *Lenhador:* Árvores (Madeira), Arbustos medicinais (Erva medicinal), Plantações (Grãos).
  - *Minerador:* Veios de ferro (Minério de ferro), Geodos arcanos (Cristal arcano).
  - *Curtidor:* Carcaças (Couro cru), Rastros de caça (Carne de caça).
- **Materiais como saldo por personagem:** 7 materiais guardados fora da bolsa (Madeira, Minério de ferro, Couro cru, Erva medicinal, Cristal arcano, Grãos, Carne de caça), compatível com MAT-001 (a bolsa é 1×1 e não empilha).
- **Produção (15 receitas) nas estações da cidade:**
  - *Ferreiro (Forja: X=-14, Z=-11):* Espada Ressonante (173), Escudo Ressonante (176), Peitoral do Vigia (102), Martelo Ressonante (100).
  - *Curtidor (Curtume: X=-14, Z=-8):* Colete do Predador (111), Luvas do Predador (112), Botas do Predador (114), Capuz do Predador (110).
  - *Alquimista (Caldeirão: X=-12, Z=-8):* Foco Ressonante (177), Anel da Aurora (125), Brinco da Aurora (126), Manto da Aurora (120).
  - *Cozinheiro (Cozinha: X=-10, Z=-8):* Caldo dos Errantes (buff de 30 min), Assado de Galhada (buff de 45 min), Ensopado da Fornalha (buff de 60 min), respeitando a regra de uma comida ativa por vez.
- **Transação:** gasto de materiais e adição à bolsa executados na mesma transação atômica serializável (`IInventarioEconomia`).
- **Mensagens (8000–8009):** `ConsultarProfissoes`/`EstadoDasProfissoes`, `AprenderProfissao`, `ColetarNo`/`ColetaConcluida`, `ProduzirReceita`/`ProducaoConcluida`, `ProfissoesRecusada`, `ConsultarNos`/`EstadoDosNos`.
- **Evento de servidor:** cada coleta aceita publica `MaterialColetado` (`Contratos/Profissoes`, material e quantidade da coleta); as [missões](missoes.md) contam o objetivo **Coletar** por ele.
- **Persistência:** `ProfissoesAprendidas` e `SaldosMateriaisProfissao`.

## Regras

### Proposta do board (2026-10-11)

Os seis nomes estão em Definido, e o personagem pode aprender todos. O resto desta seção (materiais, saciedade, receitas e forja) continua **não validado** (seção 08 do [Livro de ideias](../brainstorm/figjam.md#asterra--livro-de-ideias-do-jogo)):

| Profissão | Papel proposto |
|---|---|
| Lenhador | madeira comum e de eco, resina |
| Minerador | ferro, quartzo, ferro estelar (veio raro atrai criaturas) |
| Curtidor | couro cru, tiras, couro reforçado |
| Ferreiro | lingotes em armas, escudos; peças comuns servem de material de fusão |
| Alquimista | energia arcana, óleo, ~~essência de promoção~~ (a promoção saiu em 2026-10-12) |
| Cozinheiro | refeições que restauram saciedade e dão benefícios |

- **Princípio:** materiais básicos continuam na cadeia de receitas; jogador novo e avançado contribuem para as mesmas metas.
- **Receitas que atravessam níveis** (exemplos): couro reforçado, chapéu da aurora, poção de energia, ~~essência de promoção (consumida ao promover raridade)~~ (a promoção saiu em 2026-10-12; [itens](itens.md#modelo-de-itens-v2-documento-mestre-2026-10-12)). Craft cria itens no nível 1.
- **Alimentação:** saciedade de 0 a 100; dano de 60% (vazia) a 100% (cheia), linear; perde 1/min fora de cidade (+1/min em combate), metade em área segura, nada desconectado. Três receitas de expedição (Caldo dos Errantes, Assado de Galhada, Ensopado da Fornalha) com buffs de 15 a 20 min. Valores do board.
- **Forja (na cidade):** ~~infundir, promover raridade, transferir skills~~ fundir (desde 2026-10-12, [fusão v2](itens.md#fusão); promoção e transferência de skill saíram) e produzir equipamentos, conforme os requisitos da peça. Desmontar não existe (DEC-005).

## Depende de

- [Itens](itens.md): equipamento produzido entra na bolsa por `IInventarioEconomia`.
- [Mundo](mundo.md): posição e área do personagem (`IPosicoesDoMundo`) para o alcance.

## Expõe

- Mensagens 8000–8009 (acima). Nenhum contrato para outras funcionalidades por enquanto.
