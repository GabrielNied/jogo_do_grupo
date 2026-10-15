// Trava `em-andamento` que ninguém está usando. O puxar grava a branch e o início
// no comentário da issue. Sem essa branch no origin e sem PR aberto, a trava sai
// depois de HORAS_TRAVA_ABANDONADA (o agente parou sem liberar).

export const HORAS_TRAVA_ABANDONADA = 24;

export function travaDoComentario(body) {
  if (!body?.includes("Tarefa em andamento")) return null;
  const branch = body.match(/\*\*Branch:\*\*\s+`([^`]+)`/)?.[1] ?? null;
  const inicio = body.match(/\*\*Início:\*\*\s+(\S+)/)?.[1] ?? null;
  const inicioMs = inicio ? Date.parse(inicio) : NaN;
  if (!branch || !Number.isFinite(inicioMs)) return null;
  return { branch, inicioMs };
}

export function ultimaTrava(comentarios) {
  let achada = null;
  for (const comentario of comentarios) {
    const trava = travaDoComentario(comentario.body);
    if (trava) achada = trava;
  }
  return achada;
}

export function travaAbandonada({
  trava,
  agoraMs,
  horas = HORAS_TRAVA_ABANDONADA,
  branchNoOrigin,
  prAberto,
}) {
  if (!trava || branchNoOrigin || prAberto) return false;
  return agoraMs - trava.inicioMs >= horas * 3_600_000;
}

export function motivoTravaAbandonada(horas = HORAS_TRAVA_ABANDONADA) {
  return `Trava abandonada: passou de ${horas} h sem a branch no origin e sem PR aberto.`;
}
