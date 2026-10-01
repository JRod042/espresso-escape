/** Cart identity used so the same bag cannot earn beans twice. */
export function cartFingerprint(lines) {
  return [...lines]
    .filter((line) => line && Number(line.qty) > 0 && line.variantId != null)
    .map((line) => `${line.variantId}:${Math.floor(Number(line.qty))}`)
    .sort()
    .join(",");
}

/**
 * Beans post once, after the shopper actually leaves for checkout and comes back.
 * The same fingerprint does not pay again.
 */
export function grantOnReturn({ pending, lastGranted, leftApp }) {
  if (!pending) return { grant: 0, lastGranted: lastGranted ?? null, pending: null };
  if (!leftApp) return { grant: 0, lastGranted: lastGranted ?? null, pending };
  if (lastGranted && lastGranted === pending.fingerprint) {
    return { grant: 0, lastGranted, pending: null };
  }
  return {
    grant: pending.beans,
    lastGranted: pending.fingerprint,
    pending: null,
  };
}

/** Shopify discount entry that lands on the cart permalink. */
export function withDiscount(cartUrl, code) {
  const url = new URL(cartUrl);
  const path = `${url.pathname}${url.search}`;
  return `${url.origin}/discount/${encodeURIComponent(code)}?redirect=${encodeURIComponent(path)}`;
}
