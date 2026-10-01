export type CategoryId =
  | "featured"
  | "hot"
  | "cold"
  | "food"
  | "beans"
  | "merch";

export type SizeOpt = { id: string; label: string; oz: number; priceDelta: number };

export const DRINK_SIZES: SizeOpt[] = [
  { id: "8", label: "8 oz", oz: 8, priceDelta: 0 },
  { id: "12", label: "12 oz", oz: 12, priceDelta: 50 },
  { id: "16", label: "16 oz", oz: 16, priceDelta: 80 },
  { id: "20", label: "20 oz", oz: 20, priceDelta: 110 },
];

export const MILKS = ["Whole", "2%", "Oat", "Almond", "Coconut"] as const;
export const SYRUPS = ["None", "Vanilla", "Caramel", "Mocha", "Hazelnut"] as const;
export const GRINDS = ["Whole bean", "Espresso", "Filter", "French press"] as const;

export type Product = {
  id: string;
  name: string;
  tagline: string;
  description: string;
  priceCents: number;
  category: Exclude<CategoryId, "featured">;
  image: string;
  notes?: string[];
  origin?: string;
  roast?: string;
  kind: "drink" | "food" | "beans" | "merch";
  featured?: boolean;
  customizable: boolean;
  calories?: string;
};

export const CATEGORIES: { id: CategoryId; label: string }[] = [
  { id: "featured", label: "Featured" },
  { id: "hot", label: "Hot coffee" },
  { id: "cold", label: "Cold" },
  { id: "food", label: "Bakery" },
  { id: "beans", label: "Bags" },
  { id: "merch", label: "House mark" },
];

