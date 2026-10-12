# 0034. Testes de contrato e do jogo, sem verificação repetida

**Data:** 2026-10-12
**Status:** aceita (decidido por Gabriel). Altera a regra 10 do `AGENTS.md` e o gatilho de PR do [ADR 0019](0019-unity-no-ci.md).

## Contexto

O custo de cada entrega cresceu mais que o jogo. Na #560 (inventário em grade), o agente gastou cerca de 310 mil tokens, boa parte esperando o Unity, rodando 588 testes EditMode na máquina e escrevendo testes de tela. Depois, o CI rodou os mesmos testes no PR, por uns 10 minutos.

O Unity no CI de PR também era a parte mais frágil: ativar a licença Personal num runner mata com exit 137 o editor que roda em outro lugar, inclusive o Unity local dos agentes. A `main` ficou quebrada por horas sem ninguém notar (#529).

E cada "conferir na cena" virava uma issue `pendente-unity`: eram 10 abertas, 4 delas tornadas inúteis por uma única troca de tela.

## Decisão

O objetivo dos testes é saber que **nenhum contrato quebrou e o jogo continua funcionando**, não cobrir cada detalhe.

- **Servidor e shared:** teste o contrato que mudou (rota ou mensagem: sucesso e os erros que o client trata) e as regras que decidem resultado (dano, drop, item, moeda), com concorrência quando algo puder duplicar ou sumir. Sem lista exaustiva de limites.
- **Client:** sem teste obrigatório. EditMode só para lógica pura que já quebrou ou é difícil ver na tela. Tela e layout são conferidos por quem faz o merge, pelo "Como testar" do PR.
- **Verificação local:** `dotnet test` para server/shared; só `npm run unity:compilar` para o client. Ninguém roda `unity:testes` por rotina.
- **CI "Client (Unity)":** roda na `main` depois do merge (e no agendamento que confere a `main`). Num PR, só com a label `testar-unity`.
- **Pendência de Unity:** só para o que precisa do Unity para existir (`.meta`, gerador). Conferir tela, compilar e testar não viram pendência.

## Consequências

- Um PR de client leva um compilar local em vez de compilar, testar e esperar 10 minutos no CI.
- Uma quebra nos testes EditMode aparece depois do merge, na `main`; o CI abre a pendência, como já fazia. Para mudança arriscada no client, ponha a label `testar-unity` no PR.
- Menos disputa pela licença do Unity entre o CI e as máquinas locais.
- O revisor bloqueia PR sem teste só quando muda contrato ou regra que decide resultado.
