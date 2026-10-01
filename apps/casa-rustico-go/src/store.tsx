import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { AppState } from "react-native";
import AsyncStorage from "@react-native-async-storage/async-storage";
import { getProduct } from "./catalog";
import { grantOnReturn } from "./checkout-grant.js";
import { memberNumber } from "./rewards";
import { WELCOME_STORAGE_KEY } from "./welcome/theme";

export type CartLine = {
  productId: string;
  variantId: number;
  variantTitle: string;
  qty: number;
  price: number;
};

export type Member = {
  name: string;
  email: string;
  beans: number;
  lifetime: number;
  memberNo: string;
  joinedAt: string;
};

const CART_KEY = "casa-rustico-go.v4";
const MEMBER_KEY = "casa-rustico-go.member.v1";
const GRANT_KEY = "casa-rustico-go.grant.v1";

type PendingGrant = { fingerprint: string; beans: number };

function sanitizeLines(lines: unknown): CartLine[] {
  if (!Array.isArray(lines)) return [];
  const out: CartLine[] = [];
  for (const raw of lines) {
    if (!raw || typeof raw !== "object") continue;
    const line = raw as Partial<CartLine>;
    const qty = Number(line.qty);
    const price = Number(line.price);
    const variantId = Number(line.variantId);
    if (typeof line.productId !== "string" || !line.productId) continue;
    if (!Number.isFinite(qty) || qty <= 0) continue;
    if (!Number.isFinite(variantId) || !Number.isFinite(price) || price < 0) continue;
    out.push({
      productId: line.productId,
      variantId,
      variantTitle: typeof line.variantTitle === "string" ? line.variantTitle : "",
      qty: Math.min(99, Math.floor(qty)),
      price,
    });
  }
  return out;
}

type ShopApi = {
  hydrated: boolean;
  welcomeSeen: boolean;
  toast: string | null;
  cart: CartLine[];
  member: Member | null;
  markWelcomeSeen: () => void;
  replayWelcome: () => void;
  flash: (msg: string) => void;
  addToCart: (line: Omit<CartLine, "qty"> & { qty?: number }) => void;
  setCartQty: (productId: string, variantId: number, qty: number) => void;
  removeFromCart: (productId: string, variantId: number) => void;
  clearCart: () => void;
  joinRewards: (name: string, email: string) => Member;
  signOutMember: () => void;
  earnBeans: (n: number) => number;
  armCheckoutGrant: (fingerprint: string, beans: number) => void;
};

const ShopContext = createContext<ShopApi | null>(null);

