# Unity runtime review

Unity 2022.3.40f1, Windows, Python 3, and an MSVC-built sibling BnBServer checkout are required.
Use fresh output directories on a drive with enough space for Assets, Library and Windows players.
Do not use the production server or account directory.

From BNB:

```powershell
python tools/unity_review/build.py --editor D:/Unity/Hub/Editor/2022.3.40f1/Editor/Unity.exe --out D:/SlapSquadValidation/review
```

This copies Assets, Packages and ProjectSettings, injects the two review scripts **only into that copy**,
and directs Unity temporary files and the package cache to the output directory. The audit examines all
enabled build scenes, prefabs under Assets/Prefab, and prefab dependencies of the build scenes
(including characters under Assets/FBX). It also checks readable meshes on character controller roots,
because CharacterSystem adds Outline at runtime. A broken serialized object reference, missing
script, unreadable outline mesh or failed Windows player build returns a nonzero exit code. An intentionally unassigned
reference is not classified as a missing asset. Review scripts under tools are not shipped by the
ordinary game build.

From BnBServer:

```powershell
python tools/benchmark/build.py --out D:/SlapSquadValidation/server-review --port 9000
```

From BNB, for each row below, use a new `--out` directory:

```powershell
python tools/unity_review/run_players.py --server-repo ../BnBServer --server-build D:/SlapSquadValidation/server-review --player D:/SlapSquadValidation/review/build/player/SlapSquadReview.exe --out D:/SlapSquadValidation/map1 --map Map1 --weapons 0,1,2,3 --balloons 0,1,2,3
```

| Map | Weapons | Balloons |
| --- | --- | --- |
| Map1 | 0,1,2,3 | 0,1,2,3 |
| Map2 | 4,5,6,0 | 4,5,6,7 |
| Map1 | 0,1,2,3 | 8,9,0,1 |

The runner checks the server build manifest for the loopback adapter and port 9000, rejects an occupied
port, creates four synthetic accounts, copies only map data from the repository, and starts the server
with that fixture as its working directory. It terminates only processes that it created. The copied
player uses a validation company/product name so the user's game PlayerPrefs are not overwritten.
Raw local logs can contain paths, machine details and game traffic; publish only reviewed summaries.

The agent invokes real LoginUI/RoomUI button events and controller methods, then observes production
TCP receivers, scene transitions, prefabs, PhysX and presentation state. It replaces physical keyboard
input with scripted calls after startup. It does not inject authoritative results, change game scripts,
or acknowledge damage to the server. The waits for local balloon cooldown and water presentation are
separate: another player's balloon and an earlier damage packet must not satisfy those conditions.

Outputs are `build/review.json`, `client-0.json` through `client-3.json`, and `summary.json`. A runtime
pass requires all four agents to complete, no captured Unity Error/Exception/Assert, and exit code 0.
Review the complete checks, not just the number. The watchdog and runner also impose deadlines.

These are actual **headless Windows player tests**, not .NET doubles and not Editor PlayMode tests.
Null graphics produces shader/video-decoder notices; rendering, sound quality, animation timing,
human controls, WAN latency and large-room load are outside this test's coverage. Index coverage does
not prove every weapon/map pairing or collision edge case. Full visual acceptance remains separate.
