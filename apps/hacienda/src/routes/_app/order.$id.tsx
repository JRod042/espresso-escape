import { createFileRoute, Link, useNavigate } from "@tanstack/react-router";
import { useState, type ReactNode } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ChevronLeft, Heart } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { listFavorites, saveDrink, toggleFavorite, type Customization } from "@/lib/api";
import { useCurrentUserState } from "@/lib/auth/use-current-user";
import {
  DRINK_SIZES,
  GRINDS,
  MILKS,
  SYRUPS,
  productById,
} from "@/lib/catalog";
import { money } from "@/lib/format";
import { useCart } from "@/store/cart";
import { cn } from "@/lib/cn";

export const Route = createFileRoute("/_app/order/$id")({ component: ProductPage });

function ProductPage() {
  const { id } = Route.useParams();
  const product = productById(id);
  const navigate = useNavigate();
  const add = useCart((s) => s.add);
  const { user } = useCurrentUserState();
  const qc = useQueryClient();
  const favQ = useQuery({
    queryKey: ["favorites"],
    queryFn: () => listFavorites(),
    enabled: Boolean(user),
  });
  const [size, setSize] = useState("8");
  const [milk, setMilk] = useState<(typeof MILKS)[number]>("Whole");
  const [shots, setShots] = useState(2);
  const [syrup, setSyrup] = useState<(typeof SYRUPS)[number]>("None");
  const [grind, setGrind] = useState<(typeof GRINDS)[number]>("Whole bean");
  const [qty, setQty] = useState(1);

  if (!product) {
    return (
      <div className="px-5 py-16 text-center">
        <p>That item isn’t on the menu.</p>
        <Link to="/order" className="mt-3 inline-block text-clay">
          Back to order
        </Link>
      </div>
    );
  }

  const custom: Customization = {};
  if (product.kind === "drink") {
    custom.size = size;
    custom.milk = milk;
    custom.shots = shots;
    custom.syrup = syrup;
  }
  if (product.kind === "beans" && product.customizable) custom.grind = grind;

  let unit = product.priceCents;
  if (product.kind === "drink") {
    unit += DRINK_SIZES.find((s) => s.id === size)?.priceDelta ?? 0;
    if (shots > 2) unit += (shots - 2) * 80;
    if (milk !== "Whole" && milk !== "2%") unit += 70;
    if (syrup !== "None") unit += 60;
  }
  const liked = favQ.data?.includes(product.id);

  return (
    <div className="-mt-1">
      <div className="relative">
        <img src={product.image} alt="" className="h-[38vh] w-full object-cover" />
        {user ? (
          <button
            type="button"
            aria-label={liked ? "Remove favorite" : "Favorite"}
            className="absolute right-4 grid size-10 place-items-center rounded-full bg-cream/90 text-ink backdrop-blur"
            style={{ top: 16 }}
            onClick={() => {
              void toggleFavorite({ data: { productId: product.id } }).then((r) => {
                void qc.invalidateQueries({ queryKey: ["favorites"] });
                toast(r.favorited ? "Saved to favorites" : "Removed");
              });
            }}
          >
            <Heart className="size-5" fill={liked ? "currentColor" : "none"} />
          </button>
        ) : null}
      </div>
      <div className="flex items-center px-2 pt-2">
        <Link
          to="/order"
          className="inline-flex h-11 items-center gap-1 px-3 text-[17px] text-clay"
          aria-label="Back to menu"
        >
          <ChevronLeft className="size-5" />
          Menu
        </Link>
      </div>
      <div className="px-5 pt-4">
        <p className="text-[13px] text-muted">{product.tagline}</p>
        <h1 className="font-display text-[32px] leading-[1.1] tracking-tight">{product.name}</h1>
        <p className="mt-1 text-[22px] tabular-nums font-medium">{money(unit)}</p>
        <p className="mt-3 text-[15px] leading-relaxed text-muted">{product.description}</p>
        {product.notes ? (
          <div className="mt-3 flex flex-wrap gap-2">
            {product.notes.map((n) => (
              <span key={n} className="rounded-full bg-ink/8 px-3 py-1 text-[12px]">
                {n}
              </span>
            ))}
          </div>
        ) : null}
      </div>

      {product.kind === "drink" ? (
        <div className="mt-5 space-y-5 px-5">
          <OptionGroup label="Size">
            {DRINK_SIZES.map((s) => (
              <Chip key={s.id} active={size === s.id} onClick={() => setSize(s.id)}>
                {s.label}
              </Chip>
            ))}
          </OptionGroup>
          <OptionGroup label="Milk">
            {MILKS.map((m) => (
              <Chip key={m} active={milk === m} onClick={() => setMilk(m)}>
                {m}
              </Chip>
            ))}
          </OptionGroup>
          <OptionGroup label="Shots">
            {[1, 2, 3, 4].map((n) => (
              <Chip key={n} active={shots === n} onClick={() => setShots(n)}>
                {n}
              </Chip>
            ))}
          </OptionGroup>
          <OptionGroup label="Syrup">
            {SYRUPS.map((s) => (
              <Chip key={s} active={syrup === s} onClick={() => setSyrup(s)}>
                {s}
              </Chip>
            ))}
          </OptionGroup>
        </div>
      ) : null}

      {product.kind === "beans" && product.customizable ? (
        <div className="mt-5 px-5">
          <OptionGroup label="Grind">
            {GRINDS.map((g) => (
              <Chip key={g} active={grind === g} onClick={() => setGrind(g)}>
                {g}
              </Chip>
            ))}
          </OptionGroup>
        </div>
      ) : null}

      <div className="mt-6 flex items-center justify-between px-5">
        <p className="text-[15px] text-muted">Quantity</p>
        <div className="flex items-center gap-3">
          <button
            type="button"
            className="grid size-9 place-items-center rounded-full bg-ink/8"
            onClick={() => setQty(Math.max(1, qty - 1))}
          >
            −
          </button>
          <span className="w-6 text-center tabular-nums text-[17px]">{qty}</span>
          <button
            type="button"
            className="grid size-9 place-items-center rounded-full bg-ink/8"
            onClick={() => setQty(qty + 1)}
          >
            +
          </button>
        </div>
      </div>

      <div className="mt-6 space-y-2 px-5 pb-8">
        <Button
          size="lg"
          variant="forest"
          className="w-full"
          onClick={() => {
            add(product.id, custom, qty);
            toast.success(`Added ${product.name}`);
            void navigate({ to: "/order" });
          }}
        >
          Add {qty} · {money(unit * qty)}
        </Button>
        {user && product.kind === "drink" ? (
          <Button
            size="lg"
            variant="secondary"
            className="w-full"
            onClick={() => {
              void saveDrink({
                data: { productId: product.id, name: `My ${product.name}`, custom },
              }).then(() => toast.success("Saved to your drinks"));
            }}
          >
            Save this drink
          </Button>
        ) : null}
      </div>
    </div>
  );
}

function OptionGroup({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div>
      <p className="mb-2 text-[13px] font-medium uppercase tracking-[0.08em] text-muted">{label}</p>
      <div className="flex flex-wrap gap-2">{children}</div>
    </div>
  );
}

function Chip({
  active,
  onClick,
  children,
}: {
  active: boolean;
  onClick: () => void;
  children: ReactNode;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        "rounded-full px-3.5 py-2 text-[14px] font-medium",
        active ? "bg-ink text-cream" : "bg-card text-ink",
      )}
    >
      {children}
    </button>
  );
}
