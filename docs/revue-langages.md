# Langages et NISMA — revue et analyse

But : passer en revue les familles de langages, y compris exotiques, et décider ce que
NISMA doit emprunter, ce qu'il doit refuser, et ce qu'il ne **peut pas** se permettre
tant que sa cible est un fichier `.nbd2`.

Ce document n'est pas un catalogue. Chaque famille est jugée sur quatre axes, et se
termine par un verdict.

---

## 0. Les quatre axes, et pourquoi ils décident tout

| Axe | Question | Pourquoi c'est décisif |
|---|---|---|
| **Temps** | le langage sait-il dire *quand* et *dans quel ordre* ? | c'est la demande nouvelle : « inscrire la simulation dans un process temporel, schedulé » |
| **Verbosité** | combien de mots pour une règle ? | « langage naturel faiblement verbeux » est une contrainte de conception, pas un luxe |
| **Polymorphisme** | peut-on écrire une règle sur un type et l'appliquer à ses sous-types ? | c'est le point qui a motivé NISMA depuis le début |
| **Cible** | ce que le langage exprime est-il encodable dans `.nbd2` ? | un langage plus expressif que sa cible produit des promesses que le moteur ne tient pas |

Le quatrième axe est un couperet. NetBioDyn fait avancer une grille par pas, en parallèle
ou en séquence, avec des comportements déclenchés par des prédicats sur la cellule
courante. **Tout ce qui n'est pas réductible à « prédicat + probabilité + transformation »
n'existe pas** dans le fichier produit. Un langage qui promet davantage mentirait.

### Trois temps qu'il faut distinguer

La confusion la plus fréquente dans ce genre de projet est de parler de « temps » comme
d'une seule chose. Il y en a trois, et ils ne se compilent pas de la même façon.

1. **Le temps de pas** (logique, discret) : `step`, `age`, « tous les 10 pas ». C'est le
   temps de NetBioDyn. Il se compile directement en prédicats (`step % 10 == 0`,
   `age > 3`).
2. **Le temps événementiel** (next-event) : une file d'échéances, un saut d'un événement
   au suivant, des durées réelles. C'est le temps de Simula, GPSS, DEVS, SimPy.
   **Il n'est pas représentable dans `.nbd2`** : le moteur ne saute pas de date en date,
   il balaie la grille à chaque pas.
3. **Le temps continu** (équations différentielles, événements de traversée de seuil) :
   Modelica, Simulink, automates hybrides. NetBioDyn en a une version pauvre : les champs
   diffusent, avec une demi-vie par pas, mais sans intégration à pas variable ni détection
   d'événement.

Conséquence : NISMA peut offrir **une syntaxe de haut niveau pour le temps**, mais il ne
peut pas offrir **une sémantique de haut niveau pour le temps** tant que la cible est
`.nbd2`. Les deux ne sont pas contradictoires — à condition de le dire et de compiler
honnêtement (cf. §20.10).

---

## 1. Simulation à événements discrets — la lignée fondatrice

**Idée** : le temps est une file d'événements datés ; on saute d'échéance en échéance.

**Représentants** : SIMULA 67 (`process`, `hold`, `passivate`, `reactivate`, ensemble de
séquencement), GPSS (transactions, blocs, facilities), SIMSCRIPT, SLAM, SIMAN, Arena,
AnyLogic (multi-méthode), SimPy (générateurs Python + `yield env.timeout(5)`), SimJulia,
DEVS (Zeigler : `sigma`, `delta_int`, `delta_ext`, `ta`), MS4 Me, Ptolemy II.

**Ce qu'il faut prendre**

- **Simula** : la triade `hold(d)` / `passivate` / `activate` est la plus petite API
  temporelle complète qui existe. Un agent qui « dort 5 pas » puis reprend, c'est
  exactement ce qu'un modélisateur veut écrire. Et Simula montre que cela tient en trois
  primitives, pas en un langage entier.
- **DEVS** : la séparation entre *état*, *durée avant l'événement interne* (`sigma`) et
  *réaction aux événements externes*. C'est le cadre le plus propre pour penser
  « combien de temps je reste dans cet état ».
- **Ptolemy II** : l'idée qu'un modèle est un **graphe d'acteurs** et que le **directeur**
  est un objet nommé et remplaçable (synchrone, événementiel, continu, Kahn). C'est
  l'antidote au flou : « qui décide de l'ordre ? » a une réponse explicite.
- **SimPy** : la preuve qu'on peut exprimer des processus concurrents avec des générateurs
  et un `yield`, donc avec très peu de syntaxe. Modèle mental : chaque agent est un
  petit programme séquentiel qui attend.

**À éviter** : la file d'échéances globale. Elle impose un moteur événementiel et rend le
résultat dépendant de l'ordre d'insertion — l'inverse de la reproductibilité qu'on veut.

