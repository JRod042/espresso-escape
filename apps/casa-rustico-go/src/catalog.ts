export type ShopifyVariant = {
  id: number;
  title: string;
  price: number;
  sku?: string;
};

export type Product = {
  id: string;
  handle: string;
  shopifyProductId: number;
  defaultVariantId: number;
  name: string;
  subtitle: string;
  price: number;
  category: "coffee" | "gear";
  origin?: string;
  roast?: string;
  notes?: string;
  detail?: string;
  badge?: string;
  accent: string;
  image: string;
  variants?: ShopifyVariant[];
};

export type Landscape = {
  id: string;
  title: string;
  kicker: string;
  image: string;
  copy: string;
};

const FILES = "https://cdn.shopify.com/s/files/1/0670/8776/1636/files";
const CDN = FILES;

export const brand = {
  name: "Casa Rústico",
  tagline: "Single-origin. Ship ready.",
  promo: "MORNING10",
  promoCopy: "10% off your first bag",
  site: "rusticopr.com",
  siteUrl: "https://rusticopr.com",
  heroImage: `${FILES}/pr-cordillera-central-vista.jpg`,
  ritualImage: `${FILES}/pr-hacienda-lealtad-landscape.jpg`,
  landscapes: [
    {
      id: "jayuya",
      title: "Jayuya",
      kicker: "The look",
      image: `${FILES}/pr-jayuya-mountain.jpg`,
      copy: "Mountain mornings. The Cordillera as the house mark.",
    },
    {
      id: "hacienda",
      title: "Hacienda Lealtad",
      kicker: "The house",
      image: `${FILES}/pr-hacienda-lealtad-landscape.jpg`,
      copy: "Hacienda memory — the culture of the cup.",
    },
    {
      id: "toro",
      title: "Toro Negro",
      kicker: "The highlands",
      image: `${FILES}/pr-toro-negro-panorama.jpg`,
      copy: "Rooted in coffee country. Ships from the U.S.",
    },
  ] satisfies Landscape[],
};

