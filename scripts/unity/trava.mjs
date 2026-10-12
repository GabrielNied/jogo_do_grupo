// Trava da máquina para o Unity em modo batch (issue #448).
//
// Vários agentes, cada um no seu worktree, dividem um computador: dois Unity ao mesmo tempo
// pesam demais, e antes cada um saía com erro e virava outra rodada. Agora quem chega espera a
// vez: a trava é um arquivo no %TEMP% criado de forma exclusiva, com o pid de quem segura.
// Trava de processo que morreu é retomada sozinha.

import { closeSync, openSync, readFileSync, rmSync, writeSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";

export const ARQUIVO_TRAVA = join(tmpdir(), "jogo-do-grupo-unity.trava");
const ESPERA_PADRAO_MIN = 30;
const INTERVALO_S = 15;

export function processoVivo(pid) {
  try {
    process.kill(pid, 0);
    return true;
  } catch (erro) {
    return erro.code === "EPERM";
  }
}

/** Lê quem segura a trava, ou null se não há trava (ou ela está ilegível). */
export function lerTrava(arquivo = ARQUIVO_TRAVA) {
  try {
    return JSON.parse(readFileSync(arquivo, "utf8"));
  } catch {
    return null;
  }
}

/** Tenta pegar a trava uma vez. Devolve true se pegou. Retoma trava de processo morto. */
export function tentarPegar(dono, { arquivo = ARQUIVO_TRAVA, vivo = processoVivo } = {}) {
  for (let tentativa = 0; tentativa < 2; tentativa++) {
    try {
      const fd = openSync(arquivo, "wx");
      writeSync(fd, JSON.stringify(dono));
      closeSync(fd);
      return true;
    } catch (erro) {
      if (erro.code !== "EEXIST") throw erro;
      const atual = lerTrava(arquivo);
      if (atual && vivo(atual.pid)) return false;
      rmSync(arquivo, { force: true });
    }
  }
  return false;
}

/** Solta a trava, só se ela é deste processo. */
export function soltar(pid = process.pid, arquivo = ARQUIVO_TRAVA) {
  if (lerTrava(arquivo)?.pid === pid) rmSync(arquivo, { force: true });
}

function dormir(segundos) {
  Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, segundos * 1000);
}

/**
 * Espera a vez do Unity: nenhuma outra trava viva e nenhum editor aberto (`editoresAbertos`
 * devolve a lista). Desiste depois de UNITY_ESPERA_MIN minutos (padrão 30) e sai com 1.
 */
export function esperarAVez({ acao, editoresAbertos, cwd = process.cwd() }) {
  const limiteMin = Number(process.env.UNITY_ESPERA_MIN ?? ESPERA_PADRAO_MIN);
  const fim = Date.now() + limiteMin * 60_000;
  const dono = { pid: process.pid, acao, cwd, inicio: new Date().toISOString() };
  let avisado = "";
  for (;;) {
    const abertos = editoresAbertos();
    if (abertos.length === 0 && tentarPegar(dono)) {
      process.on("exit", () => soltar());
      return;
    }
    const atual = lerTrava();
    const motivo = atual
      ? `unity:${atual.acao} em ${atual.cwd} (pid ${atual.pid}, desde ${atual.inicio})`
      : `editor do Unity aberto: ${abertos.map((l) => l.replace("|", " ")).join("; ")}`;
    if (Date.now() >= fim) {
      console.error(`✖ Desisti depois de ${limiteMin} min esperando o Unity. Ocupado por ${motivo}.`);
      process.exit(1);
    }
    if (motivo !== avisado) {
      console.log(`⏳ Unity ocupado por ${motivo}. Esperando a vez (até ${limiteMin} min)...`);
      avisado = motivo;
    }
    dormir(INTERVALO_S);
  }
}
