# ADR-0004: NaptanId preserves case rather than normalizing it

**Status:** Accepted
**Date:** 2026-08-23

## Context

`NaptanId` wraps a fixed-length identifier in ATCO Code format (see
`CONTEXT.md`). Its previous representation was a heap-allocated `byte[]`,
which was also the cause of ADR-0001's noted bug: `GetHashCode` hashed the
array reference, not its contents, so equal ids hashed differently (fixed in
#1).

Fixing that reference-hash bug required deciding how `NaptanId` should be
represented internally, and the ATCO Code spec's suffix alphabet
(`A-Za-z0-9`) opened a real fork: pack the identifier into a single integer,
or keep a byte-per-character layout but move it off the heap.

Packing was genuinely on the table. The ATCO Code's fixed 12-character form
is base-36-representable (10 digits + 26 letters) — 36¹² ≈ 4.74×10¹⁸, which
fits under `ulong.MaxValue` ≈ 1.84×10¹⁹ with room to spare — so `NaptanId`
could collapse to a single `ulong`: no allocation, equality as one integer
comparison, hashing near-free.

## Decision

**`NaptanId` preserves case** rather than normalizing to uppercase, and is
represented as a 12-byte buffer stored inline in the struct
(`[InlineArray(12)]`) rather than a packed integer.

## Alternatives considered

**Base-36 packing into a single `ulong`, normalizing to uppercase on
parse.** Rejected. Base-36 packing only works if the alphabet is
case-insensitive — 26 letters, not 52 — and the ATCO Code spec permits mixed
case, even though every id observed from TfL so far is uppercase-only.
Base-62 (case-sensitive) does not fit a 12-character id in a `ulong`.
Normalizing case would have made `NaptanId` case-insensitive by
construction, silently discarding information the spec allows a producer to
send. The performance win was real, but not worth encoding an assumption
about the data that isn't actually guaranteed.

## Consequences

- `NaptanId` is heavier than the packed alternative would have been (12
  bytes inline vs. 8), but carries no allocation and no assumption about
  case that the spec doesn't make.
- The inline buffer has no null state, which incidentally fixed
  `default(NaptanId)` throwing `NullReferenceException` (#7) — a value-type
  buffer is always valid, unlike the `byte[]` it replaced.
- The padding scheme inherited from the old representation still can't
  distinguish a real leading `'0'` from padding, so `FromString("0123")` and
  `FromString("123")` currently collide (#6). Untouched by this decision —
  it's a padding-scheme problem, not a storage-container one — but any
  future revisit of the representation should fix it at the same time.
- If a future profiling result shows `NaptanId`'s size or allocation
  behaviour actually matters, base-36 packing is available as a follow-up —
  but it would need the case-sensitivity question reopened first, not
  assumed away.
