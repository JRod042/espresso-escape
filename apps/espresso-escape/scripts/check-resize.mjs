/** Layout changes must keep hazards and beans on the new floor. */
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const root = join(dirname(fileURLToPath(import.meta.url)), "..");
const src = readFileSync(join(root, "src/game/engine.ts"), "utf8");
if (!/const dy = world\.groundY - run\.world\.groundY/.test(src)) {
  throw new Error("resizeRun must measure the floor delta");
}
if (!/hazard\.y \+= dy/.test(src) || !/bean\.y \+= dy/.test(src)) {
  throw new Error("resizeRun must shift hazards and beans with the floor");
}

function shift(run, nextGround) {
  const dy = nextGround - run.groundY;
  return {
    groundY: nextGround,
    playerY: run.playerY + dy,
    hazardY: run.hazardY + dy,
    beanY: run.beanY + dy,
  };
}

const moved = shift({ groundY: 700, playerY: 662, hazardY: 654, beanY: 644 }, 640);
if (moved.playerY !== 602 || moved.hazardY !== 594 || moved.beanY !== 584) {
  throw new Error("floor delta should move every body by the same amount");
}
if (moved.groundY !== 640) throw new Error("new floor should stick");

console.log("check-resize: PASS");
