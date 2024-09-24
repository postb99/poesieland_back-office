using System.Text;
using System.Xml;
using System.Xml.Schema;
using FluentAssertions;
using Toolbox;
using Toolbox.Settings;
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

    [Theory]
    [Trait("UnitTest", "XmlSchema")]
    [InlineData("XmlStorageFile", "Latin1")]
    public void ShouldValidateSchema(string xmlFileKey, string encoding)
    {
        _errorsCount = 0;
        new Validator(_testOutputHelper).Validate(xmlFileKey, encoding);

        _errorsCount.Should().Be(0);
    }

    private class Validator
    {
        private static ITestOutputHelper _testOutputHelper;

        public Validator(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public void Validate(string xmlFileKey, string encoding)
        {
            var configuration = Helpers.GetConfiguration();
            XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
            xmlReaderSettings.Schemas.Add("https://github.com/Xarkam/poesieland/ns",
                configuration[Constants.XML_SCHEMA_FILE]);
            xmlReaderSettings.ValidationType = ValidationType.Schema;
            xmlReaderSettings.ValidationEventHandler += ValidationEventHandler;

            using var streamReader = new StreamReader(configuration[xmlFileKey], Encoding.GetEncoding(encoding));
            using var xmlReader = XmlReader.Create(streamReader, xmlReaderSettings);

            _testOutputHelper.WriteLine($"Validating {configuration[xmlFileKey]}");
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
                _testOutputHelper.WriteLine($"[{e.Exception.LineNumber}] {e.Message}");
            }
        }
    }
}