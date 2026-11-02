# Measuring the original (Wine version)

The original used for side-by-side checks is Mars Explorer 4.0.2 under the `/Applications/Mars Explorer.app` Wine wrapper. It is newer than the 2.22 source in `unity/`, so values taken only from it are recorded as inferences (see Decisions in [PLAN.md](../PLAN.md)).

Provenance (checked 2026-10-31, macOS `27.2`): this is not the untouched Windows release. It comes from the community "original game with fixes" build on gitea.moe (`VIA256/marsxplr-decomp`), linked from the `GandalfPotter/Mars-Explorer-Guide` README. That repository and its releases refuse automated fetches (HTTP 403), so its list of fixes has not been read. What the install itself shows:

- The engine and content are the original's: `mainData` and `sharedassets0.assets` are built with Unity `2.6.1f3`, and the bundled Mono is 1.2.18.
- The game scripts are rebuilt. `Assembly - UnityScript.dll` (dated 5 January 2026, as are all game files) reports version `4.0.2`, but it checks for updates at `gitea.moe/VIA256/new-marsxplr-web-elements`, sends players to the gitea releases page and the Discord server, links to `web.archive.org` copies of retired pages, and adds a decompression step for downloaded content.

Comparison with the official download (2026-11-02, macOS `27.2`). The official Windows installer was taken from the Wayback Machine copy of the site's download page (`https://web.archive.org/web/20200320064312/http://marsxplr.com/view-267`) and kept as `archive/Windows/MarsExplorer.exe` (untracked; Excelsior Installer, 6,246,395 bytes, SHA-256 `d225d8991d9c9369bf7e36b494e464e2a92a4583f83981ce7d5b24a297885c74`). It was installed silently (`/batch`) under Wine into a throwaway prefix; the installer names itself "Mars Explorer 2.22". When run, the official game shows a "Mars Explorer 3.0" update message; a page on the original site said that message announced the reboot at Syn3h.com and could be ignored. So the download is the classic 2.22 release, the same version as the source in `unity/`. Comparing every file with the community install by SHA-256:

- Identical: `Mars Explorer.exe` (the Unity player, compiled 30 November 2009), `mainData`, `level0` to `level2`, all four `sharedassets*.assets`, `unity default resources`, `mono-1-vc.dll`, and the Mono class libraries. All scenes, materials, shaders, textures and meshes are therefore the official ones, and every finding read from the data files stands.
- Different: `Assembly - UnityScript.dll` (243,200 bytes officially, 222,208 in the community copy), both `Assembly - CSharp` assemblies, `lib/UnityEngine.dll` and `lib/UnityDomainLoad.exe`. The official script strings are partly obfuscated; the community copy has them in plain text and changes the update check, the download links and the login page link. The decompiler comparison is below.
- Only in the official install: `Assembly - UnityScript - first pass.dll`, `Ionic.Zlib.dll`, `TerrainControllerData.dll`, `lib/Boo.Lang.dll`, `lib/UnityScript.Lang.dll` and `Uninstall.exe`.

Reference wrapper (2026-11-02): the official files are wrapped with Sikarugir Creator (template `1.0.24`, engine `WS12WineSikarugir11.0_1`) as `~/Applications/Sikarugir/Mars Explorer 2.22.app`, running `C:\Program Files\Mars Explorer\Mars Explorer.exe`. It launches to the menus; if the wrapper reverts `Program Name and Path` to `/nothing.exe` (it did once, straight after creation), set it again with `PlistBuddy` in `Contents/Info.plist`. World downloads (checked 2026-11-02): the original update server `http://dat.marsxplr.com/upd3` still answers from S3 (`v=3.0`, `v2=2.22`, hence the 3.0 message), and Fort Boxy (`map.marsxplr.com` on S3) still downloads. The other listed worlds were hosted on `papawheely.com`, now a parked domain, and old public Dropbox links, which no longer serve files, so they fail in the official build. Fort Boxy from S3 also fails in the Sikarugir wrapper with "World loading error: syntax error", although the S3 file is byte-identical to `reference/whirlds/FortBoxy.utw`. Served from a local HTTP server (`python3 -m http.server 8765` in a folder holding the file) and loaded with Host → Use Custom World → `http://127.0.0.1:8765/FortBoxy.utw`, it loads and plays (2026-11-02). Use that route for new comparisons (the S3 failure is narrowed down below). The community wrapper is kept in `archive/Community Wine Version/` for reference only.

