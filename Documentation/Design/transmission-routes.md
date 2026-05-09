# Transmission Routes

## Status

Design reference

## Summary

Diseases should eventually define their own transmission routes instead of sharing one generic residue mechanic.

## Possible routes

```text
Airborne
Droplet
Surface
BodilyFluid
Vector
FoodWater
MedicalProcedure
AnimalContact
CorpseContact
```

## v0.2 route

Version 0.2 should implement only:

```text
Airborne / droplet-style proximity exposure
```

This is enough for a standalone flu-like respiratory disease.

## Excluded routes for v0.2

Surface contamination:

```text
Infected pawn uses object
Object becomes contaminated
Another pawn uses object
Exposure increases
```

This is realistic, but should be added after the respiratory core works.

Floor residue:

```text
Pathogen appears as filth on the floor
Pawn steps on it
Exposure increases
```

This should not be a primary route for respiratory disease.

Vector transmission:

```text
Infected pawn or animal
Vector acquires pathogen
Vector infects another pawn later
```

This would be needed for malaria-style diseases, but it is out of scope for v0.2.

## Future design goal

Each disease should specify:

- allowed routes
- route weights
- incubation range
- infectious period
- exposure threshold
- decay rules
- immunity behavior
