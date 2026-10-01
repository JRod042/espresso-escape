import { createFileRoute, Link } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { ChevronRight } from "lucide-react";
import { LargeTitle } from "@/components/app-shell";
import { AvatarLink } from "@/components/avatar-link";
import { ProductTile } from "@/components/product-tile";
import { RewardsCard, RewardsCardGuest } from "@/components/rewards-card";
import { getProfile, listOrders, type Customization } from "@/lib/api";
import { useCurrentUserState } from "@/lib/auth/use-current-user";
import { OFFERS, PRODUCTS, productById, storeById, type Offer } from "@/lib/catalog";
import { useCart } from "@/store/cart";
import { firstName, greeting, money, relativeTime } from "@/lib/format";

export const Route = createFileRoute("/_app/")({ component: Home });

function Home() {
  const { user } = useCurrentUserState();
  const profileQ = useQuery({
    queryKey: ["profile"],
    queryFn: () => getProfile(),
    enabled: Boolean(user),
  });
  const ordersQ = useQuery({
    queryKey: ["orders"],
    queryFn: () => listOrders(),
    enabled: Boolean(user),
  });
  const name = firstName(
    profileQ.data?.displayName ?? user?.displayName,
    user?.primaryEmail,
  );
  const add = useCart((s) => s.add);
  const clearCart = useCart((s) => s.clear);
  const featured = PRODUCTS.filter((p) => p.featured);
  const last = ordersQ.data?.[0];

  function replayLast() {
    if (!last) return;
    try {
      const parsed = JSON.parse(last.itemsJson) as {
        productId?: string;
        qty?: number;
        custom?: Customization;
      }[];
      if (!Array.isArray(parsed)) return;
      const lines = parsed.filter((line) => line.productId && productById(line.productId));
      if (!lines.length) return;
      clearCart();
      for (const line of lines) {
        add(line.productId!, line.custom ?? {}, line.qty && line.qty > 0 ? line.qty : 1);
      }
    } catch {
      /* still open the menu */
    }
  }

  return (
    <>
      <LargeTitle
        kicker={greeting()}
        title={user ? name : "Casa Rústico"}
        trailing={<AvatarLink />}
      />

      {user && profileQ.data ? (
        <Link to="/account" className="block">
          <RewardsCard profile={profileQ.data} />
        </Link>
      ) : (
        <Link to={user ? "/account" : "/login"} className="block">
          <RewardsCardGuest />
        </Link>
      )}

      <section className="mt-7">
        <div className="mb-3 flex items-end justify-between px-5">
          <h2 className="text-[20px] font-semibold tracking-tight">For you</h2>
        </div>
        <div className="flex gap-3 overflow-x-auto px-5 snap-x">
          {OFFERS.map((o) => {
            const card = <OfferCard offer={o} />;
            const className = "pressable snap-item relative block min-w-[78%] overflow-hidden rounded-lg";
            if (o.href.startsWith("/account")) {
              return (
                <Link key={o.id} to="/account" className={className}>
                  {card}
                </Link>
              );
            }
            const cat = new URLSearchParams(o.href.split("?")[1] ?? "").get("cat") ?? undefined;
            return (
              <Link key={o.id} to="/order" search={cat ? { cat } : {}} className={className}>
                {card}
              </Link>
            );
          })}
        </div>
      </section>

      {last ? (
        <section className="mt-7 px-5">
          <h2 className="mb-3 text-[20px] font-semibold tracking-tight">Order again</h2>
          <Link
            to="/order"
            onClick={replayLast}
            className="pressable flex items-center gap-3 rounded-lg bg-card p-3"
          >
            <div className="min-w-0 flex-1">
              <p className="text-[15px] font-medium">
                {storeById(last.storeId)?.name ?? "Pickup"}
              </p>
              <p className="text-[13px] text-muted">
                {money(last.totalCents)} · {relativeTime(last.createdAt)} · +{last.beansEarned} beans
              </p>
            </div>
            <ChevronRight className="size-4 text-subtle" />
          </Link>
        </section>
      ) : null}

      <section className="mt-7">
        <div className="mb-3 flex items-end justify-between px-5">
          <h2 className="text-[20px] font-semibold tracking-tight">Featured</h2>
          <Link to="/order" className="text-[15px] text-clay">
            See menu
          </Link>
        </div>
        <div className="flex gap-3 overflow-x-auto px-5 snap-x pb-2">
          {featured.map((p) => (
            <div key={p.id} className="w-[168px] shrink-0 snap-item">
              <ProductTile product={p} />
            </div>
          ))}
        </div>
      </section>

      <section className="mt-7 px-5 pb-4">
        <h2 className="mb-3 text-[20px] font-semibold tracking-tight">The morning ritual</h2>
        <div className="overflow-hidden rounded-lg">
          <img src="/products/cafe.jpg" alt="" className="h-44 w-full object-cover" />
          <div className="bg-card px-4 py-4">
            <p className="font-display text-[22px] leading-tight tracking-tight">
              Puerto Rico’s highlands, a short menu.
            </p>
            <p className="mt-2 text-[15px] leading-snug text-muted">
              Growing origin is always listed. We only call a lot Puerto Rican-grown when it truly is.
            </p>
          </div>
        </div>
      </section>
    </>
  );
}

function OfferCard({ offer }: { offer: Offer }) {
  return (
    <>
      <img src={offer.image} alt="" className="h-44 w-full object-cover" />
      <div className="absolute inset-0 bg-gradient-to-t from-ink/75 via-ink/15 to-transparent" />
      <div className="absolute inset-x-0 bottom-0 p-4 text-cream">
        <p className="text-[12px] uppercase tracking-[0.14em] text-cream/70">Offer</p>
        <p className="font-display text-[22px] leading-tight">{offer.title}</p>
        <p className="mt-1 text-[13px] text-cream/80">{offer.body}</p>
      </div>
    </>
  );
}
