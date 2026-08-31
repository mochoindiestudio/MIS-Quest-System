# Unity CLI — working notes for this project

Reference compiled 2026-08-31 from https://docs.unity.com/en-us/unity-cli (experimental package).
This project uses the **Unity CLI** to drive the Editor instead of an MCP server connection.

## Environment status (verified 2026-08-31)

Everything needed is already installed and working:

| Thing | Status |
|---|---|
| `unity` CLI | **1.0.0-beta.5**, installed at `C:\Users\crist\AppData\Local\Unity\bin\unity` |
| PATH | Bash tool: prepend `export PATH="$PATH:/c/Users/crist/AppData/Local/Unity/bin"`. PowerShell picks it up in new shells. |
| Auth | Signed in as Cristian Mocho (cristiandmocho@gmail.com) |
| Editor | `6000.3.19f1` installed (also `6000.5.7f1`) |
| Pipeline package | `com.unity.pipeline` **0.5.0-exp.1** already in `Packages/manifest.json` |
| Pipeline server | Running on port **7800**, reachable, state `ready` |
| Editor process | Unity is open on this project (PID varies) |
| Claude Code skill | `unity-cli` skill installed at `C:\Users\crist\.claude\skills\unity-cli` (via `unity skill install claude-code`) |
| Tool count | **143** built-in Editor commands exposed via `unity command` |

There is also an older `unity-mcp-skill` in `~/.claude/skills/` — ignore it; we are using the CLI path.

## How it works

The `com.unity.pipeline` package runs a local HTTP API (port 7800) inside the running Editor.
`unity command` / `unity eval` forward requests to it. Entirely local — same privileges as your
own terminal, localhost only, no Unity AI subscription.

**Requires Unity 6.0+.** If the project has C# compile errors the Editor boots into **Safe Mode**,
the Pipeline package does not load, and `unity command` / `unity status` / `unity list` cannot
connect. In that case: run `unity pipeline list` to confirm Safe Mode, then fix the compile
errors in source and restart Unity (this is the correct move, not a fallback to blind YAML edits).

## Core workflow

```bash
export PATH="$PATH:/c/Users/crist/AppData/Local/Unity/bin"   # Bash tool only
cd "E:/Mocho Indie Studio/MIS Quest System"

unity status                         # confirm connected Editor — look for State "ready"
unity list                           # all 143 commands with descriptions (Name/Group/Description)
unity command                        # same list, richer (--query, --tag, --group_by, --detail)
unity command <name> [args]          # run one command
unity command <name> --help          # not supported per-tool; use `unity command --query <name>` or `unity list`
```

### Reading state (safe, read-only)

```bash
unity command editor_status
unity command get_scene_hierarchy
unity command get_authoring_root          # -> {"root":"Assets"}  (bare paths resolve under here)
unity command find_gameobjects
unity command get_component_properties
unity command console                     # captured Editor console output
```

### Running C#

```bash
# Roslyn eval against the live Editor. Pass code via the `code` param / positional.
unity command eval 'return UnityEngine.Application.unityVersion;'
unity command eval 'new UnityEngine.GameObject("Joe");'
unity command eval_file path/to/snippet.cs
```

Result comes back as JSON in the `Result` column (use `--format json` / `--json` to parse):
`{"success":true,"result":"6000.3.19f1","diagnostics":[],...}`

### Authoring (mutates the project — Editor keeps in-memory state in sync)

