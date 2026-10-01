import { Image, Linking, Pressable, ScrollView, StyleSheet, Text, View } from "react-native";
import { brand, colombia, formatPrice, gear, getProduct } from "../catalog";
import { cartFingerprint } from "../checkout-grant.js";
import { beansForSpend } from "../rewards";
import { cartPermalink } from "../shopify";
import { cartCount, cartSubtotal, lineUnitPrice, useShop } from "../store";
import { colors, fonts } from "../theme";
import { BrassButton, Kicker, ProductCard, Qty, StoreTitle, ui } from "./ui";

export function BagScreen({ openProduct }: { openProduct: (id: string) => void }) {
  const { cart, setCartQty, removeFromCart, flash, member, armCheckoutGrant } = useShop();
  const count = cartCount(cart);
  const subtotal = cartSubtotal(cart);
  const checkout = cartPermalink(
    cart.map((l) => ({ variantId: l.variantId, qty: l.qty })),
    { discountCode: brand.promo },
  );
  const mug = gear()[0];
  const hasCoffee = cart.some((l) => l.productId !== "cr-mug");
  const hasMug = cart.some((l) => l.productId === "cr-mug");
  const suggest = hasCoffee && !hasMug && mug ? mug : !hasCoffee ? colombia : null;
  const beans = member ? beansForSpend(subtotal, member.lifetime) : 0;

  const pay = () => {
    if (member && beans > 0) {
      armCheckoutGrant(
        cartFingerprint(cart.map((l) => ({ variantId: l.variantId, qty: l.qty }))),
        beans,
      );
    }
    void Linking.openURL(checkout);
  };

  if (count === 0) {
    return (
      <View>
        <StoreTitle title="Bag." sub="Your bag is empty." />
        <View style={{ paddingHorizontal: 20, paddingTop: 24 }}>
          <Text style={s.dim}>Start with Colombia, the house bag.</Text>
          <View style={{ marginTop: 24 }}>
            <BrassButton label="Shop Colombia" onPress={() => openProduct(colombia.id)} />
          </View>
        </View>
      </View>
    );
  }

  return (
    <View style={{ flex: 1 }}>
      <ScrollView contentContainerStyle={s.page} showsVerticalScrollIndicator={false}>
        <StoreTitle title="Bag." sub={`${count} ${count === 1 ? "item" : "items"}`} />
        {cart.map((line) => {
          const product = getProduct(line.productId);
          return (
            <View key={`${line.productId}-${line.variantId}`} style={s.row}>
              {product ? (
                <Pressable onPress={() => openProduct(product.id)}>
                  <Image source={{ uri: product.image }} style={s.thumb} />
                </Pressable>
              ) : null}
              <View style={{ flex: 1, minWidth: 0 }}>
                <Text style={s.name}>{product?.name ?? line.productId}</Text>
                <Text style={s.meta}>{line.variantTitle}</Text>
                <Text style={s.linePrice}>{formatPrice(lineUnitPrice(line) * line.qty)}</Text>
                <View style={s.lineActions}>
                  <Qty
                    value={line.qty}
                    onChange={(n) => setCartQty(line.productId, line.variantId, n)}
                    min={0}
                  />
                  <Pressable
                    onPress={() => removeFromCart(line.productId, line.variantId)}
                    hitSlop={8}
                  >
                    <Text style={s.remove}>Remove</Text>
                  </Pressable>
                </View>
              </View>
            </View>
          );
        })}

        <Pressable
          onPress={() => flash(`${brand.promo} · apply at checkout`)}
          style={({ pressed }) => [s.promo, pressed && ui.pressed]}
        >
          <Kicker>Promo</Kicker>
          <Text style={s.promoCode}>{brand.promo}</Text>
          <Text style={s.dim}>{brand.promoCopy}. Apply at checkout.</Text>
        </Pressable>

        {suggest ? (
          <View style={s.suggest}>
            <Kicker>Add</Kicker>
            <View style={{ width: 176, marginTop: 12 }}>
              <ProductCard product={suggest} onPress={() => openProduct(suggest.id)} />
            </View>
          </View>
        ) : null}
      </ScrollView>

      <View style={s.dock}>
        <View>
          <Text style={s.subLabel}>{member && beans ? `+${beans} beans` : "Subtotal"}</Text>
          <Text style={s.subVal}>{formatPrice(subtotal)}</Text>
        </View>
        <Pressable
          onPress={pay}
          style={({ pressed }) => [s.checkout, pressed && ui.pressed]}
          accessibilityLabel="Check out on rusticopr.com"
        >
          <Text style={s.checkoutText}>Check Out</Text>
        </Pressable>
      </View>
    </View>
  );
}

const s = StyleSheet.create({
  page: { paddingBottom: 24 },
  dim: { color: colors.linenDim, fontFamily: fonts.sans, fontSize: 15, lineHeight: 21 },
  row: {
    flexDirection: "row",
    gap: 16,
    paddingHorizontal: 20,
    paddingVertical: 16,
    borderBottomWidth: 1,
    borderBottomColor: colors.line,
  },
  thumb: { width: 80, height: 80, borderRadius: 8, backgroundColor: colors.elevated },
  name: { color: colors.linen, fontFamily: fonts.sansSemi, fontSize: 16 },
  meta: { marginTop: 2, color: colors.linenMuted, fontFamily: fonts.sans, fontSize: 14 },
  linePrice: {
    marginTop: 4,
    color: colors.linen,
    fontFamily: fonts.sansSemi,
    fontSize: 14,
    fontVariant: ["tabular-nums"],
  },
  lineActions: {
    marginTop: 12,
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
  },
  remove: { color: colors.linenMuted, fontFamily: fonts.sansSemi, fontSize: 14 },
  promo: {
    marginHorizontal: 20,
    marginTop: 20,
    backgroundColor: colors.elevated,
    borderRadius: 16,
    padding: 16,
  },
  promoCode: { marginTop: 4, color: colors.linen, fontFamily: fonts.sansSemi, fontSize: 16 },
  suggest: { marginTop: 32, paddingHorizontal: 20 },
  dock: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    paddingHorizontal: 20,
    paddingVertical: 12,
    borderTopWidth: 1,
    borderTopColor: colors.line,
    backgroundColor: colors.bg,
    gap: 16,
  },
  subLabel: { color: colors.linenMuted, fontFamily: fonts.sans, fontSize: 12 },
  subVal: {
    color: colors.linen,
    fontFamily: fonts.sansSemi,
    fontSize: 16,
    fontVariant: ["tabular-nums"],
  },
  checkout: {
    flex: 1,
    minHeight: 48,
    borderRadius: 999,
    backgroundColor: colors.linen,
    alignItems: "center",
    justifyContent: "center",
  },
  checkoutText: { color: colors.ink, fontFamily: fonts.sansBold, fontSize: 16 },
});

