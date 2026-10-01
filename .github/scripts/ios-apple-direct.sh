#!/usr/bin/env bash
# Archive an Expo iOS app and upload the IPA to App Store Connect.
# Runs on a Mac (or a GitHub macos runner). Does not call EAS Build or EAS Submit,
# so expo.dev does not charge a build credit for the upload.
#
# Required env:
#   IOS_DIST_P12_BASE64       Apple Distribution certificate (.p12), base64
#   IOS_DIST_P12_PASSWORD     password for that .p12
#   IOS_PROFILE_BASE64        App Store provisioning profile for this bundle id, base64
#   APPLE_API_KEY_P8          App Store Connect API key (.p8 contents)
#   APPLE_API_KEY_ID          key id (AuthKey_<id>.p8)
#   APPLE_API_ISSUER_ID       issuer uuid
# Optional:
#   APPLE_TEAM_ID             defaults to FY5H9V76QL
#   KEYCHAIN_PASSWORD         defaults to a random password for this run
#
# Usage (from the app directory):
#   bash ../../.github/scripts/ios-apple-direct.sh
set -euo pipefail

if [[ "$(uname -s)" != "Darwin" ]]; then
  echo "iOS archive needs macOS (Xcode). This script does not use expo.dev."
  echo "Run it on a Mac, or dispatch the GitHub workflow 'iOS direct to TestFlight'."
  exit 1
fi

cd "${1:-$PWD}"

: "${IOS_DIST_P12_BASE64:?Set IOS_DIST_P12_BASE64 (base64 of the Apple Distribution .p12)}"
: "${IOS_DIST_P12_PASSWORD:?Set IOS_DIST_P12_PASSWORD}"
: "${IOS_PROFILE_BASE64:?Set IOS_PROFILE_BASE64 (base64 of the App Store .mobileprovision)}"
: "${APPLE_API_KEY_P8:?Set APPLE_API_KEY_P8}"
: "${APPLE_API_KEY_ID:?Set APPLE_API_KEY_ID}"
: "${APPLE_API_ISSUER_ID:?Set APPLE_API_ISSUER_ID}"

TEAM_ID="${APPLE_TEAM_ID:-FY5H9V76QL}"
WORKDIR="${RUNNER_TEMP:-$(mktemp -d)}"
mkdir -p "$WORKDIR"

eval "$(node --input-type=module -e '
import fs from "node:fs";
const app = JSON.parse(fs.readFileSync("app.json", "utf8")).expo;
const q = (v) => JSON.stringify(String(v));
process.stdout.write(
  `BUNDLE_ID=${q(app.ios.bundleIdentifier)}\n` +
  `BUILD_NUMBER=${q(app.ios.buildNumber)}\n` +
  `MARKETING_VERSION=${q(app.version)}\n` +
  `APP_NAME=${q(app.name)}\n`
);
')"

echo "Direct App Store upload: $APP_NAME $MARKETING_VERSION ($BUILD_NUMBER) $BUNDLE_ID"
echo "No EAS build credit is used. Apple still rejects a build number it already has."
echo "If this number was uploaded before, run npm run bump:ios and commit first."

decode() {
  local out="$1"
  local b64="$2"
  OUT="$out" B64="$b64" python3 -c 'import os,base64,pathlib; pathlib.Path(os.environ["OUT"]).write_bytes(base64.b64decode("".join(os.environ["B64"].split())))'
}

P12="$WORKDIR/dist.p12"
PROFILE="$WORKDIR/app.mobileprovision"
decode "$P12" "$IOS_DIST_P12_BASE64"
decode "$PROFILE" "$IOS_PROFILE_BASE64"

PROFILE_NAME="$(security cms -D -i "$PROFILE" | plutil -extract Name raw -)"
PROFILE_UUID="$(security cms -D -i "$PROFILE" | plutil -extract UUID raw -)"
echo "Provisioning profile: $PROFILE_NAME ($PROFILE_UUID)"

KC_PASS="${KEYCHAIN_PASSWORD:-$(openssl rand -base64 24)}"
KC="$WORKDIR/app-signing.keychain-db"
cleanup() {
  security delete-keychain "$KC" >/dev/null 2>&1 || true
}
trap cleanup EXIT

