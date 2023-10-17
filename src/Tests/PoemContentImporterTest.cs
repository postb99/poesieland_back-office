using FluentAssertions;
using Toolbox;
using Toolbox.Settings;

namespace Tests;

public class PoemContentImporterTest
{
    public class TomlMetadataProcessorTest : PoemContentImporterTest
    {
        [Fact]
        private void ShouldImportTomlMetadata()
        {
            var configuration = Helpers.GetConfiguration();
            var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                configuration[Constants.CONTENT_ROOT_DIR], "1_premiere_saison\\j_avais_l_heur_de_m_asseoir.md");
            var poemContentImporter = new PoemContentImporter();
            var poem = poemContentImporter.Import(poemContentFilePath, configuration);
            poemContentImporter.HasTomlMetadata.Should().BeTrue();
            poemContentImporter.HasYamlMetadata.Should().BeFalse();
            poem.Title.Should().Be("J'avais l'heur de m'asseoir...");
            poem.Id.Should().Be("j_avais_l_heur_de_m_asseoir_1");
            poem.TextDate.Should().Be("07.12.1995");
            poem.Categories.Count.Should().Be(1);
            poem.Categories.First().Name.Should().Be("Amour");
            poem.Categories.First().SubCategories.Count.Should().Be(1);
            poem.Categories.First().SubCategories.First().Should().Be("Femme");
            poem.PoemType.Should().Be("sonnet");
            poem.VerseLength.Should().Be("12");
        }
        