export const PRODUCTS: Product[] = [
  {
    id: "drip",
    name: "Hacienda drip",
    tagline: "The weekday cup",
    description:
      "Our house brew — rotating Colombia and Brazil, roasted for a clean weekday morning. Aroma first, then cocoa and cane sugar.",
    priceCents: 325,
    category: "hot",
    image: "/products/drip.jpg",
    kind: "drink",
    featured: true,
    customizable: true,
    calories: "5 cal",
    notes: ["Cocoa", "Cane sugar", "Clean finish"],
  },
  {
    id: "pourover",
    name: "Colombia pour-over",
    tagline: "Smallholders near Medellín",
    description:
      "V60 pour-over of our hero lot. Medium roast, citrus snap over caramel. The cup we built the brand around.",
    priceCents: 475,
    category: "hot",
    image: "/products/pourover.jpg",
    kind: "drink",
    featured: true,
    customizable: true,
    origin: "Colombia",
    roast: "Medium",
    notes: ["Citrus", "Caramel", "Cocoa"],
  },
  {
    id: "cortado",
    name: "Cortado",
    tagline: "Equal parts, no theater",
    description:
      "A tight 1:1 espresso and steamed milk. Crema intact. The afternoon cup of the highlands.",
    priceCents: 425,
    category: "hot",
    image: "/products/cortado.jpg",
    kind: "drink",
    featured: true,
    customizable: true,
    calories: "90 cal",
  },
  {
    id: "conleche",
    name: "Café con leche",
    tagline: "The house ritual",
    description:
      "Espresso stretched with steamed milk the way a Puerto Rican morning asks for it — not a latte, not a cappuccino. Ours.",
    priceCents: 450,
    category: "hot",
    image: "/products/conleche.jpg",
    kind: "drink",
    featured: true,
    customizable: true,
    calories: "140 cal",
  },
  {
    id: "espresso",
    name: "Espresso",
    tagline: "Two shots, short",
    description:
      "A double ristretto-leaning espresso of the house blend. Thick crema, dark chocolate, a little orange peel.",
    priceCents: 300,
    category: "hot",
    image: "/products/espresso.jpg",
    kind: "drink",
    customizable: true,
    calories: "10 cal",
  },
  {
    id: "coldbrew",
    name: "Cold brew",
    tagline: "Sixteen hours",
    description:
      "Coarse Colombia steeped overnight. Low acid, cocoa, a quiet sweetness. Served over ice, no sugar unless you ask.",
    priceCents: 495,
    category: "cold",
    image: "/products/coldbrew.jpg",
    kind: "drink",
    featured: true,
    customizable: true,
    calories: "5 cal",
  },
  {
    id: "icedleche",
    name: "Iced café con leche",
    tagline: "The warm-weather house cup",
    description:
      "Espresso, cold milk, ice. Same ritual as the hot cup, built for a Gulf afternoon.",
    priceCents: 525,
    category: "cold",
    image: "/products/icedleche.jpg",
    kind: "drink",
    customizable: true,
    calories: "130 cal",
  },
  {
    id: "mallorca",
    name: "Mallorca",
    tagline: "Powdered, still warm",
    description:
      "A Puerto Rican sweet bread — soft, eggy, dusted. The pastry that belongs next to café con leche.",
    priceCents: 425,
    category: "food",
    image: "/products/mallorca.jpg",
    kind: "food",
    featured: true,
    customizable: false,
    calories: "320 cal",
  },
  {
    id: "quesito",
    name: "Quesito",
    tagline: "Cream cheese, laminated",
    description:
      "Flaky pastry wrapped around sweet cream cheese. Golden, a little sticky, gone in four bites.",
    priceCents: 395,
    category: "food",
    image: "/products/quesito.jpg",
    kind: "food",
    customizable: false,
    calories: "280 cal",
  },
  {
    id: "colombia",
    name: "Casa Rústico — Colombia",
    tagline: "Hero bag · 12 oz",
    description:
      "Medium roast, whole bean, 12 oz. Smallholders near Medellín. Cocoa, caramel, a citrus snap. Partner-roasted in the U.S. under our label.",
    priceCents: 2500,
    category: "beans",
    image: "/products/bag.jpg",
    kind: "beans",
    featured: true,
    customizable: true,
    origin: "Colombia",
    roast: "Medium",
    notes: ["Cocoa", "Caramel", "Citrus"],
  },
  {
    id: "costarica",
    name: "Casa Rústico — Costa Rica",
    tagline: "Honey process · 12 oz",
    description:
      "Honey-processed lot. Bright apple, cane sugar, a tea-like finish. 12 oz kraft bag.",
    priceCents: 2500,
    category: "beans",
    image: "/products/bag.jpg",
    kind: "beans",
    customizable: true,
    origin: "Costa Rica",
    roast: "Medium-light",
    notes: ["Apple", "Cane sugar", "Tea"],
  },
  {
    id: "guatemala",
    name: "Casa Rústico — Guatemala",
    tagline: "Antigua valley · 12 oz",
    description:
      "Chocolate, baking spice, a floral top note. Grown in the Antigua valley. 12 oz kraft bag.",
    priceCents: 2500,
    category: "beans",
    image: "/products/bag.jpg",
    kind: "beans",
    customizable: true,
    origin: "Guatemala",
    roast: "Medium",
    notes: ["Chocolate", "Spice", "Floral"],
  },
  {
    id: "ethiopia",
    name: "Casa Rústico — Ethiopia Natural",
    tagline: "Sun-dried · 12 oz",
    description:
      "Natural process. Blueberry, jasmine, bergamot. The loudest cup on a short menu.",
    priceCents: 2500,
    category: "beans",
    image: "/products/bag.jpg",
    kind: "beans",
    customizable: true,
    origin: "Ethiopia",
    roast: "Light-medium",
    notes: ["Blueberry", "Jasmine", "Bergamot"],
  },
  {
    id: "sumatra",
    name: "Casa Rústico — Sumatra",
    tagline: "Wet-hulled · 12 oz",
    description:
      "Earth, cedar, dark chocolate. A heavier body for the evening cup.",
    priceCents: 2500,
    category: "beans",
    image: "/products/bag.jpg",
    kind: "beans",
    customizable: true,
    origin: "Sumatra",
    roast: "Medium-dark",
    notes: ["Cedar", "Earth", "Dark chocolate"],
  },
  {
    id: "brazil",
    name: "Casa Rústico — Brazil Santos",
    tagline: "Nutty · 12 oz",
    description:
      "Milk chocolate, roasted peanut, low acid. The blend backbone and a fine daily drinker.",
    priceCents: 2500,
    category: "beans",
    image: "/products/bag.jpg",
    kind: "beans",
    customizable: true,
    origin: "Brazil",
    roast: "Medium",
    notes: ["Milk chocolate", "Peanut", "Low acid"],
  },
  {
    id: "capsules",
    name: "Single-serve capsules",
    tagline: "12 pods · house roast",
    description:
      "The weekday workaround. Twelve capsules of the house roast, packed in kraft. Compatible with standard espresso machines.",
    priceCents: 1499,
    category: "beans",
    image: "/products/capsules.jpg",
    kind: "beans",
    customizable: false,
  },
  {
    id: "mug",
    name: "White glossy mug",
    tagline: "11 oz · house mark",
    description:
      "A cup you actually use. Gloss white ceramic, 11 oz, house mark on the face.",
    priceCents: 1495,
    category: "merch",
    image: "/products/mug.jpg",
    kind: "merch",
    customizable: false,
  },
  {
    id: "hoodie",
    name: "Highland hoodie",
    tagline: "Heavyweight cream",
    description:
      "Cream heavyweight cotton with the mountain house mark. Built for a cool highland morning — or a warehouse at 3 a.m.",
    priceCents: 4800,
    category: "merch",
    image: "/products/hoodie.jpg",
    kind: "merch",
    customizable: false,
  },
];