export const products: Product[] = [
  {
    id: "cr-colombia",
    handle: "casa-rustico-colombia",
    shopifyProductId: 9169712840932,
    defaultVariantId: 49540770201828,
    name: "Colombia",
    subtitle: "Medium · 12 oz",
    price: 25,
    category: "coffee",
    origin: "Medellín, Antioquia",
    roast: "Medium",
    notes: "Dried orange, berry, chocolate",
    detail:
      "Smallholder farms near Medellín. Castillo, Caturra, Colombia & Typica at 1300–1500 m. Fully washed, solar-dried.",
    badge: "House favorite",
    accent: "#8B5E3C",
    image: `${CDN}/kraft_blank_final_latest_d8ef99ab-eaab-4274-b98b-8c3fbcd09ec5.jpg?v=1785445582`,
    variants: [
      { id: 49540770201828, title: "Whole Bean / 12oz", price: 25, sku: "C-COLOMBIA-WH-12OZ-KR" },
      { id: 49542190596324, title: "Espresso / 12oz", price: 25, sku: "C-COLOMBIA-ES-12OZ-KR" },
      { id: 49682879381732, title: "Whole Bean / 1 lb", price: 33, sku: "C-COLOMBIA-WH-1LB-KR" },
    ],
  },
  {
    id: "cr-costa-rica",
    handle: "casa-rustico-costa-rica",
    shopifyProductId: 9169059578084,
    defaultVariantId: 49538978513124,
    name: "Costa Rica",
    subtitle: "Medium · 12 oz",
    price: 25,
    category: "coffee",
    origin: "Alajuela",
    roast: "Medium",
    notes: "Sweet apple, raisin, honey",
    detail: "Micro farms in Alajuela. Caturra & Catuai at 1300–1445 m. Eco-pulped, sun-dried.",
    accent: "#A67C52",
    image: `${CDN}/kraft_blank_final_latest.jpg?v=1785426934`,
    variants: [
      { id: 49538978513124, title: "Whole Bean / 12oz", price: 25 },
      { id: 49542191644900, title: "Espresso / 12oz", price: 25 },
    ],
  },
  {
    id: "cr-brazil",
    handle: "casa-rustico-brazil-santos",
    shopifyProductId: 9169713823972,
    defaultVariantId: 49540771774692,
    name: "Brazil Santos",
    subtitle: "Medium · 12 oz",
    price: 25,
    category: "coffee",
    origin: "Paraná & São Paulo",
    roast: "Medium",
    notes: "Elegant, smooth, cocoa",
    detail: "Fazenda Santa Barbara estates. Catuai & Mundo Novo at 750–1050 m. Pulped natural.",
    accent: "#6B4423",
    image: `${CDN}/kraft_blank_final_latest_65cad81a-6af2-4151-a71b-22f1b71691f0.jpg?v=1785445610`,
    variants: [
      { id: 49540771774692, title: "Whole Bean / 12oz", price: 25 },
      { id: 49542188269796, title: "Espresso / 12oz", price: 25 },
    ],
  },
  {
    id: "cr-ethiopia",
    handle: "casa-rustico-ethiopia-natural",
    shopifyProductId: 9169715069156,
    defaultVariantId: 49540773970148,
    name: "Ethiopia Natural",
    subtitle: "Med-light · 12 oz",
    price: 25,
    category: "coffee",
    origin: "Sidama Zone",
    roast: "Med-light",
    notes: "Milk chocolate, fruity, caramel",
    detail:
      "Sidama smallholders. Indigenous heirloom cultivars at 1700–1900 m. Full natural on raised beds.",
    badge: "Limited",
    accent: "#9C4A3C",
    image: `${CDN}/kraft_blank_final_latest_6c291d07-9359-4e4a-8905-9e24e076edda.jpg?v=1785445641`,
    variants: [
      { id: 49540773970148, title: "Whole Bean / 12oz", price: 25 },
      { id: 49542192922852, title: "Espresso / 12oz", price: 25 },
    ],
  },
  {
    id: "cr-guatemala",
    handle: "casa-rustico-guatemala",
    shopifyProductId: 9169716609252,
    defaultVariantId: 49540776394980,
    name: "Guatemala",
    subtitle: "Medium · 12 oz",
    price: 25,
    category: "coffee",
    origin: "Antigua",
    roast: "Medium",
    notes: "Dark chocolate, bright fruit, butterscotch",
    detail: "Antigua smallholders. Bourbon, Catuai, Caturra & Typica at 1200–1616 m. Fully washed.",
    accent: "#7A5C3E",
    image: `${CDN}/kraft_blank_final_latest_98b9aa9d-8b85-4393-9611-fdd1992eeb22.jpg?v=1785445668`,
    variants: [
      { id: 49540776394980, title: "Whole Bean / 12oz", price: 25 },
      { id: 49542194168036, title: "Espresso / 12oz", price: 25 },
    ],
  },
  {
    id: "cr-sumatra",
    handle: "casa-rustico-sumatra",
    shopifyProductId: 9170108448996,
    defaultVariantId: 49541941166308,
    name: "Sumatra",
    subtitle: "12 oz",
    price: 25,
    category: "coffee",
    origin: "Aceh, Takengon",
    roast: "Medium-dark",
    notes: "Dark chocolate, dried fruit, earthy",
    detail: "KBQB coop. Wet-hulled (giling basah). Tim Tim, Typica, Ateng at 1100–1600 m.",
    accent: "#4A3728",
    image: `${CDN}/kraft_blank_final_latest_e2b0f0f9-8d3f-47d9-957e-6c98c445447e.jpg?v=1785461247`,
    variants: [
      { id: 49541941166308, title: "Whole Bean / 12oz", price: 25 },
      { id: 49542201147620, title: "Espresso / 12oz", price: 25 },
    ],
  },
  {
    id: "cr-kenya",
    handle: "casa-rustico-kenya",
    shopifyProductId: 9170060116196,
    defaultVariantId: 49541800591588,
    name: "Kenya",
    subtitle: "12 oz",
    price: 25,
    category: "coffee",
    origin: "Othaya, Nyeri",
    roast: "Medium",
    notes: "Bright, orange, lemon, floral",
    detail: "Nyeri County. SL28, SL34, Ruiru 11 & Batian at 1700–1890 m. Fully washed, raised beds.",
    badge: "New",
    accent: "#B87333",
    image: `${CDN}/kraft_blank_final_latest_3f457b74-0402-4110-815f-a8e23be0af19.jpg?v=1785460000`,
    variants: [
      { id: 49541800591588, title: "Whole Bean / 12oz", price: 25 },
      { id: 49542196166884, title: "Espresso / 12oz", price: 25 },
    ],
  },
  {
    id: "cr-honduras",
    handle: "casa-rustico-honduras",
    shopifyProductId: 9170061000932,
    defaultVariantId: 49541802426596,
    name: "Honduras",
    subtitle: "Medium-dark · 12 oz",
    price: 25,
    category: "coffee",
    origin: "Marcala, La Paz",
    roast: "Medium-dark",
    notes: "Caramel, spice, brown sugar",
    detail: "Café Orgánicos Marcala cooperative. Fully washed, sun-dried at 1300–1700 m.",
    accent: "#7A5230",
    image: `${CDN}/kraft_blank_final_latest_886c7c4b-491b-40da-8b31-3df01a8eb8a1.jpg?v=1785460000`,
    variants: [
      { id: 49541802426596, title: "Whole Bean / 12oz", price: 25 },
      { id: 49542195347684, title: "Espresso / 12oz", price: 25 },
    ],
  },
  {
    id: "cr-peru",
    handle: "casa-rustico-peru",
    shopifyProductId: 9170100814052,
    defaultVariantId: 49541923471588,
    name: "Peru",
    subtitle: "Medium · 12 oz",
    price: 25,
    category: "coffee",
    origin: "Piura & Amazonas",
    roast: "Medium",
    notes: "Salted caramel, silky sweet, citrus",
    detail: "Cooperativa Norandino. Fully washed at 1100–1700 m.",
    accent: "#8B6914",
    image: `${CDN}/kraft_blank_final_latest_5051ac1a-1b3f-4519-aff5-715a67c0dd08.jpg?v=1785460000`,
    variants: [
      { id: 49541923471588, title: "Whole Bean / 12oz", price: 25 },
      { id: 49542199214308, title: "Espresso / 12oz", price: 25 },
    ],
  },
  {
    id: "cr-bali",
    handle: "casa-rustico-bali",
    shopifyProductId: 9199949381860,
    defaultVariantId: 49701039145188,
    name: "Bali Blue",
    subtitle: "Med-dark · 12 oz",
    price: 25,
    category: "coffee",
    origin: "Kintamani, Bali",
    roast: "Med-dark",
    notes: "Dark chocolate, molasses, brown sugar",
    detail: "Smallholders from Kintamani. Bourbon, Typica, Catimor. Wet-hulled, raised beds.",
    accent: "#5C4033",
    image: `${CDN}/kraft_blank_final_latest_d8ef99ab-eaab-4274-b98b-8c3fbcd09ec5.jpg?v=1785445582`,
    variants: [
      { id: 49701039145188, title: "Whole Bean / 12oz", price: 25 },
      { id: 49701039210724, title: "Espresso / 12oz", price: 25 },
    ],
  },
  {
    id: "cr-capsules",
    handle: "casa-rustico-single-serve-capsules",
    shopifyProductId: 9172160348388,
    defaultVariantId: 49550427324644,
    name: "Single Serve Capsules",
    subtitle: "12 pack",
    price: 14.99,
    category: "coffee",
    origin: "Blend",
    notes: "Dark chocolate, molasses, brown sugar",
    detail: "Compatible pods · same single-origin quality in a weekday format.",
    accent: "#C4A484",
    image: `${CDN}/Single_Serve_Capsules_12_Pack.jpg?v=1785502910`,
    variants: [
      { id: 49550427324644, title: "12 Pack", price: 14.99 },
      { id: 49550436696292, title: "60 Pack", price: 59.99 },
    ],
  },
  {
    id: "cr-mug",
    handle: "casa-rustico-white-glossy-mug",
    shopifyProductId: 9166472741092,
    defaultVariantId: 49532026847460,
    name: "White Glossy Mug",
    subtitle: "House mark · 11 oz",
    price: 14.95,
    category: "gear",
    notes: "Ceramic · dishwasher safe",
    detail: "Everyday house mug with the Casa Rústico mark.",
    accent: "#E8E0D4",
    image: `${CDN}/white-glossy-mug-white-11-oz-handle-on-right-6a6ba31ce0e58.jpg?v=1785439012`,
    variants: [
      { id: 49532026847460, title: "11 oz", price: 14.95 },
      { id: 49532026880228, title: "15 oz", price: 16.95 },
    ],
  },
];

