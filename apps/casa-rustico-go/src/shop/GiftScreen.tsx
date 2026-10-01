import { useState } from "react";
import { Image, Linking, Pressable, ScrollView, StyleSheet, Text, View } from "react-native";
import { brand, formatPrice, giftAmounts, giftDesigns } from "../catalog";
import { useShop } from "../store";
import { colors, fonts } from "../theme";
import { BrassButton, Chip, StoreTitle, ui } from "./ui";

export function GiftScreen({ onJoin }: { onJoin: () => void }) {
  const { flash, member } = useShop();
  const [design, setDesign] = useState<(typeof giftDesigns)[number]["id"]>("kraft");
  const [amount, setAmount] = useState<(typeof giftAmounts)[number]>(25);
  const art = giftDesigns.find((d) => d.id === design) ?? giftDesigns[0];

  const send = () => {
    if (!member) {
      onJoin();
      return;
    }
    const subject = encodeURIComponent("A Casa Rústico gift");
    const body = encodeURIComponent(
      `A Casa Rústico gift — ${formatPrice(amount)} ${art.name}. Choose it at ${brand.siteUrl}`,
    );
    flash("Opens Mail with a note. Buy the card on rusticopr.com.");
    void Linking.openURL(`mailto:?subject=${subject}&body=${body}`);
  };

  return (
    <ScrollView contentContainerStyle={s.page} showsVerticalScrollIndicator={false}>
      <StoreTitle title="Gift." sub="A note for someone, or buy the card on rusticopr.com." />
      <View style={s.hero}>
        <Image source={{ uri: art.image }} style={StyleSheet.absoluteFill} resizeMode="cover" />
        <View style={s.heroCopy}>
          <Text style={s.heroKicker}>{art.name}</Text>
          <Text style={s.heroAmt}>{formatPrice(amount)}</Text>
        </View>
      </View>
      <Text style={s.label}>Design</Text>
      <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={s.row}>
        {giftDesigns.map((d) => (
          <Chip key={d.id} label={d.name} on={design === d.id} onPress={() => setDesign(d.id)} />
        ))}
      </ScrollView>
      <Text style={s.label}>Amount</Text>
      <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={s.row}>
        {giftAmounts.map((n) => (
          <Chip key={n} label={formatPrice(n)} on={amount === n} onPress={() => setAmount(n)} />
        ))}
      </ScrollView>
      <View style={{ paddingHorizontal: 20, marginTop: 8, gap: 12 }}>
        <BrassButton label={member ? `Email a ${formatPrice(amount)} note` : "Join to send"} onPress={send} />
        <Pressable onPress={() => Linking.openURL(brand.siteUrl)} style={({ pressed }) => pressed && ui.pressed}>
          <Text style={s.link}>Or buy on {brand.site}</Text>
        </Pressable>
      </View>
    </ScrollView>
  );
}

const s = StyleSheet.create({
  page: { paddingBottom: 96 },
  hero: {
    marginHorizontal: 20,
    height: 180,
    borderRadius: 18,
    overflow: "hidden",
    backgroundColor: colors.elevated,
    justifyContent: "flex-end",
  },
  heroCopy: { padding: 16, backgroundColor: "rgba(18,14,11,0.45)" },
  heroKicker: { color: colors.brass, fontFamily: fonts.sansSemi, fontSize: 12, letterSpacing: 1.6 },
  heroAmt: { color: colors.linen, fontFamily: fonts.display, fontSize: 32, marginTop: 4 },
  label: {
    marginTop: 22,
    marginBottom: 10,
    paddingHorizontal: 20,
    color: colors.linenMuted,
    fontFamily: fonts.sansSemi,
    fontSize: 13,
  },
  row: { paddingHorizontal: 20 },
  link: { color: colors.brass, fontFamily: fonts.sansSemi, fontSize: 14 },
});
