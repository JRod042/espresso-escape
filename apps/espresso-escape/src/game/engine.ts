import { direct } from "./director";
import {
  type Bean,
  type Hazard,
  type HazardKind,
  type CoachCue,
  type World,
  BUFFER_S,
  COYOTE_S,
  HEEL_MERCY_S,
  JUMP_V,
  MAX_DT,
  PLAYER_H,
  aabbHits,
  beanHitbox,
  gravityFor,
  hazardHitbox,
  mulberry32,
  playerHitbox,
  speedForRun,
} from "./physics";

export type { CoachCue };

export type DeathKind = HazardKind;

export type Run = {
  world: World;
  playerX: number;
  playerY: number;
  vy: number;
  holding: boolean;
  coyote: number;
  buffer: number;
  airborne: boolean;
  hazards: Hazard[];
  beans: Bean[];
  score: number;
  beansTaken: number;
  nextId: number;
  time: number;
  distance: number;
  untilHazard: number;
  hazardsSpawned: number;
  lastKind: HazardKind | null;
  seenPorta: boolean;
  seenSteam: boolean;
  cue: CoachCue;
  cueFor: number;
  rng: () => number;
  paused: boolean;
  dead: boolean;
  deathKind: DeathKind | null;
  jumped: boolean;
  justJumped: boolean;
  justLanded: boolean;
  justBean: number;
  heelMercy: number;
};

export function createRun(
  world: World,
  playerX: number,
  seed = Date.now()
): Run {
  return {
    world,
    playerX,
    playerY: world.groundY - PLAYER_H,
    vy: 0,
    holding: false,
    coyote: COYOTE_S,
    buffer: 0,
    airborne: false,
    hazards: [],
    beans: [],
    score: 0,
    beansTaken: 0,
    nextId: 1,
    time: 0,
    distance: 0,
    untilHazard: 0,
    hazardsSpawned: 0,
    lastKind: null,
    seenPorta: false,
    seenSteam: false,
    cue: "tap",
    cueFor: 3.2,
    rng: mulberry32(seed >>> 0 || 1),
    paused: false,
    dead: false,
    deathKind: null,
    jumped: false,
    justJumped: false,
    justLanded: false,
    justBean: 0,
    heelMercy: 0,
  };
}

export function resizeRun(run: Run, world: World, playerX: number): void {
  const dy = world.groundY - run.world.groundY;
  run.world = world;
  run.playerX = playerX;
  if (dy !== 0) {
    run.playerY += dy;
    for (const hazard of run.hazards) hazard.y += dy;
    for (const bean of run.beans) bean.y += dy;
  }
  const floor = world.groundY - PLAYER_H;
  if (run.playerY > floor) run.playerY = floor;
}

function tryJump(run: Run): boolean {
  if (run.paused || run.dead) return false;
  const floor = run.world.groundY - PLAYER_H;
  const grounded = run.playerY >= floor - 1;
  if (!grounded && run.coyote <= 0) return false;
  run.vy = JUMP_V;
  run.playerY = Math.min(run.playerY, floor - 0.5);
  run.jumped = true;
  run.justJumped = true;
  run.airborne = true;
  run.coyote = 0;
  run.buffer = 0;
  if (run.cue === "tap") {
    run.cue = null;
    run.cueFor = 0;
  }
  return true;
}

/** Touch-down. Buffers if the roast is still in the air. */
export function requestJump(run: Run): boolean {
  if (run.paused || run.dead) return false;
  run.holding = true;
  if (tryJump(run)) return true;
  run.buffer = BUFFER_S;
  return false;
}

export function releaseJump(run: Run): void {
  run.holding = false;
}

function swapPop<T>(list: T[], i: number): void {
  const last = list[list.length - 1];
  if (last === undefined) return;
  list[i] = last;
  list.pop();
}

/** Advance one frame. Mutates `run` in place — no allocations on the quiet path. */
export function tick(run: Run, dt: number): void {
  if (run.paused || run.dead) return;
  const step = dt > MAX_DT ? MAX_DT : dt < 0 ? 0 : dt;
  if (step === 0) return;

  run.justJumped = false;
  run.justLanded = false;
  run.justBean = 0;

  const speed = speedForRun(run.time);
  const playerX = run.playerX;
  const floor = run.world.groundY - PLAYER_H;

  run.heelMercy = Math.max(0, run.heelMercy - step);
  run.vy += gravityFor(run.vy, run.holding) * step;
  run.playerY += run.vy * step;
  if (run.playerY >= floor) {
    if (run.airborne) {
      run.justLanded = true;
      if (run.jumped) run.heelMercy = HEEL_MERCY_S;
    }
    run.playerY = floor;
    run.vy = 0;
    run.airborne = false;
    run.coyote = COYOTE_S;
    if (run.buffer > 0) tryJump(run);
  } else {
    run.airborne = true;
    run.coyote = Math.max(0, run.coyote - step);
    run.buffer = Math.max(0, run.buffer - step);
  }

  run.time += step;
  run.distance += speed * step;
  run.score += step * 8;

  direct(run, step);

  for (let i = 0; i < run.hazards.length; ) {
    const h = run.hazards[i];
    h.x -= speed * step;
    if (h.x + h.w <= -48) {
      run.score += 2;
      swapPop(run.hazards, i);
    } else i += 1;
  }

  const me = playerHitbox(playerX, run.playerY);
  for (let i = 0; i < run.beans.length; ) {
    const b = run.beans[i];
    b.x -= speed * step;
    if (b.taken || b.x + b.w <= -24) {
      swapPop(run.beans, i);
      continue;
    }
    if (aabbHits(me, beanHitbox(b))) {
      run.score += 5;
      run.beansTaken += 1;
      run.justBean += 5;
      swapPop(run.beans, i);
      continue;
    }
    i += 1;
  }

  for (let i = 0; i < run.hazards.length; i += 1) {
    const h = run.hazards[i];
    if (aabbHits(me, hazardHitbox(h))) {
      if (run.heelMercy > 0 && h.kind !== "steam") continue;
      run.dead = true;
      run.deathKind = h.kind;
      return;
    }
  }
}
