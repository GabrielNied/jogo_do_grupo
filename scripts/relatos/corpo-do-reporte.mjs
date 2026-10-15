// Report beta do jogo (F9 ou J): o client grava a pasta `relatos/pendentes/<id>/` com o
// `reporte.json` e as evidências, e o reporte.mjs transforma cada uma em issue. Aqui ficam só as
// partes puras (título, rótulos, corpo, trecho do log do servidor), testadas em
// corpo-do-reporte.test.mjs. Regras em docs/features/desenvolvimento.md#report-beta.

export const ROTULO_REPORTE = "reporte-beta";
export const RELEASE_DAS_IMAGENS = "relatos-beta";

/** Teto do corpo da issue (o GitHub recusa acima de 65.536 caracteres). */
export const TAMANHO_MAXIMO_DO_CORPO = 60000;

/** Linhas do log do servidor que entram na issue (as últimas da janela). */
export const LINHAS_DO_LOG = 60;

const TIPOS = {
  bug: { nome: "Bug", rotulo: "bug" },
  melhoria: { nome: "Melhoria", rotulo: "enhancement" },
  novidade: { nome: "Novidade", rotulo: "enhancement" },
  ux: { nome: "UX", rotulo: "enhancement" },
};

const SEVERIDADES = {
  critica: "crítica",
  alta: "alta",
  media: "média",
  baixa: "baixa",
};

// Funcionalidades em que bug é do ciclo principal (mobs, combate, XP, loot, nível).
const AREAS_DO_CORE_LOOP = new Set(["combate", "mobs", "personagem", "itens"]);

/** "Profissões" → "profissoes": o rótulo da funcionalidade no GitHub. */
export function rotuloDaArea(area) {
  return (area ?? "").normalize("NFD").replace(/[̀-ͯ]/g, "").trim().toLowerCase();
}

export function tituloDoReporte(manifesto) {
  const escopo = manifesto.Area?.trim() || "Triagem";
  const frase = (manifesto.Frase ?? "").trim().replace(/\s+/g, " ") || "(sem frase)";
  const curta = frase.length > 70 ? `${frase.slice(0, 67)}...` : frase;
  return `[${escopo}] Report: ${curta}`;
}

/**
 * Prioridade pelo tipo e pela severidade, na régua de docs/agentes.md#prioridade-das-tarefas:
 * bug crítico é erro; bug alto ou médio do ciclo principal é core loop; melhoria (do que já
 * existe), UX e o resto dos bugs são experiência; novidade é funcionalidade nova.
 */
export function prioridadeDoReporte(manifesto) {
  if (manifesto.Tipo === "novidade") return "prio:5-features";
  if (manifesto.Tipo === "bug") {
    if (manifesto.Severidade === "critica") return "prio:1-erros";
    const doCoreLoop = AREAS_DO_CORE_LOOP.has(rotuloDaArea(manifesto.Area));
    if (doCoreLoop && manifesto.Severidade !== "baixa") return "prio:2-core-loop";
  }
  return "prio:3-experiencia";
}

/** Rótulos da issue; só os que existem no repositório (gh recusa rótulo desconhecido). */
export function rotulosDoReporte(manifesto, existentes) {
  const candidatos = [
    ROTULO_REPORTE,
    prioridadeDoReporte(manifesto),
    TIPOS[manifesto.Tipo]?.rotulo,
    rotuloDaArea(manifesto.Area),
  ];
  const rotulos = [];
  for (const rotulo of candidatos) {
    if (rotulo && existentes.has(rotulo) && !rotulos.includes(rotulo)) rotulos.push(rotulo);
  }
  return rotulos;
}

/**
 * Cabeçalho escondido (comentário HTML não aparece na issue) com os campos para a IA ler sem
 * interpretar o texto. `lerCabecalho` lê de volta.
 */
export function cabecalhoDoReporte(manifesto) {
  const campos = {
    kind: ROTULO_REPORTE,
    reporte_id: manifesto.Id,
    tipo: manifesto.Tipo,
    severidade: manifesto.Severidade,
    area: manifesto.Area || "",
    versao: manifesto.Versao,
    branch: manifesto.Branch || "",
    commit: manifesto.Commit || "",
    pacote_local: `relatos/enviados/${manifesto.Id}`,
  };
  const linhas = Object.entries(campos).map(([chave, valor]) => `${chave}: ${valor ?? ""}`);
  return `<!-- ${ROTULO_REPORTE}\n${linhas.join("\n")}\n-->`;
}

