// Jeu de la vie — deux comportements, aucune structure.
# declare
- model "Jeu de la vie" description "Automate cellulaire de Conway"
- grid 50 50
- seed 42
- agent Cell lime walls

# model
## growth
- Birth: Cell spawns when finds 3 Cell
- Death: Cell dies when finds Cell < 2 or finds Cell > 3

# start
- 178 Cell random