export type Store = {
  id: string;
  name: string;
  city: string;
  region: string;
  address: string;
  hours: string;
  amenities: string[];
  lat: number;
  lng: number;
  image: string;
  pickup: boolean;
};

export const STORES: Store[] = [
  {
    id: "sanjuan",
    name: "Casa Rústico Atelier",
    city: "San Juan",
    region: "Puerto Rico",
    address: "151 Calle del Cristo, Old San Juan",
    hours: "6:30 AM – 6:00 PM",
    amenities: ["Mobile order", "Wifi", "Indoor", "House-mark merch"],
    lat: 18.4655,
    lng: -66.1057,
    image: "/products/cafe.jpg",
    pickup: true,
  },
  {
    id: "jayuya",
    name: "Highland Outpost",
    city: "Jayuya",
    region: "Puerto Rico",
    address: "Carretera 144, Cordillera Central",
    hours: "7:00 AM – 4:00 PM",
    amenities: ["Mobile order", "Pour-over bar", "Mountain view"],
    lat: 18.2186,
    lng: -66.5916,
    image: "/products/highlands.jpg",
    pickup: true,
  },
  {
    id: "hammond",
    name: "Hammond Pickup",
    city: "Hammond",
    region: "Louisiana",
    address: "1100 NW Railroad Ave",
    hours: "7:00 AM – 5:00 PM",
    amenities: ["Mobile order", "Wifi", "Parking"],
    lat: 30.5044,
    lng: -90.4612,
    image: "/products/cafe.jpg",
    pickup: true,
  },
  {
    id: "nola",
    name: "Magazine Street",
    city: "New Orleans",
    region: "Louisiana",
    address: "4206 Magazine St",
    hours: "6:30 AM – 7:00 PM",
    amenities: ["Mobile order", "Wifi", "Patio"],
    lat: 29.9207,
    lng: -90.0843,
    image: "/products/cafe.jpg",
    pickup: true,
  },
  {
    id: "temecula",
    name: "Roastery Partner",
    city: "Temecula",
    region: "California",
    address: "Partner-roasted · pickup by appointment",
    hours: "By appointment",
    amenities: ["Bag pickup", "Roastery"],
    lat: 33.4936,
    lng: -117.1484,
    image: "/products/bag.jpg",
    pickup: true,
  },
  {
    id: "ship",
    name: "Ships from the U.S.",
    city: "Nationwide",
    region: "United States",
    address: "Quiet logistics · partner packed under our label",
    hours: "Ships in 2–4 days",
    amenities: ["Whole-bean", "Capsules", "Merch"],
    lat: 39.5,
    lng: -98.35,
    image: "/products/bag.jpg",
    pickup: false,
  },
];

export type Reward = {
  id: string;
  beans: number;
  name: string;
  detail: string;
};

export const REWARDS: Reward[] = [
  { id: "r25", beans: 25, name: "Extra shot or house syrup", detail: "A customization, on us." },
  { id: "r50", beans: 50, name: "Hacienda drip or pastry", detail: "The weekday cup, or a mallorca." },
  { id: "r150", beans: 150, name: "Handcrafted drink", detail: "Cortado, con leche, cold brew." },
  { id: "r200", beans: 200, name: "Bakery and a drip", detail: "The morning pair." },
  { id: "r400", beans: 400, name: "House-mark merch", detail: "Mug or a bag of Colombia." },
];

export type Offer = {
  id: string;
  title: string;
  body: string;
  image: string;
  cta: string;
  href: string;
  beans?: number;
};

