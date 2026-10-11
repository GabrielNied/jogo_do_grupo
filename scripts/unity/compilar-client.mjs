// Compila os scripts do client (e o shared) contra as DLLs do Unity, sem abrir o editor.
// Cada .asmdef vira um projeto separado, com as mesmas referências, então pega:
// erros de compilação, uso de C# acima de 9, acesso indevido a `internal`, uma funcionalidade
// referenciando outra e `UnityEngine` no shared. Não substitui o teste no editor.
//
// Uso: npm run unity:compilar
// Unity em outro lugar: defina UNITY_EDITOR_DATA com a pasta "Editor/Data" da versão do projeto.

import { execFileSync } from "node:child_process";
import { existsSync, mkdirSync, readdirSync, readFileSync, statSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const RAIZ = resolve(dirname(fileURLToPath(import.meta.url)), "../..");
const CLIENT = join(RAIZ, "client");
const SAIDA = join(tmpdir(), "jogo-do-grupo-unity-compilar");

const versao = readFileSync(join(CLIENT, "ProjectSettings/ProjectVersion.txt"), "utf8").match(
  /m_EditorVersion:\s*(\S+)/,
)[1];
const dadosUnity =
  process.env.UNITY_EDITOR_DATA ??
  (process.platform === "darwin"
    ? `/Applications/Unity/Hub/Editor/${versao}/Unity.app/Contents`
    : `C:/Program Files/Unity/Hub/Editor/${versao}/Editor/Data`);
const dllsUnity = join(dadosUnity, "Managed/UnityEngine");
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
const pacotesUnity = join(CLIENT, "Library/ScriptAssemblies");
const avisos = [];
const projetos = [];

for (const a of asmdefs) {
  const arquivos = scripts(a.pasta);
  if (arquivos.length === 0) continue;
  const referencias = (a.references ?? []).map((r) => {
    if (nossos.has(r)) return `    <ProjectReference Include="${join(SAIDA, r, r + ".csproj")}" />`;
    const dll = join(pacotesUnity, r + ".dll");
    if (!existsSync(dll)) avisos.push(`${a.name}: referência "${r}" não encontrada (abra o projeto no Unity uma vez).`);
    return `    <Reference Include="${dll}" Private="false" />`;
  });
  const unity = a.noEngineReferences
    ? ""
    : `    <Reference Include="${dllsUnity}/UnityEngine.*Module.dll" Private="false" />\n    <Reference Include="${dllsUnity}/UnityEngine.dll" Private="false" />`;
  const csproj = `<Project Sdk="Microsoft.NET.Sdk">
  <!-- Gerado por scripts/unity/compilar-client.mjs a partir de ${a.arquivo}. Não edite. -->
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

let saida = "";
let falhou = false;
try {
  saida = execFileSync("dotnet", ["build", join(SAIDA, "Client.slnx"), "-nologo", "-v", "q", "-clp:NoSummary"], {
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
console.log(`✔ Client compila contra o Unity ${versao}: 0 erros, ${avisosCompilacao.length} aviso(s).`);
