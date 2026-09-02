# MIS Quest System — Roadmap

Single source of truth for what's done and what's next. Design detail lives in
`docs/quest-system-spec.md`; release notes in the package `CHANGELOG.md`.

## Now

- [ ] **Phase 1 — v0.2.0 · Object-model simplification** *(code + docs done; awaiting visual check + `push-it`)*

## Phases

### Phase 1 — v0.2.0 · Object-model simplification
- [x] Unify `ObjectiveCompletion` + `QuestCondition` into one `QuestCondition` tree
- [x] `Objective`: `completeWhen` / `failWhen` (single, optional)
- [x] `Quest`: `unlockedBy` + `unlockMode` + `advancedUnlock`; remove `Quest.FailConditions`
- [x] `QuestStateCondition` takes a `Quest` reference, not a GUID string
- [x] QuestList graph window — prerequisites authored as node-to-node edges;
      opens on double-clicking a `QuestList`; drop-to-add / New Quest
- [x] Quest graph node views + spec + README updated
- [x] Compiles clean via `unity command`; runtime unlock flow smoke-tested
- [ ] Visual check in the Editor (user)
- [ ] `push-it` — `0.2.0` bump + CHANGELOG

### Phase 2 — v0.3.0 · QuestList graph polish
- [ ] External-prerequisite chips, cycle detection, tidy auto-layout
- [ ] Right-click canvas quick-add; multi-select drag

### Phase 3 — v0.4.0 · EditMode test suite
- [ ] Tests asmdef referencing `Runtime`
- [ ] Cover: state machines, stage progression, signal counting, prereqs (All/Any/advanced),
      objective fail → quest fail, time limit, capture/restore round-trip

### Phase 4 — v0.5.0 · Sample scene
- [x] Playable demo in `Assets/QuestSystemDemo/` — "Water from the Well" mini-game
      (tilemap, inventory, crafting, quest log / toast / pause HUD). See `docs/quest-demo-scene.md`.
      Added `QuestLog.OnObjectiveActivated`.
- [ ] Trim it into the package `Samples~/` with a `.meta` and `package.json` `samples` entry

### Phase 5 — v0.6.0 · Dialog System bridge + polish
- [ ] Sample glue: `DialogRunner.OnResponseEvent` → `QuestSignals.Report`
- [ ] Per-condition signal counters (multi-goal objectives: "10 wood AND 5 stone")
- [ ] README / spec / XML-doc pass

### Phase 6 — v1.0.0 · Release
- [ ] Final public API review
- [ ] Publish

## Backlog (unscheduled)

- Blackboard / facts database for richer conditions
- `ObjectiveStateCondition` ("objective X of quest Y is complete")
- Raw-id variant of `QuestStateCondition` (cross-package references)
- Graph validation / orphan-objective tooling
- JSON export beyond the snapshot classes

## Done

- **v0.1.0** (2026-08-31) — package scaffold, data layer, `QuestLog` runtime engine,
  `QuestSignals` bus, save/load snapshots, `QuestLogHost`, Quest graph editor window,
  custom asset icons.
