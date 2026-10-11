# Interface (Core)

**Responsável:**
**Status:** em definição (números em **proposta**, a calibrar)
**Faixa de IDs de mensagem:** não tem (tudo local no client; o servidor não participa)

## Resumo

Telas e menus do client que não pertencem a uma funcionalidade do jogo. Por enquanto, o menu de opções aberto com Esc (issue #89). Código no Core do client; mudanças em `Core/` pedem review do responsável.

## Definido

- O menu abre com **Esc**, tem **continuar**, **opções**, **sair da conta** e **sair do jogo**. "Sair da conta" já existe (apaga o token gravado; veja [personagem.md](personagem.md)).
- Opções iniciais (da issue #89): volume (geral, música, efeitos), qualidade gráfica, resolução e tela cheia, sensibilidade da câmera, idioma. Gravadas neste computador.
- Remapear teclas fica fora (tarefa própria, #90). Os atalhos padrão e as regras de troca estão em [Teclas e atalhos](#teclas-e-atalhos) (em proposta).

## Em aberto

- [ ] Abrir o menu pausa o jogo? Proposta: **não**. O jogo é online e persistente, o mundo segue rodando; o personagem fica parado e vulnerável (sem tela de pausa).
- [ ] Idiomas disponíveis na primeira versão (proposta: só português; a opção existe mas lista um idioma).
- [ ] Sair do jogo/da conta dentro de dungeon ou combate: sai na hora ou tem espera (proposta: espera de 10 s em combate)? Decisão do grupo.
- [ ] ~~Remapear teclas (#90): só listado, não desenhado aqui.~~ substituído em 2026-10-11 por [Teclas e atalhos](#teclas-e-atalhos) (proposta de atalhos padrão e regras de troca)
- [ ] Teclas e atalhos (#90), decisões do grupo:
  - [ ] Aprovar os atalhos padrão da tabela (proposta). Pontos de atenção: **ataque pesado no botão direito** (no New World o pesado é segurar o botão esquerdo e o bloqueio fica no direito; aqui o bloqueio vai para **Q**) e **esquiva no Espaço** (no New World é Alt)
  - [ ] Tecla já usada: **trocar as duas ações** (proposta) ou deixar a outra sem tecla?
  - [ ] Tecla de **correr**: segurar (proposta) ou alternar?
  - [ ] Combate ([combate.md](combate.md)) ainda não decidiu se **bloqueio**, **stamina** e **troca de arma** entram na fatia jogável; as linhas dessas ações da tabela só valem se entrarem
  - [ ] [personagem.md](personagem.md) só define andar e correr; **pular** não existe nas docs, então não tem atalho (se entrar, sugestão: Espaço vira pular e a esquiva vai para Alt)
- [ ] HUD configurável (#93): só listado, não desenhado aqui.
- [ ] Áudio (#96): só listado; os sons em si não são desenhados aqui.

## Regras

**Base:** menus de pausa de jogos de PC online (WoW, New World). **Igual:** Esc abre o menu; opções ficam num submenu. **Diferente:** não pausa o jogo (proposta).

### Menu (Esc)

| Item | Regra |
|---|---|
| Esc | abre o menu; Esc de novo ou **Continuar** fecha. Se há outro painel aberto (inventário, mapa), Esc fecha o painel primeiro |
| Continuar | fecha o menu |
| Opções | abre a lista abaixo; **Voltar** retorna ao menu |
| Sair da conta | apaga o token e volta ao login (já existe) |
| Sair do jogo | fecha o aplicativo |

Com o menu aberto, o cursor fica livre e a câmera e o movimento não recebem input (**proposta**).

### Opções iniciais

| Opção | Valores (proposta) | Padrão |
|---|---|---|
| Volume geral / música / efeitos | 0 a 100, passo 5 | 80 / 70 / 80 |
| Qualidade gráfica | Baixa, Média, Alta (níveis de qualidade do Unity) | Média |
| Resolução | as do monitor | resolução atual |
| Tela cheia | sim/não | sim |
| Sensibilidade da câmera | 0,5x a 2,0x, passo 0,1 | 1,0x |
| Idioma | Português | Português |

- Gravadas neste computador (arquivo local do client, não na conta); valor inválido ou ausente volta ao padrão. Aplicam na hora, sem botão "Aplicar" (proposta).
- Servidor autoritativo não se aplica: nada aqui afeta regra de jogo.
- **Testes esperados** (client, EditMode): salvar e ler as opções; valor fora da faixa é limitado; arquivo corrompido volta aos padrões.

### Teclas e atalhos

**Base:** New World (combate de ação em 3ª pessoa no PC, veja [referências](../referencias.md)) e MMOs de PC em geral. **Igual:** WASD para andar, mouse para câmera e ataque, menu no Esc, tecla fixa por ação com atalhos de painel em letras. **Diferente:** esquiva no Espaço (no New World é Alt) e ataque pesado no botão direito do mouse (no New World é segurar o esquerdo); ambos em **proposta**. Só teclado e mouse; gamepad fica fora.

A tela fica em **Opções > Teclas**, com uma linha por ação (nome, tecla atual, botão **Trocar**) e um botão **Restaurar padrão**. Os nomes das ações seguem [combate.md](combate.md) (esquiva, ataque leve, ataque pesado, bloqueio) e [personagem.md](personagem.md) (andar, correr).

| Ação | Tecla padrão (proposta) | Troca? |
|---|---|---|
| Andar para frente / trás / esquerda / direita | W / S / A / D | sim, cada direção |
| Correr | Shift (segurar) | sim |
| Câmera | mover o mouse | não (sensibilidade fica em Opções) |
| Esquiva | Espaço | sim |
| Ataque leve | botão esquerdo do mouse | sim |
| Ataque pesado | botão direito do mouse | sim |
| Bloqueio (se entrar no combate) | Q | sim |
| Skills 1, 2 e 3 (se entrarem, ver combate.md) | 1, 2, 3 | sim |
| Trocar de arma (se entrar, S10) | R | sim |
| Interagir | E | sim |
| Mapa | M | sim |
| Inventário | I | sim |
| Menu | Esc | **não** (reservada) |

**Regras de troca (proposta):**

- Cada ação tem **uma** tecla ou botão. Clicar em **Trocar** e apertar a nova tecla grava; Esc durante a captura cancela e mantém a tecla antiga.
- **Tecla já usada por outra ação:** as duas ações **trocam de tecla** (a outra fica com a tecla antiga desta) e a tela avisa "X agora usa Y". Nenhuma ação fica sem tecla.
- **Reservadas, não podem ser usadas nem trocadas:** Esc (menu e cancelar), Alt+F4 e teclas do sistema (Windows, Cmd). Tentar usar uma delas mostra aviso e não muda nada.
- Botões do mouse só valem para ações de combate (ataque leve, ataque pesado, bloqueio, skills); andar, interagir, mapa e inventário aceitam só teclado. A câmera segue o mouse e não é uma ação trocável.
- **Voltar ao padrão:** por ação (botão ao lado da linha) e para todas (botão **Restaurar padrão**, com confirmação).
- Mudanças aplicam na hora, sem botão "Aplicar", e valem também com o menu aberto novamente.
- Com o menu ou a captura de tecla aberta, as ações do jogo não disparam (mesma regra do menu).

**Onde fica gravado:** neste computador, no mesmo arquivo local das opções iniciais (não na conta, e o servidor não participa). Guarda só as teclas diferentes do padrão, por nome de ação. Ação desconhecida, tecla inválida, reservada ou repetida no arquivo volta ao padrão daquela ação; arquivo corrompido volta todas ao padrão.

**Testes esperados** (client, EditMode): os padrões não têm tecla repetida nem reservada; trocar para tecla livre grava e lê de volta; trocar para tecla usada troca as duas ações; tecla reservada é recusada sem mudar nada; botão do mouse recusado em ação só de teclado; restaurar uma ação e restaurar todas voltam ao padrão; arquivo corrompido ou com tecla repetida volta aos padrões.

### Fora desta primeira versão

Remapear teclas, ajustes de HUD, acessibilidade (daltonismo, tamanho de texto), controle de gamepad e opções de rede.

## Depende de

[Personagem](personagem.md) (sair da conta), Core do client (input, UI base).

## Expõe

Nada.
