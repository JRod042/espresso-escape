import { createServerFn } from "@tanstack/react-start";
import { z } from "zod";
import { getSql } from "@/lib/db";
import { authMiddleware } from "@/lib/auth/middleware";
import {
  GIFT_DESIGNS,
  PRODUCTS,
  REWARDS,
  STORES,
  beanRateFor,
  memberNumber,
  productById,
  rewardOffCents,
  storeById,
  tierFor,
  type Product,
} from "@/lib/catalog";

export type Profile = {
  userId: string;
  displayName: string | null;
  birthdayMonth: number | null;
  birthdayDay: number | null;
  favoriteStoreId: string | null;
  beans: number;
  lifetimeBeans: number;
  walletCents: number;
  memberNo: string;
  createdAt: string;
  tierId: "welcome" | "cosecha" | "hacienda";
  tierName: string;
};

export type OrderRow = {
  id: number;
  storeId: string;
  status: string;
  totalCents: number;
  beansEarned: number;
  beansSpent: number;
  payWith: string;
  itemsJson: string;
  createdAt: string;
};

export type LedgerRow = {
  id: number;
  delta: number;
  reason: string;
  createdAt: string;
};

export type GiftRow = {
  id: number;
  recipientName: string;
  recipientEmail: string | null;
  amountCents: number;
  designId: string;
  message: string | null;
  code: string;
  createdAt: string;
};

export type Customization = {
  size?: string;
  milk?: string;
  shots?: number;
  syrup?: string;
  grind?: string;
};

export type CartLine = {
  productId: string;
  qty: number;
  custom: Customization;
  redeemRewardId?: string;
};

type Sql = Awaited<ReturnType<typeof getSql>>;

function giftCode() {
  const alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
  let s = "CR-";
  for (let i = 0; i < 8; i++) s += alphabet[Math.floor(Math.random() * alphabet.length)];
  return s;
}

async function ensureProfile(sql: Sql, userId: string): Promise<void> {
  const no = memberNumber(userId);
  // One statement so two first requests cannot both grant welcome beans.
  await sql`
    with ins as (
      insert into profiles (user_id, beans, lifetime_beans, wallet_cents, member_no)
      values (${userId}, 50, 50, 0, ${no})
      on conflict (user_id) do nothing
      returning user_id
    )
    insert into bean_ledger (user_id, delta, reason)
    select user_id, 50, 'Welcome to Hacienda Rewards' from ins
  `;
}

function mapProfile(row: {
  user_id: string;
  display_name: string | null;
  birthday_month: number | null;
  birthday_day: number | null;
  favorite_store_id: string | null;
  beans: number;
  lifetime_beans: number;
  wallet_cents: number;
  member_no: string;
  created_at: string;
}): Profile {
  const tier = tierFor(row.lifetime_beans);
  return {
    userId: row.user_id,
    displayName: row.display_name,
    birthdayMonth: row.birthday_month,
    birthdayDay: row.birthday_day,
    favoriteStoreId: row.favorite_store_id,
    beans: row.beans,
    lifetimeBeans: row.lifetime_beans,
    walletCents: row.wallet_cents,
    memberNo: row.member_no,
    createdAt: row.created_at,
    tierId: tier.id,
    tierName: tier.name,
  };
}

async function loadProfile(sql: Sql, userId: string): Promise<Profile> {
  await ensureProfile(sql, userId);
  const rows = await sql<{
    user_id: string;
    display_name: string | null;
    birthday_month: number | null;
    birthday_day: number | null;
    favorite_store_id: string | null;
    beans: number;
    lifetime_beans: number;
    wallet_cents: number;
    member_no: string;
    created_at: string;
  }>`select user_id, display_name, birthday_month, birthday_day, favorite_store_id,
            beans, lifetime_beans, wallet_cents, member_no, created_at
     from profiles where user_id = ${userId}`;
  return mapProfile(rows[0]);
}

export const getProfile = createServerFn({ method: "GET" })
  .middleware([authMiddleware])
  .handler(async ({ context }) => {
    const sql = await getSql();
    return loadProfile(sql, context.userId);
  });

