# Performance Notes

## Status

Planned

## Goal

Keep respiratory transmission simulation lightweight.

## Main risk

The expensive part is checking nearby pawns around every infectious pawn.

Do not run the full simulation every tick.

## Recommended interval

Run respiratory exposure every fixed interval, for example:

```text
250 ticks
```

This is frequent enough to simulate exposure but avoids constant scanning.

## Filtering order

Use cheap checks before expensive checks.

Recommended order:

1. Is simulation enabled?
2. Is pawn spawned and humanlike?
3. Is pawn infectious?
4. Are there nearby pawns within radius?
5. Is target valid?
6. Is target already infected or immune?
7. Is same room or outdoors?
8. Calculate exposure.

## Radius

Start with a modest radius:

```text
5 to 6 tiles
```

Avoid scanning very large areas until a room-based model exists.

## Future optimization

If needed, use room-based transmission instead of pawn-to-pawn radius checks.

Room-based approach:

```text
infectious pawn contributes airborne load to room
pawns in room inhale from room load
room load decays over time
```

That may scale better and feel more realistic, but it is more complex.
