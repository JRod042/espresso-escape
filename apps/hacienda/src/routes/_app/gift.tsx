import { createFileRoute } from "@tanstack/react-router";
import { useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { LargeTitle } from "@/components/app-shell";
import { Button } from "@/components/ui/button";
import { listGifts, sendGift } from "@/lib/api";
import { RedirectToSignIn } from "@/lib/auth/gates";
import { useCurrentUserState } from "@/lib/auth/use-current-user";
import { GIFT_AMOUNTS, GIFT_DESIGNS } from "@/lib/catalog";
import { money, relativeTime } from "@/lib/format";
import { cn } from "@/lib/cn";

export const Route = createFileRoute("/_app/gift")({ component: Gift });

function Gift() {
  const { user, isPending } = useCurrentUserState();
  const qc = useQueryClient();
  const giftsQ = useQuery({
    queryKey: ["gifts"],
    queryFn: () => listGifts(),
    enabled: Boolean(user),
  });
  const [designId, setDesignId] = useState(GIFT_DESIGNS[0].id);
  const [amount, setAmount] = useState(2500);
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);
  const design = GIFT_DESIGNS.find((d) => d.id === designId) ?? GIFT_DESIGNS[0];

  if (isPending) return <div className="mx-5 mt-8 h-64 animate-pulse rounded-xl bg-ink/10" />;
  if (!user) return <RedirectToSignIn />;

  return (
    <>
      <LargeTitle kicker="eGift" title="Gift" />
      <p className="px-5 pb-3 text-[13px] text-muted">Paid from your Casa Card balance.</p>
      <div className="px-5">
        <div className="overflow-hidden rounded-lg">
          <img src={design.image} alt="" className="h-44 w-full object-cover" />
        </div>
        <div className="mt-3 flex gap-2">
          {GIFT_DESIGNS.map((d) => (
            <button
              key={d.id}
              type="button"
              onClick={() => setDesignId(d.id)}
              className={cn(
                "overflow-hidden rounded-md ring-2",
                designId === d.id ? "ring-clay" : "ring-transparent",
              )}
            >
              <img src={d.image} alt={d.name} className="size-14 object-cover" />
            </button>
          ))}
        </div>
        <p className="mt-4 text-[13px] font-medium uppercase tracking-[0.08em] text-muted">
          Amount
        </p>
        <div className="mt-2 flex flex-wrap gap-2">
          {GIFT_AMOUNTS.map((c) => (
            <button
              key={c}
              type="button"
              onClick={() => setAmount(c)}
              className={cn(
                "h-10 min-w-16 rounded-full px-3 text-[14px] tabular-nums",
                amount === c ? "bg-ink text-cream" : "bg-card",
              )}
            >
              {money(c)}
            </button>
          ))}
        </div>
        <form
          className="mt-5 space-y-2"
          onSubmit={(e) => {
            e.preventDefault();
            setBusy(true);
            void sendGift({
              data: {
                recipientName: name,
                recipientEmail: email,
                amountCents: amount,
                designId,
                message,
              },
            })
              .then(async (r) => {
                await qc.invalidateQueries({ queryKey: ["gifts"] });
                await qc.invalidateQueries({ queryKey: ["profile"] });
                const shareText = `A Casa Rústico eGift for ${money(amount)}. Code ${r.code}`;
                if (navigator.share) {
                  try {
                    await navigator.share({ title: "Casa Rústico eGift", text: shareText });
                  } catch {
                    /* dismissed */
                  }
                }
                toast.success(`Sent · code ${r.code}`);
                setName("");
                setEmail("");
                setMessage("");
              })
              .catch((err) => toast.error(err instanceof Error ? err.message : "Could not send"))
              .finally(() => setBusy(false));
          }}
        >
          <input
            required
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Recipient name"
            className="h-12 w-full rounded-md bg-card px-4 text-[16px] outline-none ring-1 ring-line"
          />
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="Email (optional)"
            className="h-12 w-full rounded-md bg-card px-4 text-[16px] outline-none ring-1 ring-line"
          />
          <textarea
            value={message}
            onChange={(e) => setMessage(e.target.value)}
            placeholder="A short note"
            rows={3}
            className="w-full rounded-md bg-card px-4 py-3 text-[16px] outline-none ring-1 ring-line"
          />
          <Button type="submit" size="lg" variant="forest" className="w-full" disabled={busy}>
            {busy ? "Sending…" : `Send ${money(amount)} eGift`}
          </Button>
        </form>

        {giftsQ.data && giftsQ.data.length > 0 ? (
          <section className="mt-8 pb-6">
            <h2 className="mb-2 text-[20px] font-semibold tracking-tight">Sent</h2>
            <ul className="overflow-hidden rounded-lg bg-card">
              {giftsQ.data.map((g) => (
                <li
                  key={g.id}
                  className="flex items-center justify-between border-b border-line px-4 py-3 last:border-0"
                >
                  <div>
                    <p className="text-[16px]">{g.recipientName}</p>
                    <p className="text-[12px] text-muted">
                      {g.code} · {relativeTime(g.createdAt)}
                    </p>
                  </div>
                  <p className="tabular-nums text-[15px]">{money(g.amountCents)}</p>
                </li>
              ))}
            </ul>
          </section>
        ) : null}
      </div>
    </>
  );
}
