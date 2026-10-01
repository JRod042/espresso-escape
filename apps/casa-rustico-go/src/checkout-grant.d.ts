export type PendingGrant = { fingerprint: string; beans: number };

export function cartFingerprint(
  lines: { variantId: string | number; qty: number }[],
): string;

export function grantOnReturn(opts: {
  pending: PendingGrant | null;
  lastGranted: string | null;
  leftApp: boolean;
}): { grant: number; lastGranted: string | null; pending: PendingGrant | null };

export function withDiscount(cartUrl: string, code: string): string;
