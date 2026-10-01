import { withDiscount } from "./checkout-grant.js";

const STORE_URL = "https://rusticopr.com";

export function productUrl(handle: string) {
  return `${STORE_URL}/products/${handle}`;
}

export function cartPermalink(
  lines: { variantId: string | number; qty: number }[],
  opts?: { discountCode?: string },
) {
  const valid = lines.filter((l) => l.qty > 0 && l.variantId);
  if (!valid.length) return `${STORE_URL}/cart`;
  const path = valid.map((l) => `${l.variantId}:${l.qty}`).join(",");
  const url = `${STORE_URL}/cart/${path}`;
  if (opts?.discountCode) return withDiscount(url, opts.discountCode);
  return url;
}
