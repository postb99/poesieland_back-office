using AutoFixture;
using FluentAssertions;
using Toolbox;
using Toolbox.Domain;
using Xunit.Abstractions;

namespace Tests;

public class EngineTest : IClassFixture<LoadDataFixture>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Engine _engine;

    public EngineTest(LoadDataFixture data, ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _engine = data.Engine;
    }

    public class StorageLoadTest : EngineTest
    {
        public StorageLoadTest(LoadDataFixture data, ITestOutputHelper testOutputHelper) : base(data, testOutputHelper)
        {
        }

        [Fact]
        [Trait("UnitTest", "XmlRead")]
        public void ShouldLoad()
        {
            _engine.Data.Should().NotBeNull();
        }

        [Fact]
        [Trait("UnitTest", "XmlRead")]
        public void ShouldLoadAcrostiche()
        {
            var poemWithAcrostiche = _engine.Data.Seasons[13].Poems.FirstOrDefault(x => x.Id == "resurrection_14");
            poemWithAcrostiche.Should().NotBeNull();
            poemWithAcrostiche!.Acrostiche.Should().Be("Résurrection");
        }

        [Fact]
        [Trait("UnitTest", "XmlRead")]
        public void ShouldLoadDoubleAcrostiche()
        {
            var poemWithFirstAndSecondAcrostiche =
                _engine.Data.Seasons[13].Poems.FirstOrDefault(x => x.Id == "l_air_cree_14");
            poemWithFirstAndSecondAcrostiche.Should().NotBeNull();
            poemWithFirstAndSecondAcrostiche!.DoubleAcrostiche.Should().NotBeNull();
            poemWithFirstAndSecondAcrostiche!.DoubleAcrostiche!.First.Should().Be("L'air");
            poemWithFirstAndSecondAcrostiche!.DoubleAcrostiche!.Second.Should().Be("créé");
        }

        [Theory]
        [Trait("UnitTest", "XmlRead")]
        [InlineData("j_avais_l_heur_de_m_asseoir_1", 1, 14)]
        [InlineData("grand_sud_1", 1, 12)]
        [InlineData("illusion_1", 1, 8)]
        public void ShouldHaveVersesCount(string poemId, int seasonId, int expectedCount)
        {
            var poem = _engine.Data.Seasons[seasonId - 1].Poems.FirstOrDefault(x => x.Id == poemId);
            poem.VersesCount.Should().Be(expectedCount);
        }

        [Theory]
        [Trait("UnitTest", "XmlRead")]
        [InlineData("j_avais_l_heur_de_m_asseoir_1", 1, false)]
        [InlineData("grand_sud_1", 1, true)]
        [InlineData("illusion_1", 1, false)]
        [InlineData("matin_privilege_15", 15, false)]
        [InlineData("ombres_et_lumieres_15", 15, true)]
        [InlineData("les_chenes_16", 16, true)]
        public void ShouldHaveQuatrains(string poemId, int seasonId, bool expectedHasQuatrain)
        {
            var poem = _engine.Data.Seasons[seasonId - 1].Poems.FirstOrDefault(x => x.Id == poemId);
            poem.HasQuatrains.Should().Be(expectedHasQuatrain);
            if (expectedHasQuatrain)
            {
                poem.Paragraphs.Count.Should().Be(poem.VersesCount / 4);
            }

            _testOutputHelper.WriteLine($"{poem.Paragraphs.Count} paragraphs, {poem.VersesCount} verses");
        }

        [Fact]
        [Trait("UnitTest", "XmlRead")]
        public void ShouldBePoemSeasonId()
        {
            _engine.Data.Seasons[0].Poems[0].SeasonId.Should().Be(1);
        }
    }

    public class ContentGenerationTest : EngineTest
    {
        public ContentGenerationTest(LoadDataFixture data, ITestOutputHelper testOutputHelper) : base(data,
            testOutputHelper)
        {
        }

        [Fact]
        [Trait("UnitTest", "ContentFiles")]
        public void ShouldBeSeasonContentDirectoryName()
        {
            _engine.Data.Seasons[0].ContentDirectoryName.Should().Be("1_premiere_saison");
        }

        [Fact]
        [Trait("UnitTest", "ContentFiles")]
        public void ShouldCreateFirstSeasonIndexFile()
        {
            _engine.GenerateSeasonIndexFile(1);
        }

        [Fact]
        [Trait("UnitTest", "ContentFiles")]
        public void ShouldBePoemContentFileName()
        {
            _engine.Data.Seasons[0].Poems[0].ContentFileName.Should().Be("j_avais_l_heur_de_m_asseoir.md");
        }

        [Fact]
        [Trait("UnitTest", "ContentFiles")]
        public void ShouldCreateFirstPoemFile()
        {
            _engine.GeneratePoemFile(_engine.Data.Seasons[0].Poems[0]);
        }

        [Theory(Skip = "Validated")]
        [Trait("UnitTest", "ContentFiles")]
        [InlineData("simplest", false, null, false, false)]
        [InlineData("only_info", false, null, false, true)]
        [InlineData("only_type", false, PoemType.Sonnet, false, false)]
        [InlineData("type_info", false, PoemType.Sonnet, false, true)]
        [InlineData("only_acrostiche", true, null, false, false)]
        [InlineData("acrostiche_type", true, PoemType.Sonnet, false, false)]
        [InlineData("acrostiche_info", true, null, false, true)]
        [InlineData("acrostiche_type_info", true, PoemType.Sonnet, false, true)]
        [InlineData("only_double_acrostiche", false, null, true, false)]
        [InlineData("type_double_acrostiche", false, PoemType.Sonnet, true, false)]
        [InlineData("double_acrostiche_info", false, null, true, true)]
        [InlineData("double_acrostiche_type_info", false, PoemType.Sonnet, true, true)]
        public void ShouldCreatePoemFileWhateverContent(string fileName, bool isAcrostiche, PoemType? poemType,
            bool isDoubleAcrostiche, bool hasInfo)
        {
            var poem = new Fixture().Build<Poem>()
                .With(x => x.TextDate, "01.01.1900")
                .With(x => x.Title, fileName)
                .With(x => x.Id, $"{fileName}_99")
                .With(x => x.Acrostiche, isAcrostiche ? "Acrostiche" : null)
                .With(x => x.PoemType, poemType?.ToString())
                .With(x => x.DoubleAcrostiche,
                    isDoubleAcrostiche ? new DoubleAcrostiche { First = "Double", Second = "Acrostiche" } : null)
                .With(x => x.Info, hasInfo ? "Some info text" : null)
                .With(x => x.VerseLength, "8")
                .Create();

            var fictiveSeason = new Season
                { Id = 99, Name = "Test", NumberedName = "Test", Summary = "Test", Poems = new List<Poem> { poem } };

            _engine.Data.Seasons.Add(fictiveSeason);
            _engine.GenerateSeasonIndexFile(fictiveSeason.Id);
            _engine.GeneratePoemFile(poem);
        }
    }

    public class ComputationTest : EngineTest
    {
        public ComputationTest(LoadDataFixture data, ITestOutputHelper testOutputHelper) : base(data, testOutputHelper)
        {
        }

        [Fact]
        [Trait("UnitTest", "Computation")]
        public void ShouldCorrectlyComputeVerseLengthDataDict()
        {
            var dataDict = _engine.FillVerseLengthDataDict(out var _);
            _testOutputHelper.WriteLine(
                $"Last non-empty season poem count: {_engine.Data.Seasons.Last(x => x.Poems.Count > 0).Poems.Count}");
            _testOutputHelper.WriteLine(
                $"Computed values for last season: {string.Join('-', dataDict.Values.Select(x => x.Last()))}");
            var sum = dataDict.Values.Sum(x => x.Last());
            _testOutputHelper.WriteLine($"Computed values sum: {sum}");
            sum.Should().BeInRange(99.9m, 100.1m);
        }

        [Fact]
        [Trait("UnitTest", "Computation")]
        public void ShouldCorrectlyFillCategoriesBubbleChartDataDict()
        {
            Dictionary<KeyValuePair<string, string>, int> dict = new();
            var xAxisLabels = new SortedSet<string>();
            var yAxisLabels = new SortedSet<string>();

            // Poem with single subcategory
            var poem = new Poem { Categories = [new Category { SubCategories = ["A"] }] };
            _engine.FillCategoriesBubbleChartDataDict(dict, xAxisLabels, yAxisLabels, poem);
            xAxisLabels.Should().BeEmpty();
            yAxisLabels.Should().BeEmpty();

            dict.Should().BeEmpty();

            // Poem with two categories
            poem = new Poem { Categories = [new Category { SubCategories = ["A", "B"] }] };
            _engine.FillCategoriesBubbleChartDataDict(dict, xAxisLabels, yAxisLabels, poem);
            
            var expectedKey = new KeyValuePair<string, string>("A", "B");
            dict.TryGetValue(expectedKey, out var counter).Should().BeTrue();
            counter.Should().Be(1);

            var unExpectedKey = new KeyValuePair<string, string>("B", "A");
            dict.TryGetValue(unExpectedKey, out var _).Should().BeFalse();
            
            xAxisLabels.ToList().Should().BeEquivalentTo(["A"]);
            yAxisLabels.ToList().Should().BeEquivalentTo(["B"]);

            // Poem with three categories
            poem = new Poem { Categories = [new Category { SubCategories = ["A", "B", "C"] }] };
            _engine.FillCategoriesBubbleChartDataDict(dict, xAxisLabels, yAxisLabels, poem);

            expectedKey = new KeyValuePair<string, string>("A", "B");
            dict.TryGetValue(expectedKey, out var counter2).Should().BeTrue();
            counter2.Should().Be(2);

            unExpectedKey = new KeyValuePair<string, string>("B", "A");
            dict.TryGetValue(unExpectedKey, out var _).Should().BeFalse();

            expectedKey = new KeyValuePair<string, string>("A", "C");
            dict.TryGetValue(expectedKey, out var counter3).Should().BeTrue();
            counter3.Should().Be(1);

            expectedKey = new KeyValuePair<string, string>("B", "C");
            dict.TryGetValue(expectedKey, out var counter4).Should().BeTrue();
            counter4.Should().Be(1);

            unExpectedKey = new KeyValuePair<string, string>("C", "B");
            dict.TryGetValue(unExpectedKey, out var _).Should().BeFalse();

            unExpectedKey = new KeyValuePair<string, string>("C", "A");
            dict.TryGetValue(unExpectedKey, out var _).Should().BeFalse();
            
            xAxisLabels.ToList().Should().BeEquivalentTo(["A", "B"]);
            yAxisLabels.ToList().Should().BeEquivalentTo(["B", "C"]);

            // Poem with two categories, one per category
            poem = new Poem
                { Categories = [new Category { SubCategories = ["A"] }, new Category() { SubCategories = ["B"] }] };
            _engine.FillCategoriesBubbleChartDataDict(dict, xAxisLabels, yAxisLabels, poem);

            expectedKey = new KeyValuePair<string, string>("A", "B");
            dict.TryGetValue(expectedKey, out var counter5).Should().BeTrue();
            counter5.Should().Be(3);

            unExpectedKey = new KeyValuePair<string, string>("B", "A");
            dict.TryGetValue(unExpectedKey, out var _).Should().BeFalse();
            
            xAxisLabels.ToList().Should().BeEquivalentTo(["A", "B"]);
            yAxisLabels.ToList().Should().BeEquivalentTo(["B", "C"]);
        }
    }

    public class ContentImportTest : EngineTest
    {
        public ContentImportTest(LoadDataFixture data, ITestOutputHelper testOutputHelper) : base(data,
            testOutputHelper)
        {
        }

        [Fact(Skip = "Validated")]
        [Trait("UnitTest", "ContentImport")]
        public void ShouldImportPoem()
        {
            //_engine.ImportPoem("j_avais_l_heur_de_m_asseoir_1");
            _engine.ImportPoem("par_omission_16");
            _engine.ImportPoem("le_jour_16");
            _engine.ImportPoem("accords_finis_16");
        }

        [Fact(Skip = "Validated")]
        [Trait("UnitTest", "ContentImport")]
        public void ShouldImportSeason()
        {
            _engine.ImportSeason(16);
        }
    }

    public class ContentCheckTest : EngineTest
    {
        public ContentCheckTest(LoadDataFixture data, ITestOutputHelper testOutputHelper) : base(data, testOutputHelper)
        {
        }

        [Fact]
        [Trait("UnitTest", "MetadataCheck")]
        public void CheckMissingYearTagInYamlMetadata()
        {
            var anomalies = _engine.CheckMissingTagsInYamlMetadata();
            _testOutputHelper.WriteLine(string.Join(Environment.NewLine, anomalies));
            anomalies.Count().Should().Be(0);
        }
    }
}