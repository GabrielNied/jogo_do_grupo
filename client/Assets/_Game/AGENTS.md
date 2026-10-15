# Interfaces do client

Ao criar ou alterar uma interface, ler obrigatoriamente [o guia Lumora UI v1](../../../design/interface/GUIA-DE-ESTILO.md). O usuário adotou o modelo visual do laboratório como padrão inicial das novas telas.

- Valores visuais têm fonte única em `design/interface/config-visual.json`; usar `JogoDoGrupo.Client.Visual`, `EstiloVisual` e `ComponentesVisuais`. Não referenciar a feature LaboratorioVisual para reutilizar aparência.
- Cores, fontes, tamanhos e espaçamentos não devem ser duplicados em literais por tela. Acrescentar tokens semânticos quando faltar um papel, gerar as saídas e conferir sincronização.
- Cada layout deve tratar largura real disponível, quebra, rolagem e leitura aumentada. Validar estados de interação, teclado, acentos, contraste e ausência de cortes conforme o guia.
- Arte aprovada permanece em `design/`. Novos assets e seus metadados são importados pelo Unity, preservando origem e licenças. Não substituir marca ou fontes silenciosamente.
- Preservar telas existentes fora da tarefa. A migração é gradual; a adoção do padrão não autoriza reestilizar outras funcionalidades.
- Valor de sensação novo (hit-stop, tremor, animação, câmera, número na tela) nasce ajustável na `Sintonia`, no `Config<Assunto>` da funcionalidade ([convenção](../../../docs/convencoes.md#valores-de-balanceamento)).
- Depois de mudar a configuração: `node design/interface/ferramentas/gerar-config-visual.mjs`, `node design/interface/ferramentas/gerar-config-visual.mjs --conferir` e testes direcionados. Registrar o que depende de Unity; não confundir comando pulado com verificação aprovada.
