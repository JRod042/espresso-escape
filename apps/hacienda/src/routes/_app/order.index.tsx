import { createFileRoute, getRouteApi, useNavigate } from "@tanstack/react-router";
import { useMemo, useState } from "react";
import { Search } from "lucide-react";
import { LargeTitle } from "@/components/app-shell";
import { ProductTile } from "@/components/product-tile";
import { CATEGORIES, PRODUCTS, STORES, productsIn, type CategoryId } from "@/lib/catalog";
import { useCart } from "@/store/cart";
import { cn } from "@/lib/cn";

const orderRoute = getRouteApi("/_app/order");

export const Route = createFileRoute("/_app/order/")({
  component: Order,
});

function Order() {
  const { cat } = orderRoute.useSearch();
  const active: CategoryId = CATEGORIES.some((c) => c.id === cat)
    ? (cat as CategoryId)
    : "featured";
  const [q, setQ] = useState("");
  const storeId = useCart((s) => s.storeId);
  const setStore = useCart((s) => s.setStore);
  const navigate = useNavigate();
  const store = STORES.find((s) => s.id === storeId);
  const list = useMemo(() => {
    const base = productsIn(active);
    if (!q.trim()) return base;
    const n = q.trim().toLowerCase();
    return PRODUCTS.filter(
      (p) =>
        p.name.toLowerCase().includes(n) ||
        p.tagline.toLowerCase().includes(n) ||
        p.origin?.toLowerCase().includes(n),
    );
  }, [active, q]);

  return (
    <>
      <LargeTitle kicker={store ? `${store.city} · ${store.hours}` : "Menu"} title="Order" />
      <div className="px-5">
        <div className="flex h-11 items-center gap-2 rounded-full bg-ink/8 px-3">
          <Search className="size-4 text-muted" />
          <input
            value={q}
            onChange={(e) => setQ(e.target.value)}
            placeholder="Search the short menu"
            className="h-11 flex-1 bg-transparent text-[16px] outline-none placeholder:text-muted"
          />
        </div>
        <div className="mt-3 flex gap-2 overflow-x-auto snap-x pb-1">
          {STORES.filter((s) => s.pickup).map((s) => (
            <button
              key={s.id}
              type="button"
              onClick={() => setStore(s.id)}
              className={cn(
                "snap-item shrink-0 rounded-full px-3 py-2 text-[13px]",
                storeId === s.id ? "bg-forest text-cream" : "bg-card text-ink",
              )}
            >
              {s.city}
            </button>
          ))}
        </div>
      </div>
      <div className="sticky top-0 z-10 mt-3 bg-paper/90 px-5 py-2 backdrop-blur-md">
        <div className="flex gap-2 overflow-x-auto snap-x">
          {CATEGORIES.map((c) => (
            <button
              key={c.id}
              type="button"
              onClick={() => {
                void navigate({ to: "/order", search: { cat: c.id } });
              }}
              className={cn(
                "snap-item shrink-0 rounded-full px-3.5 py-2 text-[14px] font-medium",
                active === c.id ? "bg-ink text-cream" : "bg-ink/8 text-ink",
              )}
            >
              {c.label}
            </button>
          ))}
        </div>
      </div>
      <div className="grid grid-cols-2 gap-3 px-5 pb-6 pt-2">
        {list.map((p) => (
          <ProductTile key={p.id} product={p} />
        ))}
      </div>
    </>
  );
}
