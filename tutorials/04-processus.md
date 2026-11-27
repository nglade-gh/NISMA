# 4. Processus et états : décrire une durée

Une règle dit ce qui se passe à un pas. Un processus dit ce qui dure : une cellule qui reste
infectée quatre pas, un cycle d'activation, une rotation. C'est la partie de NISMA qui
décrit le temps du phénomène, et non celui de la boucle.

## Le script

```ni
# declare
- model "Cycle" description "Un rythme régulier dans un état"
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
```

## Un processus est une machine à états

`# process Cell` ouvre un processus pour le type `Cell`. Chaque `##` nomme un **état** :
`Quiescent`, `Active`. Une cellule se trouve toujours dans l'un des états du processus.

`- stays 2 steps` : dans cet état, elle reste deux pas, puis elle en sort. La sortie est
écrite par la règle suivante, `becomes Quiescent` — sans elle, la compilation refuse : une
durée de séjour qui ne mène nulle part n'est pas un processus.

`- becomes Active every 3 steps` : le passage est périodique. `every 3 steps` devient
`step % 3 == 0` dans le fichier produit : c'est le rythme de la simulation, pas un compteur
propre à la cellule.

## Ce qui se compile

Un état n'est pas un objet du fichier produit. Le compilateur le traduit en conditions sur
les grandeurs de la cellule — l'âge notamment. `stays 2 steps` puis `becomes Quiescent`
devient une garde du type `age >= 2` sur la transition, et l'entrée dans un état remet le
compteur à zéro. Vous décrivez une durée, il produit une condition : le `.nbd2` reste un
modèle NetBioDyn ordinaire.

## À toi

1. Compile : le résumé doit annoncer deux comportements, un par transition.
2. Passe `stays 2 steps` à `stays 6 steps` — les cellules restent actives six fois plus
   longtemps.
3. Remplace `every 3 steps` par `every 1 steps` : l'activation devient systématique, le
   cycle disparaît.
4. Supprime la ligne `becomes Quiescent` et compile : c'est une erreur, et le message dit
   pourquoi. Une durée sans sortie n'est pas une description complète.

## Erreurs fréquentes

- **Un état sans transition** : il ne se quitte jamais ; le compilateur le signale.
- **`stays` sans `becomes`** : même chose, la durée n'a pas de conséquence.
- **Deux processus pour un même type** : un seul processus par type, avec autant d'états que
  nécessaire.

## Ce qu'il faut retenir

- `# process TYPE` puis `## État` pour chaque état.
- `stays N steps` donne une durée, `becomes X` donne la sortie — les deux vont ensemble.
- `every N steps` traduit une périodicité en condition sur le pas courant.