export const OFFERS: Offer[] = [
  {
    id: "morning10",
    title: "MORNING10",
    body: "10% off bags before 11. The highland weekday.",
    image: "/products/bag.jpg",
    cta: "Order beans",
    href: "/order?cat=beans",
  },
  {
    id: "double-pm",
    title: "After two",
    body: "Double beans on any cold cup after 2 PM.",
    image: "/products/coldbrew.jpg",
    cta: "See cold",
    href: "/order?cat=cold",
    beans: 2,
  },
  {
    id: "birthday",
    title: "Birthday cup",
    body: "A handcrafted drink on your birthday, every year.",
    image: "/products/conleche.jpg",
    cta: "Add birthday",
    href: "/account",
  },
  {
    id: "challenge",
    title: "Three origins",
    body: "Buy any three origin bags this month — 50 bonus beans.",
    image: "/products/highlands.jpg",
    cta: "Shop origins",
    href: "/order?cat=beans",
    beans: 50,
  },
];

export type GiftDesign = {
  id: string;
  name: string;
  image: string;
};

export const GIFT_DESIGNS: GiftDesign[] = [
  { id: "kraft", name: "Kraft morning", image: "/products/gift-kraft.jpg" },
  { id: "forest", name: "Highland forest", image: "/products/gift-forest.jpg" },
  { id: "hacienda", name: "Hacienda light", image: "/products/highlands.jpg" },
];

export const GIFT_AMOUNTS = [1000, 1500, 2500, 5000, 10000];

export type Tier = {
  id: "welcome" | "cosecha" | "hacienda";
  name: string;
  minLifetime: number;
  perks: string[];
};

export const TIERS: Tier[] = [
  {
    id: "welcome",
    name: "Welcome",
    minLifetime: 0,
    perks: ["Free to join", "Birthday drink", "Member offers"],
  },
  {
    id: "cosecha",
    name: "Cosecha",
    minLifetime: 100,
    perks: ["2× beans with Casa Card", "Free extra shot monthly", "Early drops"],
  },
  {
    id: "hacienda",
    name: "Hacienda",
    minLifetime: 300,
    perks: ["2× beans always", "A free drink each month", "First look at lots"],
  },
];

export function productById(id: string) {
  return PRODUCTS.find((p) => p.id === id);
}

export function storeById(id: string) {
  return STORES.find((s) => s.id === id);
}

export function productsIn(cat: CategoryId) {
  if (cat === "featured") return PRODUCTS.filter((p) => p.featured);
  return PRODUCTS.filter((p) => p.category === cat);
}

const REWARD_OFF_CENTS: Record<string, number> = {
  r25: 70,
  r50: 325,
  r150: 450,
  r200: 750,
  r400: 1495,
};

/** Cents taken off the order when this reward is redeemed. */
export function rewardOffCents(rewardId: string | undefined): number {
  if (!rewardId) return 0;
  return REWARD_OFF_CENTS[rewardId] ?? 0;
}

/**
 * Beans earned per dollar. Casa Card pays 2×. Hacienda tier pays 2× on any tender,
 * which is the "2× beans always" perk.
 */
export function beanRateFor(lifetimeBeans: number, payWith: "wallet" | "card"): number {
  if (tierFor(lifetimeBeans).id === "hacienda") return 2;
  return payWith === "wallet" ? 2 : 1;
}

export function tierFor(lifetime: number): Tier {
  let current = TIERS[0];
  for (const t of TIERS) {
    if (lifetime >= t.minLifetime) current = t;
  }
  return current;
}

export function nextReward(beans: number): { reward: Reward; have: number; need: number } {
  const reward = REWARDS.find((r) => beans < r.beans) ?? REWARDS[REWARDS.length - 1];
  const prev = [...REWARDS].reverse().find((r) => r.beans <= beans);
  const floor = beans >= reward.beans ? reward.beans : (prev?.beans ?? 0);
  const have = Math.min(beans, reward.beans) - (beans >= reward.beans ? 0 : floor);
  const span = reward.beans - floor;
  return { reward, have: beans >= reward.beans ? span : have, need: span };
}

export function memberNumber(userId: string): string {
  let h = 2166136261;
  for (let i = 0; i < userId.length; i++) {
    h ^= userId.charCodeAt(i);
    h = Math.imul(h, 16777619);
  }
  const n = Math.abs(h).toString().padStart(10, "0");
  return (`6081${n}0000`).slice(0, 16);
}

export function formatMember(no: string) {
  return no.replace(/(.{4})/g, "$1 ").trim();
}