Representative commands (run `unity list` for the full set):
- GameObjects: `create_gameobject`, `create_gameobjects`, `delete_gameobject`, `add_component`
- Scenes: `create_scene`, `add_scene_to_build`, `save_scene` *(verify exact name via `unity list`)*
- Assets: `create_script`, `create_asset`, `create_folder`, `copy_asset`, `delete_asset` (needs `confirm=true`), `import_asset`, `find_assets`
- Prefabs: `create_prefab`, `create_prefab_variant`, `instantiate_prefab`, `apply_prefab_overrides`
- Scripts: `create_script` → then `recompile`, poll `recompile_status`, then `attach_script`
  (a newly created script's type does not exist until a recompile completes)
- Play mode: `editor_play`, `editor_pause`, `editor_stop`, `editor_focus`
- Also: animator/timeline/animation-clip commands, lighting/navmesh/occlusion bakes (async + `*_status` poll), `build` + `build_status`, `audit` + `audit_status`, `capture_game_view`, `capture_scene_view`

Destructive commands (`delete_asset`, `clear_baked_lighting`, `clear_navmesh`, ...) require `confirm=true`.

**Rule: while `unity status` shows a reachable Editor, never hand-edit `.unity` / `.prefab` /
`.asset` / `.meta` YAML.** Raw edits are error-prone (fileIDs/GUIDs), invisible to the running
Editor until reimport, and easily hit the wrong file. Only edit files directly when `unity status`
shows NO reachable Editor — and say so explicitly.

## Passing arguments to commands

`unity command <name> key=value key2=value2` for simple args. For structured/JSON payloads prefer
`--format json` and check `unity command --query <name>` or `unity list` output for the parameter
schema. Example patterns seen in tool docs: `confirm=true`, `count=5`, `positions=[[0,0,0],...]`.
When unsure of a command's params, inspect: `unity command --query <name> --detail full`.

## CLI-level commands (no Editor needed)

```bash
unity --version / unity --help / unity <cmd> --help
unity auth login | status | logout | list | switch <acct>
unity editors -i        # installed          unity editors -r   # available releases
unity editors running   # running Editor instances
unity install lts | 6000.3.19f1 [-m android ios webgl]
unity install-modules -e 6000.3.19f1 -m android
unity open ./            # open project (or: unity ./)
unity pipeline install | list | upgrade
unity build              # batch build (Unity 6+: no --execute-method needed; --profile <name>)
unity run [--command <name>]
unity test [--mode EditMode|PlayMode] [--report-format junit] [--output results.xml]
unity shell              # interactive session w/ history + completion
unity doctor / unity env / unity logs --follow
unity skill install --list | unity skill install claude-code | unity skill refresh
unity mcp / unity mcp configure <claude|cursor|vscode|windsurf>   # only if you want MCP mode instead
```

## Global flags & env

- `--format human|json|tsv|ndjson`, `--json`, `--no-banner`, `--non-interactive`, `--quiet`, `--verbose`
- Env: `UNITY_FORMAT`, `UNITY_PROJECT_PATH`, `UNITY_NON_INTERACTIVE`, `UNITY_NO_BANNER`,
  `UNITY_SERVICE_ACCOUNT_ID` / `UNITY_SERVICE_ACCOUNT_SECRET` (CI auth)
- **Read failures from stdout, not stderr**: under `--format json` branch on `success` and
  `errors[0].code`; `data` is usually `null` on failure.

## Exit codes

`0` ok · `1` general · `2` bad args · `3` auth · `4` precondition (no license etc.) · `6`
command-specific failure (build/test failed) · `7` Unity service unreachable (retry) · `130`
SIGINT · `143` SIGTERM

## Tests via CLI

```bash
unity test "E:/Mocho Indie Studio/MIS Quest System" --mode EditMode \
  --report-format junit --output ./test-results.xml --timeout 600
# exit 0 = pass, 6 = test failures
```
Single test: `unity test --help` for the current filter flag (historically `-testFilter <fqn>`).
PlayMode/EditMode tests still need their own asmdef referencing the code-under-test.

## Docs index

- Entry: https://docs.unity.com/en-us/unity-cli
- Replace MCP server: https://docs.unity.com/en-us/unity-cli/replace-mcp-server-unity-cli
- Use the CLI: https://docs.unity.com/en-us/unity-cli/use-unity-cli
- Reference: https://docs.unity.com/en-us/unity-cli/unity-cli-reference
- Release notes: https://docs.unity.com/en-us/unity-cli/release-notes
- Pipeline package: https://docs.unity.com/en-us/unity-production-pipeline/local-tools-cli/unity-pipeline-package
- Local skill (authoritative, offline): `C:\Users\crist\.claude\skills\unity-cli\SKILL.md` + `references/`