export function ShopProvider({ children }: { children: ReactNode }) {
  const [hydrated, setHydrated] = useState(false);
  const [welcomeSeen, setWelcomeSeen] = useState(true);
  const [toast, setToast] = useState<string | null>(null);
  const [cart, setCart] = useState<CartLine[]>([]);
  const [member, setMember] = useState<Member | null>(null);
  const toastTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const pendingGrant = useRef<PendingGrant | null>(null);
  const lastGrant = useRef<string | null>(null);

  useEffect(() => {
    let alive = true;
    (async () => {
      let lines: CartLine[] = [];
      let nextMember: Member | null = null;
      let seen = false;
      try {
        const rawCart = await AsyncStorage.getItem(CART_KEY);
        if (rawCart) {
          const parsed = JSON.parse(rawCart) as { cart?: unknown } | unknown;
          const rawLines = Array.isArray(parsed) ? parsed : (parsed as { cart?: unknown })?.cart;
          lines = sanitizeLines(rawLines);
        }
      } catch {
        lines = [];
      }
      try {
        const rawMember = await AsyncStorage.getItem(MEMBER_KEY);
        if (rawMember) {
          const parsed = JSON.parse(rawMember) as Member;
          if (parsed?.memberNo && parsed?.email) nextMember = parsed;
        }
      } catch {
        nextMember = null;
      }
      try {
        seen = (await AsyncStorage.getItem(WELCOME_STORAGE_KEY)) === "1";
      } catch {
        seen = false;
      }
      try {
        const rawGrant = await AsyncStorage.getItem(GRANT_KEY);
        if (rawGrant) lastGrant.current = rawGrant;
      } catch {
        /* a missing grant key just allows the next real return to count */
      }
      if (!alive) return;
      setCart(lines);
      setMember(nextMember);
      setWelcomeSeen(seen);
      setHydrated(true);
    })();
    return () => {
      alive = false;
    };
  }, []);

  useEffect(() => {
    if (!hydrated) return;
    void AsyncStorage.setItem(CART_KEY, JSON.stringify({ cart })).catch(() => undefined);
  }, [cart, hydrated]);

  useEffect(() => {
    if (!hydrated) return;
    if (!member) {
      void AsyncStorage.removeItem(MEMBER_KEY).catch(() => undefined);
      return;
    }
    void AsyncStorage.setItem(MEMBER_KEY, JSON.stringify(member)).catch(() => undefined);
  }, [member, hydrated]);

  const markWelcomeSeen = useCallback(() => {
    setWelcomeSeen(true);
    void AsyncStorage.setItem(WELCOME_STORAGE_KEY, "1").catch(() => undefined);
  }, []);

  const replayWelcome = useCallback(() => {
    setWelcomeSeen(false);
    void AsyncStorage.removeItem(WELCOME_STORAGE_KEY).catch(() => undefined);
  }, []);

  const flash = useCallback((msg: string) => {
    setToast(msg);
    if (toastTimer.current) clearTimeout(toastTimer.current);
    toastTimer.current = setTimeout(() => {
      setToast((cur) => (cur === msg ? null : cur));
    }, 1700);
  }, []);

  const addToCart = useCallback((line: Omit<CartLine, "qty"> & { qty?: number }) => {
    const qty = line.qty ?? 1;
    setCart((s) => {
      const i = s.findIndex((l) => l.productId === line.productId && l.variantId === line.variantId);
      if (i >= 0) {
        const next = [...s];
        next[i] = { ...next[i], qty: next[i].qty + qty };
        return next;
      }
      return [...s, { ...line, qty }];
    });
  }, []);

  const setCartQty = useCallback((productId: string, variantId: number, qty: number) => {
    setCart((s) =>
      qty <= 0
        ? s.filter((l) => !(l.productId === productId && l.variantId === variantId))
        : s.map((l) => (l.productId === productId && l.variantId === variantId ? { ...l, qty } : l)),
    );
  }, []);

  const removeFromCart = useCallback((productId: string, variantId: number) => {
    setCart((s) => s.filter((l) => !(l.productId === productId && l.variantId === variantId)));
  }, []);

  const clearCart = useCallback(() => setCart([]), []);

  const joinRewards = useCallback((name: string, email: string) => {
    const seed = `${email.trim().toLowerCase()}|${Date.now()}`;
    const next: Member = {
      name: name.trim() || "Member",
      email: email.trim().toLowerCase(),
      beans: 0,
      lifetime: 0,
      memberNo: memberNumber(seed),
      joinedAt: new Date().toISOString(),
    };
    setMember(next);
    return next;
  }, []);

  const signOutMember = useCallback(() => setMember(null), []);

  const earnBeans = useCallback((n: number) => {
    let awarded = 0;
    setMember((cur) => {
      if (!cur || n <= 0) return cur;
      awarded = n;
      return { ...cur, beans: cur.beans + n, lifetime: cur.lifetime + n };
    });
    return awarded;
  }, []);

  const armCheckoutGrant = useCallback((fingerprint: string, beans: number) => {
    if (!fingerprint || beans <= 0) {
      pendingGrant.current = null;
      return;
    }
    pendingGrant.current = { fingerprint, beans };
  }, []);

  const settleCheckoutGrant = useCallback(
    (leftApp: boolean) => {
      const decision = grantOnReturn({
        pending: pendingGrant.current,
        lastGranted: lastGrant.current,
        leftApp,
      });
      pendingGrant.current = decision.pending;
      if (decision.lastGranted && decision.lastGranted !== lastGrant.current) {
        lastGrant.current = decision.lastGranted;
        void AsyncStorage.setItem(GRANT_KEY, decision.lastGranted).catch(() => undefined);
      }
      if (decision.grant > 0) earnBeans(decision.grant);
      return decision.grant as number;
    },
    [earnBeans],
  );

  useEffect(() => {
    let left = false;
    const sub = AppState.addEventListener("change", (state) => {
      if (state !== "active") {
        left = true;
        return;
      }
      if (!left) return;
      left = false;
      const n = settleCheckoutGrant(true);
      if (n > 0) flash(`+${n} beans · Hacienda Rewards`);
    });
    return () => sub.remove();
  }, [flash, settleCheckoutGrant]);

  const api = useMemo<ShopApi>(
    () => ({
      hydrated,
      welcomeSeen,
      toast,
      cart,
      member,
      markWelcomeSeen,
      replayWelcome,
      flash,
      addToCart,
      setCartQty,
      removeFromCart,
      clearCart,
      joinRewards,
      signOutMember,
      earnBeans,
      armCheckoutGrant,
    }),
    [
      hydrated,
      welcomeSeen,
      toast,
      cart,
      member,
      markWelcomeSeen,
      replayWelcome,
      flash,
      addToCart,
      setCartQty,
      removeFromCart,
      clearCart,
      joinRewards,
      signOutMember,
      earnBeans,
      armCheckoutGrant,
    ],
  );

  return <ShopContext.Provider value={api}>{children}</ShopContext.Provider>;
}

export function useShop() {
  const ctx = useContext(ShopContext);
  if (!ctx) throw new Error("useShop must be used inside ShopProvider");
  return ctx;
}

export function cartCount(cart: CartLine[]) {
  return cart.reduce((n, l) => n + l.qty, 0);
}

export function lineUnitPrice(line: CartLine): number {
  const product = getProduct(line.productId);
  const variant = product?.variants?.find((v) => v.id === line.variantId);
  if (variant) return variant.price;
  if (product && product.defaultVariantId === line.variantId) return product.price;
  return line.price;
}

export function cartSubtotal(cart: CartLine[]) {
  return cart.reduce((n, l) => n + lineUnitPrice(l) * l.qty, 0);
}

export function firstName(name: string | null | undefined) {
  const t = (name ?? "").trim();
  if (!t) return "there";
  return t.split(/\s+/)[0];
}
