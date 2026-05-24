# Transmission Routes

## Status

Design reference

## Summary

Diseases should define their own transmission routes instead of sharing one generic residue mechanic. In v1, Pathogenics only implements the shared-air respiratory route.

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

## v1 route

Version 1.0 implements only:

```text
Airborne / droplet-style proximity exposure
```

This is enough for the custom coronavirus disease and conservative vanilla flu integration.

## Excluded routes for v1

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

This would be needed for malaria-style diseases, but it is out of scope for v1.

## Future design goal

Each disease should specify:

- allowed routes
- route weights
- incubation range
- infectious period
- exposure threshold
- decay rules
- immunity behavior
