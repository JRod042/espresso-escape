import { useState } from "react";
import { Drawer } from "vaul";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import { Minus, Plus, X } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { getProfile, placeOrder } from "@/lib/api";
import { productById, REWARDS, rewardOffCents, storeById, STORES } from "@/lib/catalog";
import { money } from "@/lib/format";
import { useCurrentUserState } from "@/lib/auth/use-current-user";
import { cartTotal, useCart } from "@/store/cart";
import { cn } from "@/lib/cn";

export function CartSheet({
  open,
  onOpenChange,
}: {
  open: boolean;
  onOpenChange: (v: boolean) => void;
}) {
  const { user, isPending } = useCurrentUserState();
  const items = useCart((s) => s.items);
  const storeId = useCart((s) => s.storeId);
  const setStore = useCart((s) => s.setStore);
  const setQty = useCart((s) => s.setQty);
  const clear = useCart((s) => s.clear);
  const navigate = useNavigate();
  const qc = useQueryClient();
  const profileQ = useQuery({
    queryKey: ["profile"],
    queryFn: () => getProfile(),
    enabled: Boolean(user),
  });
  const [payWith, setPayWith] = useState<"wallet" | "card">("wallet");
  const [redeem, setRedeem] = useState<string | undefined>();
  const [busy, setBusy] = useState(false);
  const off = rewardOffCents(redeem);
  const total = Math.max(0, cartTotal(items) - off);
  const store = storeById(storeId);

  async function checkout() {
    if (!user) {
      onOpenChange(false);
      void navigate({ to: "/login" });
      return;
    }
    if (store && !store.pickup) {
      toast.error("Pick a café. That location ships bags.");
      return;
    }
    setBusy(true);
    try {
      const res = await placeOrder({
        data: {
          storeId,
          payWith,
          redeemRewardId: redeem,
          items: items.map((it) => ({
            productId: it.productId,
            qty: it.qty,
            custom: it.custom,
          })),
        },
      });
      clear();
      await qc.invalidateQueries();
      onOpenChange(false);
      toast.success(
        `Order #${res.orderId} · ${money(res.totalCents)} · +${res.beansEarned} beans`,
      );
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Could not place order");
    } finally {
      setBusy(false);
    }
  }

  return (
    <Drawer.Root open={open} onOpenChange={onOpenChange}>
      <Drawer.Portal>
        <Drawer.Overlay className="fixed inset-0 z-50 bg-ink/40" />
        <Drawer.Content className="fixed inset-x-0 bottom-0 z-50 flex max-h-[92dvh] flex-col rounded-t-xl bg-paper">
          <div className="mx-auto mt-2 h-1 w-10 rounded-full bg-ink/15" />
          <div className="flex items-center justify-between px-5 py-3">
            <Drawer.Title className="font-display text-[22px] tracking-tight">
              Your order
            </Drawer.Title>
            <button
              type="button"
              onClick={() => onOpenChange(false)}
              className="grid size-9 place-items-center rounded-full bg-ink/8"
              aria-label="Close"
            >
              <X className="size-4" />
            </button>
          </div>
          <div className="min-h-0 flex-1 overflow-y-auto px-5 pb-6">
            <label className="mb-3 block text-[13px] font-medium text-muted">Pickup</label>
            <div className="mb-4 flex gap-2 overflow-x-auto snap-x pb-1">
              {STORES.filter((s) => s.pickup).map((s) => (
                <button
                  key={s.id}
                  type="button"
                  onClick={() => setStore(s.id)}
                  className={cn(
                    "snap-item shrink-0 rounded-full px-3 py-2 text-[13px]",
                    storeId === s.id ? "bg-forest text-cream" : "bg-ink/8 text-ink",
                  )}
                >
                  {s.city}
                </button>
              ))}
            </div>
            <p className="mb-3 text-[13px] text-muted">{store?.name} · {store?.hours}</p>
            <ul className="space-y-3">
              {items.map((it) => {
                const p = productById(it.productId);
                if (!p) return null;
                const bits = [
                  it.custom.size ? `${it.custom.size} oz` : null,
                  it.custom.milk,
                  it.custom.syrup && it.custom.syrup !== "None" ? it.custom.syrup : null,
                  it.custom.grind,
                ].filter(Boolean);
                return (
                  <li key={it.key} className="flex gap-3">
                    <img src={p.image} alt="" className="size-16 rounded-md object-cover" />
                    <div className="min-w-0 flex-1">
                      <p className="text-[15px] font-medium">{p.name}</p>
                      {bits.length ? (
                        <p className="text-[12px] text-muted">{bits.join(" · ")}</p>
                      ) : null}
                      <div className="mt-1 flex items-center gap-2">
                        <button
                          type="button"
                          className="grid size-7 place-items-center rounded-full bg-ink/8"
                          onClick={() => setQty(it.key, it.qty - 1)}
                          aria-label="Decrease"
                        >
                          <Minus className="size-3.5" />
                        </button>
                        <span className="w-4 text-center tabular-nums text-[15px]">{it.qty}</span>
                        <button
                          type="button"
                          className="grid size-7 place-items-center rounded-full bg-ink/8"
                          onClick={() => setQty(it.key, it.qty + 1)}
                          aria-label="Increase"
                        >
                          <Plus className="size-3.5" />
                        </button>
                      </div>
                    </div>
                  </li>
                );
              })}
            </ul>

            {user && profileQ.data ? (
              <div className="mt-5">
                <p className="mb-2 text-[13px] font-medium text-muted">Pay with</p>
                <div className="grid grid-cols-2 gap-2">
                  <button
                    type="button"
                    onClick={() => setPayWith("wallet")}
                    className={cn(
                      "rounded-md px-3 py-3 text-left",
                      payWith === "wallet" ? "bg-forest text-cream" : "bg-card",
                    )}
                  >
                    <p className="text-[13px] opacity-70">Casa Card · 2× beans</p>
                    <p className="tabular-nums text-[17px] font-medium">
                      {money(profileQ.data.walletCents)}
                    </p>
                  </button>
                  <button
                    type="button"
                    onClick={() => setPayWith("card")}
                    className={cn(
                      "rounded-md px-3 py-3 text-left",
                      payWith === "card" ? "bg-forest text-cream" : "bg-card",
                    )}
                  >
                    <p className="text-[13px] opacity-70">Card · 1× beans</p>
                    <p className="text-[17px] font-medium">Bank card</p>
                  </button>
                </div>
                <p className="mt-4 mb-2 text-[13px] font-medium text-muted">Redeem</p>
                <div className="flex flex-wrap gap-2">
                  <button
                    type="button"
                    onClick={() => setRedeem(undefined)}
                    className={cn(
                      "rounded-full px-3 py-1.5 text-[13px]",
                      !redeem ? "bg-ink text-cream" : "bg-ink/8",
                    )}
                  >
                    None
                  </button>
                  {REWARDS.filter((r) => profileQ.data && profileQ.data.beans >= r.beans).map(
                    (r) => (
                      <button
                        key={r.id}
                        type="button"
                        onClick={() => setRedeem(r.id)}
                        className={cn(
                          "rounded-full px-3 py-1.5 text-[13px]",
                          redeem === r.id ? "bg-clay text-cream" : "bg-ink/8",
                        )}
                      >
                        {r.beans} · {r.name}
                      </button>
                    ),
                  )}
                </div>
              </div>
            ) : null}
          </div>
          <div
            className="border-t border-line bg-paper px-5 pt-3"
            style={{ paddingBottom: "max(16px, env(safe-area-inset-bottom))" }}
          >
            <div className="mb-3 flex items-center justify-between text-[17px]">
              <span>Total</span>
              <span className="text-right">
                {off > 0 ? (
                  <span className="mr-2 text-[13px] font-normal text-muted">−{money(off)}</span>
                ) : null}
                <span className="tabular-nums font-medium">{money(total)}</span>
              </span>
            </div>
            <Button
              size="lg"
              variant="forest"
              className="w-full"
              disabled={busy || !items.length || isPending}
              onClick={() => void checkout()}
            >
              {user ? (busy ? "Placing…" : "Place order") : "Sign in to order"}
            </Button>
          </div>
        </Drawer.Content>
      </Drawer.Portal>
    </Drawer.Root>
  );
}
