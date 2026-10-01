import { Image, Pressable, StyleSheet, Text, View } from "react-native";
import { priceLabel, type Product } from "../catalog";
import { colors, fonts } from "../theme";

export function StoreTitle({ title, sub }: { title: string; sub?: string }) {
  return (
    <View style={ui.titleWrap}>
      <Text style={ui.title}>{title}</Text>
      {sub ? <Text style={ui.sub}>{sub}</Text> : null}
    </View>
  );
}

export function ProductCard({
  product,
  large,
  onPress,
}: {
  product: Product;
  large?: boolean;
  onPress: () => void;
}) {
  return (
    <Pressable
      onPress={onPress}
      accessibilityRole="button"
      accessibilityLabel={`${product.name}, ${priceLabel(product)}`}
      style={({ pressed }) => [large ? ui.cardLarge : ui.card, pressed && ui.pressed]}
    >
      <View style={[ui.photo, large ? ui.photoLarge : ui.photoGrid]}>
        <Image source={{ uri: product.image }} style={StyleSheet.absoluteFill} resizeMode="cover" />
        {product.badge ? (
          <View style={ui.badge}>
            <Text style={ui.badgeText}>{product.badge}</Text>
          </View>
        ) : null}
      </View>
      <Text style={ui.cardName} numberOfLines={1}>
        {product.name}
      </Text>
      <Text style={ui.cardPrice}>{priceLabel(product)}</Text>
    </Pressable>
  );
}

export function Chip({
  label,
  on,
  onPress,
}: {
  label: string;
  on: boolean;
  onPress: () => void;
}) {
  return (
    <Pressable
      onPress={onPress}
      accessibilityRole="button"
      accessibilityState={{ selected: on }}
      style={[ui.chip, on && ui.chipOn]}
    >
      <Text style={[ui.chipText, on && ui.chipTextOn]}>{label}</Text>
    </Pressable>
  );
}

export function Qty({
  value,
  onChange,
  min = 0,
}: {
  value: number;
  onChange: (n: number) => void;
  min?: number;
}) {
  return (
    <View style={ui.qtyRow}>
      <Pressable
        onPress={() => onChange(Math.max(min, value - 1))}
        accessibilityLabel="Decrease"
        style={ui.qtyBtn}
      >
        <Text style={ui.qtyGlyph}>−</Text>
      </Pressable>
      <Text style={ui.qtyVal}>{value}</Text>
      <Pressable onPress={() => onChange(value + 1)} accessibilityLabel="Increase" style={ui.qtyBtn}>
        <Text style={ui.qtyGlyph}>+</Text>
      </Pressable>
    </View>
  );
}

export function BrassButton({
  label,
  onPress,
  disabled,
}: {
  label: string;
  onPress: () => void;
  disabled?: boolean;
}) {
  return (
    <Pressable
      onPress={onPress}
      disabled={disabled}
      style={({ pressed }) => [ui.brass, pressed && ui.pressed, disabled && { opacity: 0.55 }]}
    >
      <Text style={ui.brassText}>{label}</Text>
    </Pressable>
  );
}

export function Kicker({ children }: { children: string }) {
  return <Text style={ui.kicker}>{children.toUpperCase()}</Text>;
}

export const ui = StyleSheet.create({
  titleWrap: { paddingHorizontal: 20, paddingBottom: 20, paddingTop: 4, paddingRight: 64 },
  title: {
    color: colors.linen,
    fontFamily: fonts.display,
    fontSize: 34,
    lineHeight: 38,
    letterSpacing: -0.4,
  },
  sub: {
    marginTop: 8,
    maxWidth: 340,
    color: colors.linenDim,
    fontFamily: fonts.sans,
    fontSize: 16,
    lineHeight: 22,
  },
  card: { width: "100%" },
  cardLarge: { width: 176, marginRight: 12 },
  photo: {
    overflow: "hidden",
    borderRadius: 12,
    backgroundColor: colors.elevated,
  },
  photoGrid: { aspectRatio: 4 / 5 },
  photoLarge: { height: 208 },
  badge: {
    position: "absolute",
    left: 10,
    top: 10,
    backgroundColor: "rgba(18,14,11,0.75)",
    borderRadius: 999,
    paddingHorizontal: 10,
    paddingVertical: 4,
  },
  badgeText: {
    color: colors.brass,
    fontFamily: fonts.sansSemi,
    fontSize: 11,
  },
  cardName: {
    marginTop: 10,
    color: colors.linen,
    fontFamily: fonts.sansSemi,
    fontSize: 14,
  },
  cardPrice: {
    marginTop: 2,
    color: colors.linenMuted,
    fontFamily: fonts.sans,
    fontSize: 14,
    fontVariant: ["tabular-nums"],
  },
  pressed: { transform: [{ scale: 0.97 }], opacity: 0.92 },
  chip: {
    minHeight: 40,
    paddingHorizontal: 16,
    borderRadius: 999,
    borderWidth: 1,
    borderColor: colors.lineBright,
    alignItems: "center",
    justifyContent: "center",
    marginRight: 8,
  },
  chipOn: { backgroundColor: colors.linen, borderColor: colors.linen },
  chipText: { color: colors.linenDim, fontFamily: fonts.sansSemi, fontSize: 14 },
  chipTextOn: { color: colors.ink },
  qtyRow: { flexDirection: "row", alignItems: "center", gap: 8 },
  qtyBtn: {
    width: 44,
    height: 44,
    borderRadius: 22,
    borderWidth: 1,
    borderColor: colors.lineBright,
    alignItems: "center",
    justifyContent: "center",
  },
  qtyGlyph: { color: colors.linen, fontSize: 20, lineHeight: 22 },
  qtyVal: {
    width: 24,
    textAlign: "center",
    color: colors.linen,
    fontFamily: fonts.sans,
    fontSize: 16,
    fontVariant: ["tabular-nums"],
  },
  brass: {
    minHeight: 44,
    paddingHorizontal: 20,
    borderRadius: 999,
    backgroundColor: colors.brass,
    alignItems: "center",
    justifyContent: "center",
    alignSelf: "flex-start",
  },
  brassText: { color: colors.ink, fontFamily: fonts.sansBold, fontSize: 14 },
  kicker: {
    color: colors.brass,
    fontFamily: fonts.sansSemi,
    fontSize: 11,
    letterSpacing: 2.4,
  },
});
