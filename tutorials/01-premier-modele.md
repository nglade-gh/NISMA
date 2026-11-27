# 1. Premier modèle : une grille, un agent, deux règles

Objectif : écrire un modèle complet, le compiler, et lire ce qu'il produit. Nous
reprendrons le jeu de la vie de Conway, qui tient en deux règles.

## Un script se lit de haut en bas

Un script NISMA est fait de sections, repérées par `#` comme les titres d'un texte :

- `# declare` — ce qui existe : la grille, la graine aléatoire, les agents, les champs ;
- `# model` — ce qui se passe : les comportements ;
- `# start` — l'état initial de la simulation.

Les lignes d'une section commencent par un tiret. Les commentaires commencent par `//`.

## Le script

```ni
# declare
- model "Jeu de la vie" description "Automate cellulaire de Conway"
- grid 50 50
- seed 42
- agent Cell [Lime] walls

# model
## growth
- Birth: Cell spawns when finds 3 Cell
- Death: Cell dies when finds Cell < 2 or finds Cell > 3

# start
- 178 Cell random
```

## Ce que chaque ligne veut dire

`grid 50 50` fixe la taille : cinquante colonnes, cinquante lignes, soit 2500 cases.

`seed 42` fixe le hasard. Deux exécutions de la même graine suivent la même trajectoire —
c'est ce qui rend un résultat reproductible.

`agent Cell [Lime]` déclare une sorte d'agent. La couleur entre crochets est facultative ;
sans elle, le compilateur en tire une dans la palette vive de Centyllion.

`walls` ferme les bords. Sans ce mot, la grille est torique : sortir à droite fait rentrer à
gauche.

`## growth` ouvre un bloc nommé. Il ne crée rien : il donne un nom au groupe de règles, et ce
nom sert de préfixe dans le fichier produit.

`Cell spawns when finds 3 Cell` : une cellule vide devient `Cell` si exactement trois de ses
voisines sont des `Cell`.

`Cell dies when finds Cell < 2 or finds Cell > 3` : une `Cell` meurt si elle est trop seule
ou trop entourée.

`178 Cell random` place cent soixante-dix-huit cellules au hasard dans la grille de départ.

## À toi

1. Colle le script dans l'éditeur, compile avec `Ctrl+Entrée`.
2. Regarde l'onglet **Résumé** : un grain, deux comportements, 2500 cellules.
3. Change `grid 50 50` en `grid 120 80`, recompile, et regarde le nombre de cellules dans le
   résumé.
4. Remplace `178` par `900` : la densité de départ change tout le comportement.

## Erreurs fréquentes

- **`agent` sans nom** : le nom est obligatoire, la couleur ne l'est pas.
- **Une règle sans `when`** : `Cell spawns` sans condition serait une naissance permanente,
  le compilateur vous demandera une condition.
- **Une faute de frappe sur un agent** : `finds 3 Cel` ne désigne aucun type déclaré ;
  l'erreur nomme la ligne et le mot inconnu.

## Ce qu'il faut retenir

- Trois sections : déclarer, modéliser, placer.
- Une règle s'écrit `SUJET verbe [when CONDITION]`.
- `seed` rend l'expérience reproductible : gardez-la pendant que vous réglez un modèle.