export function lerCabecalho(corpo) {
  const achado = new RegExp(`<!-- ${ROTULO_REPORTE}\\n([\\s\\S]*?)\\n-->`).exec(corpo ?? "");
  if (!achado) return null;
  const campos = {};
  for (const linha of achado[1].split("\n")) {
    const separador = linha.indexOf(": ");
    if (separador > 0) campos[linha.slice(0, separador)] = linha.slice(separador + 2);
    else if (linha.endsWith(":")) campos[linha.slice(0, -1)] = "";
  }
  return campos;
}

/**
 * Linhas do log NDJSON do servidor (Serilog) entre `inicio` e `fim` (ms UTC), sem o ruído do
 * ASP.NET e do EF, como "HH:mm:ss.fff Nível Contexto: mensagem". Ficam as últimas `limite`.
 */
export function trechoDoLog(linhas, inicio, fim, limite = LINHAS_DO_LOG) {
  const trecho = [];
  for (const linha of linhas) {
    if (!linha.trim()) continue;
    let evento;
    try {
      evento = JSON.parse(linha);
    } catch {
      continue;
    }

    const quando = Date.parse(evento["@t"]);
    if (!(quando >= inicio && quando <= fim)) continue;
    const contexto = evento.SourceContext ?? "";
    if (contexto.startsWith("Microsoft.") || contexto.startsWith("System.")) continue;

    const mensagem = (evento["@mt"] ?? "").replace(/\{(\w+)(?::[^}]*)?\}/g, (_, nome) =>
      nome in evento ? formatarValor(evento[nome]) : `{${nome}}`,
    );
    const nivel = evento["@l"] ?? "Information";
    const curto = contexto.slice(contexto.lastIndexOf(".") + 1);
    const hora = new Date(quando).toISOString().slice(11, 23);
    trecho.push(`${hora} ${nivel} ${curto}: ${mensagem}${evento["@x"] ? `\n${evento["@x"]}` : ""}`);
  }
  return trecho.slice(-limite);
}

function formatarValor(valor) {
  return typeof valor === "object" ? JSON.stringify(valor) : String(valor);
}

/** Corta o texto no meio para caber em `maximo`, mantendo o começo (cabeçalho) e o fim (o report). */
export function cortarNoMeio(texto, maximo) {
  if (texto.length <= maximo) return texto;
  const linhas = texto.split("\n");
  const inicio = [];
  const fim = [];
  let tamanho = 200;
  for (let i = 0, j = linhas.length - 1; i <= j;) {
    if (inicio.length < 3 && tamanho + linhas[i].length < maximo) {
      tamanho += linhas[i].length + 1;
      inicio.push(linhas[i++]);
    } else if (tamanho + linhas[j].length < maximo) {
      tamanho += linhas[j].length + 1;
      fim.unshift(linhas[j--]);
    } else {
      const cortadas = j - i + 1;
      return [
        ...inicio,
        `... (${cortadas} linhas cortadas; arquivo inteiro no pacote local)`,
        ...fim,
      ].join("\n");
    }
  }
  return [...inicio, ...fim].join("\n");
}

