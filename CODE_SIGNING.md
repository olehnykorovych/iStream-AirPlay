# Code signing policy

iStream's Windows releases are built by [GitHub Actions](.github/workflows/release.yml) from the source code in this repository and code-signed through the [SignPath Foundation](https://signpath.org) open source program.

> **Status:** application to SignPath Foundation submitted. Until it's approved, releases are unsigned and Windows SmartScreen may warn on first run (*More info → Run anyway*). Once signing is active, this page and the release notes say so.

**Once approved:** Free code signing provided by [SignPath.io](https://signpath.io), certificate by [SignPath Foundation](https://signpath.org).

## What gets signed

| File | Built from |
|---|---|
| `iStream.exe` (the app) | `src/iStream.cs`, `src/Controls.cs`, `src/Strings.cs`, `src/IconArt.cs`, `src/Updater.cs` |
| `uninstall.exe` | `src/Setup.cs`, `src/Controls.cs`, `src/IconArt.cs` |
| `iStream-Setup-<version>.exe` (the installer) | the same setup code, with the signed app and the engine embedded |

Only files built from this repository's source code are signed. The `engine/` folder holds the UxPlay engine from the official [uxplay-windows](https://github.com/leapbtw/uxplay-windows) release, unchanged. It is downloaded and checked against its SHA-256 by [`scripts/get-engine.ps1`](scripts/get-engine.ps1), and it is **not** signed by this project. Its licenses are listed in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

Every signing request comes from the release workflow, so a signed file always matches a commit in this repository. Nothing is signed from a developer's computer.

## Team roles

| Role | Members |
|---|---|
| Committers and reviewers | [olehnykorovych](https://github.com/olehnykorovych) |
| Approvers (approve each signing request) | [olehnykorovych](https://github.com/olehnykorovych) |

All team members use multi-factor authentication for GitHub and SignPath.

## Privacy policy

This program will not transfer any information to other networked systems unless specifically requested by the user or the person installing or operating it, with these exceptions:

- **AirPlay on your local network.** iStream announces itself on your local network (mDNS / Bonjour) and receives the screen your iPhone or iPad sends to it. Nothing leaves your local network.
- **Update check.** With *Settings → Updates → Update automatically* on (the default), iStream sends a plain HTTPS request to GitHub's public API (`api.github.com`) 30 seconds after it starts and then every 6 hours, to find the latest release. If there is one, it downloads the installer from GitHub. No personal data is sent. Turn this off in Settings. GitHub's privacy statement: <https://docs.github.com/site-policy/privacy-policies/github-general-privacy-statement>.

iStream has no telemetry, no accounts and no ads.
