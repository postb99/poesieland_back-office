using System.Text;
using System.Xml;
using System.Xml.Schema;
using FluentAssertions;
using Toolbox;
using Xunit.Abstractions;

namespace Tests;

public class XmlSchemaValidationTest
{
    private readonly ITestOutputHelper _testOutputHelper;
    private static int _errorsCount = 0;

    public XmlSchemaValidationTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void ShouldValidateSchema()
    {
        _errorsCount = 0;
        new Validator(_testOutputHelper).Validate();
        _errorsCount.Should().Be(0);
        // 772 errors because of inconsistent tags order, before cleanup
    }

    private class Validator
    {
        private static ITestOutputHelper _testOutputHelper;

        public Validator(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public void Validate()
        {
            var configuration = Helpers.GetConfiguration();
            XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
            xmlReaderSettings.Schemas.Add("https://github.com/Xarkam/poesieland/ns",
                configuration[Settings.XML_SCHEMA_FILE]);
            xmlReaderSettings.ValidationType = ValidationType.Schema;
            xmlReaderSettings.ValidationEventHandler += ValidationEventHandler;

            using var streamReader = new StreamReader(configuration[Settings.XML_STORAGE_FILE], Encoding.Latin1);
            using var xmlReader = XmlReader.Create(streamReader, xmlReaderSettings);

            while (xmlReader.Read())
            {
            }
        }

        static void ValidationEventHandler(object sender, ValidationEventArgs e)
        {
            if (e.Severity == XmlSeverityType.Warning)
            {
                _testOutputHelper.WriteLine("WARNING: ");
                _testOutputHelper.WriteLine(e.Message);
            }
            else if (e.Severity == XmlSeverityType.Error)
            {
                _errorsCount++;
                _testOutputHelper.WriteLine("ERROR: ");
                _testOutputHelper.WriteLine(e.Message);
            }
        }
    }
}