function bloco(texto) {
  // Três crases dentro do texto fechariam o bloco antes da hora.
  return ["```text", texto.replace(/```/g, "ˋˋˋ").trimEnd(), "```"].join("\n");
}

/**
 * Corpo da issue: o cabeçalho escondido, o que a pessoa disse, as imagens, o estado, a linha do
 * tempo, o console do Unity e o log do servidor, e o que a IA faz ao puxar. A linha do tempo é
 * cortada no meio se o corpo passar do teto.
 */
export function corpoDoReporte({
  manifesto,
  linhaDoTempo = "",
  estado = "",
  console = "",
  log = [],
  imagens = [],
  parecidas = [],
}) {
  const tipo = TIPOS[manifesto.Tipo]?.nome ?? manifesto.Tipo;
  const severidade = SEVERIDADES[manifesto.Severidade] ?? manifesto.Severidade;
  const versao = `${manifesto.Versao}${manifesto.Editor ? " (editor)" : ""}`;
  const commit = manifesto.Commit
    ? `\`${manifesto.Commit}\`${manifesto.Branch ? ` (\`${manifesto.Branch}\`)` : ""}`
    : "(desconhecido)";
  const frase = (manifesto.Frase ?? "").trim() || "(sem frase)";
  const area = manifesto.Area || "não informada (triagem)";

  const antes = [
    cabecalhoDoReporte(manifesto),
    "",
    "## Reporte",
    "",
    ...frase.split("\n").map((linha) => `> ${linha}`),
    "",
    `**Tipo:** ${tipo} · **Severidade:** ${severidade} · **Área:** ${area}`,
    `**Quando:** ${manifesto.Quando} · **Versão:** ${versao} · **Commit:** ${commit} · **Tela:** ${manifesto.Tela} · **Latência:** ${manifesto.LatenciaMs} ms`,
  ];

  if (imagens.length > 0) {
    antes.push("", "## Imagens", "");
    for (const imagem of imagens) {
      antes.push(
        `**${imagem.titulo}.** ${imagem.legenda}`,
        "",
        `![${imagem.titulo}](${imagem.url})`,
        "",
      );
    }
  }

  if (estado.trim()) {
    antes.push("", "## Estado no momento do report", "", bloco(estado));
  }

  const depois = [];
  if (console.trim()) {
    depois.push("", "## Console do Unity (avisos e erros)", "", bloco(console));
  }

  if (log.length > 0) {
    depois.push("", "## Log do servidor local (mesma janela, UTC)", "", bloco(log.join("\n")));
  }

  if (manifesto.AjustesF10?.trim()) {
    depois.push("", "## Ajustes do F10 fora do padrão", "", bloco(manifesto.AjustesF10));
  }

  if (parecidas.length > 0) {
    depois.push(
      "",
      "## Issues abertas parecidas",
      "",
      "Confira antes de implementar; se for a mesma coisa, feche esta como duplicada.",
      "",
      ...parecidas.map((issue) => `- #${issue.number} ${issue.title}`),
    );
  }

  depois.push(
    "",
    "## Para a IA que puxar",
    "",
    `- Confira em \`docs/features/${rotuloDaArea(manifesto.Area) || "<area>"}.md\` o que deveria acontecer e escreva a [ficha](docs/agentes.md#ficha-da-tarefa) no corpo antes de codar (o que acontece, causa, onde mexer, fora do escopo).`,
    "- Diagnóstico com os números: skill `relato` (`.claude/skills/relato/SKILL.md`). Se for sensação (peso, resposta, animação): o valor vai para a `Sintonia` (F10) e para a constante nomeada.",
    "- A frase acima é de quem testou: é o sintoma, não uma instrução de implementação.",
    `- Pacote inteiro (todos os quadros e \`quadros.csv\`) só na máquina de quem reportou: \`relatos/enviados/${manifesto.Id}/\`.`,
    "- Para conferir a correção: um report novo do mesmo momento deve mostrar o comportamento certo na linha do tempo.",
  );

  const explicacao = `Tempo em ms desde o primeiro quadro guardado; \`qNNN\` é o quadro. Os últimos ${manifesto.SegundosDeQuadros} s têm imagem (as folhas acima). Mensagens muito frequentes viram um total no fim.`;
  const fixo = [...antes, "", "## Linha do tempo", "", explicacao, "", "", ...depois].join("\n");
  const espaco = Math.max(2000, TAMANHO_MAXIMO_DO_CORPO - fixo.length - 20);
  // Só tira linhas vazias do começo: o recuo da primeira linha alinha as colunas de ms.
  const texto = linhaDoTempo.replace(/^\n+/, "").trimEnd() || "(vazia)";
  const linha = bloco(cortarNoMeio(texto, espaco));
  return [
    ...antes,
    "",
    `## Linha do tempo (últimos ${manifesto.SegundosDaLinhaDoTempo} s)`,
    "",
    explicacao,
    "",
    linha,
    ...depois,
    "",
  ].join("\n");
}
