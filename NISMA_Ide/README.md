# NISMA — atelier

Un IDE pour le langage NISMA : éditeur avec coloration syntaxique, compilation par
**NISMA Core**, sortie `.nbd2` téléchargeable. Un seul processus, aucune dépendance
externe, aucun build front-end.

## Lancer

```
cd NISMA
/workspace/dotnet/dotnet run --project NISMA_Ide
```

puis ouvrir <http://127.0.0.1:5080>. Le port se change avec `--urls` :

```
/workspace/dotnet/dotnet run --project NISMA_Ide --urls http://0.0.0.0:5080
```

Rien à installer : `Microsoft.NET.Sdk.Web` est dans le SDK 8, l'interface est du HTML,
du CSS et du JavaScript servis tels quels. Pas de `npm`, pas de bundler, pas d'étape de
compilation côté navigateur.

## Ce que fait l'atelier

- **Coloration syntaxique** lexicale : sections (`#`), blocs (`##`), puces, mots-clés,
  types (mots capitalisés), nombres, chaînes, opérateurs, commentaires `//`.
- **Compilation** par `Ctrl+Entrée` ou le bouton ; le résultat s'affiche en trois onglets :
  résumé chiffré, `.nbd2` complet, problèmes.
- **Erreurs situées** : la ligne fautive est surlignée dans la marge et dans le texte, le
  message est cliquable et place le curseur sur la ligne.
- **Téléchargement** du `.nbd2` par `Ctrl+S` (Blob + lien `download`, donc sans
  restriction à Chrome ni Edge).
- **Exemples** lus dans `NISMA/scripts/*.ni` — une seule source de vérité, le serveur ne
  recopie rien.
- **Persistance** du script courant dans `localStorage` ; un rechargement ne perd rien.
- **À propos** : le titre `NISMA` est un bouton qui ouvre une fenêtre donnant le nom complet
  (*NetBioDyn Instructions for Simulation and Model Authoring*, initiales mises en valeur),
  l'auteur, la version — date et heure de construction plus commit court — et le lien du
  dépôt. Les valeurs viennent de `GET /api/about`, qui lit le commit dans le dépôt git
  contenant le binaire et la date dans le fichier de l'assemblage ; si git est absent, le
  commit est annoncé « inconnu » plutôt qu'inventé.

## Aide et tutoriels

L'onglet **Aide** affiche, dans l'interface, les trois textes de référence du dépôt :
`GUIDE.md` (guide du modélisateur), `LANGAGE.md` (référence du langage) et `GLOSSAIRE.md`
(une entrée par commande, en ordre alphanumérique). Le serveur les lit sur le disque et les
rend en HTML — il n'y a aucune copie dans `wwwroot`, donc l'aide affichée est exactement
celle de l'arbre, et modifier un texte suffit à le voir changer.

Les sections sont **repliables** : chaque titre devient une section fermée, avec son
sommaire à gauche et un bouton « Tout déplier ». Une **recherche** filtre le sommaire sur le
titre *et* sur le texte des sections ; quand il ne reste qu'un résultat, sa section s'ouvre
d'elle-même. La structuration se fait sur le HTML rendu, pas dans le Markdown : les trois
textes restent lisibles tels quels dans un éditeur ou sur GitHub.

Le bouton **Tutoriel**, à côté de la liste des exemples, ouvre une fenêtre à deux colonnes :
les tutoriels à gauche, le texte à droite. Les cinq tutoriels de `NISMA/tutorials/`
apprennent le langage dans l'ordre — premier modèle, champs, types et tags, processus,
modificateurs. Chaque modèle complet porte un bouton **Ouvrir dans l'éditeur** : le script
est chargé dans l'éditeur et la fenêtre se ferme, il ne reste qu'à compiler.

