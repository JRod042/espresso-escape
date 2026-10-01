# Casa Rustico Go — iOS / TestFlight

**No Expo charge:** GitHub → Actions → **iOS direct to TestFlight**, app `casa-rustico-go`. Or, on a Mac, `npm run ios:direct`. That archives with Xcode and uploads with Apple’s Transporter. See [docs/IOS_GITHUB_BUILDS.md](../../docs/IOS_GITHUB_BUILDS.md). The EAS steps below still spend a cloud build credit ($2 iOS after the free monthly builds).

Bundle ID: **`com.jrod042.casarusticogo`**  
Expo: **`@jrod42/casa-rustico-go`**  
Apple Team: **`FY5H9V76QL`**  
GitHub base directory: **`apps/casa-rustico-go`**

These notes mirror the Omni (`JRod042/project-1`) EAS fixes that already shipped.

---

## Current failure (GitHub Actions)

```
Distribution Certificate is not validated for non-interactive builds.
Failed to set up credentials.
Credentials are not set up. Run this command again in interactive mode.
```

EXPO_TOKEN is fine. The Expo project simply has **no Distribution Certificate yet**.
Non-interactive / CI builds refuse to create one (eas-cli source behavior).

### Fix once (then CI works forever)

**Fastest path — Expo web UI**
1. Open https://expo.dev/accounts/jrod42/projects/casa-rustico-go/credentials
2. Select iOS → bundle `com.jrod042.casarusticogo` → **App Store**
3. Generate / assign Distribution Certificate + App Store Provisioning Profile
4. Complete Apple login
5. Confirm both show valid

**CLI path (interactive, once)**
```bash
cd apps/casa-rustico-go
npm ci
npx eas-cli credentials -p ios
# or: npx eas-cli build --platform ios --profile production   # no --non-interactive
```

After the certificate exists on EAS, re-run the GitHub workflow (or `eas build --non-interactive`).

An App Store Connect API key alone is **not enough**.

---

## Omni fixes applied here

| Omni issue | Fix in this app |
|---|---|
| Missing / wrong profile for tester builds | Explicit `internal` + `preview` (+ `production` for TestFlight) |
| `autoIncrement` unsupported / risky | **Not used** — bump with `npm run bump:ios` |
| `pod install` / precompiled modules | `EXPO_USE_PRECOMPILED_MODULES=0` on all profiles |
| Privacy manifest CocoaPods crash | `expo-build-properties` → `privacyManifestAggregationEnabled: false` |
| `expo-dev-client` on store builds | Not a dependency; only `development` sets `developmentClient` |
| API key alone ≠ credentials | Need Distribution Cert + App Store profile on Expo |
| Green build ≠ TestFlight | Must **Submit** IPA to App Store Connect |

---

## Build matrix

| Profile | Distribution | Use |
|---|---|---|
| `production` | store | TestFlight / App Store (preferred) |
| `internal` / `preview` | internal | Device testers via Expo (needs Ad Hoc devices) |
| `development` | internal + dev client | Native debug |

```bash
cd apps/casa-rustico-go
npm ci
npm run preflight:ios
# TestFlight path (after credentials exist):
npx eas-cli build --platform ios --profile production
# then Submit on expo.dev (or eas submit)
```

GitHub → Expo: base directory **`apps/casa-rustico-go`**, profile **`production`**.

---

## Before every Expo build

```
[ ] Apple App ID com.jrod042.casarusticogo exists
[ ] App Store Connect app exists (add ascAppId to eas.json submit when known)
[ ] Expo credentials: Distribution Cert + App Store profile VALID  ← current blocker
[ ] GitHub connected; base directory = apps/casa-rustico-go
[ ] Bumped ios.buildNumber if Apple already used current number (npm run bump:ios)
[ ] npm ci green
[ ] npm run preflight:ios green
[ ] Expo → Build from GitHub → main → iOS → production
[ ] Submit → TestFlight → Ready to Test
```

### Submit note

`submit.production.ios.appleTeamId` is set (`FY5H9V76QL`).  
When the ASC app exists, add `"ascAppId": "<digits>"` under `submit.production.ios` (same as Omni).

---

## If a build fails again

Paste the **full red error block** from the Expo build log (credentials / Xcode / pod lines), not only `build:internal`.
