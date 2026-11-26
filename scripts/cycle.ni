// Un rythme : la cellule ne s'active que tous les 3 pas, et reste active 2 pas.
# declare
- model "Cycle" description "Rythme régulier dans un état"
- grid 40 40
- seed 5
- agent Cell [Lime]

# process Cell
## Quiescent
- becomes Active every 3 steps

## Active
- stays 2 steps
- becomes Quiescent

# start
- 100 Cell random
