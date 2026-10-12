# 0035. Fonte SDF própria e escala da interface pela resolução

**Data:** 2026-10-12
**Status:** aceita (substitui o [ADR 0028](0028-escala-legivel-das-interfaces.md))

## Contexto

Em 1920 × 1080 o texto pequeno saía borrado e as janelas pareciam estreitas (issue #584). Duas causas:

- As fontes Lumora entravam no USS como `.ttf`. O Unity gerava em tempo de execução um Font Asset genérico, sem amostragem escolhida para os tamanhos do jogo.
- Com `ConstantPixelSize` ([ADR 0028](0028-escala-legivel-das-interfaces.md)), a interface não acompanha a resolução: em 2560 × 1440 tudo fica pequeno no meio da tela, e cada funcionalidade criava o próprio `PanelSettings`.

## Decisão

- **Fonte:** cada fonte de `ConfigVisual.Fontes` tem um Font Asset SDF (TextCore) próprio, `<fonte>-SDF.asset` ao lado do `.ttf`, e o USS aponta para ele. O Unity cria os assets por script de editor (`npm run unity:executar -- JogoDoGrupo.Client.Core.Visual.Editor.GeradorDeFontes.Gerar`). Valores em `GeradorDeFontes`:
  - amostragem de 56 pt com margem de 6 (cerca de 10%), SDF com antisserrilhado: nítido de 12 a 32 px e até 4/3 disso em 2560 × 1440;
  - atlas de 1024 × 1024, com mais páginas se faltar espaço;
  - modo dinâmico (o estático está obsoleto no Unity 6.6): o Unity guarda o asset sem glifos e preenche o atlas ao jogar, então ele não vira diff no git. O gerador confere se a fonte cobre ASCII, Latin-1 (acentos do português) e os símbolos da interface (▲ ▼ → • · × − – — “ ”) e avisa o que faltar;
  - a Nunito (títulos) não tem as setas: os outros papéis caem na fonte de leitura para o que não tiverem.
- **Escala:** `EstiloCore.ConfigurarEscala` usa `ScaleWithScreenSize`, referência 1920 × 1080, seguindo a altura (`Responsividade.CorrespondenciaAlturaPercentual` = 100). Em 16:9 a interface fica com a mesma proporção em qualquer resolução; em 21:9 ou 16:10 a altura lógica continua 1080 e as janelas cabem na vertical.
- **Um ponto só:** `EstiloCore.CriarConfiguracaoDoPainel(nome, ordem)` cria o `PanelSettings` de qualquer interface (Core e funcionalidades), com escala, tema e `DontSave`.

## Consequências

- Em 1280 × 720 o texto de 16 px fica com cerca de 10,7 px na tela, o custo que o ADR 0028 evitava. O SDF mantém o contorno nítido nesse tamanho. Se a leitura em 720p não bastar, o próximo passo é a preferência de tamanho de texto (`EstiloVisual.DefinirTamanhoTexto`) ou um piso de escala, não voltar ao tamanho constante.
- Os limiares `lumora-compacto` e `lumora-estreito` passam a valer em pixels lógicos: em 16:9 a largura lógica é sempre 1920, então só janelas estreitas (ex.: 4:3 abaixo da referência) os ativam.
- Uma funcionalidade nova não cria `PanelSettings` à mão: chama o ajudante do Core.
- Trocar a fonte em `config-visual.json` exige gerar o Font Asset da nova fonte.
