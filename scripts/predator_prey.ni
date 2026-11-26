// Proie-prédateur — aucune couleur écrite : NISMA les tire dans la palette vive,
// en évitant celles déjà prises, exactement comme Centyllion pour un agent neuf.
# declare
- model "Proie-prédateur" description "Herbivores, prédateurs et herbe"
- grid 80 80
- seed 3

- agent Prey
- agent Predator
- field Grass diff 0.2 life 6 dirs 4
- Prey produces Grass 0.4

# model
## reproduction
- Prey spawns when finds Prey >= 2 and Grass > 0.5

## prédation
- neighbour Prey -> Predator when finds Predator >= 3
- Predator dies when finds Prey < 1

# start
- 400 Prey random
- 40 Predator random
