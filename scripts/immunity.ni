// Immunité — un type abstrait, un tag, un champ, deux blocs.
# declare
- model "Immunité" description "Cellules infectées et réponse immune"
- grid 60 60
- seed 7

- agent Cell lime
- agent Virus crimson mob 1
- agent Debris grey

- abstract Immune < Cell
- agent RestImmune < Immune
- agent ActImmune < Immune with Active
- tag Active

- field IL2 diff 0.3 life 4 dirs 8
- Cell makes IL2 1.0

# model
## activation
- Immune -> ActImmune when IL2 > 1

## infection
- neighbour Virus -> Debris when finds Virus
  ; set IL2 0
- Cell -> Debris when finds Virus >= 3 and finds Cell < 2

# start
- 800 Cell random
- 40 Virus random
- 20 RestImmune random