## What the community build changed (2026-11-02)

Both builds were decompiled with ILSpy (`ilspycmd` 8.2, run from a scratch .NET 8 SDK) and compared class by class, by string literals, numeric constants and method signatures, then read where they differed. Unity 2.6.1f3 player, macOS `27.2` (Darwin 27.2.0). The decompiled official code was checked against `unity/` where it mattered.

How the community build was made: they decompiled the official assemblies to C# and recompiled them with a modern C# compiler (pattern matching, `yield` iterators in place of Boo generator classes, string literals un-obfuscated). That is why every file differs and why `Boo.Lang.dll`, `UnityScript.Lang.dll` and the UnityScript "first pass" assembly are gone. `TerrainControllerData` was folded into the main assembly, and `Ionic.Zlib.dll` was replaced by .NET's `System.IO.Compression.GZipStream`.

Changes with no behavioural effect (recompilation noise):

- `lib/UnityEngine.dll`: rebuilt without Boo's `[DuckTyped]` attributes; the method bodies are the same apart from reordered conditionals. The only theoretical difference is in `Mathf.Min`/`Max` when an argument is NaN.
- Both `Assembly - CSharp` assemblies (image effects, water, `MouseLook`, `MeshCombineUtility`): only reordered expressions.
- Vehicle scripts (`Buggy`, `Tank`, `Jet`, `Hovercraft`, `Vehicle`, `VehicleBot`), `CameraVehicle` and the Settings quality code: same logic. Example: the official decompile's `input.y + 0.5f * 100f` and the community's `input.y + 50f` are the same constant.

Real changes, by category:

| Change | Category | Faithful to 2.22? |
|---|---|---|
| Update check moved from `http://dat.marsxplr.com/upd3` to `http://gitea.moe/VIA256/new-marsxplr-web-elements/raw/branch/main/upd3`; version reported as `4.0.2` (official `2.22`), compared with a new `SemVer` class; update text now points to the Discord server and the gitea releases page | Update or link | No. The `4.0.2` number is invented by the community. Also, `SemVer`'s `>`/`<` operators compare each field separately (`2.30 > 3.0` is true), so the update prompt can misfire |
| Login and registration links moved to a `web.archive.org` copy of `marsxplr.com/user/login`; the account check (MD5 with a built-in salt, "Authcode failed") removed | Networking or online service | Online only. No effect on single player |
| Links opened from the lobby go through a JavaScript `window.open` pop-up helper, with HTML stripped and escaped; the Whirld wiki link goes to a `web.archive.org` copy | Update or link | Web player only |
| Chat and server-name `LanguageFilter` reduced to `return str` (the profanity list is removed) | Networking or online service | No, multiplayer only |
| "Cound not join game" fixed to "Could not join game"; `"<<  Stop Hosting Game"` loses a space | Text | Cosmetic |
| Gzip decompression in `WhirldIn` changed from `Ionic.Zlib` to `System.IO.Compression.GZipStream` | World download | No, and broken (see below) |
| Terrain splatmap check removed: the official build refuses a splatmap that is not ARGB32, square and a power of two ("Terrain Splatmap Unusable"); the community build applies it anyway | World download | No; more permissive |
| `.utw` parser rejects definitions shorter than 10 characters as a syntax error (the official build only checks the first character) | World download | Near enough |
| New "QuarryCam Enabled" setting (`PlayerPrefs` `quarryCam`, off by default): the camera follows the quarry vehicle, and the HUD swaps the distance readout. The official build reads a `superCam` preference it never uses | Gameplay (camera, Settings) | No: a new feature. Off by default, so default play is unchanged |
| Laser target prediction (`Rocket`): the wait used to sample the target's velocity and acceleration is `Time.fixedDeltaTime * 0.5` instead of `* 5` (three places) | Gameplay (laser) | No. `unity/…/Rocket.js` lines 27, 35 and 39 and the official build both use `* 5` |
| `Jet.flightAngDrag` field default 0.1 instead of 0.01 (`Jet.js` line 19: `.01`) | Gameplay (jet flight) | No effect: the shipped jet prefab (`sharedassets3.assets`) serialises `flightAngDrag` = 3 (and `hoverAngDrag` = 1), which overrides either default (checked 2026-11-02 with UnityPy) |
| Server settings string serialised with a leading `;` before each key (`;bugspd:` and so on) | Networking | Multiplayer format only |

