using FluentAssertions;
using Toolbox;
using Toolbox.Settings;

namespace Tests;

   public class YamlMetadataProcessorTest
    {
        [Fact]
        private void ShouldImportYamlMetadata()
        {
            var configuration = Helpers.GetConfiguration();
            var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                configuration[Constants.CONTENT_ROOT_DIR], "18_dix_huitieme_saison\\saisons.md");
            var poemContentImporter = new PoemContentImporter();
            var (poem, position) = poemContentImporter.Import(poemContentFilePath, configuration);
            poemContentImporter.HasYamlMetadata.Should().BeTrue();
            poemContentImporter.HasTomlMetadata.Should().BeFalse();
            poem.Title.Should().Be("Saisons");
            poem.Id.Should().Be("saisons_18");
            poem.TextDate.Should().Be("01.11.2023");
            poem.Categories.Count.Should().Be(2);
            var saisonsCategorieIndex = poem.Categories.FindIndex(x => x.Name == "Saisons");
            var natureCategorieIndex = poem.Categories.FindIndex(x => x.Name == "Nature");
            poem.Categories[saisonsCategorieIndex].SubCategories.Count.Should().Be(4);
            poem.Categories[saisonsCategorieIndex].SubCategories.First().Should().Be("Automne");
            poem.Categories[natureCategorieIndex].SubCategories.First().Should().Be("Climat");
            poem.VerseLength.Should().Be("8");
            poem.PoemType.Should().BeNull();
            position.Should().Be(11);
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

        [Fact]
        private void ShouldImportTypeYamlMetadata()
        {
            var configuration = Helpers.GetConfiguration();
            var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                configuration[Constants.CONTENT_ROOT_DIR], "17_dix_septieme_saison\\a_bacchus.md");
            var poemContentImporter = new PoemContentImporter();
            var (poem, position) = poemContentImporter.Import(poemContentFilePath, configuration);
            poemContentImporter.HasYamlMetadata.Should().BeTrue();
            poemContentImporter.HasTomlMetadata.Should().BeFalse();
            poem.PoemType.Should().Be("sonnet");
        }

        [Fact]
         private void ShouldImportSingleLineInfoYamlMetadata()
         {
             var configuration = Helpers.GetConfiguration();
             var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                 configuration[Constants.CONTENT_ROOT_DIR], "17_dix_septieme_saison\\a_bacchus.md");
             var poemContentImporter = new PoemContentImporter();
             var (poem, position) = poemContentImporter.Import(poemContentFilePath, configuration);
             poemContentImporter.HasYamlMetadata.Should().BeTrue();
             poemContentImporter.HasTomlMetadata.Should().BeFalse();
             poem.Info.Should().Be("Reprise d'un poème-chanson de 1994");
         }
         
        [Fact]
        private void ShouldImportMultilineInfoYamlMetadata()
        {
            var configuration = Helpers.GetConfiguration();
            var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                configuration[Constants.CONTENT_ROOT_DIR], "22_vingt_deuxieme_saison\\l_automne_est_venu.md");
            var poemContentImporter = new PoemContentImporter();
            var (poem, position) = poemContentImporter.Import(poemContentFilePath, configuration);
            poemContentImporter.HasYamlMetadata.Should().BeTrue();
            poemContentImporter.HasTomlMetadata.Should().BeFalse();
            /*
            {{% notice style="primary" %}}
            Acrostiche : l'automne venu.

            Les poèmes qui commencent par ce vers...
            {{% include "../../includes/l_automne_est_venu" hidefirstheading %}}
            {{% /notice %}}
            */
            var infoLines = poem.Info.Split(Environment.NewLine);
            infoLines.Length.Should().Be(6);
            infoLines[0].Should().Be("{{% notice style=\"primary\" %}}");
            infoLines[5].Should().Be("{{% /notice %}}");
        }
        
        // UT for no info / mono-line info / multiline info.
        // Then also for TOML. Edit a TOML file.
        
        [Fact]
        private void ShouldImportNullInfoYamlMetadata()
        {
            var configuration = Helpers.GetConfiguration();
            var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                configuration[Constants.CONTENT_ROOT_DIR], "18_dix_huitieme_saison\\novembre.md");
            var poemContentImporter = new PoemContentImporter();
            var (poem, position) = poemContentImporter.Import(poemContentFilePath, configuration);
            poemContentImporter.HasYamlMetadata.Should().BeTrue();
            poemContentImporter.HasTomlMetadata.Should().BeFalse();
            poem.Info.Should().BeNull();
        }
        
        [Fact]
        private void ShouldImportPicturesYamlMetadata()
        {
            var configuration = Helpers.GetConfiguration();
            var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                configuration[Constants.CONTENT_ROOT_DIR], "18_dix_huitieme_saison\\present_simple.md");
            var poemContentImporter = new PoemContentImporter();
            var (poem, position) = poemContentImporter.Import(poemContentFilePath, configuration);
            poemContentImporter.HasYamlMetadata.Should().BeTrue();
            poemContentImporter.HasTomlMetadata.Should().BeFalse();
            //poem.Info.Should().Be("Vers variable : 6, 3");
            poem.VerseLength.Should().Be("11");
            poem.Pictures.Count.Should().Be(2);
            poem.Pictures[0].Should().Be("17 décembre 2023");
            poem.Pictures[1].Should().Be("Avec mon chien le 5 juillet 2022");
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
                .Be("Faune");
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

        [Fact]
        private void ShouldExtractTags()
        {
            var configuration = Helpers.GetConfiguration();
            var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                configuration[Constants.CONTENT_ROOT_DIR], "18_dix_huitieme_saison\\saisons.md");
            var poemContentImporter = new PoemContentImporter();
            var (tags, year, poemId, _) = poemContentImporter.GetTagsYearVariableVerseLength(poemContentFilePath, configuration);
            poemContentImporter.HasYamlMetadata.Should().BeTrue();
            poemContentImporter.HasTomlMetadata.Should().BeFalse();
            tags.Count.Should().Be(3);
            tags[0].Should().Be("2023");
            tags[1].Should().Be("saisons");
            tags[2].Should().Be("nature");
        }
    }
