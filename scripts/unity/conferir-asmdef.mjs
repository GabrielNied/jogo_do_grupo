// Confere, sem abrir o Unity, que todo namespace do projeto usado por um script C# pertence ao
// próprio .asmdef do script ou a um .asmdef que ele referencia. Sem a referência o Unity só
// avisa na compilação (CS0234) e abre em safe mode, e o CI de Unity roda depois do merge (#1158).
//
// O que olha: `using` e nomes completos que caem num namespace declarado por algum arquivo do
// client ou do shared. Namespaces de fora (System, UnityEngine) ficam de fora.
// O que não olha: tipo ou assinatura por tipo; isso é do `npm run unity:compilar`.
//
// Uso: npm run conferir-asmdef

import { execFileSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const raiz = join(dirname(fileURLToPath(import.meta.url)), "../..");

const PASTAS = ["client/Assets", "shared/Package"];

const DECLARACAO = /^\s*namespace\s+([\w.]+)/gm;
const USING = /^\s*using\s+(?:static\s+)?(?:\w+\s*=\s*)?([\w.]+)\s*;/gm;
const NOME_COMPLETO = /\bJogoDoGrupo(?:\.\w+)+/g;

// Uma passada só, para um `//` dentro de string e uma aspa dentro de comentário não se confundirem.
// Literais viram vazios: `InternalsVisibleTo("...Tests")` não é uso de namespace.
const COMENTARIO_OU_LITERAL =
  /\/\*[\s\S]*?\*\/|\/\/.*$|@"(?:[^"]|"")*"|\$?"(?:\\.|[^"\\\n])*"|'(?:\\.|[^'\\\n])+'/gm;

function semComentarios(texto) {
  // Mantém as quebras de linha para o número da linha no erro continuar certo.
  return texto.replace(COMENTARIO_OU_LITERAL, (m) => m.replace(/[^\n]/g, ""));
}

/** Maior prefixo (por segmento) de `nome` que é um namespace declarado, ou null. */
function namespaceDeclarado(nome, declarados) {
  const partes = nome.split(".");
  for (let n = partes.length; n > 0; n--) {
    const candidato = partes.slice(0, n).join(".");
    if (declarados.has(candidato)) return candidato;
  }
  return null;
}

/**
 * @param {{ nome: string, caminho: string, referencias: string[] }[]} asmdefs
 *   `caminho` é o do arquivo .asmdef; `referencias` já resolvidas para nomes de asmdef.
 * @param {{ caminho: string, texto: string }[]} arquivos scripts .cs
 * @returns {string[]} erros
 */
export function verificarReferencias(asmdefs, arquivos) {
  const porPasta = asmdefs
    .map((a) => ({ ...a, pasta: a.caminho.slice(0, a.caminho.lastIndexOf("/") + 1) }))
    .sort((a, b) => b.pasta.length - a.pasta.length);
  const donoDe = (caminho) => porPasta.find((a) => caminho.startsWith(a.pasta));

  const declarados = new Map();
  const lidos = arquivos.map((arquivo) => {
    const texto = semComentarios(arquivo.texto);
    const dono = donoDe(arquivo.caminho);
    if (dono) {
      for (const [, ns] of texto.matchAll(DECLARACAO)) {
        if (!declarados.has(ns)) declarados.set(ns, new Set());
        declarados.get(ns).add(dono.nome);
      }
    }
    return { ...arquivo, texto, dono };
  });

  const erros = [];
  for (const { caminho, texto, dono } of lidos) {
    if (!dono) continue;
    const vistos = new Set();
    const conferir = (nome, indice) => {
      const ns = namespaceDeclarado(nome, declarados);
      if (!ns) return;
      const donos = declarados.get(ns);
      if (donos.has(dono.nome) || [...donos].some((d) => dono.referencias.includes(d))) return;
      const chave = `${ns}|${[...donos][0]}`;
      if (vistos.has(chave)) return;
      vistos.add(chave);
      const linha = texto.slice(0, indice).split("\n").length;
      erros.push(
        `${caminho}:${linha}: usa ${ns}, que é de ${[...donos].join(" ou ")}, mas ${dono.nome} não o referencia. ` +
          `Acrescente em references de ${dono.caminho} (ou, entre funcionalidades, passe por um contrato no shared).`,
      );
    };
    for (const m of texto.matchAll(USING)) conferir(m[1], m.index);
    for (const m of texto.matchAll(NOME_COMPLETO)) {
      const inicioDaLinha = texto.lastIndexOf("\n", m.index) + 1;
      if (/^\s*namespace\b/.test(texto.slice(inicioDaLinha, m.index + 10))) continue;
      conferir(m[0], m.index);
    }
  }
  return erros;
}

function listar() {
  const saida = execFileSync(
    "git",
    ["ls-files", "--cached", "--others", "--exclude-standard", "--", ...PASTAS],
    { cwd: raiz, encoding: "utf8", maxBuffer: 64 * 1024 * 1024 },
  );
  return saida.split("\n").filter(Boolean);
}

/** `references` do asmdef pode vir por nome ou por "GUID:<guid>" (o guid mora no .meta). */
export function lerAsmdefs(arquivosAsmdef, ler) {
  const porGuid = new Map();
  const brutos = arquivosAsmdef.map((caminho) => {
    const json = JSON.parse(ler(caminho).replace(/^﻿/, ""));
    let guid = null;
    try {
      guid = ler(`${caminho}.meta`).match(/^guid:\s*(\w+)/m)?.[1] ?? null;
    } catch {}
    if (guid) porGuid.set(guid, json.name);
    return { nome: json.name, caminho, brutas: json.references ?? [] };
  });
  return brutos.map(({ brutas, ...a }) => ({
    ...a,
    referencias: brutas.map((r) => (r.startsWith("GUID:") ? (porGuid.get(r.slice(5)) ?? r) : r)),
  }));
}

function main() {
  const ler = (relativo) => readFileSync(join(raiz, relativo), "utf8");
  const arquivos = listar().filter((a) => existsSync(join(raiz, a)));
  const asmdefs = lerAsmdefs(
    arquivos.filter((a) => a.endsWith(".asmdef")),
    ler,
  );
  const scripts = arquivos.filter((a) => a.endsWith(".cs")).map((c) => ({ caminho: c, texto: ler(c) }));
  const erros = verificarReferencias(asmdefs, scripts);

  if (erros.length) {
    console.error("conferir-asmdef: falhou\n" + erros.map((e) => `- ${e}`).join("\n"));
    process.exit(1);
  }
  console.log("conferir-asmdef: ok");
}

if (process.argv[1]?.includes("conferir-asmdef.mjs")) {
  main();
}
