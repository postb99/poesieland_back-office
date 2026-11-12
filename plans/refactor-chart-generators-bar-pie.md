# Plan de refactorisation des générateurs Bar et Pie

## Objectif

Rendre les générateurs de graphiques Bar et Pie plus lisibles et préparer leurs calculs aux tests unitaires. Le travail se fera par étapes : clarifier d'abord les helpers, refactoriser ensuite les générateurs, puis ajouter les tests unitaires dans une étape ultérieure.

## Périmètre

- `ChartDataFileHelper.cs` et la nouvelle classe statique `ChartDataFileDataHelper`.
- Tous les générateurs de `ChartDataFileGenerator.Bar.cs`.
- Tous les générateurs de `ChartDataFileGenerator.Pie.cs` et leurs méthodes privées associées.
- Pas d'ajout de tests unitaires dans les étapes de refactorisation.

## Étape 1 — Clarifier les helpers

### Décision validée

- `ChartDataFileHelper` reste un writer d'instance : il conserve les opérations qui dépendent de son `StreamWriter`, de son `ChartType` ou du nombre de jeux de données, notamment `WriteBeforeData`, `WriteData` et `WriteAfterData`.
- Créer `ChartDataFileDataHelper` comme classe `static` pour les méthodes sans état qui sont partagées et utiles aux générateurs. Cette classe pourra regrouper des méthodes de nature différente pour le moment ; ne pas créer prématurément d'autres abstractions.
- Les calculs métier propres à Bar/Pie resteront près de ces générateurs plutôt que d'être ajoutés automatiquement au helper partagé.
- Les méthodes sans état actuellement dans `ChartDataFileHelper` seront examinées une par une et déplacées vers `ChartDataFileDataHelper` lorsqu'elles représentent bien des fonctions de données/formatage partagées. La méthode `FormatCategoriesBubbleChartLabelOptions`, actuellement d'instance sans utiliser l'état du writer, est notamment candidate à devenir statique et à rejoindre ce helper.
- Adapter les appelants affectés, y compris Radar, Line et Bubble si les méthodes déplacées y sont utilisées, sans refactoriser ces générateurs au-delà du nécessaire.

### Exemple pour choisir l'emplacement des calculs Bar/Pie

Deux options possibles pour extraire, par exemple, le comptage des poèmes par longueur :

```csharp
// Option A : méthode privée dans le fichier partiel du générateur.
private static Dictionary<int, int> CountPoemsByLength(IEnumerable<Poem> poems) { ... }
```

```csharp
// Option B : méthode publique dans une classe de calcul dédiée.
public static Dictionary<int, int> CountPoemsByLength(IEnumerable<Poem> poems) { ... }
```

L'option A garde l'algorithme près du générateur et évite d'élargir l'API publique. L'option B expose directement un calcul réutilisable et testable depuis un projet de tests séparé, mais ajoute une classe/API supplémentaire. Proposition : commencer par l'option A ; si le projet de tests ne peut pas tester ces méthodes privées proprement, décider alors si une classe dédiée est justifiée.

## Étape 2 — Refactoriser les générateurs Bar

- Extraire la collecte et le comptage des métriques régulières et variables, puis la création de leurs data lines.
- Extraire le calcul des comptes saisonniers et des éventuelles séries empilées pour `GenerateOverSeasonsChartDataFile`.
- Décomposer `GeneratePoemIntervalBarChartDataFile` en calcul de dates/comptages, intervalles, séries, data lines de fréquence et contenus Markdown.
- Extraire la conversion de couleur des métriques variables en fonction de leur parité.
- Préserver les noms de fichiers, identifiants, titres, filtres, couleurs et formats produits.

## Étape 3 — Refactoriser les générateurs Pie

- Extraire les comptages de longueurs de poèmes et de sonnets, puis leurs data lines.
- Mutualiser le comptage et la conversion ordonnée des catégories en data lines utilisés par les vues par saison/métrique et par année.
- Extraire les agrégations d'intensité et de jours de semaine ainsi que la création des lignes françaises et anglaises.
- Garder les décisions de chemin, titre et identifiant dans les méthodes qui orchestrent l'écriture.
- Préserver les filtres et comportements actuels, y compris l'exclusion de la date sentinelle là où elle existe.

## Contraintes de compatibilité

- Ne pas modifier les sorties générées, l'ordre des lignes, les couleurs, le texte français/anglais, les règles de filtrage ou les chemins.
- Ne pas ajouter de dépendance.
- Favoriser de petites extractions lisibles plutôt qu'une abstraction générique.
- Garder les calculs extraits indépendants du système de fichiers et du `StreamWriter`.

## Validation et tests

- La première étape de clarification du helper ne comprendra ni build ni génération de fichiers.
- Ajouter les tests unitaires dans une deuxième phase distincte, après le refactoring, en commençant par les agrégations et conversions de données.
- Au moment de l'étape de tests, choisir une stratégie permettant de tester les calculs privés sans rendre publique une API uniquement pour les tests ; une classe dédiée de calculs ne sera créée que si elle apporte une vraie frontière réutilisable.
- ChartDataFileGenerator.bar : rendre internal ces méthodes de calcul et leur ajouter un test unitaire : CollectMetricCounts, CalculateIntervalsAndSeries.
## Questions restantes

Aucune décision bloquante pour l'étape 1. Le choix entre méthodes privées dans le générateur et classe de calcul dédiée sera réévalué lorsque la structure du projet de tests et son accès aux unités de calcul seront examinés.

## Étape 4 — Compléter les tests unitaires des helpers

L'inventaire des tests existants montre que les méthodes suivantes ont déjà une couverture :

- `ChartDataFileDataHelper.FormatCategoriesBubbleChartLabelOptions`.
- `ChartDataFileDataHelper.FillCategoriesBubbleChartDataDict`.
- `ChartDataFileDataHelper.FillCategoryMetricBubbleChartDataDict`.
- `ChartDataFileDataHelper.FillMetricDataDict` (test avec données réelles).
- `ChartDataFileDataHelper.InitMonthDayDictionary` est utilisé dans un test de données, mais ce test vérifie un scénario métier plus large plutôt que toutes les propriétés du dictionnaire.
- `ChartDataFileHelper.WriteData` a quelques vérifications dans les tests de régression de sécurité, sans couverture complète de ses surcharges et formats.

Tests unitaires à ajouter ou compléter :

- `ChartDataFileDataHelper.GetRadarChartLabel` : libellés français de mois/saisons et dates sans libellé.
- `ChartDataFileDataHelper.GetRadarEnChartLabel` : équivalents anglais.
- `ChartDataFileDataHelper.GetMonthLabel` : correspondance pour les douze mois et valeur inconnue.
- `ChartDataFileDataHelper.AddDataLine` : répartition dans chacun des quatre quarts, couleur et rayon calculé.
- `ChartDataFileDataHelper.InitMonthDayDictionary` : clés attendues et valeurs initiales, notamment les limites de jours par mois.
- `ChartDataFileHelper.WriteBeforeData` et `WriteAfterData` : contenu généré par type de graphique et options pertinentes.
- Compléter `ChartDataFileHelper.WriteData` pour couvrir les surcharges et la structure des jeux de données, au-delà des cas de sécurité déjà présents.