How they got worlds working: they did not change how worlds are found or parsed. The `.utw` reader, `GetURL` and the world list format (`w=nme:…;url:…`) are as in the official build, and the official build already had gzip support for `.gz` meshes and compressed terrain through `Ionic.Zlib`. The working worlds come from the new update file on gitea: it lists world URLs that still resolve (not checked: gitea refuses automated fetches), in place of the dead `papawheely.com` and Dropbox links in `dat.marsxplr.com/upd3`. Their replacement gzip code is faulty. It sizes the output buffer to the compressed length and makes a single `Read` call. For meshes it then calls `byte[].ToString()`, which yields the text `System.Byte[]`, not the decompressed data. So `.gz` meshes cannot load, and compressed terrain heightmaps are truncated (they fail at end of stream for any useful resolution). Worlds made only of `.utw` primitives, uncompressed meshes and asset bundles are unaffected; Fort Boxy is one of those.

Fort Boxy from S3 in the official build: the error comes from the first-character check in `WhirldIn.Generate`. It means the player received a non-empty body that does not start with `[` or `{`. The file on S3 is correct: it starts with `[marsxplr:camo:1;…`, with `Content-Type: binary/octet-stream`, and it comes back the same with Unity's user agent (`UnityPlayer/2.6.1f3 (http://unity3d.com)`), with HTTP/1.0, and with or without `Pragma`, `Accept`, `Expect` and `Accept-Encoding` headers. Through `map.marsxplr.com` (a CNAME to the bucket) it is also the same. Wine's networking is not involved: `Mars Explorer.exe` has its own statically linked libcurl. One probe sent as `curl -X GET -H "Content-Length: 0"` got S3's `400` XML error (`<?xml…`). That would produce exactly this error, because the 2.6 player does not turn an HTTP error status into `WWW.error`. A repeat without `-X` returned 200, so this is not confirmed. To settle it, capture the player's actual request: run a logging relay on `127.0.0.1` that forwards to the bucket, and load `http://127.0.0.1:<port>/FortBoxy.utw` through it. This could not be done automatically, because the Wine game window cannot be granted to computer-use control. The local-server route stays the working path.

Effect on earlier Wine-only measurements (all made with the community build):

- Visuals, lighting, shaders, particles and trails: unaffected; the content and engine are identical.
- Buggy, tank, hovercraft and jet handling figures (speeds, flight, suspension): unaffected as far as code goes, since the vehicle logic is unchanged. The jet's `flightAngDrag` default change has no effect (the prefab sets 3).
- Laser behaviour against moving targets (homing lead, time to impact, hit rate, bot accuracy): affected. The community build samples the target over a tenth of the original interval, so lead and acceleration estimates are noisier and the shot leaves sooner. Re-measure these in the official wrapper.
- Camera: unaffected unless QuarryCam was switched on.
- Worlds with `.gz` meshes or compressed terrain: cannot have loaded correctly in the community build; do not use them as references.

So the Wine version is good evidence for how the engine draws (lighting, shaders, particles, trails), but not for script behaviour or values: any of those could carry a fix from the decompilation. Script behaviour is taken from `unity/` (see Decisions in [PLAN.md](../PLAN.md)). The ports were written from the source, so a match with Wine still supports them; a difference found only in Wine (for example in the handling figures) should be traced in the source before it is acted on.

- Host a local Fort Boxy game. It runs at about 11–21 FPS.
- For look comparisons, lock Rendering Quality to Fantastic, the visibility cap to maximum, and the Agile chase camera at the maximum chase distance (20). Use the same chase distance in the port.
- Capture the window with `screencapture -l <window id>` on the `wine-preloader` window. Ordinary full-screen captures may leave it out.
- Measure mean brightness of floor-only patches on the open floor south of the castle, from far chase views. First-person and low camera angles are not comparable.
- HUD figures (speed, altitude) are read off screenshots, so timings are accurate to about ±0.5 s.
- Wine does not pass Cmd through as the Windows key, so `Fire3` brakes cannot be tested there.

In the port, `Tools -> MarsXPLR -> Run Buggy Track Test`, `Run Buggy Handling Probe`, `Run Buggy Flight Probe`, `Log Scene Look` and `Log Lighting Over Time` provide the matching runs. The editor only advances frames while it is in front.
