using System.Globalization;
using FluentAssertions;
using Toolbox;
using Toolbox.Xml;
using Xunit.Abstractions;

namespace Tests;

public class XmlStorageReworkTest : IClassFixture<LoadDataFixture>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Root _data;
    private readonly Engine _engine;

    public XmlStorageReworkTest(LoadDataFixture data, ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _data = data.Engine.Data;
        _engine = data.Engine;
    }
    
    [Fact]
    public void RewriteXmlTagsInOrder()
    {
        _engine.SaveCleaned();
        var cleanedData = _engine.LoadCleaned();
        
        using var initialDataStringWriter = new StringWriter();
        using var cleanedDataStringWriter = new StringWriter();
        
        _engine.XmlSerializer.Serialize(initialDataStringWriter, _data);
        _engine.XmlSerializer.Serialize(cleanedDataStringWriter, cleanedData);

        cleanedDataStringWriter.ToString().Length.Should().Be(initialDataStringWriter.ToString().Length);
    }
}