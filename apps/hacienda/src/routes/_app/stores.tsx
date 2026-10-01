import { createFileRoute } from "@tanstack/react-router";
import { useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Heart, Navigation } from "lucide-react";
import { toast } from "sonner";
import { LargeTitle } from "@/components/app-shell";
import { getProfile, updateProfile } from "@/lib/api";
import { useCurrentUserState } from "@/lib/auth/use-current-user";
import { STORES } from "@/lib/catalog";
import { useCart } from "@/store/cart";
import { cn } from "@/lib/cn";

export const Route = createFileRoute("/_app/stores")({ component: Stores });

function Stores() {
  const { user } = useCurrentUserState();
  const qc = useQueryClient();
  const setStore = useCart((s) => s.setStore);
  const storeId = useCart((s) => s.storeId);
  const profileQ = useQuery({
    queryKey: ["profile"],
    queryFn: () => getProfile(),
    enabled: Boolean(user),
  });
  const [selected, setSelected] = useState(storeId);
  const fav = profileQ.data?.favoriteStoreId;
  const current = STORES.find((s) => s.id === selected) ?? STORES[0];

  return (
    <>
      <LargeTitle kicker="Find a house" title="Stores" />
      <div className="px-5">
        <div className="relative overflow-hidden rounded-lg bg-forest">
          <img src={current.image} alt="" className="h-40 w-full object-cover opacity-80" />
          <div className="absolute inset-0 bg-gradient-to-t from-forest via-forest/20 to-transparent" />
          <svg viewBox="0 0 360 160" className="pointer-events-none absolute inset-0 h-full w-full">
            {STORES.map((s, i) => {
              const x = 40 + ((s.lng + 120) / 90) * 260;
              const y = 30 + ((32 - s.lat) / 20) * 100;
              const on = s.id === selected;
              return (
                <circle
                  key={s.id}
                  cx={Math.min(330, Math.max(20, x))}
                  cy={Math.min(140, Math.max(18, y + i))}
                  r={on ? 7 : 4.5}
                  fill={on ? "#F3EEE6" : "color-mix(in oklab, #F3EEE6 55%, transparent)"}
                />
              );
            })}
          </svg>
          <p className="absolute bottom-3 left-4 font-display text-[22px] text-cream">
            {current.city}
          </p>
        </div>
        <ul className="mt-4 overflow-hidden rounded-lg bg-card">
          {STORES.map((s) => {
            const active = s.id === selected;
            const isFav = fav === s.id;
            return (
              <li key={s.id} className={cn("border-b border-line last:border-0", active && "bg-paper")}>
                <button
                  type="button"
                  className="flex w-full items-start gap-3 px-4 py-3 text-left"
                  onClick={() => {
                    setSelected(s.id);
                    if (s.pickup) setStore(s.id);
                  }}
                >
                  <div className="min-w-0 flex-1">
                    <p className="text-[17px] font-medium tracking-tight">{s.name}</p>
                    <p className="text-[13px] text-muted">
                      {s.address} · {s.city}, {s.region}
                    </p>
                    <p className="mt-0.5 text-[13px] text-muted">
                      {s.hours}
                      {s.pickup ? "" : " · Ships — not a pickup counter"}
                    </p>
                    <div className="mt-1.5 flex flex-wrap gap-1.5">
                      {s.amenities.map((a) => (
                        <span key={a} className="rounded-full bg-ink/8 px-2 py-0.5 text-[11px]">
                          {a}
                        </span>
                      ))}
                    </div>
                  </div>
                  <span className="flex flex-col items-center gap-2">
                    {user ? (
                      <span
                        role="button"
                        className="grid size-9 place-items-center rounded-full bg-ink/8"
                        onClick={(e) => {
                          e.stopPropagation();
                          void updateProfile({
                            data: { favoriteStoreId: isFav ? null : s.id },
                          }).then(() => {
                            void qc.invalidateQueries({ queryKey: ["profile"] });
                            toast(isFav ? "Removed favorite" : `Favorite · ${s.name}`);
                          });
                        }}
                      >
                        <Heart className="size-4" fill={isFav ? "currentColor" : "none"} />
                      </span>
                    ) : null}
                    <a
                      href={`https://maps.apple.com/?ll=${s.lat},${s.lng}&q=${encodeURIComponent(s.name)}`}
                      className="grid size-9 place-items-center rounded-full bg-ink/8 text-ink"
                      onClick={(e) => e.stopPropagation()}
                      aria-label="Directions"
                    >
                      <Navigation className="size-4" />
                    </a>
                  </span>
                </button>
              </li>
            );
          })}
        </ul>
      </div>
    </>
  );
}
