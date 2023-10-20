using System.Xml.Serialization;
using Toolbox.Domain;
using Xunit.Abstractions;

namespace Tests;

public class XmlSerializationTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public XmlSerializationTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void ShouldSerializeToStream()
    {
        var poem = new Poem
        {
            Id = "test_id_0",
            Categories = new List<Category>
                { new Category { Name = "TestCat", SubCategories = new List<string> { "TestSubCat" } } },
            Paragraphs = new List<Paragraph> { new Paragraph { Verses = new List<string> { "Verse1", "Verse2" } } },
            Title = "Title",
            TextDate = "19.10.2023",
            VerseLength = "8"
        };

        var data = new Root();
        data.Seasons = new List<Season> { new Season() };
        data.Seasons.First().Poems = new List<Poem> { poem };
        
        var xmlSerializer = new XmlSerializer(typeof(Root));
        using var memoryStream = new MemoryStream();
        xmlSerializer.Serialize(memoryStream, data);
        memoryStream.Position = 0;
        _testOutputHelper.WriteLine(new StreamReader(memoryStream).ReadToEnd());
    }
}