export const updateProfile = createServerFn({ method: "POST" })
  .middleware([authMiddleware])
  .validator(
    z.object({
      displayName: z.string().trim().max(80).optional(),
      birthdayMonth: z.number().int().min(1).max(12).nullable().optional(),
      birthdayDay: z.number().int().min(1).max(31).nullable().optional(),
      favoriteStoreId: z.string().nullable().optional(),
    }),
  )
  .handler(async ({ context, data }) => {
    const sql = await getSql();
    await ensureProfile(sql, context.userId);
    if (data.displayName !== undefined) {
      await sql`update profiles set display_name = ${data.displayName} where user_id = ${context.userId}`;
    }
    if (data.birthdayMonth !== undefined) {
      await sql`update profiles set birthday_month = ${data.birthdayMonth} where user_id = ${context.userId}`;
    }
    if (data.birthdayDay !== undefined) {
      await sql`update profiles set birthday_day = ${data.birthdayDay} where user_id = ${context.userId}`;
    }
    if (data.favoriteStoreId !== undefined) {
      await sql`update profiles set favorite_store_id = ${data.favoriteStoreId} where user_id = ${context.userId}`;
    }
    return loadProfile(sql, context.userId);
  });

function linePrice(product: Product, custom: Customization, qty: number) {
  let unit = product.priceCents;
  if (product.kind === "drink" && custom.size) {
    const sizes = [
      { id: "8", d: 0 },
      { id: "12", d: 50 },
      { id: "16", d: 80 },
      { id: "20", d: 110 },
    ];
    unit += sizes.find((s) => s.id === custom.size)?.d ?? 0;
  }
  if (custom.shots && custom.shots > 2) unit += (custom.shots - 2) * 80;
  if (custom.milk && custom.milk !== "Whole" && custom.milk !== "2%") unit += 70;
  if (custom.syrup && custom.syrup !== "None") unit += 60;
  return unit * qty;
}

export const placeOrder = createServerFn({ method: "POST" })
  .middleware([authMiddleware])
  .validator(
    z.object({
      storeId: z.string(),
      payWith: z.enum(["wallet", "card"]),
      redeemRewardId: z.string().optional(),
      items: z
        .array(
          z.object({
            productId: z.string(),
            qty: z.number().int().min(1).max(20),
            custom: z.object({
              size: z.string().optional(),
              milk: z.string().optional(),
              shots: z.number().int().min(1).max(4).optional(),
              syrup: z.string().optional(),
              grind: z.string().optional(),
            }),
          }),
        )
        .min(1)
        .max(20),
    }),
  )
  .handler(async ({ context, data }) => {
    const sql = await getSql();
    await ensureProfile(sql, context.userId);
    const store = storeById(data.storeId);
    if (!store) throw new Error("Unknown store");
    if (!store.pickup) {
      throw new Error("That location ships bags. Pick a café for mobile order.");
    }

    const priced = data.items.map((item) => {
      const product = productById(item.productId);
      if (!product) throw new Error("Unknown product");
      return {
        ...item,
        name: product.name,
        unitCents: linePrice(product, item.custom, 1),
        lineCents: linePrice(product, item.custom, item.qty),
      };
    });
    let total = priced.reduce((s, i) => s + i.lineCents, 0);

    const prof = (
      await sql<{ beans: number; wallet_cents: number; lifetime_beans: number }>`
        select beans, wallet_cents, lifetime_beans from profiles where user_id = ${context.userId}
      `
    )[0];

    let beansSpent = 0;
    if (data.redeemRewardId) {
      const reward = REWARDS.find((r) => r.id === data.redeemRewardId);
      if (!reward) throw new Error("Unknown reward");
      if (prof.beans < reward.beans) throw new Error("Not enough beans");
      beansSpent = reward.beans;
      total = Math.max(0, total - rewardOffCents(reward.id));
    }

    const hour = new Date().getHours();
    const coldBonus = hour >= 14 && priced.some((i) => productById(i.productId)?.category === "cold");
    const beanRate = beanRateFor(prof.lifetime_beans, data.payWith);
    let beansEarned = Math.floor((total / 100) * beanRate);
    if (coldBonus) beansEarned *= 2;

    if (data.payWith === "wallet" && prof.wallet_cents < total) {
      throw new Error("Not enough Casa Card balance. Reload on Scan.");
    }

    const itemsJson = JSON.stringify(priced);
    const inserted = await sql.transaction(async (tx) => {
      const updated = await tx<{ wallet_cents: number }>`
        update profiles
        set wallet_cents = case
              when ${data.payWith} = 'wallet' then wallet_cents - ${total}
              else wallet_cents
            end,
            beans = beans - ${beansSpent} + ${beansEarned},
            lifetime_beans = lifetime_beans + ${beansEarned}
        where user_id = ${context.userId}
          and beans >= ${beansSpent}
          and (${data.payWith} <> 'wallet' or wallet_cents >= ${total})
        returning wallet_cents
      `;
      if (!updated.length) return null;
      if (beansEarned) {
        await tx`insert into bean_ledger (user_id, delta, reason)
          values (${context.userId}, ${beansEarned}, ${"Order · " + store.name})`;
      }
      if (beansSpent) {
        await tx`insert into bean_ledger (user_id, delta, reason)
          values (${context.userId}, ${-beansSpent}, ${"Redeemed reward"})`;
      }
      const rows = await tx<{ id: number }>`
        insert into orders (user_id, store_id, status, total_cents, beans_earned, beans_spent, pay_with, items_json)
        values (${context.userId}, ${data.storeId}, 'preparing', ${total}, ${beansEarned}, ${beansSpent}, ${data.payWith}, ${itemsJson})
        returning id
      `;
      return rows[0] ?? null;
    });
    if (!inserted) {
      const again = (
        await sql<{ beans: number; wallet_cents: number }>`
          select beans, wallet_cents from profiles where user_id = ${context.userId}
        `
      )[0];
      if (beansSpent && again.beans < beansSpent) throw new Error("Not enough beans");
      throw new Error("Not enough Casa Card balance. Reload on Scan.");
    }
    return { orderId: inserted.id, totalCents: total, beansEarned, beansSpent };
  });