        [Fact]
        private void ShouldImportInfoTomlMetadata()
        {
            var configuration = Helpers.GetConfiguration();
            var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                configuration[Constants.CONTENT_ROOT_DIR], "12_douzieme_saison\\barcarolle.md");
            var poemContentImporter = new PoemContentImporter();
            var poem = poemContentImporter.Import(poemContentFilePath, configuration);
            poemContentImporter.HasTomlMetadata.Should().BeTrue();
            poemContentImporter.HasYamlMetadata.Should().BeFalse();
            poem.Categories.Count.Should().Be(1);
            poem.Categories.First().SubCategories.Count.Should().Be(1);
            poem.Categories.First().SubCategories.First().Should().Be("Musique, chant");
            poem.Info.Should().Be("Inspiré par l'air homonyme d'Offenbach.");
            
        }

        [Fact]
        private void ShouldImportDoubleAcrosticheTomlMetadata()
        {
            var configuration = Helpers.GetConfiguration();
            var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                configuration[Constants.CONTENT_ROOT_DIR], "10_dixieme_saison\\cathedrale_de_lumieres.md");
            var poemContentImporter = new PoemContentImporter();
            var poem = poemContentImporter.Import(poemContentFilePath, configuration);
            poemContentImporter.HasTomlMetadata.Should().BeTrue();
            poemContentImporter.HasYamlMetadata.Should().BeFalse();
            poem.DoubleAcrostiche.First.Should().Be("Cathédrale");
            poem.DoubleAcrostiche.Second.Should().Be("de lumières");
        }
        
        [Fact]
        private void ShouldImportMultipleCategoriesTomlMetadata()
        {
            var configuration = Helpers.GetConfiguration();
            var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                configuration[Constants.CONTENT_ROOT_DIR], "15_quinzieme_saison\\du_gris_au_noir.md");
            var poemContentImporter = new PoemContentImporter();
            var poem = poemContentImporter.Import(poemContentFilePath, configuration);
            poemContentImporter.HasTomlMetadata.Should().BeTrue();
            poemContentImporter.HasYamlMetadata.Should().BeFalse();
            poem.Acrostiche.Should().Be("Du gris au noir");
            poem.Categories.Count.Should().Be(2);
            poem.Categories.FirstOrDefault(x => x.Name == "Saisons").SubCategories.Count.Should().Be(1);
            poem.Categories.FirstOrDefault(x => x.Name == "Ombres et lumières").SubCategories.Count.Should().Be(2);
            poem.Categories.FirstOrDefault(x => x.Name == "Saisons").SubCategories.FirstOrDefault().Should().Be("Automne");
            poem.Categories.FirstOrDefault(x => x.Name == "Ombres et lumières").SubCategories.Should().Contain("Ville");
            poem.Categories.FirstOrDefault(x => x.Name == "Ombres et lumières").SubCategories.Should().Contain("Crépuscule");
        }
    }

    public class YamlMetadataProcessorTest : PoemContentImporterTest
    {
        [Fact]
        private void ShouldImportYamlMetadata()
        {
            var configuration = Helpers.GetConfiguration();
            var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                configuration[Constants.CONTENT_ROOT_DIR], "16_seizieme_saison\\sur_les_toits_la_pluie.md");
            var poemContentImporter = new PoemContentImporter();
            var poem = poemContentImporter.Import(poemContentFilePath, configuration);
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
        }
        
        [Fact]
        private void ShouldImportDoubleAcrosticheYamlMetadata()
        {
            var configuration = Helpers.GetConfiguration();
            var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                configuration[Constants.CONTENT_ROOT_DIR], "16_seizieme_saison\\les_chenes.md");
            var poemContentImporter = new PoemContentImporter();
            var poem = poemContentImporter.Import(poemContentFilePath, configuration);
            poemContentImporter.HasYamlMetadata.Should().BeTrue();
            poemContentImporter.HasTomlMetadata.Should().BeFalse();
            poem.DoubleAcrostiche.First.Should().Be("Chênes");
            poem.DoubleAcrostiche.Second.Should().Be("destin");
        }
        
        [Fact]
        private void ShouldImportTypeYamlMetadata()
        {
            var configuration = Helpers.GetConfiguration();
            var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                configuration[Constants.CONTENT_ROOT_DIR], "16_seizieme_saison\\pantoun_du_reve.md");
            var poemContentImporter = new PoemContentImporter();
            var poem = poemContentImporter.Import(poemContentFilePath, configuration);
            poemContentImporter.HasYamlMetadata.Should().BeTrue();
            poemContentImporter.HasTomlMetadata.Should().BeFalse();
            poem.PoemType.Should().Be("pantoun");
        }
        
        [Fact]
        private void ShouldImportInfoYamlMetadata()
        {
            var configuration = Helpers.GetConfiguration();
            var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                configuration[Constants.CONTENT_ROOT_DIR], "16_seizieme_saison\\je_vivrai.md");
            var poemContentImporter = new PoemContentImporter();
            var poem = poemContentImporter.Import(poemContentFilePath, configuration);
            poemContentImporter.HasYamlMetadata.Should().BeTrue();
            poemContentImporter.HasTomlMetadata.Should().BeFalse();
            poem.Info.Should().Be("Les états d'âme d'une catherinette");
        }

        [Fact]
        private void ShouldImportMultipleCategoriesYamlMetadata()
        {
            var configuration = Helpers.GetConfiguration();
            var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                configuration[Constants.CONTENT_ROOT_DIR], "16_seizieme_saison\\oiseaux_de_juillet.md");
            var poemContentImporter = new PoemContentImporter();
            var poem = poemContentImporter.Import(poemContentFilePath, configuration);
            poemContentImporter.HasYamlMetadata.Should().BeTrue();
            poemContentImporter.HasTomlMetadata.Should().BeFalse();
            poem.Categories.Count.Should().Be(2);
            poem.Categories.FirstOrDefault(x => x.Name == "Saisons").SubCategories.Count.Should().Be(1);
            poem.Categories.FirstOrDefault(x => x.Name == "Nature").SubCategories.Count.Should().Be(1);
            poem.Categories.FirstOrDefault(x => x.Name == "Saisons").SubCategories.FirstOrDefault().Should().Be("Eté");
            poem.Categories.FirstOrDefault(x => x.Name == "Nature").SubCategories.FirstOrDefault().Should().Be("Animaux");
        }
    }
}