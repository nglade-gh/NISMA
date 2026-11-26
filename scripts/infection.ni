// Un processus : la cellule traverse trois états, avec durées et effets.
// « Cell » n'est plus un grain unique : chaque état est un grain, et la durée de
// séjour se compile en prédicat d'âge.
# declare
- model "Infection" description "Cycle d'infection d'une cellule"
- grid 80 80
- seed 11

- agent Cell [Lime]
- agent Virus [Crimson] mob 1
- field IL2 [Orange] diff 0.3 life 4 dirs 8

# process Cell
## Healthy
- moves 0.1
- becomes Infected when finds Virus >= 1

## Infected
- produces IL2 0.4
- stays 4 steps
- becomes Sick

## Sick
- a neighbour Cell becomes Infected when finds Cell >= 1
- dies after 6 steps

# start
- 800 Cell random
- 40 Virus random
