# iOS builds from GitHub

These are **iOS** App Store apps (not Android-first).

## Ship without an Expo charge

EAS Submit is free. The charge is the **cloud build**: an iOS medium worker is **$2** after the Free plan’s 15 iOS builds. The workflow `.github/workflows/ios-testflight.yml` calls `eas build`, so every TestFlight upload spends a build credit.

**Direct path (no expo.dev bill):** GitHub → **Actions** → **iOS direct to TestFlight**. That job runs on a macOS runner, archives with Xcode, and uploads with Apple’s Transporter. It never calls `eas build` or `eas submit`.

The same script runs on your own Mac (`npm run ios:direct` inside the app folder), which also avoids GitHub macOS minutes.

One-time secrets on the GitHub repo:

| Secret | What it is |
|---|---|
| `IOS_DIST_P12_BASE64` | Apple Distribution `.p12`, base64 |
| `IOS_DIST_P12_PASSWORD` | Password for that `.p12` |
| `IOS_PROFILE_GO_BASE64` | App Store profile for `com.jrod042.casarusticogo` |
| `IOS_PROFILE_ESCAPE_BASE64` | App Store profile for `com.jrod042.espressoescape` |
| `APPLE_API_KEY_P8` | App Store Connect API key contents |
| `APPLE_API_KEY_ID` | Key id |
| `APPLE_API_ISSUER_ID` | Issuer id |

Export the `.p12` from Keychain Access (Apple Distribution) or download the cert Expo already stored (`npx eas-cli credentials -p ios` — that download is not a build). Encode with `base64 -i cert.p12`. Profiles come from [developer.apple.com](https://developer.apple.com/account/resources/profiles/list) → App Store → the bundle id.

Bump `ios.buildNumber` with `npm run bump:ios` and commit before each upload. Apple rejects a number it already accepted. Bundle ids stay as they are.

Hacienda is the web café, not an Expo app. Build it with `npm run build` in `apps/hacienda` and run the Node server on whatever host you already use. Expo does not bill it.

## EAS cloud builds (still available)

Hardening below mirrors fixes already proven on **Omni** (`JRod042/project-1`).

> **Current blocker (Aug 23 2026 runs #1–#3)**  
> All GitHub Actions runs fail with the exact eas-cli message:
> ```
> Distribution Certificate is not validated for non-interactive builds.
> Failed to set up credentials.
> Credentials are not set up. Run this command again in interactive mode.
> ```
> This is **not** an EXPO_TOKEN problem (whoami succeeds). It means the two
> Expo projects do not yet have a validated Distribution Certificate + App
> Store Provisioning Profile stored on EAS. Non-interactive / CI builds
> refuse to create them.

## Expo projects

| App | Expo project | Base directory on GitHub | Bundle ID |
|-----|--------------|--------------------------|-----------|
| Casa Rustico Go | https://expo.dev/accounts/jrod42/projects/casa-rustico-go | `apps/casa-rustico-go` | `com.jrod042.casarusticogo` |
| Espresso Escape | https://expo.dev/accounts/jrod42/projects/espresso-escape | `apps/espresso-escape` | `com.jrod042.espressoescape` |

Per-app checklists: [`apps/casa-rustico-go/IOS.md`](../apps/casa-rustico-go/IOS.md), [`apps/espresso-escape/IOS.md`](../apps/espresso-escape/IOS.md).

## One-time credential bootstrap (REQUIRED before any CI build)

Public eas-cli source and dozens of GitHub issues confirm the same rule:
**the first Distribution Certificate for a project must be created/validated interactively.**

### Option A — Expo web UI (fastest, no local Mac required)

1. Open the project credentials page:
   - Casa Rustico Go → https://expo.dev/accounts/jrod42/projects/casa-rustico-go/credentials
   - Espresso Escape → https://expo.dev/accounts/jrod42/projects/espresso-escape/credentials
2. Select the **iOS** bundle identifier.
3. Choose **App Store** distribution (needed for TestFlight).
4. Let Expo generate / select a **Distribution Certificate** and matching **App Store Provisioning Profile**.
5. Complete Apple login when prompted.
6. Confirm both items show as valid on the credentials page.

### Option B — Local interactive CLI (once per app)

```bash
cd apps/casa-rustico-go   # or apps/espresso-escape
npm ci
npx eas-cli credentials -p ios
# follow prompts → App Store → generate/use Distribution Certificate + Profile

# or simply run the first build interactively (no --non-interactive):
npx eas-cli build --platform ios --profile production
```

After either option succeeds, the GitHub workflow will pass the credentials step.

An App Store Connect API key alone is **not enough** — the Distribution Certificate must exist on the Expo project.

## EAS profiles

| Profile | Distribution | Use for |
|---------|--------------|---------|
| `production` | store | **TestFlight / App Store (preferred)** |
| `internal` | internal | Ad hoc / Expo install links (needs registered devices) |
| `preview` | same as `internal` | Alias |
| `development` | internal + dev client | Native debug only |

### Correct commands (after credentials exist)

```bash
cd apps/casa-rustico-go   # or apps/espresso-escape
npm ci
npm run preflight:ios

# TestFlight path
npm run eas:build:ios          # profile production
# after green build → Submit on expo.dev
npm run eas:submit:ios
```

## Omni lessons applied to both apps

| Omni failure | What we do here |
|---|---|
| `Error: build:internal command failed` | Treat as **generic wrapper** — read the real log line above it |
| Credentials not set up | Document Distribution Cert + App Store profile (API key alone fails) |
| `autoIncrement` unsupported / reuse | **No autoIncrement**; `npm run bump:ios` |
| `pod install` exit 1 | `EXPO_USE_PRECOMPILED_MODULES=0` on every profile |
| Privacy manifest CocoaPods crash | `privacyManifestAggregationEnabled: false` via `expo-build-properties` |
| Green build not in TestFlight | Build ≠ Submit — must Submit IPA to ASC |
| Flaky GitHub base dir | Expo project base directory must be the app folder |

## GitHub → EAS connection checklist

1. Link GitHub: https://expo.dev/settings#connections  
2. Install Expo GitHub App on account JRod042  
3. For each project → **GitHub** → repo `JRod042/Casa-Rustico`  
4. Set **Base directory** to the app path above  
5. Credentials → iOS → **App Store** → Distribution Certificate + Provisioning Profile (Apple team `FY5H9V76QL`)  
6. First interactive local build optional (see bootstrap above)  
7. Then **Build from GitHub** → iOS → **production** → when green → **Submit**

## Workflow

Root workflow: `.github/workflows/ios-testflight.yml` (workflow_dispatch, chooses app + profile + submit).
Each app also has `.eas/workflows/build-ios.yml` for Expo-side triggers.