export function getProduct(id: string) {
  return products.find((p) => p.id === id);
}

export function formatPrice(n: number) {
  return n % 1 === 0 ? `$${n}` : `$${n.toFixed(2)}`;
}

/** Lowest variant, prefixed when the bag has more than one price. */
export function priceLabel(product: Product): string {
  const prices = (product.variants ?? []).map((v) => v.price);
  if (!prices.length) return formatPrice(product.price);
  const min = Math.min(...prices);
  const max = Math.max(...prices);
  return max - min > 0.001 ? `From ${formatPrice(min)}` : formatPrice(min);
}

export function coffees() {
  return products.filter((p) => p.category === "coffee");
}

export function origins() {
  return products.filter((p) => p.category === "coffee" && p.id !== "cr-capsules");
}

export function gear() {
  return products.filter((p) => p.category === "gear");
}

export const colombia = products[0];

export type CafeStore = {
  id: string;
  name: string;
  city: string;
  region: string;
  address: string;
  hours: string;
  amenities: string[];
  image: string;
  pickup: boolean;
};

export const stores: CafeStore[] = [
  {
    id: "sanjuan",
    name: "Casa Rústico Atelier",
    city: "San Juan",
    region: "Puerto Rico",
    address: "151 Calle del Cristo, Old San Juan",
    hours: "6:30 AM – 6:00 PM",
    amenities: ["Mobile order", "Wifi", "Indoor", "House-mark merch"],
    image: `${FILES}/pr-cordillera-central-vista.jpg`,
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
    image: `${FILES}/pr-jayuya-mountain.jpg`,
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
    image: `${FILES}/pr-hacienda-lealtad-landscape.jpg`,
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
    image: `${FILES}/pr-toro-negro-panorama.jpg`,
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
    image: `${CDN}/kraft_blank_final_latest_d8ef99ab-eaab-4274-b98b-8c3fbcd09ec5.jpg?v=1785445582`,
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
    image: `${CDN}/kraft_blank_final_latest_d8ef99ab-eaab-4274-b98b-8c3fbcd09ec5.jpg?v=1785445582`,
    pickup: false,
  },
];

export const giftDesigns = [
  { id: "kraft", name: "Kraft morning", image: `${CDN}/kraft_blank_final_latest_d8ef99ab-eaab-4274-b98b-8c3fbcd09ec5.jpg?v=1785445582` },
  { id: "forest", name: "Highland forest", image: `${FILES}/pr-toro-negro-panorama.jpg` },
  { id: "hacienda", name: "Hacienda light", image: `${FILES}/pr-hacienda-lealtad-landscape.jpg` },
] as const;

export const giftAmounts = [10, 15, 25, 50, 100] as const;
