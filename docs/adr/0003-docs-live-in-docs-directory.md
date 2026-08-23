# ADR-0003: Documentation lives in `docs/`, which is also the Obsidian vault

**Status:** Accepted
**Date:** 2026-08-23

## Context

Documentation had accumulated in three places: a design doc at the repo root, a
separate `tube-app/` directory that turned out to be an **Obsidian vault**
holding frontend and domain notes, and nothing at all in the conventional
locations.

The vault presented a genuine tension. Notes written in Obsidian were the most
current thinking in the project, but they sat in a directory whose name collided
with both the repository name and the .NET solution name, and nothing linked
them to the rest of the repo.

Separately, the installed agent skills expect documentation in specific places:
`CONTEXT.md` at the repo root as a glossary, `docs/adr/` for decisions, and
`docs/agents/` for their own configuration.

## Decision

**All documentation lives in `docs/`, and the Obsidian vault points there.**

- `docs/` — long-form design docs
- `docs/adr/` — architecture decision records
- `docs/agents/` — configuration for AI agents working in this repo
- `docs/research/` — output from research spikes
- `CONTEXT.md` at the repo root — domain glossary, created when terms are
  actually resolved
- `.scratch/` — ephemeral per-feature working files, gitignored

`.obsidian/` is gitignored: the notes are tracked, the editor's settings are not.

## Alternatives considered

**Move the vault outside the repository.** Rejected — it means maintaining two
homes for overlapping content, and guarantees the notes and the repo drift apart.

**Keep both, and sync by hand.** Rejected for the same reason, more so.

## Consequences

- One copy of every document, editable in Obsidian, visible on GitHub, and
  readable by agents. This is the point of the decision.
- Obsidian `[[wikilinks]]` do not render on GitHub. Prefer standard Markdown
  links for anything meant to be read there.
- Documents written as personal notes are now public-facing, which warrants a
  little more care in how they're written.
- `CONTEXT.md` was the notable gap at the time; it now exists, seeded with
  `NaptanId`/ATCO Code (resolved alongside #1/#7, see ADR-0004). The domain
  still has real ambiguity — *StopPoint* vs *Station*, *Vehicle* vs *Train* —
  unresolved terms should be added when they're deliberately settled, not
  guessed at.
