# Design de Lumora

Ponto de entrada da identidade visual, referências de interface e materiais de design do jogo. Esta pasta fica na raiz, separada de `client/`, `server/` e `shared/`. Nada aqui é importado automaticamente pelo jogo.

## Comece por aqui

- [Galeria visual](interface/index.html): pranchas de interface, materiais, mini marcas e elementos separados. Abrir localmente no navegador; o GitHub exibe o código HTML.
- [Tipografia escolhida](tipografia/comparacao-sem-serifa.html): opção **01 acolhedora**, com fontes locais. O seletor mantém alternativas como histórico.
- [Especificação tipográfica atual](tipografia/PADRAO-TIPOGRAFICO.md): a seção **APROVADO — padrão atual** prevalece sobre propostas anteriores.
- [Estilos tipográficos reutilizáveis](tipografia/lumora-typography.css).
- [Logo horizontal aprovada](marca/logo-horizontal.png).
- [Referência aprovada do símbolo](marca/simbolo-referencia.png): duas cascas de pedra envolvendo essência; o texto dessa composição antiga não é a matriz atual do lettering.
- [Guia das aplicações](interface/LEIA-ME.md): usos, limitações e preparação para implementação.
- [Laboratório de interfaces no Unity](laboratorio-unity/README.md): exemplos isolados em UI Toolkit para comparar com as pranchas; implementação ainda pendente de validação na engine.

## Organização

```text
design/
  README.md              entrada e estado das decisões
  AGENTS.md              instruções para preservar a identidade
  marca/                 matrizes visuais aprovadas
  tipografia/            fontes, licenças, comparação e estilos
  interface/             galeria, pranchas e elementos de apoio
    pranchas/            conceitos de telas e materiais
    elementos/           fundos, moldura e referências raster
    exploracoes/         estudos ainda não finalizados
  historico/             versões anteriores e registro de decisões
```

Os arquivos de histórico são preservados, não recomendações vigentes. Cópias de logos nas pranchas e no kit antigo são referências; usar `marca/` como fonte das matrizes aprovadas. Pacotes ZIP antigos não foram adicionados: seus conteúdos já estão organizados e versionados. Os originais externos ao repositório foram preservados.

## Direção de marca

Fantasia medieval alegre, social e grandiosa. Viajantes com capacidades extraordinárias chegam em busca de aventura e se tornam fundadores. Poderes impressionam quem presencia; criações e contribuições deixam marcas no mundo. Fundação e legado são o coração da marca; magia e descoberta dão energia à experiência.

Construção, territórios personalizados e memoriais compõem a visão de longo prazo. As pranchas não definem regras de gameplay nem confirmam entrega desses sistemas no MVP; regras oficiais continuam em `docs/features/`.

## Estado das decisões

| Parte | Estado | Referência |
|---|---|---|
| Nome Lumora / assinatura A New Horizon | Escolhidos para desenvolver a identidade; nome comercial ainda provisório por usos existentes | [Registro inicial](historico/DIRETRIZES.md) |
| Logo horizontal, dourado, O rachado e essência inclinada atrás da letra | Aprovados | [Matriz horizontal](marca/logo-horizontal.png) |
| Símbolo, pedra mel fosca, sombras e essência ampla | Base aprovada | [Referência do símbolo](marca/simbolo-referencia.png) |
| Paleta e linguagem inicial de folhas/calçada | Aprovadas como direção | [Guia visual](interface/LEIA-ME.md) |
| Nunito Sans nos títulos e Source Sans 3 na interface | Opção 01 aprovada | [Padrão tipográfico](tipografia/PADRAO-TIPOGRAFICO.md) |
| Novas telas, mini marcas, ícones e relevos | Explorações, não matrizes finais | [Pranchas](interface/index.html) |
| Silhuetas separadas | Exigem limpeza/finalização | [Explorações](interface/exploracoes/) |

As fontes acompanham as licenças OFL em [tipografia/fontes/](tipografia/fontes/). Não instalar nem importar fontes na engine como parte desta organização. A escolha das fontes de apoio não altera o lettering da logo.

## Uso e manutenção

1. Consultar as matrizes aprovadas antes de editar; alterar apenas os atributos solicitados.
2. Não substituir uma referência aprovada por reprodução gerada numa prancha.
3. Preservar histórico e registrar aprovação por atributo.
4. Atualizar a especificação do assunto em seu documento de origem; apontar links em vez de criar outro padrão concorrente.
5. Ao implementar, preparar componentes separados, limpar silhuetas e conferir contraste, escala, acentos e legibilidade em telas reais.

PNGs e TTFs seguem o Git LFS já configurado pelo repositório. Após clonar, usar `git lfs pull` caso os arquivos binários ainda sejam ponteiros. As galerias e comparações usam caminhos relativos e não dependem da pasta original do computador.
