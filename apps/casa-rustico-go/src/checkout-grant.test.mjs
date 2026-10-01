import assert from "node:assert/strict";
import test from "node:test";
import { cartFingerprint, grantOnReturn, withDiscount } from "./checkout-grant.js";

test("fingerprint ignores order and drops empty lines", () => {
  assert.equal(
    cartFingerprint([
      { variantId: 2, qty: 1 },
      { variantId: 1, qty: 2.9 },
      { variantId: 3, qty: 0 },
    ]),
    "1:2,2:1",
  );
});

test("beans wait until the shopper leaves and do not repeat", () => {
  const pending = { fingerprint: "1:1", beans: 12 };
  assert.deepEqual(grantOnReturn({ pending, lastGranted: null, leftApp: false }), {
    grant: 0,
    lastGranted: null,
    pending,
  });
  assert.deepEqual(grantOnReturn({ pending, lastGranted: null, leftApp: true }), {
    grant: 12,
    lastGranted: "1:1",
    pending: null,
  });
  assert.deepEqual(grantOnReturn({ pending, lastGranted: "1:1", leftApp: true }), {
    grant: 0,
    lastGranted: "1:1",
    pending: null,
  });
});

test("discount url redirects back to the cart", () => {
  assert.equal(
    withDiscount("https://rusticopr.com/cart/9:1", "MORNING10"),
    "https://rusticopr.com/discount/MORNING10?redirect=%2Fcart%2F9%3A1",
  );
});
