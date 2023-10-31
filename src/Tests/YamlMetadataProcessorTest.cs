using FluentAssertions;
using Toolbox;
using Toolbox.Settings;

namespace Tests;

   public class YamlMetadataProcessorTest
    {
        [Fact(Skip = "Metadata updated to TOML")]
        private void ShouldImportYamlMetadata()
        {
            var configuration = Helpers.GetConfiguration();
            var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                configuration[Constants.CONTENT_ROOT_DIR], "16_seizieme_saison\\sur_les_toits_la_pluie.md");
            var poemContentImporter = new PoemContentImporter();
            var (poem, position) = poemContentImporter.Import(poemContentFilePath, configuration);
            poemContentImporter.HasYamlMetadata.Should().BeTrue();
            poemContentImporter.HasTomlMetadata.Should().BeFalse();
            poem.Title.Should().Be("Sur les toits la pluie");
            poem.Id.Should().Be("sur_les_toits_la_pluie_16");
            poem.Acrostiche.Should().Be("Sur les toits la pluie");
            poem.TextDate.Should().Be("31.05.2004");
            poem.Categories.Count.Should().Be(1);
            poem.Categories.First().Name.Should().Be("Nature");
            poem.Categories.First().SubCategories.Count.Should().Be(1);
            poem.Categories.First().SubCategories.First().Should().Be("Eau douce");
            poem.VerseLength.Should().Be("6");
            poem.PoemType.Should().BeNull();
            poem.Info.Should().BeNull();
            position.Should().Be(34);
        }

        [Fact(Skip = "Metadata updated to TOML")]
        private void ShouldImportDoubleAcrosticheYamlMetadata()
        {
            var configuration = Helpers.GetConfiguration();
            var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                configuration[Constants.CONTENT_ROOT_DIR], "16_seizieme_saison\\les_chenes.md");
            var poemContentImporter = new PoemContentImporter();
            var (poem, position) = poemContentImporter.Import(poemContentFilePath, configuration);
            poemContentImporter.HasYamlMetadata.Should().BeTrue();
            poemContentImporter.HasTomlMetadata.Should().BeFalse();
            poem.DoubleAcrostiche.First.Should().Be("Chênes");
            poem.DoubleAcrostiche.Second.Should().Be("destin");
        }

        [Fact(Skip = "Metadata updated to TOML")]
        private void ShouldImportTypeYamlMetadata()
        {
            var configuration = Helpers.GetConfiguration();
            var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                configuration[Constants.CONTENT_ROOT_DIR], "16_seizieme_saison\\pantoun_du_reve.md");
            var poemContentImporter = new PoemContentImporter();
            var (poem, position) = poemContentImporter.Import(poemContentFilePath, configuration);
            poemContentImporter.HasYamlMetadata.Should().BeTrue();
            poemContentImporter.HasTomlMetadata.Should().BeFalse();
            poem.PoemType.Should().Be("pantoun");
        }

        [Fact(Skip = "Metadata updated to TOML")]
        private void ShouldImportInfoYamlMetadata()
        {
            var configuration = Helpers.GetConfiguration();
            var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                configuration[Constants.CONTENT_ROOT_DIR], "16_seizieme_saison\\je_vivrai.md");
            var poemContentImporter = new PoemContentImporter();
            var (poem, position) = poemContentImporter.Import(poemContentFilePath, configuration);
            poemContentImporter.HasYamlMetadata.Should().BeTrue();
            poemContentImporter.HasTomlMetadata.Should().BeFalse();
            poem.Info.Should().Be("Les états d'âme d'une catherinette");
        }

        [Fact(Skip = "Metadata updated to TOML")]
        private void ShouldImportMultipleCategoriesYamlMetadata()
        {
            var configuration = Helpers.GetConfiguration();
            var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                configuration[Constants.CONTENT_ROOT_DIR], "16_seizieme_saison\\oiseaux_de_juillet.md");
            var poemContentImporter = new PoemContentImporter();
            var (poem, position) = poemContentImporter.Import(poemContentFilePath, configuration);
            poemContentImporter.HasYamlMetadata.Should().BeTrue();
            poemContentImporter.HasTomlMetadata.Should().BeFalse();
            poem.Categories.Count.Should().Be(2);
            poem.Categories.FirstOrDefault(x => x.Name == "Saisons").SubCategories.Count.Should().Be(1);
            poem.Categories.FirstOrDefault(x => x.Name == "Nature").SubCategories.Count.Should().Be(1);
            poem.Categories.FirstOrDefault(x => x.Name == "Saisons").SubCategories.FirstOrDefault().Should().Be("Eté");
            poem.Categories.FirstOrDefault(x => x.Name == "Nature").SubCategories.FirstOrDefault().Should()
                .Be("Animaux");
        }

        [Fact(Skip = "Metadata updated to TOML")]
        private void ShouldImportMultipleCategoriesWithMoreSpacesYamlMetadata()
        {
            var configuration = Helpers.GetConfiguration();
            var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                configuration[Constants.CONTENT_ROOT_DIR], "17_dix_septieme_saison\\hiver_en_ville.md");
            var poemContentImporter = new PoemContentImporter();
            var (poem, position) = poemContentImporter.Import(poemContentFilePath, configuration);
            poemContentImporter.HasYamlMetadata.Should().BeTrue();
            poemContentImporter.HasTomlMetadata.Should().BeFalse();
            poem.Categories.Count.Should().Be(2);
            poem.Categories.FirstOrDefault(x => x.Name == "Saisons").SubCategories.Count.Should().Be(1);
            poem.Categories.FirstOrDefault(x => x.Name == "Ombres et lumières").SubCategories.Count.Should().Be(1);
            poem.Categories.FirstOrDefault(x => x.Name == "Saisons").SubCategories.FirstOrDefault().Should()
                .Be("Hiver");
            poem.Categories.FirstOrDefault(x => x.Name == "Ombres et lumières").SubCategories.FirstOrDefault().Should()
                .Be("Ville");
        }

        [Fact(Skip = "Metadata updated to TOML")]
        private void ShouldExtractTags()
        {
            var configuration = Helpers.GetConfiguration();
            var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                configuration[Constants.CONTENT_ROOT_DIR], "17_dix_septieme_saison\\givre.md");
            var poemContentImporter = new PoemContentImporter();
            var yearAndTags = poemContentImporter.Extract(poemContentFilePath);
            poemContentImporter.HasYamlMetadata.Should().BeTrue();
            poemContentImporter.HasTomlMetadata.Should().BeFalse();
            yearAndTags.tags.Count.Should().Be(2);
        }
    }
