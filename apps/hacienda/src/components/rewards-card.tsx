import { nextReward } from "@/lib/catalog";
import type { Profile } from "@/lib/api";
import { cn } from "@/lib/cn";

export function RewardsCard({ profile }: { profile: Profile }) {
  const { reward, have, need } = nextReward(profile.beans);
  const pct = need > 0 ? Math.min(1, have / need) : 1;
  const r = 36;
  const c = 2 * Math.PI * r;
  const dash = c * pct;
  return (
    <section className="mx-5 overflow-hidden rounded-xl bg-forest text-cream shadow-[0_12px_32px_color-mix(in_oklab,var(--color-forest)_35%,transparent)]">
      <div className="relative px-5 py-5">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-0 opacity-40"
          style={{
            background:
              "radial-gradient(120% 80% at 100% 0%, color-mix(in oklab, var(--color-grove) 80%, transparent), transparent 55%)",
          }}
        />
        <div className="relative flex items-center gap-4">
          <div className="relative size-[88px] shrink-0">
            <svg viewBox="0 0 88 88" className="size-full progress-ring">
              <circle
                cx="44"
                cy="44"
                r={r}
                fill="none"
                stroke="color-mix(in oklab, var(--color-cream) 18%, transparent)"
                strokeWidth="6"
              />
              <circle
                cx="44"
                cy="44"
                r={r}
                fill="none"
                stroke="var(--color-cream)"
                strokeWidth="6"
                strokeLinecap="round"
                strokeDasharray={`${dash} ${c}`}
              />
            </svg>
            <div className="absolute inset-0 grid place-items-center">
              <div className="text-center leading-none">
                <p className="font-display text-[22px] tabular-nums tracking-tight">
                  {profile.beans}
                </p>
                <p className="mt-1 text-[10px] uppercase tracking-[0.14em] text-cream/70">
                  beans
                </p>
              </div>
            </div>
          </div>
          <div className="min-w-0 flex-1">
            <p className="text-[11px] font-medium uppercase tracking-[0.16em] text-cream/60">
              Hacienda Rewards · {profile.tierName}
            </p>
            <p className="mt-1 font-display text-[22px] leading-tight tracking-tight">
              {profile.beans >= reward.beans
                ? "A reward is ready"
                : `${reward.beans - profile.beans} to ${reward.name.toLowerCase()}`}
            </p>
            <p className="mt-1 text-[13px] text-cream/70">
              {profile.beans >= reward.beans
                ? `Redeem ${reward.name} at checkout.`
                : `Next: ${reward.name}. ${need} beans in this band.`}
            </p>
          </div>
        </div>
      </div>
    </section>
  );
}

export function RewardsCardGuest({ className }: { className?: string }) {
  return (
    <section
      className={cn(
        "mx-5 overflow-hidden rounded-xl bg-forest px-5 py-6 text-cream",
        className,
      )}
    >
      <p className="text-[11px] font-medium uppercase tracking-[0.16em] text-cream/60">
        Hacienda Rewards
      </p>
      <h2 className="mt-2 font-display text-[28px] leading-[1.1] tracking-tight">
        Join for beans on every cup.
      </h2>
      <p className="mt-2 max-w-sm text-[15px] leading-snug text-cream/75">
        Free to join. Earn 2 beans per dollar on Casa Card, a birthday drink, and a short honest menu.
      </p>
      <p className="mt-4 inline-flex h-10 items-center rounded-full bg-cream px-4 text-[15px] font-medium text-forest">
        Join now
      </p>
    </section>
  );
}
