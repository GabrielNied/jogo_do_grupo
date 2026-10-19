# Profissões

**Responsável:**
**Status:** primeira entrega jogável (coleta e Ferreiro); demais profissões na #335
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

## Primeira entrega jogável (#192, 2026-10-19)

Decisões de design do dono da issue, onde a doc não fechava. Valores iniciais de teste em `ConfigProfissoes` (shared); o balanceamento final e as outras duas profissões são da [#335](https://github.com/GabrielNied/jogo_do_grupo/issues/335).

- **Aprender:** de graça e na hora, na janela da **forja da cidade** (E no marcador, rua oeste, perto do ferreiro). Pode aprender as quatro que existem hoje (Lenhador, Minerador, Curtidor, Ferreiro), sem limite; Alquimista e Cozinheiro aparecem como "Em breve". Sem nível, sem XP (coleta e craft não dão XP).
- **Coleta:** seis pontos no **campo ao norte do portão** (duas árvores, dois veios de ferro, duas carcaças), tecla **E** a até 3 m (o servidor aceita 4 m). Exige a profissão do ponto. Cada coleta rende 2 do material e o ponto some para todos por **60 s**; quem chega primeiro leva, decidido no servidor por uma trava (dois pedidos juntos: um leva, o outro recebe `NoEsgotado`). O estado dos pontos é por área (cada canal da cidade tem os seus) e fica só na memória: reiniciar o servidor devolve todos.
- **Materiais são saldo por personagem**, fora da bolsa (Madeira, Minério de ferro, Couro cru), como os [materiais de fusão](itens.md) (MAT-001): a bolsa é 1×1 e não empilha, e isso é invariante de Itens.
- **Produção (Ferreiro), na forja:** Espada (4 minério, 2 madeira), Escudo (3 minério, 1 madeira, 2 couro) e Peitoral do Vigia (3 minério, 3 couro). Entrega um equipamento real do catálogo no nível +0, pelo mesmo caminho da loja (`IInventarioEconomia`); atributos sorteados como no loot comum. Gasto de material e item gravados na mesma transação: sem material ou com a bolsa cheia, nada muda. O jogador equipa pelo inventário (I).
- **Mensagens (8000–8009):** `ConsultarProfissoes`/`EstadoDasProfissoes`, `AprenderProfissao`, `ColetarNo`/`ColetaConcluida`, `ProduzirReceita`/`ProducaoConcluida`, `ProfissoesRecusada` (código em `CodigosErroProfissoes`, sem efeito) e `ConsultarNos`/`EstadoDosNos` (pontos esgotados e segundos para voltar; o client os esconde e consulta a cada 15 s).
- **Persistência:** `ProfissoesAprendidas` e `SaldosMateriaisProfissao` (quantidade nunca negativa).
- **No jogo:** pontos e forja são marcadores procedurais até haver modelo; a janela segue a Lumora UI v1. A dica de E mostra "requer <profissão>" para quem ainda não aprendeu.
- **Fora desta entrega:** Alquimista, Cozinheiro e o buff de comida, forja de fusão, receitas que atravessam níveis e nível de profissão ([#335](https://github.com/GabrielNied/jogo_do_grupo/issues/335)).

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
