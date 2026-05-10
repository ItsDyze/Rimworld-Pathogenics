# Pathogenics Documentation

This folder contains design notes, roadmap items, and technical documentation for the mod.

The project is pivoting away from the original generic floor-residue implementation toward a more realistic disease simulation model.

## Current direction

Version 0.2 focuses on one standalone respiratory disease rather than trying to cover every vanilla infection with the same residue mechanic.

The goal is to build one complete disease loop first:

```text
Outsider arrives infected or incubating
        ↓
Pawn may become infectious before symptoms
        ↓
Nearby pawns accumulate exposure through shared air
        ↓
Exposure can trigger incubation
        ↓
Symptoms appear later as a visible disease
        ↓
Isolation and distance reduce spread
```

## Folder structure

```text
Documentation/
├─ Roadmap/
│  ├─ Planned/
│  └─ Done/
├─ Design/
├─ Technical/
└─ Templates/
```

## Documentation rules

- Keep each feature in its own `.md` file.
- Move roadmap files from `Planned` to `Done` when implemented.
- Keep design documents focused on decisions and rationale.
- Keep technical documents focused on architecture, save data, hooks, and implementation constraints.
