// Compila os scripts do client (e o shared) contra as DLLs do Unity, sem abrir o editor.
// Cada .asmdef vira um projeto separado, com as mesmas referências, então pega:
// erros de compilação, uso de C# acima de 9, acesso indevido a `internal`, uma funcionalidade
// referenciando outra e `UnityEngine` no shared. Não substitui o teste no editor.
//
// Uso: npm run unity:compilar
// Unity em outro lugar: defina UNITY_EDITOR_DATA com a pasta "Editor/Data" da versão do projeto.

import { execFileSync } from "node:child_process";
import { createHash } from "node:crypto";
import { existsSync, mkdirSync, readdirSync, readFileSync, statSync, writeFileSync } from "node:fs";
import { homedir, tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { pularSemUnity, temUnityNaMaquina } from "./sem-unity.mjs";

const RAIZ = resolve(dirname(fileURLToPath(import.meta.url)), "../..");
const CLIENT = join(RAIZ, "client");
// Uma pasta por checkout: worktrees compilando ao mesmo tempo não disputam os mesmos arquivos (CS2012).
const SAIDA = join(
  tmpdir(),
  `jogo-do-grupo-unity-compilar-${createHash("sha1").update(RAIZ).digest("hex").slice(0, 8)}`,
);

// Library/ do Unity (test runner, NUnit, pacotes): num worktree de agente ela não existe,
// então usa a do checkout principal do repositório.
function acharLibrary() {
  const local = join(CLIENT, "Library");
  if (existsSync(local)) return local;
  try {
    const comum = execFileSync("git", ["rev-parse", "--path-format=absolute", "--git-common-dir"], {
      cwd: RAIZ,
      encoding: "utf8",
    }).trim();
    const principal = join(dirname(comum), "client/Library");
    if (existsSync(principal)) return principal;
  } catch {}
  return local;
}
const LIBRARY = acharLibrary();

const versao = readFileSync(join(CLIENT, "ProjectSettings/ProjectVersion.txt"), "utf8").match(
  /m_EditorVersion:\s*(\S+)/,
)[1];
const dadosUnity =
  process.env.UNITY_EDITOR_DATA ??
  (process.platform === "darwin"
    ? `/Applications/Unity/Hub/Editor/${versao}/Unity.app/Contents`
    : `C:/Program Files/Unity/Hub/Editor/${versao}/Editor/Data`);
const dllsUnity = join(dadosUnity, "Managed/UnityEngine");
// Nenhum Unity nesta máquina (ex.: agente na nuvem): pula e deixa para o CI.
// Unity de outra versão, ou UNITY_EDITOR_DATA errado, continua sendo erro.
if (!existsSync(dllsUnity) && !temUnityNaMaquina()) {
  pularSemUnity("unity:compilar", `não há Unity instalado neste ambiente (${process.platform}).`);
}
if (!existsSync(dllsUnity)) {
  console.error(`✖ Não achei as DLLs do Unity ${versao} em ${dllsUnity}.`);
  console.error("  Instale essa versão pelo Unity Hub ou defina UNITY_EDITOR_DATA.");
  process.exit(2);
}

// Todos os .asmdef do shared e do jogo.
function acharAsmdefs(pasta) {
  if (!existsSync(pasta)) return [];
  return readdirSync(pasta, { recursive: true })
    .filter((f) => f.endsWith(".asmdef"))
    .map((f) => join(pasta, f));
}
const asmdefs = [
  ...acharAsmdefs(join(RAIZ, "shared/Package/Runtime")),
  ...acharAsmdefs(join(CLIENT, "Assets/_Game")),
].map((arquivo) => {
  const def = JSON.parse(readFileSync(arquivo, "utf8"));
  return { arquivo, pasta: dirname(arquivo), ...def };
});
const pastasDeAsmdef = new Set(asmdefs.map((a) => a.pasta));

// Scripts de um assembly: os .cs da pasta, sem entrar em subpastas que têm o próprio .asmdef.
function scripts(pasta) {
  const lista = [];
  for (const nome of readdirSync(pasta)) {
    const caminho = join(pasta, nome);
    if (statSync(caminho).isDirectory()) {
      if (!pastasDeAsmdef.has(caminho)) lista.push(...scripts(caminho));
    } else if (nome.endsWith(".cs")) {
      lista.push(caminho);
    }
  }
  return lista;
}

const nossos = new Set(asmdefs.map((a) => a.name));
const pacotesUnity = join(LIBRARY, "ScriptAssemblies");
const avisos = [];
// Assembly de teste sem NUnit não compila; terminar verde assim escondia erro de teste (#947).
const pulados = [];
const projetos = [];

// NUnit do Unity: só existe depois que o editor abriu o projeto (Library/PackageCache).
function acharNunit() {
  const cache = join(LIBRARY, "PackageCache");
  if (!existsSync(cache)) return null;
  const achado = readdirSync(cache, { recursive: true }).find(
    (f) => f.endsWith("nunit.framework.dll") && f.includes("unity-custom"),
  );
  return achado ? join(cache, achado) : null;
}
const ehEditor = (a) => (a.includePlatforms ?? []).includes("Editor");
const ehTeste = (a) =>
  (a.optionalUnityReferences ?? []).includes("TestAssemblies") ||
  (a.references ?? []).some((r) => /TestRunner/.test(r)) ||
  (a.precompiledReferences ?? []).includes("nunit.framework.dll");

for (const a of asmdefs) {
  const arquivos = scripts(a.pasta);
  if (arquivos.length === 0) continue;
  const nunit = ehTeste(a) ? acharNunit() : null;
  if (ehTeste(a) && !nunit) {
    pulados.push(a.name);
    continue;
  }
  const referencias = (a.references ?? []).map((r) => {
    if (nossos.has(r)) return `    <ProjectReference Include="${join(SAIDA, r, r + ".csproj")}" />`;
    const dll = join(pacotesUnity, r + ".dll");
    if (!existsSync(dll)) avisos.push(`${a.name}: referência "${r}" não encontrada (abra o projeto no Unity uma vez).`);
    return `    <Reference Include="${dll}" Private="false" />`;
  });
  const dlls = [`${dllsUnity}/UnityEngine.*Module.dll`, `${dllsUnity}/UnityEngine.dll`];
  if (ehEditor(a) || ehTeste(a)) {
    dlls.push(`${dllsUnity}/UnityEditor.*Module.dll`);
  }
  if (nunit) {
    dlls.push(nunit);
    for (const dll of ["UnityEngine.TestRunner.dll", "UnityEditor.TestRunner.dll"]) {
      if (existsSync(join(pacotesUnity, dll))) dlls.push(join(pacotesUnity, dll));
    }
  }
  const unity = a.noEngineReferences
    ? ""
    : dlls.map((d) => `    <Reference Include="${d}" Private="false" />`).join("\n");
  const csproj = `<Project Sdk="Microsoft.NET.Sdk">
  <!-- Gerado por scripts/unity/compilar-client.mjs. Não edite. -->
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <Nullable>disable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <AssemblyName>${a.name}</AssemblyName>
    <AllowUnsafeBlocks>${a.allowUnsafeCode ? "true" : "false"}</AllowUnsafeBlocks>
    <NoWarn>CS1591</NoWarn>
  </PropertyGroup>
  <ItemGroup>
${arquivos.map((f) => `    <Compile Include="${f}" />`).join("\n")}
${unity}
${referencias.join("\n")}
  </ItemGroup>
</Project>
`;
  mkdirSync(join(SAIDA, a.name), { recursive: true });
  writeFileSync(join(SAIDA, a.name, a.name + ".csproj"), csproj);
  projetos.push(a.name);
}

const solucao = `<Solution>\n${projetos.map((p) => `  <Project Path="${p}/${p}.csproj" />`).join("\n")}\n</Solution>\n`;
writeFileSync(join(SAIDA, "Client.slnx"), solucao);

console.log(`Unity ${versao} · ${projetos.length} assemblies: ${projetos.join(", ")}`);
for (const aviso of avisos) console.warn(`⚠ ${aviso}`);

// Os projetos gerados nao usam pacotes NuGet externos. Quando o cache local existe,
// restaurar por ele evita tentativas de rede em ambientes isolados.
const cacheNuget = process.env.NUGET_PACKAGES ?? join(homedir(), ".nuget/packages");
let restauradoLocalmente = false;
if (existsSync(cacheNuget)) {
  try {
    execFileSync(
      "dotnet",
      ["restore", join(SAIDA, "Client.slnx"), "--source", cacheNuget, "-p:NuGetAudit=false", "-v", "q"],
      { stdio: "pipe" },
    );
    restauradoLocalmente = true;
  } catch {
    // Em uma maquina sem o pacote basico no cache, o build normal pode restaura-lo.
  }
}

let saida = "";
let falhou = false;
try {
  const argumentos = ["build", join(SAIDA, "Client.slnx"), "-nologo", "-v", "q", "-clp:NoSummary"];
  if (restauradoLocalmente) argumentos.push("--no-restore");
  saida = execFileSync("dotnet", argumentos, {
    encoding: "utf8",
    stdio: ["ignore", "pipe", "pipe"],
  });
} catch (erro) {
  saida = `${erro.stdout ?? ""}${erro.stderr ?? ""}`;
  falhou = true;
}

const linhas = [...new Set(saida.split(/\r?\n/).filter((l) => / (error|warning) [A-Z]+\d+/.test(l)))];
const erros = linhas.filter((l) => / error /.test(l));
const avisosCompilacao = linhas.filter((l) => / warning /.test(l));
for (const l of linhas) console.log(l.replace(/\s*\[[^\]]*\.csproj\]$/, ""));

if (falhou || erros.length) {
  console.error(`✖ Client não compila: ${erros.length} erro(s), ${avisosCompilacao.length} aviso(s).`);
  process.exit(1);
}
if (pulados.length) {
  console.error(`✖ Testes não compilados (sem NUnit; abra o projeto no Unity uma vez para criar a Library): ${pulados.join(", ")}.`);
  process.exit(1);
}
console.log(`✔ Client compila contra o Unity ${versao}: 0 erros, ${avisosCompilacao.length} aviso(s).`);
