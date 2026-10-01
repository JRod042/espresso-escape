export type Tab = "home" | "order" | "scan" | "gift" | "stores";

export type Screen =
  | { kind: "tab"; tab: Tab }
  | { kind: "product"; productId: string; back: Tab; fromBag?: boolean }
  | { kind: "bag"; back: Tab }
  | { kind: "ritual"; back: Tab }
  | { kind: "story"; back: Tab }
  | { kind: "account"; back: Tab }
  | { kind: "join"; back: Tab };

export type ShopNav = {
  openProduct: (id: string) => void;
  openTab: (tab: Tab) => void;
  openBag: () => void;
  openAccount: () => void;
  openJoin: () => void;
  openStory: () => void;
  openRitual: () => void;
};

export function activeTab(screen: Screen): Tab {
  if (screen.kind === "tab") return screen.tab;
  return screen.back;
}
