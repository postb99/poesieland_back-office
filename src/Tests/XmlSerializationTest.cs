﻿using System.Xml.Serialization;
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
    [Trait("UnitTest", "XmlSerialization")]
    public void ShouldSerializeToStream()
    {
        var poem = new Poem
        {
            Id = "test_id_0",
            Categories = [new Category { Name = "TestCat", SubCategories = ["TestSubCat"] }],
            Paragraphs = [new Paragraph { Verses = ["Verse1", "Verse2"] }],
            Title = "Title",
            TextDate = "19.10.2023",
            VerseLength = "8"
        };

        var data = new Root();
        data.Seasons = [new Season()];
        data.Seasons.First().Poems = [poem];
        
        var xmlSerializer = new XmlSerializer(typeof(Root));
        using var memoryStream = new MemoryStream();
        xmlSerializer.Serialize(memoryStream, data);
        memoryStream.Position = 0;
        _testOutputHelper.WriteLine(new StreamReader(memoryStream).ReadToEnd());
    }
}