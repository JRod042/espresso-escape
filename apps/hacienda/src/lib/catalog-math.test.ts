import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { beanRateFor, nextReward, rewardOffCents } from "./catalog.ts";

describe("reward math", () => {
  it("uses the same cent discounts as checkout", () => {
    assert.equal(rewardOffCents(undefined), 0);
    assert.equal(rewardOffCents("r25"), 70);
    assert.equal(rewardOffCents("r50"), 325);
    assert.equal(rewardOffCents("r150"), 450);
    assert.equal(rewardOffCents("r200"), 750);
    assert.equal(rewardOffCents("r400"), 1495);
    assert.equal(rewardOffCents("missing"), 0);
  });

  it("pays Hacienda tier 2× on card, and Casa Card 2× below that", () => {
    assert.equal(beanRateFor(0, "card"), 1);
    assert.equal(beanRateFor(0, "wallet"), 2);
    assert.equal(beanRateFor(100, "card"), 1);
    assert.equal(beanRateFor(100, "wallet"), 2);
    assert.equal(beanRateFor(300, "card"), 2);
    assert.equal(beanRateFor(300, "wallet"), 2);
  });

  it("measures progress inside the current bean band", () => {
    const mid = nextReward(100);
    assert.equal(mid.have, 50);
    assert.equal(mid.need, 100);
    assert.equal(mid.have / mid.need, 0.5);
  });
});