export const listOrders = createServerFn({ method: "GET" })
  .middleware([authMiddleware])
  .handler(async ({ context }) => {
    const sql = await getSql();
    await ensureProfile(sql, context.userId);
    return sql<OrderRow>`
      select id, store_id as "storeId", status, total_cents as "totalCents",
             beans_earned as "beansEarned", beans_spent as "beansSpent",
             pay_with as "payWith", items_json as "itemsJson", created_at as "createdAt"
      from orders where user_id = ${context.userId}
      order by created_at desc limit 30
    `;
  });

export const listLedger = createServerFn({ method: "GET" })
  .middleware([authMiddleware])
  .handler(async ({ context }) => {
    const sql = await getSql();
    await ensureProfile(sql, context.userId);
    return sql<LedgerRow>`
      select id, delta, reason, created_at as "createdAt"
      from bean_ledger where user_id = ${context.userId}
      order by created_at desc limit 40
    `;
  });

export const reloadWallet = createServerFn({ method: "POST" })
  .middleware([authMiddleware])
  .validator(z.object({ amountCents: z.number().int().min(500).max(10000) }))
  .handler(async ({ context, data }) => {
    const sql = await getSql();
    await ensureProfile(sql, context.userId);
    let bonus = 0;
    if (data.amountCents >= 5000) bonus = 25;
    else if (data.amountCents >= 3000) bonus = 10;
    await sql`update profiles
      set wallet_cents = wallet_cents + ${data.amountCents},
          beans = beans + ${bonus},
          lifetime_beans = lifetime_beans + ${bonus}
      where user_id = ${context.userId}`;
    if (bonus) {
      await sql`insert into bean_ledger (user_id, delta, reason)
        values (${context.userId}, ${bonus}, ${"Reload bonus"})`;
    }
    return loadProfile(sql, context.userId);
  });

export const scanPay = createServerFn({ method: "POST" })
  .middleware([authMiddleware])
  .validator(
    z.object({
      amountCents: z.number().int().min(100).max(4000),
      storeId: z.string(),
    }),
  )
  .handler(async ({ context, data }) => {
    const sql = await getSql();
    await ensureProfile(sql, context.userId);
    const store = storeById(data.storeId) ?? STORES[0];
    const beansEarned = Math.floor((data.amountCents / 100) * 2);
    const itemsJson = JSON.stringify([
      { productId: "drip", name: "In-store purchase", qty: 1, lineCents: data.amountCents },
    ]);
    const paid = await sql.transaction(async (tx) => {
      const updated = await tx<{ wallet_cents: number }>`
        update profiles
        set wallet_cents = wallet_cents - ${data.amountCents},
            beans = beans + ${beansEarned},
            lifetime_beans = lifetime_beans + ${beansEarned}
        where user_id = ${context.userId}
          and wallet_cents >= ${data.amountCents}
        returning wallet_cents
      `;
      if (!updated.length) return false;
      await tx`insert into bean_ledger (user_id, delta, reason)
        values (${context.userId}, ${beansEarned}, ${"In-store · " + store.name})`;
      await tx`
        insert into orders (user_id, store_id, status, total_cents, beans_earned, beans_spent, pay_with, items_json)
        values (${context.userId}, ${store.id}, 'completed', ${data.amountCents}, ${beansEarned}, 0, 'wallet', ${itemsJson})
      `;
      return true;
    });
    if (!paid) throw new Error("Not enough Casa Card balance. Reload first.");
    return { beansEarned, amountCents: data.amountCents };
  });