Les deux pages viennent des routes `GET /api/help`, `GET /api/tutorials` et
`GET /api/tutorials/{id}`. Le rendu Markdown est fait par `Markdown.cs`, une centaine de
lignes écrites à la main pour le sous-ensemble réellement employé par la documentation
(titres, listes, tableaux, blocs de code, gras, italique, liens) : pas de dépendance
ajoutée.

## Architecture

```
navigateur                     NISMA_Ide (ASP.NET)              NISMA_Lang + NISMA_Core
┌──────────────────┐   POST    ┌──────────────────┐   appel    ┌────────────────────┐
│ éditeur + rendu  │ ────────► │ /api/compile     │ ─────────► │ Parser → Compiler  │
│ (HTML/CSS/JS)    │ ◄──────── │ {ok, nbd2, ...}  │ ◄───────── │ JsonUtils.SaveNbd  │
└──────────────────┘   JSON    └──────────────────┘            └────────────────────┘
```

| Route | Rôle |
|---|---|
| `GET /` | l'interface |
| `GET /api/health` | sonde de vie |
| `GET /api/examples` | les scripts de `NISMA/scripts` |
| `POST /api/compile` | `{script}` → `{ok, nbd2, notes, summary}` ou `{ok:false, error:{line,message}}` |

Le compilateur n'est **pas** réimplémenté dans le navigateur : l'API, c'est NISMA Core.
C'est le point qui garantit qu'un script compilé ici et un script compilé en ligne de
commande donnent le même fichier, au bit près — et c'est aussi la raison du choix
d'architecture ci-dessous.

### Pourquoi pas Kotlin/JS tout de suite

Kotlin/JS serait le bon choix pour une intégration finale dans Centyllion, qui est déjà
Kotlin/JS. Mais NISMA Core est en C# : un IDE entièrement navigateur demanderait soit une
seconde implémentation du compilateur en Kotlin, soit une compilation de NISMA Core en
WebAssembly. Les deux sont possibles ; aucune ne se justifie avant que la syntaxe soit
stabilisée, parce que deux compilateurs divergent toujours.

L'interface est donc écrite dans un seul fichier `app.js` sans dépendance, volontairement
séparé en trois blocs — surlignage, état, appels — pour pouvoir être porté dans un module
Kotlin/JS sans réécrire la logique. Le jour où Centyllion embarque le compilateur, seul
`fetch('/api/compile')` change de nature : il devient un appel direct.

## Vers une version graphique

Le point d'articulation est déjà là : un script est analysé en un **`Script`** (cf.
`NISMA_Lang/Syntax.cs`), et c'est cet arbre que le compilateur consomme. Une vue
graphique n'a donc pas à produire du texte : elle produit le même arbre.

Chemin prévu, dans l'ordre :

1. **Exposer l'arbre** : `POST /api/parse` → `Script` en JSON (les enregistrements C#
   se sérialisent tels quels, à condition d'annoter les hiérarchies de `Declaration` et
   `Cond` avec `JsonDerivedType`).
2. **Rendre l'arbre** : blocs et cartes à partir de cet objet, coloration partagée avec
   la vue texte.
3. **Éditer les deux sens** : texte → arbre → vue graphique, puis vue graphique → arbre →
   texte. Le format texte reste la source de vérité, et un fichier écrit à la main
   s'ouvre dans la vue graphique sans conversion.

Tant que l'étape 1 n'est pas faite, la vue graphique serait un éditeur de texte déguisé.

## Limites connues

- Le surlignage est **lexical** : il ne sait pas si un nom est déclaré. Un identifiant
  inconnu n'est pas souligné ; c'est le compilateur qui le signale.
- Une seule erreur est rapportée : le compilateur s'arrête à la première, ce qui est
  cohérent avec sa règle « aucun fichier en cas d'erreur ».
- Pas d'autocomplétion ni de navigation dans le code. Utile, mais après stabilisation de
  la grammaire.
- Pas de tests d'interface automatisés : l'API est testée par `curl`, l'interface a été
  vérifiée dans un navigateur réel (rendu, compilation, erreur située, onglets).