**Verdict** : ★★★★★ pour la **sémantique** (durées d'état, directeur explicite),
★☆☆☆☆ pour l'**implémentation** dans la cible actuelle.

---

## 2. Systèmes de production à règles — l'ordonnancement oublié

**Idée** : un ensemble de règles, un ensemble de faits, un **agenda** ; à chaque cycle on
calcule le *conflit set*, on le résout par *salience*, on exécute, et la *réfraction*
empêche une règle de se réappliquer sur les mêmes faits.

**Représentants** : OPS5, CLIPS, Jess, Drools (RETE/PHREAK), Soar, production systems des
années 1980.

**Ce qu'il faut prendre**

- L'idée de **salience** : quand deux règles s'appliquent à la même cellule, laquelle
  gagne ? Aujourd'hui NetBioDyn répond « les deux, dans l'ordre du fichier ». Une
  priorité déclarative serait une amélioration réelle, et elle se compile en ordonnant les
  comportements.
- La **réfraction** : ne pas réappliquer indéfiniment la même règle sur le même état.
  C'est le problème de la boucle `A -> B` puis `B -> A`.
- Le mot **agenda** : nommer la liste des règles applicables à un pas est une notion que
  le modélisateur comprend immédiatement.

**À éviter** : le moteur RETE complet, le chaînage avant général, les faits typés à la
OPS5. On n'a pas besoin d'un système expert ; on a besoin d'une priorité et d'un ordre.

**Verdict** : ★★★★☆ pour la priorité/agenda, ★☆☆☆☆ pour le moteur.

---

## 3. Langages synchrones — le temps logique déterministe

**Idée** : le temps est une suite d'instants logiques ; à chaque instant, toutes les
équations sont résolues simultanément, et l'ordre d'écriture n'a aucune influence.

**Représentants** : Esterel (impératif, `pause`, `await`, `emit`), Lustre (flots,
`pre`, `->`, `when`), Signal (relations d'horloge), SCADE (industrie), ReactiveML
(OCaml), Céu (`await`, `emit`, événements), hiphop.js.

**Ce qu'il faut prendre**

- **L'horloge explicite** : Lustre distingue les flots qui battent à chaque instant de
  ceux qui battent plus rarement, et le *calcul d'horloge* vérifie la cohérence. C'est la
  bonne façon de penser `every 3 steps` : ce n'est pas une condition, c'est **une autre
  horloge**.
- **`pre`** : « la valeur au pas précédent ». NetBioDyn ne l'a pas ; c'est une vraie
  limitation pour exprimer des dynamiques (dérivées, tendances). Une variable mémorisée
  par cellule serait un ajout lourd côté moteur.
- **La simultanéité** : `every` en parallèle, sans ordre — c'est déjà la sémantique
  `parallel` de NetBioDyn, et c'est un point commun solide.

**À éviter** : la causalité synchrone stricte (interdire les cycles d'instant), qui
imposerait une analyse statique dont NISMA n'a pas besoin.

**Verdict** : ★★★★☆ pour les notions d'horloge et d'instant, ★★☆☆☆ pour `pre`.

---

## 4. Dataflow et FRP — le temps comme flot

**Idée** : tout est flot de valeurs ; le temps est implicite.

**Représentants** : Lucid, Fran, Yampa (flèches), Elm (pre-0.17), ReactiveX/RxJS,
FrTime, Zélus (synchrone + ODE).

**Ce qu'il faut prendre** : presque rien syntaxiquement, mais **une leçon** : les
opérateurs temporels se composent mieux que les conditions. `every 3 steps` + `for 10
steps` + `until condition` est un petit algèbre, et il vaut mieux le définir une fois que
laisser chaque modélisateur écrire `step % 3 == 0 && step < 30`.

**À éviter** : le style point-free et les opérateurs combinatoires, qui sont exactement
l'anti-verbosité recherchée.

**Verdict** : ★★☆☆☆ (à cause du style), ★★★★☆ pour l'algèbre temporelle.

---

## 5. Hybride et continu — modes et événements

**Représentants** : Modelica (équations, `when`, événements), Simulink/Stateflow
(machines à états, transitions gardées), HyVisual, SpaceEx, Flow*.

**Ce qu'il faut prendre** : **Stateflow**. Un modèle y est un ensemble d'états nommés avec
des transitions gardées et des actions d'entrée/sortie. C'est exactement la structure
qu'un biologiste dessine au tableau : « la cellule est *quiescente*, elle passe en
*activée* si IL2 > 1, elle y reste 5 pas, puis *prolifère* ».

**À éviter** : les équations acausales, la détection d'événements, l'intégration à pas
variable. Rien de tout cela n'a de sens sur une grille à pas fixe.

**Verdict** : ★★★★★ pour la notion d'**état nommé avec durée**, ★☆☆☆☆ pour le reste.

---

## 6. Automates temporisés et stochastiques — la vérification

**Représentants** : UPPAAL (temporisés), PRISM (MDP/CTMC), Modest (automates temporisés
stochastiques), Storm.

**Ce qu'il faut prendre** : la discipline de **nommer les horloges** et de borner les
attentes (`x <= 10` comme invariant). Appliquée à NISMA, cela donne des durées d'état
bornées, donc des modèles qui ne peuvent pas rester bloqués indéfiniment.

**À éviter** : la vérification de propriétés. C'est un autre métier, et le modèle compilé
n'est pas l'objet vérifié (le moteur, la discrétisation et l'aléatoire s'en mêlent).

**Verdict** : ★★★☆☆ pour les invariants de durée, ★☆☆☆☆ pour le model checking.

---

## 7. Réseaux de Petri — jetons, concurrence, temps

**Représentants** : Petri nets, réseaux colorés (CPN Tools), réseaux temporisés et
stochastiques (GSPN), Workflow nets.

**Ce qu'il faut prendre** : la lecture **« jeton = agent, transition = réaction »**, qui
est exactement la sémantique de NetBioDyn, et la notion de **conflit structurel** (deux
transitions qui consomment le même jeton). Cette lecture explique pourquoi deux
comportements qui s'appliquent à la même cellule sont un conflit, pas une faute.

**À éviter** : la syntaxe graphique comme seule syntaxe (illisible en diff), et le
formalisme complet.

**Verdict** : ★★★☆☆ — surtout pédagogique, pour expliquer NISMA.

---

## 8. Réécriture et modélisation de réactions — la parenté directe

**Représentants** : Kappa (graphes de sites, règles `A(x~u), B(y~p) -> A(x~p), B(y~u) @
k`), BioNetGen/BNGL (`begin reaction rules`), BioCham (règles + logique temporelle),
Antimony/SBML/CellML (réseaux d'équations), little b, Chromar (règles sur des agents),
MGS (collections topologiques), GP/GrGen/PORGY (réécriture de graphes), L-systems.

**Ce qu'il faut prendre**

- **Kappa** est le meilleur exemple de **règle locale et compacte** : on décrit le
  voisinage minimal, pas l'état global. NISMA fait déjà cela avec `finds 3 Cell`.
- **Les graphes de sites** : l'idée qu'un agent a des *sites* avec des états, et qu'une
  règle ne parle que des sites qui l'intéressent. C'est une réponse élégante au problème
  « combien de propriétés faut-il donner à un agent ? ». C'est aussi la meilleure piste
  pour la future vue graphique : des nœuds, des sites, des liens.
- **MGS** : les *collections topologiques* et les transformations définies par voisinage.
  MGS calcule l'espace et l'état **ensemble** — exactement ce que fait une grille
  NetBioDyn. C'est la référence la plus proche de NISMA dans toute la littérature, et
  c'est aussi le meilleur modèle pour une représentation graphique : une collection, un
  voisinage, une règle de remplacement.
- **BioCham** : des règles + des propriétés en logique temporelle sur les trajectoires.
  La bonne façon de dire « il ne peut pas y avoir plus de 10 % de cellules infectées ».

**À éviter** : les taux continus (Kappa est stochastique à taux, NetBioDyn est à
probabilité par pas), et l'expressivité des graphes de sites complets (nœuds, sites,
liens, motifs imbriqués) — c'est un autre projet.

**Verdict** : ★★★★★, la famille la plus proche. MGS et Kappa sont les deux sources à
lire en priorité.

---

## 9. Plateformes multi-agents — l'ordonnancement déclaré

**Représentants** : NetLogo (`ask`, `tick`, `every`), StarLogo TNG, GAMA/GAML (`species`,
`reflex`, `aspect`, `experiment`, `tick`), Repast (`@ScheduledMethod(start, interval)`,
`Schedule`), MASON (`schedule.step`), Mesa (Python, `agent.step()` piloté par un
`Model`), AgentScript, FLAME GPU.

**Ce qu'il faut prendre**

- **Repast** : la planification **par annotation sur la méthode** de l'agent. C'est
  l'idée la plus directement transposable à NISMA : la temporalité d'un agent est une
  propriété de son comportement, écrite à côté de lui, pas dans un tableau global.
- **GAML** : `reflex` (comportement automatique) vs action explicite, et `aspect` pour la
  vue. Deux notions que NISMA devra avoir le jour où il y aura une vue graphique.
- **NetLogo** : `tick` comme point de synchronisation, et la distinction entre
  « demander à tous les agents » et « demander à un agent ». La syntaxe est verbeuse, mais
  la sémantique est claire et enseignée depuis vingt ans.

**À éviter** : le style « boucle impérative sur les agents » (`ask turtles [ ... ]`), qui
introduit un ordre implicite et casse le déterminisme.

**Verdict** : ★★★★☆ pour `@ScheduledMethod` et `reflex`, ★★☆☆☆ pour la syntaxe.

---

## 10. Calculs de processus — la coordination

**Représentants** : CCS, CSP, pi-calcul, ambients, BioAmbients, Brane calculi,
Beta-binders, chorégraphies (Chor, Scribble).

**Ce qu'il faut prendre** : le vocabulaire de la **synchronisation** (rendez-vous,
broadcast, émission conditionnelle) et la notion de **protocole global** (chorégraphie) —
utile si un jour plusieurs simulations doivent coopérer.

**À éviter** : tout le reste. Ce sont des langages de spécification de concurrence, pas de
modélisation de population sur grille.

**Verdict** : ★☆☆☆☆.

---

## 11. Contraintes et logique — décrire plutôt que construire

**Représentants** : Prolog, Datalog, Answer Set Programming (clingo), MiniZinc, Alloy,
SQL, TLA+/TLC, logiques temporelles LTL/CTL.

**Ce qu'il faut prendre**

- **ASP/clingo** : pour l'**état initial**. Aujourd'hui `# start` ne sait dire que « 200
  Cell random » ou « Cell in (5,5) ». Une formulation par contraintes — « 200 cellules, au
  plus 2 par colonne, loin des bords » — est exactement le genre de chose qu'ASP fait
  mieux que n'importe quelle boucle, et qu'on peut résoudre **hors ligne** avant
  d'écrire la grille dans le `.nbd2`.
- **TLA+** : pour spécifier le **système de transitions** lui-même. Utile pour vérifier
  que deux comportements ne se contredisent pas, mais c'est un outil de développeur, pas
  de modélisateur.

**À éviter** : mettre un solveur dans la boucle de simulation (coût), et exposer la
logique du premier ordre au modélisateur (verbosité maximale).

**Verdict** : ★★★☆☆ pour l'état initial par contraintes, ★☆☆☆☆ pour le reste.

---

## 12. Aspects et features — la composition des variantes

**Représentants** : AspectJ (pointcuts, tissage), AHEAD/FOP (couches, step-wise
refinement), delta-modelling, traits Scala, mixins, décorateurs Python, annotations Java.

**Ce qu'il faut prendre** : c'est **exactement** le modèle mental de vos modificateurs.
Un pointcut `cellule de type Active` qui tisse un comportement est un tag qui étend un
comportement. Le tissage à la compilation correspond à votre expansion. Deux
avertissements de la littérature, tous deux vérifiés dans la pratique :

- le tissage implicite rend le programme difficile à lire (« aspect spaghetti ») — d'où
  l'intérêt d'exiger que toute extension soit **nommée** et visible dans le `.nbd2` ;
- la composition de deux aspects peut être incohérente, et il faut une règle d'ordre
  explicite.

**Verdict** : ★★★★☆ comme justification théorique des tags ; rien à copier
syntaxiquement.

---

## 13. Typage par tags — la question du polymorphisme

**Représentants** : OCaml (variants polymorphes `[> `Active ]`), Haskell (classes de
types, dictionnaires), Julia (dispatch multiple), CLOS (méthodes génériques), Dylan,
Kea (predicate dispatch), Scala (traits), C++ (concepts), Clojure (protocols +
metadata).

**Ce qu'il faut prendre**

- **OCaml, variants polymorphes** : c'est littéralement « un tag est un type, et une
  valeur peut en porter plusieurs ». La théorie de l'inférence de tags existe, elle est
  saine, et elle confirme qu'un agent portant `{Active, Infected}` est une construction
  raisonnable, pas un bricolage.
- **Haskell, classes de types** : la résolution **à la compilation**, par dictionnaire.
  C'est votre modèle : on résout les tags en ensembles concrets, puis on étend. Pas de
  dispatch à l'exécution, donc pas de coût et pas de surprise.
- **Julia, dispatch multiple** : le contre-exemple utile. Le dispatch à l'exécution est
  élégant mais il introduit de l'ambiguïté (deux méthodes également applicables) et un
  coût par appel. Julia résout cela par un ordre de spécificité ; vous, vous avez choisi
  l'erreur explicite. C'est le bon choix pour un modèle scientifique reproductible.

**À éviter** : le polymorphisme structurel (sous-typage par enregistrements), les
interfaces implicites, et tout mécanisme dont la résolution dépend de l'ordre de
chargement.

**Verdict** : ★★★★★ — la famille qui valide la conception retenue.

---

## 14. Méta-programmation et staging — l'expansion

**Représentants** : Racket (macros hygiéniques, `#lang`), Scheme, MetaOCaml, Terra,
Nim (macros), D (mixin templates), C++ (templates), Template Haskell.

**Ce qu'il faut prendre** : la **séparation des phases**. Votre expansion
(résolution → extension → émission) est une phase de compilation distincte ; l'appeler
ainsi, et refuser toute dépendance de l'exécution à la phase de compilation, évite la
moitié des bugs de ce genre de système.

**À éviter** : les macros ouvertes. Un langage dont l'utilisateur peut redéfinir la
syntaxe perd la propriété qui fait sa valeur ici : un vocabulaire fermé, donc analysable
et vérifiable.

**Verdict** : ★★★☆☆ (concept), ★☆☆☆☆ (mécanisme).

---

## 15. Langages quasi naturels — la faible verbosité

**Représentants** : Inform 7, Attempto Controlled English (ACE), AppleScript, HyperTalk,
SQL, Gherkin/Cucumber, Wolfram Language, Literate programming.

**Ce qu'il faut prendre**

- **Inform 7** est le seul précédent sérieux d'un langage de programmation en anglais
  courant qui a produit des œuvres réelles. Trois leçons :
  1. la phrase **entière** est l'unité, pas le token : « A cell becomes infected if it
     finds three viruses » ;
  2. les **rulebooks nommés** organisent l'ordre d'exécution (before / instead / after /
     every turn) — c'est de l'ordonnancement exprimé en langage naturel, et c'est
     exactement votre besoin temporel ;
  3. la verbosité tue à la longue : les jeux Inform réels sont pleins de formulations
     figées, parce que la langue naturelle complète est ingérable.
- **ACE** : la preuve qu'on peut rester dans un sous-ensemble **contrôlé** de l'anglais,
  avec une grammaire fermée et une traduction automatique vers la logique. C'est le
  garde-fou : « naturel » ne veut pas dire « libre ».
- **Gherkin** : `Given / When / Then`. Trois mots, une structure, et un modèle mental que
  tout le monde comprend. Pour NISMA, c'est un candidat sérieux pour la section
  `# scenario` : décrire une situation initiale, une règle, un résultat attendu — donc un
  test exécutable écrit par le modélisateur.
- **SQL** : le précédent d'un langage déclaratif à mots-clés anglais, verbeux mais
  enseigné partout. Sa leçon : la **régularité** (une clause par concept, toujours dans le
  même ordre) compte plus que la concision.

**À éviter** : l'analyse du langage naturel réel (anaphores, ellipses, synonymes). Aucun
projet n'en est sorti indemne, et un modèle scientifique ne peut pas dépendre d'une
interprétation.

**Verdict** : ★★★★★ pour les rulebooks et Given/When/Then, ★☆☆☆☆ pour le NL libre.

---

## 16. Exotiques — ce qu'ils enseignent, et ce qu'ils n'enseignent pas

**APL, J, K, BQN** (tacit, dense) : la densité maximale. Une ligne fait un paragraphe.
Enseignement : la densité n'est pas la concision. Un modèle illisible n'est pas un modèle
reproductible. À rejeter pour NISMA ; à garder comme rappel que la brièveté n'est pas un
but.

**Forth, Factor** (concaténatif, pile) : composition sans nommer les arguments. Enseigne
la valeur d'un vocabulaire de mots courts (`finds`, `spawns`, `dies`) ; rejeter la pile.

**Smalltalk, Self** (messages, prototype) : « tout est objet qui répond à des messages ».
C'est l'ancêtre de votre idée de tags : un agent qui répond à `Active` est un agent dont
le comportement est défini par ce qu'il accepte. Intéressant, mais le dispatch dynamique
est précisément ce que vous refusez.

**Prolog / Mercury** (relations, unification) : la règle comme implication. Bonne source
d'inspiration pour `# start` par contraintes.

**Befunge, Piet, Shakespeare, Malbolge, Brainfuck** : à mentionner pour une seule raison —
ils prouvent qu'un langage peut être *complet* et *inutilisable*. Le critère de NISMA
n'est pas la Turing-complétude, c'est la **lisibilité d'un modèle par un biologiste**.

**Wolfram Language** : réécriture de termes, règles `x -> y`, tout est expression. C'est
la démonstration la plus aboutie qu'un langage à base de règles de réécriture peut être
confortable. Mais sa surface est immense, et son implémentation propriétaire.

**Verdict** : ★☆☆☆☆ en général, ★★★☆☆ pour Prolog/Wolfram comme inspiration de
réécriture.

---

## 17. Édition et DSL — la vue graphique de demain

**Représentants** : JetBrains MPS (édition projectionnelle), Xtext, Langium, ANTLR,
tree-sitter, Blockly, Scratch, Snap!, Node-RED, Max/MSP, Pure Data, LabVIEW, Unreal
Blueprints.

**Ce qu'il faut prendre**

- **MPS** : l'édition **projectionnelle** — le programme n'est jamais du texte, il est
  toujours un arbre affiché. C'est la seule façon d'avoir deux vues (texte et blocs)
  strictement équivalentes, sans conversion. Mais c'est une infrastructure lourde.
- **Blockly** : la preuve qu'un éditeur de blocs peut générer du texte **et** être embarqué
  dans une page web avec 300 Ko de JavaScript. C'est le bon point de départ si la vue
  graphique doit vivre dans Centyllion.
- **Max/MSP, Node-RED** : deux précédents de « programmation par câblage » qui ont duré,
  avec la même leçon : le graphe doit rester petit, sinon il devient un plat de spaghettis
  illisible. Une vue graphique doit être une **vue**, pas le seul mode d'édition.

**Verdict** : ★★★★☆ pour Blockly et l'idée de deux vues sur un même arbre.

---

## 18. Configuration déclarative moderne — la concision utile

**Représentants** : HCL/Terraform, CUE, Dhall, Nix, Starlark, Jsonnet.

**Ce qu'il faut prendre** : CUE et Dhall montrent qu'on peut avoir un langage **total**
(pas de boucles infinies), **déterministe** et **concis** en supprimant les constructions
inutiles. NISMA devrait être total : aucune construction du langage ne doit pouvoir
produire une boucle.

**À éviter** : l'évaluation paresseuse et les fonctions d'ordre supérieur, inutiles ici.

**Verdict** : ★★★☆☆.

---

## 19. Tableau comparatif

Notes de 1 à 5. « Coût » = effort d'implémentation **dans NISMA**, pas difficulté du
langage.

| Famille | Verbosité ↓ | Temps | Tags | Coût | Intérêt |
|---|---|---|---|---|---|
| Événementiel (Simula, DEVS, SimPy) | 3 | 5 | 1 | 5 | 4 (sémantique) |
| Production (CLIPS, Drools) | 3 | 4 | 1 | 2 | 3 |
| Synchrone (Lustre, Esterel, Céu) | 4 | 5 | 1 | 3 | 4 |
| Dataflow/FRP | 2 | 4 | 1 | 3 | 2 |
| Hybride (Modelica, Stateflow) | 3 | 5 | 1 | 5 | 3 (états) |
| Automates temporisés | 2 | 5 | 1 | 3 | 2 |
| Petri | 2 | 3 | 1 | 2 | 2 |
| Réécriture (Kappa, MGS, BNGL) | 4 | 2 | 3 | 2 | 5 |
| ABM (GAMA, Repast, NetLogo) | 3 | 4 | 2 | 3 | 4 |
| Calculs de processus | 2 | 3 | 1 | 1 | 1 |
| Contraintes (ASP, Prolog) | 3 | 1 | 1 | 2 | 3 |
| Aspects / features | 3 | 2 | 5 | 2 | 3 |
| Typage par tags | 4 | 1 | 5 | 4 | 5 |
| Staging / macros | 3 | 1 | 2 | 3 | 2 |
| Quasi naturel (Inform 7, Gherkin) | 5 | 4 | 1 | 3 | 5 |
| Exotiques | 1 | 1 | 1 | 1 | 1 |
| Édition (MPS, Blockly) | — | — | — | 4 | 3 |
| Config (CUE, Dhall) | 5 | 1 | 1 | 2 | 3 |

Lecture : les trois familles à fort intérêt et faible coût sont **réécriture**, **typage
par tags** et **quasi naturel**. Les deux à fort intérêt mais coût élevé — **événementiel**
et **hybride** — ne s'implémentent pas dans la cible actuelle, mais doivent informer la
syntaxe dès maintenant.
---

## 20. Dix décisions concrètes pour NISMA

Chaque décision indique **la source**, **la forme proposée**, et **ce qu'elle devient dans
le `.nbd2`** — parce qu'une syntaxe qui ne se compile pas n'est pas une décision, c'est un
souhait.

### 20.1 Un directeur explicite

Source : Ptolemy II. Le modèle nomme qui décide de l'ordre.

```
# declare
- schedule parallel          // défaut
- schedule sequential        // équivalent de update sequential
```

Dans le `.nbd2` : `model.updatePlan`. Un troisième mode (`shuffled`, ordre tiré au sort à
chaque pas) **n'est pas représentable** aujourd'hui : le proposer obligerait à toucher le
moteur. Le refuser explicitement vaut mieux que l'accepter et l'ignorer.

### 20.2 Des états nommés avec durée

Source : Stateflow, DEVS (`sigma`). C'est la réponse à « inscrire la simulation dans un
processus temporel ».

```
# model
## cycle cellulaire
- Cell is Quiescent
- Cell becomes Active when IL2 > 1
- Cell stays Active for 5 steps
- Cell returns to Quiescent
```

Compilation, entièrement dans le format actuel :

| Phrase | `.nbd2` |
|---|---|
| `Cell is Quiescent` | grain `Cell` + grain `Quiescent`… ou, mieux, un tag |
| `becomes Active when IL2 > 1` | comportement `Cell -> Active`, prédicat `field(0) > 1` |
| `stays Active for 5 steps` | comportement `Active -> Quiescent` avec `age >= 5` |
| `returns to Quiescent` | comportement `Active -> Quiescent` |

**La durée se compile en prédicat d'âge.** C'est la découverte de cette revue : le temps
que NetBioDyn sait déjà porter est l'âge, et `age` est exactement un compteur de séjour
dans l'état courant. Il n'y a donc rien à ajouter au moteur pour offrir des durées
d'état — seulement une syntaxe qui les rende lisibles.

Limite : `age` est remis à zéro à chaque transformation. Un agent qui revient dans un état
qu'il a déjà connu repart de zéro. C'est cohérent avec NetBioDyn, et il faut le dire.

### 20.3 Des horloges plutôt que des conditions

Source : Lustre (calcul d'horloge), GAMA (`every`).

```
- Cell acts every 3 steps
- Cell acts after 10 steps
- Cell acts for 20 steps
```

Compilation : `step % 3 == 0`, `step > 10`, `step <= 20` — en formule ou en prédicat
selon la combinaison. Trois mots, aucune arithmétique exposée, et la différence entre
« tous les 3 pas », « à partir du pas 10 » et « pendant 20 pas » est enfin explicite.

### 20.4 Des rulebooks nommés et ordonnés

Source : Inform 7 (before / instead / after / every turn).

```
# model
## infection (before growth)
- neighbour Virus -> Debris when finds Virus

## growth (after infection)
- Cell spawns when finds 3 Cell
```

Compilation : l'ordre des blocs devient l'ordre des comportements dans `behaviours[]`.
Ce n'est pas cosmétique : en mode séquentiel, cet ordre **est** la sémantique. Et il rend
lisible ce qui est aujourd'hui implicite (l'ordre de déclaration).

Une priorité numérique (`- priority 2`) serait plus précise mais moins lisible ; les deux
sont possibles, je recommande de commencer par l'ordre des blocs.

### 20.5 Un scénario exécutable

Source : Gherkin. Le modèle porte ses propres tests.

```
# scenario "l'infection gagne en 20 pas"
- given 50 Cell and 5 Virus
- when 20 steps
- then finds Virus > 20
```

**Attention** : NISMA ne simule pas. Un tel scénario n'est aujourd'hui qu'une
**spécification** inerte. Trois options, à trancher :

1. l'écrire dans le `.nbd2` comme description (utile, mais non vérifié) ;
2. l'exécuter dans Centyllion en mode headless (nécessite un point d'entrée) ;
3. l'exécuter dans un interpréteur NISMA (nécessite un moteur, donc du travail réel).

Je recommande l'option 1 maintenant, l'option 3 seulement si le besoin de scénarios est
confirmé par l'usage. Un scénario qui ne s'exécute pas est une documentation ; une
documentation qui mentit est pire que rien.

### 20.6 Une contrainte de verbosité, mesurée

Source : ACE, Gherkin, et l'échec par verbosité d'Inform 7.

Règle de conception : **une règle = une phrase = au plus 12 mots**, et **un modèle
d'enseignement = au plus 40 lignes**. Ces deux nombres se vérifient automatiquement sur
tous les exemples du dépôt, et deviennent un test de non-régression du langage.

Vérification immédiate sur l'existant : le jeu de la vie fait 14 lignes, l'immunité 31,
proie-prédateur 24. La contrainte est tenue.

### 20.7 Le polymorphisme : suivre OCaml et Haskell, pas Julia

Source : variants polymorphes (OCaml), classes de types (Haskell), dispatch multiple
(Julia, contre-exemple).

- Un tag est un **type**, pas une valeur : il n'apparaît jamais dans le `.nbd2`.
- La résolution se fait **à la compilation**, par ensemble de candidats.
- Candidat multiple sur un **produit** = **erreur**, jamais un choix par défaut. C'est
  déjà implémenté et vérifié (`A, B répondent tous à ce nom`).
- Sur un **réactif**, l'ambiguïté est levée par expansion (tous les candidats), ce qui est
  correct parce qu'une cellule ne porte qu'un grain.

C'est la seule asymétrie du système, et elle est justifiée : un réactif peut désigner
plusieurs cas sans ambiguïté de sens, un produit non.

### 20.8 L'état initial par contraintes

Source : ASP/clingo, Prolog.

```
# start
- 200 Cell where column <= 2
```

Formulation actuelle : impossible. Formulation réaliste : générer la grille **hors ligne**
(avec un petit solveur ou un tirage avec rejet) et l'écrire dans le `.nbd2`. Le langage
peut offrir la syntaxe `where` et un tirage par rejet borné, sans solveur : c'est
suffisant pour « pas sur les bords », « au plus 2 par colonne ».

### 20.9 Une vue graphique qui est une vue

Source : MGS (collections topologiques), Blockly (blocs web), MPS (édition
projectionnelle).

Le pivot doit rester **l'arbre**. Une vue « footprint » est un rendu de cet arbre, pas un
second langage :

- chaque agent devient une carte avec ses propriétés ;
- chaque comportement devient une flèche entre deux cartes, annotée de sa condition ;
- chaque bloc devient un conteneur ;
- le voisinage (`finds`, `neighbour`) devient un pictogramme de grille 3×3 — c'est là que
  MGS est le meilleur guide : un motif spatial se dessine, il ne s'écrit pas.

Aucune vue graphique ne doit être le **seul** moyen d'éditer : Max/MSP et Node-RED
montrent qu'un graphe devient illisible au-delà de quelques dizaines de nœuds, et qu'un
texte reste indispensable pour les grands modèles.

### 20.10 Refuser le temps qu'on ne sait pas tenir

Source : DEVS, SimPy, Lustre — et la contrainte `.nbd2`.

Le langage **ne doit pas** offrir :

- `after 3.5 seconds` (temps réel : le moteur est à pas fixe) ;
- `wait until queue non empty` (événementiel : pas de file) ;
- `Cell remembers last value` (mémoire par cellule : pas de variable d'agent) ;
- `priority` sur autre chose que l'ordre des comportements.

Chacune de ces phrases serait un mensonge à la compilation, et le pire défaut d'un DSL est
de réussir à compiler ce qu'il ne peut pas exécuter.

---

## 21. Les cinq pièges à ne pas répéter

1. **Le langage naturel libre.** Accepto et Inform 7 s'en sortent parce que leur domaine
   est étroit et leur grammaire fermée. Dès qu'on accepte deux formulations pour la même
   règle, on perd l'analyse statique et on double les tests.
2. **L'ordre implicite.** `ask turtles [...]` (NetLogo) ou le parcours d'une collection
   donne un ordre qui n'est pas écrit. Sur un modèle scientifique, cela rend un résultat
   non reproductible sans qu'on sache pourquoi.
3. **Le dispatch à l'exécution.** Confortable, mais il déplace l'ambiguïté du fichier vers
   l'exécution, donc vers l'utilisateur qui ne comprend pas pourquoi son modèle diverge.
4. **La macro ouverte.** Un langage que l'utilisateur peut étendre syntaxiquement n'est
   plus analysable par un outil, donc n'est plus outillable.
5. **La densité confondue avec la concision.** APL prouve qu'on peut tout dire en une
   ligne ; il prouve aussi que personne ne relit cette ligne six mois plus tard.

---

## 22. Le plafond : ce que `.nbd2` ne peut pas porter

Ce tableau est le plus important du document. Il fixe ce que NISMA peut promettre.

| Besoin temporel | Représentable en `.nbd2` ? | Par quoi |
|---|---|---|
| tous les N pas | oui | `step % N == 0` |
| à partir du pas N | oui | `step >= N` |
| pendant N pas | oui | `step <= N` (ou `age < N`) |
| durée de séjour dans un état | oui | `age >= N` |
| ordre entre comportements | oui | ordre de `behaviours[]` + plan séquentiel |
| probabilité par pas | oui | `probability` |
| décalage d'un pas (valeur précédente) | **non** | pas de mémoire par cellule |
| durée réelle (secondes, horloge) | **non** | pas fixe imposé par la boucle |
| saut d'événement en événement | **non** | balayage exhaustif de la grille |
| priorité fine (au-delà de l'ordre) | **non** | pas de notion de conflit |
| attente d'une condition globale | **partiel** | modificateur, évalué une fois par pas |
| état par agent au-delà du grain et de l'âge | **non** | un seul id de grain + un âge |

**La décision à prendre** est donc celle-ci : NISMA est-il

- **(A) une macro-syntaxe au-dessus de `.nbd2`**, avec un plafond assumé et un message
  clair quand une phrase dépasse ce plafond ; ou
- **(B) un langage de simulation autonome**, dont `.nbd2` n'est qu'un des exports, avec
  son propre moteur (et alors la moitié de cette revue redevient pertinente : Simula,
  DEVS, Lustre).

Aujourd'hui NISMA est en (A), et c'est cohérent : il produit des modèles que NetBioDyn
exécute. Passer en (B) est un autre projet — mais la syntaxe peut être conçue pour que le
passage soit possible, en gardant chaque construction étiquetée par son temps (pas,
événement, état). Les sections 20.2 à 20.4 vont dans ce sens : elles n'ajoutent aucune
sémantique nouvelle, elles rendent lisible une sémantique qui existe déjà.

---

## 23. Comment trancher : protocole d'évaluation

Une revue de langages ne décide pas ; un essai décide. Protocole proposé, une demi-journée
de travail :

1. Choisir cinq modèles de référence déjà présents : jeu de la vie, proie-prédateur,
   immunité, autopulse, dyctie.
2. Écrire chacun dans **deux** variantes de syntaxe : la variante actuelle (verbes
   anglais) et une variante « phrase » inspirée d'Inform 7 / Gherkin.
3. Mesurer, pour chaque variante : nombre de lignes, nombre de mots par règle, nombre
   d'erreurs de compilation au premier jet, temps de relecture par une tierce personne.
4. Décider sur les chiffres, pas sur l'impression.

Critère de réussite : la variante retenue doit produire **exactement** le même `.nbd2` que
l'autre (même arbre, mêmes identifiants), sinon la comparaison porte sur deux langages
différents.

---

## 24. Inventaire (pour l'exhaustivité)

Ce qui suit n'est pas analysé, mais recensé : c'est la liste des familles parcourues, pour
qu'on sache ce qui a été examiné et écarté.

**Simulation** : SIMULA 67, GPSS, SIMSCRIPT, GASP, SLAM, SIMAN, Arena, AnyLogic, ExtendSim,
FlexSim, MODSIM, SimPy, SimJulia, Salabim, DEVS, Parallel DEVS, MS4 Me, Ptolemy II,
CADP, SHIFT.

**Multi-agents** : NetLogo, StarLogo, StarLogo TNG, GAMA/GAML, Repast, MASON, Mesa, ABMS,
Agents.jl, AgentScript, FLAME, FLAME GPU, Swarm, Ascape, Sugarscape, SeSam, JAS.

**Réécriture et biologie** : Kappa, BioNetGen/BNGL, BioCham, Antimony, SBML, CellML,
little b, Chromar, MGS, GP, GrGen, PORGY, L-systems, Cellang, CompuCell3D, Chaste,
Morpheus, Tissue Forge, BIO-LGCA.

**Synchrones et réactifs** : Esterel, Lustre, Signal, SCADE, Argos, Statecharts,
ReactiveML, Céu, hiphop.js, Zélus, Lucid, Fran, Yampa, Elm, RxJS, FrTime.

**Hybrides et automates** : Modelica, Simulink, Stateflow, HyVisual, SpaceEx, Flow*,
UPPAAL, Kronos, PRISM, Storm, Modest, MRMC.

**Concurrence** : CCS, CSP, pi-calcul, ambients, BioAmbients, Brane calculi,
Beta-binders, Chor, Scribble, Orc, Reo, Petri nets, CPN Tools, GSPN.

**Règles et logique** : OPS5, CLIPS, Jess, Drools, Soar, Prolog, Mercury, Datalog,
clingo, DLV, MiniZinc, Choco, Alloy, TLA+, TLC, NuSMV, SPIN, Coq, Isabelle, Lean.

**Quasi naturel** : Inform 7, ACE, CLOnE, GF, AppleScript, HyperTalk, SQL, Gherkin,
Literate programming, Wolfram Language, COBOL, Rexx, Logo, Hypertalk.

**Exotiques** : APL, J, K, BQN, Forth, Factor, Joy, Self, Smalltalk, Befunge, Piet,
Shakespeare, Whitespace, Malbolge, Brainfuck, INTERCAL, LOLCODE, Chef, Taxi.

**Typage et composition** : Haskell, OCaml, Scala, Julia, CLOS, Dylan, Kea, Kotlin,
Rust, C++ concepts, AspectJ, AHEAD, delta-modelling, Racket, Scheme, MetaOCaml, Terra,
Nim, D, Template Haskell.

**Édition et DSL** : MPS, Xtext, Langium, ANTLR, tree-sitter, Blockly, Scratch, Snap!,
Node-RED, Max/MSP, Pure Data, LabVIEW, Unreal Blueprints, Excel/Resolver.

**Configuration** : HCL/Terraform, CUE, Dhall, Nix, Starlark, Jsonnet, YAML, TOML.

---

## 25. Sources consultées

- Simula 67, chapitre 12 « Class Simulation » (process, hold, passivate, ensemble de
  séquencement, avis d'événement) : mn.uio.no/english/services/it/simula
- Inform 7 Handbook, chap. 4 « Action Processing » (rulebooks before/instead/after,
  « every turn », séquence du tour) : inform-7-handbook.readthedocs.io
- Ptolemy II, *System Design, Modeling, and Simulation* (acteurs, directeurs) :
  ptolemy.berkeley.edu/books/Systems
- DEVS, Vangheluwe (formalisme de Zeigler, avance au prochain événement) :
  cs.mcgill.ca/~hv/classes/MS/DEVS.pdf
- Lustre, « The synchronous data flow programming language LUSTRE » (calcul d'horloge) :
  cs.ucf.edu/~lboloni/Teaching/EEL6897_2007/slides/Lustre.pdf
- MGS, « un langage déclaratif de calcul spatial » (collections topologiques,
  transformations par voisinage) : mgs.spatial-computing.org ; hal.science/hal-01106954
- GAMA / GAML (species, reflex, aspect, tick) : gama-platform.org/wiki/GamlLanguage
- NetLogo 7, guide de transition (ticks, mise à jour sur tick) : docs.netlogo.org
- Kappa, manuel KaSim (graphes de sites, règles) : tools.kappalanguage.org
- BioNetGen, *Rule-Based Modeling of Biochemical Systems* (blocs BNGL) :
  vcell.org/bionetgen/downloads/BioNetGen-book.pdf
- OCaml, « Polymorphic variants » : ocaml.org/manual/polyvariant.html
- AspectJ Programming Guide (pointcuts, tissage) : eclipse.dev/aspectj
- Feature-oriented programming (couches, raffinement par étapes) :
  en.wikipedia.org/wiki/Feature-oriented_programming
- Attempto Controlled English, manuel v3 : attempto.ifi.uzh.ch
- Gherkin Reference (Given/When/Then, langues) : cucumber.io/docs/gherkin/reference
- Modelica, tutoriel (événements, `when`) : openmodelica.org
- Stateflow, sémantique des charts : mathworks.com/help/stateflow
- Céu, « The Design and Implementation of the Synchronous Language CÉU » :
  dl.acm.org/doi/10.1145/3035544
- SimPy, concepts de base (processus, timeout) : simpy.readthedocs.io
- GPSS, résumé du langage (transactions, blocs) : ACM SIGPLAN HOPL
- clingo / ASP, guide Potassco : potassco.org/guide
- JetBrains MPS (édition projectionnelle) : jetbrains.com/mps