security create-keychain -p "$KC_PASS" "$KC"
security set-keychain-settings -lut 21600 "$KC"
security unlock-keychain -p "$KC_PASS" "$KC"
security import "$P12" -P "$IOS_DIST_P12_PASSWORD" -A -t cert -f pkcs12 -k "$KC"
security set-key-partition-list -S apple-tool:,apple:,codesign: -s -k "$KC_PASS" "$KC"
security list-keychains -d user -s "$KC" "$HOME/Library/Keychains/login.keychain-db"
security default-keychain -s "$KC"

mkdir -p "$HOME/Library/MobileDevice/Provisioning Profiles"
cp "$PROFILE" "$HOME/Library/MobileDevice/Provisioning Profiles/$PROFILE_UUID.mobileprovision"

export EXPO_USE_PRECOMPILED_MODULES=0
npx expo prebuild --platform ios --no-install --non-interactive

python3 - <<'PY'
from pathlib import Path
p = Path("ios/Podfile")
text = p.read_text()
snippet = """
  installer.pods_project.targets.each do |target|
    target.build_configurations.each do |config|
      config.build_settings["CODE_SIGNING_ALLOWED"] = "NO"
    end
  end
"""
needle = "post_install do |installer|"
if "CODE_SIGNING_ALLOWED" not in text:
    if needle not in text:
        raise SystemExit("Podfile has no post_install block")
    text = text.replace(needle, needle + snippet, 1)
    p.write_text(text)
PY

(cd ios && pod install)

WORKSPACE="$(find ios -maxdepth 1 -name '*.xcworkspace' ! -name 'project.xcworkspace' | head -1)"
if [[ -z "$WORKSPACE" ]]; then
  echo "No Xcode workspace after prebuild"
  exit 1
fi
SCHEME="$(basename "$WORKSPACE" .xcworkspace)"
ARCHIVE="$WORKDIR/app.xcarchive"
EXPORT_DIR="$WORKDIR/export"
mkdir -p "$EXPORT_DIR"

EXPORT_PLIST="$WORKDIR/ExportOptions.plist"
cat > "$EXPORT_PLIST" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>method</key>
  <string>app-store-connect</string>
  <key>teamID</key>
  <string>${TEAM_ID}</string>
  <key>signingStyle</key>
  <string>manual</string>
  <key>signingCertificate</key>
  <string>Apple Distribution</string>
  <key>provisioningProfiles</key>
  <dict>
    <key>${BUNDLE_ID}</key>
    <string>${PROFILE_NAME}</string>
  </dict>
  <key>uploadSymbols</key>
  <true/>
</dict>
</plist>
EOF

xcodebuild \
  -workspace "$WORKSPACE" \
  -scheme "$SCHEME" \
  -configuration Release \
  -destination 'generic/platform=iOS' \
  -archivePath "$ARCHIVE" \
  CODE_SIGN_STYLE=Manual \
  DEVELOPMENT_TEAM="$TEAM_ID" \
  "CODE_SIGN_IDENTITY=Apple Distribution" \
  PROVISIONING_PROFILE_SPECIFIER="$PROFILE_NAME" \
  archive

xcodebuild -exportArchive \
  -archivePath "$ARCHIVE" \
  -exportPath "$EXPORT_DIR" \
  -exportOptionsPlist "$EXPORT_PLIST"

IPA="$(find "$EXPORT_DIR" -name '*.ipa' | head -1)"
if [[ -z "$IPA" ]]; then
  echo "Export did not produce an IPA"
  exit 1
fi
echo "IPA: $IPA"

KEY_DIR="$HOME/.appstoreconnect/private_keys"
mkdir -p "$KEY_DIR"
printf '%s\n' "$APPLE_API_KEY_P8" > "$KEY_DIR/AuthKey_${APPLE_API_KEY_ID}.p8"
chmod 600 "$KEY_DIR/AuthKey_${APPLE_API_KEY_ID}.p8"

if xcrun iTMSTransporter -m upload -assetFile "$IPA" -apiKey "$APPLE_API_KEY_ID" -apiIssuer "$APPLE_API_ISSUER_ID"; then
  echo "Uploaded to App Store Connect with iTMSTransporter."
else
  echo "iTMSTransporter failed; trying altool."
  xcrun altool --upload-app --type ios --file "$IPA" \
    --apiKey "$APPLE_API_KEY_ID" --apiIssuer "$APPLE_API_ISSUER_ID"
fi

echo "Upload finished. Open App Store Connect → TestFlight for $BUNDLE_ID."
echo "expo.dev was not used."
