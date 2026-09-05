# AGENTS.md

## Rôle de ce fichier

Ce fichier contient des règles de développement.

Il doit rester volontairement compact.

## Principes de développement

- Comprendre l'existant avant de modifier.
- Ne pas inventer une API, une fonction, un export, un événement ou un comportement d'une dépendance.
- Vérifier la version réellement utilisée lorsqu'un comportement dépend d'une version.
- Préférer les API publiques et stables des dépendances.
- Ne jamais modifier une dépendance upstream directement sauf si le projet est explicitement un fork/overlay de cette dépendance.
- Ne pas ajouter une dépendance sans besoin concret.
- Préférer les solutions simples, lisibles et maintenables.
- Éviter la duplication de logique métier.
- Centraliser les paramètres configurables.
- Ne pas introduire une abstraction générique sans cas d'usage réel.

---

## Performance

La performance est une contrainte d'architecture, pas une optimisation de dernière minute.

Principes :

- privilégier une architecture événementielle ;
- éviter le polling lorsqu'un événement peut exprimer le changement ;
- éviter les boucles permanentes inutiles ;
- éviter les traitements globaux répétés sur une liste d'objets ou enregistrements ;
- batcher les opérations lorsque cela réduit réellement les pics ;
- ne pas micro-optimiser sans bénéfice identifiable ;
- lorsqu'une solution prétend être plus performante, préférer une mesure, un profilage ou au minimum une justification technique explicite.

---

## Données et persistance

- Utiliser la couche/API publique existante lorsqu'elle répond correctement au besoin.
- Utiliser la couche de persistance existante du projet.
- Préférer une API métier lorsqu'elle existe réellement et couvre le besoin ;
- Les écritures persistantes doivent être cohérentes, idempotentes lorsque nécessaire et protégées contre les doublons.
- Ne pas créer une copie parallèle d'une donnée dont une dépendance est déjà la source de vérité.
- Les opérations critiques doivent gérer les échecs partiels.

---

## Dépendances et priorité aux frameworks existants

Lorsqu'une dépendance déjà installée fournit une fonctionnalité adaptée, elle doit être examinée avant de développer un mécanisme parallèle.

Cette priorité n'est pas dogmatique.

Une solution interne peut être préférée lorsque :

- l'API existante ne couvre pas le besoin ;
- son comportement est incompatible avec la version installée ;
- elle impose une contrainte d'architecture injustifiée ;
- ou une solution plus simple apporte un bénéfice concret de performance ou de fiabilité.

Toute décision importante de contourner une solution existante doit expliquer pourquoi.

---

## Commentaires et maintenabilité

Les commentaires doivent principalement expliquer **pourquoi** une décision existe.

Documenter particulièrement :

- choix d'architecture ;
- contraintes de performance ;
- mécanismes de sécurité et anti-triche ;
- interactions avec des dépendances externes ;
- timers, files, batches et cycles de vie ;
- comportements imposés par une API ou une version particulière ;
- contournements ;
- valeurs qui sembleraient arbitraires à un futur développeur.

Éviter les commentaires qui répètent littéralement le code.

Les fonctions importantes doivent avoir une responsabilité identifiable et des noms explicites.

---

## Méthode de travail de l'agent

Avant de coder :

3. inspecter le code réellement concerné ;
4. identifier les APIs/dépendances utilisées ;
5. vérifier les hypothèses importantes dans le code ou la documentation de la version utilisée ;
6. proposer ou appliquer la plus petite modification cohérente.

Pendant l'implémentation :

- préserver les décisions d'architecture existantes ;
- ne pas élargir le périmètre sans nécessité ;
- conserver la compatibilité annoncée par le projet ;
- commenter les décisions non évidentes.

Après l'implémentation :

- vérifier les chemins critiques ;
- rechercher les régressions évidentes ;
- exécuter les tests/lint/build disponibles lorsque possible ;
- signaler clairement ce qui n'a pas pu être vérifié.

---

## Interdictions générales

Ne pas :

- inventer une API ;
- contourner une sécurité côté serveur par commodité ;
- introduire un polling sans justification ;
- créer un thread permanent pour une action ponctuelle ;
- dupliquer une source de vérité ;
- modifier silencieusement une règle métier ;
- ajouter une dépendance inutile ;
- laisser des secrets dans le dépôt ;
- masquer un échec critique ;
- prétendre avoir testé quelque chose qui n'a pas été exécuté.

---

## Critère de qualité

Une modification est acceptable lorsqu'elle est :

- correcte fonctionnellement ;
- sécurisée ;
- maintenable ;
- observable lorsque nécessaire ;
- raisonnable en coût CPU, mémoire, réseau et I/O ;
- compatible avec les versions déclarées du projet.