export const sendGift = createServerFn({ method: "POST" })
  .middleware([authMiddleware])
  .validator(
    z.object({
      recipientName: z.string().trim().min(1).max(80),
      recipientEmail: z.string().trim().max(120).optional(),
      amountCents: z.number().int(),
      designId: z.string(),
      message: z.string().trim().max(240).optional(),
    }),
  )
  .handler(async ({ context, data }) => {
    if (!GIFT_DESIGNS.some((d) => d.id === data.designId)) throw new Error("Unknown design");
    if (![1000, 1500, 2500, 5000, 10000].includes(data.amountCents)) {
      throw new Error("Choose a gift amount");
    }
    const sql = await getSql();
    await ensureProfile(sql, context.userId);
    const code = giftCode();
    const email = data.recipientEmail && data.recipientEmail.includes("@") ? data.recipientEmail : null;
    const msg = data.message ?? null;
    const sent = await sql.transaction(async (tx) => {
      const updated = await tx<{ wallet_cents: number }>`
        update profiles
        set wallet_cents = wallet_cents - ${data.amountCents}
        where user_id = ${context.userId}
          and wallet_cents >= ${data.amountCents}
        returning wallet_cents
      `;
      if (!updated.length) return false;
      await tx`
        insert into gifts (user_id, recipient_name, recipient_email, amount_cents, design_id, message, code)
        values (${context.userId}, ${data.recipientName}, ${email}, ${data.amountCents}, ${data.designId}, ${msg}, ${code})
      `;
      return true;
    });
    if (!sent) throw new Error("Not enough Casa Card balance. Reload on Scan.");
    return { code };
  });

export const listGifts = createServerFn({ method: "GET" })
  .middleware([authMiddleware])
  .handler(async ({ context }) => {
    const sql = await getSql();
    await ensureProfile(sql, context.userId);
    return sql<GiftRow>`
      select id, recipient_name as "recipientName", recipient_email as "recipientEmail",
             amount_cents as "amountCents", design_id as "designId", message, code,
             created_at as "createdAt"
      from gifts where user_id = ${context.userId}
      order by created_at desc limit 20
    `;
  });

export const toggleFavorite = createServerFn({ method: "POST" })
  .middleware([authMiddleware])
  .validator(z.object({ productId: z.string() }))
  .handler(async ({ context, data }) => {
    if (!productById(data.productId)) throw new Error("Unknown product");
    const sql = await getSql();
    await ensureProfile(sql, context.userId);
    const existing = await sql<{ product_id: string }>`
      select product_id from favorites
      where user_id = ${context.userId} and product_id = ${data.productId}
    `;
    if (existing.length) {
      await sql`delete from favorites where user_id = ${context.userId} and product_id = ${data.productId}`;
      return { favorited: false };
    }
    await sql`insert into favorites (user_id, product_id) values (${context.userId}, ${data.productId})`;
    return { favorited: true };
  });

export const listFavorites = createServerFn({ method: "GET" })
  .middleware([authMiddleware])
  .handler(async ({ context }) => {
    const sql = await getSql();
    await ensureProfile(sql, context.userId);
    const rows = await sql<{ product_id: string }>`
      select product_id from favorites where user_id = ${context.userId}
    `;
    return rows.map((r) => r.product_id);
  });

export const saveDrink = createServerFn({ method: "POST" })
  .middleware([authMiddleware])
  .validator(
    z.object({
      productId: z.string(),
      name: z.string().trim().min(1).max(60),
      custom: z.object({
        size: z.string().optional(),
        milk: z.string().optional(),
        shots: z.number().int().optional(),
        syrup: z.string().optional(),
        grind: z.string().optional(),
      }),
    }),
  )
  .handler(async ({ context, data }) => {
    const sql = await getSql();
    await ensureProfile(sql, context.userId);
    const json = JSON.stringify(data.custom);
    await sql`
      insert into saved_drinks (user_id, product_id, name, custom_json)
      values (${context.userId}, ${data.productId}, ${data.name}, ${json})
    `;
    return { ok: true as const };
  });

export const listSaved = createServerFn({ method: "GET" })
  .middleware([authMiddleware])
  .handler(async ({ context }) => {
    const sql = await getSql();
    await ensureProfile(sql, context.userId);
    return sql<{ id: number; productId: string; name: string; customJson: string }>`
      select id, product_id as "productId", name, custom_json as "customJson"
      from saved_drinks where user_id = ${context.userId}
      order by created_at desc
    `;
  });

export const catalogPayload = createServerFn({ method: "GET" }).handler(async () => {
  return { products: PRODUCTS, stores: STORES, rewards: REWARDS };
});
