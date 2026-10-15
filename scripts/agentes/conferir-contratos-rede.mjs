// Garante que parâmetros do transporte UDP (LiteNetLib) estão alinhados entre shared,
// servidor, client Unity e client de teste — evita repetir #725/#732/#826.
//
// Uso:
//   npm run conferir-contratos-rede
//   node scripts/agentes/conferir-contratos-rede.mjs [base]   # ex.: origin/main no pre-push

import { execFileSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const raiz = join(dirname(fileURLToPath(import.meta.url)), "../..");

export const ARQUIVOS = {
  sharedTransporte: "shared/Package/Runtime/Core/TempoReal/TransporteTempoReal.cs",
  servidor: "server/src/JogoDoGrupo.Server/Core/TempoReal/ServidorTempoReal.cs",
  clientTransporte: "client/Assets/_Game/Core/Scripts/TempoReal/TransporteLiteNetLib.cs",
  clientTesteUdp: "tests/JogoDoGrupo.Server.Tests/Infra/ClienteTempoRealDeTeste.cs",
  clientTesteUnity: "client/Assets/_Game/Core/Tests/TransporteLiteNetLibTestes.cs",
};

const PADRAO_CANAIS = /ChannelsCount\s*=\s*TransporteTempoReal\.Canais/;
const PADRAO_LITERAL = /ChannelsCount\s*=\s*\d+/;

export function ler(relativo) {
  return readFileSync(join(raiz, relativo), "utf8");
}

/** @param {Record<string, string>} conteudos */
export function verificarCanais(conteudos) {
  const erros = [];

  if (!conteudos.sharedTransporte.includes("public const byte Canais")) {
    erros.push(`${ARQUIVOS.sharedTransporte}: falta TransporteTempoReal.Canais no shared.`);
  }

  for (const [nome, caminho] of [
    ["servidor", ARQUIVOS.servidor],
    ["clientTransporte", ARQUIVOS.clientTransporte],
    ["clientTesteUdp", ARQUIVOS.clientTesteUdp],
  ]) {
    const texto = conteudos[nome];
    if (!PADRAO_CANAIS.test(texto)) {
      erros.push(`${caminho}: use ChannelsCount = TransporteTempoReal.Canais (não literal).`);
    }
    if (PADRAO_LITERAL.test(texto)) {
      erros.push(`${caminho}: ChannelsCount numérico literal — use TransporteTempoReal.Canais.`);
    }
  }

  if (!conteudos.clientTesteUnity.includes("Sequenciada_NoUltimoCanalDoServidor_Chega")) {
    erros.push(
      `${ARQUIVOS.clientTesteUnity}: falta o teste do transporte Unity no último canal (#826).`,
    );
  }

  return erros;
}

function git(args) {
  try {
    return execFileSync("git", args, { cwd: raiz, encoding: "utf8" }).trim();
  } catch {
    return "";
  }
}

/** @param {string} base */
export function arquivosAlteradosDesde(base) {
  if (!base) return [];
  const saida = git(["diff", "--name-only", `${base}...HEAD`]);
  return saida ? saida.split("\n").map((l) => l.trim()).filter(Boolean) : [];
}

/** @param {string} base */
export function diffServidorDesde(base) {
  if (!base) return "";
  return git(["diff", `${base}...HEAD`, "--", ARQUIVOS.servidor]);
}

/**
 * @param {string[]} alterados
 * @param {string} diffServidor
 */
export function verificarPareamentoNaBranch(alterados, diffServidor) {
  const set = new Set(alterados);
  const tocouShared = set.has(ARQUIVOS.sharedTransporte);
  const tocouServidorTransporte =
    set.has(ARQUIVOS.servidor) &&
    /ChannelsCount|CanalParaEntidade|TransporteTempoReal/.test(diffServidor);

  if (!tocouShared && !tocouServidorTransporte) {
    return [];
  }

  if (set.has(ARQUIVOS.clientTransporte)) {
    return [];
  }

  return [
    "Mudou o transporte UDP no servidor ou no shared, mas não alterou " +
      `${ARQUIVOS.clientTransporte}. ` +
      "No mesmo PR: shared + ServidorTempoReal + TransporteLiteNetLib (+ teste EditMode se aplicável).",
  ];
}

function main() {
  const base = process.argv[2] && !process.argv[2].startsWith("--") ? process.argv[2] : null;

  const conteudos = {
    sharedTransporte: ler(ARQUIVOS.sharedTransporte),
    servidor: ler(ARQUIVOS.servidor),
    clientTransporte: ler(ARQUIVOS.clientTransporte),
    clientTesteUdp: ler(ARQUIVOS.clientTesteUdp),
    clientTesteUnity: ler(ARQUIVOS.clientTesteUnity),
  };

  const erros = [...verificarCanais(conteudos)];

  if (base && git(["rev-parse", "--verify", base])) {
    const alterados = arquivosAlteradosDesde(base);
    erros.push(...verificarPareamentoNaBranch(alterados, diffServidorDesde(base)));
  }

  if (erros.length) {
    console.error("conferir-contratos-rede: falhou\n" + erros.map((e) => `- ${e}`).join("\n"));
    process.exit(1);
  }

  console.log("conferir-contratos-rede: ok");
}

if (process.argv[1]?.includes("conferir-contratos-rede.mjs")) {
  main();
}
