using System.Text;
using System.Xml;
using System.Xml.Schema;
using Microsoft.Extensions.Configuration;
using Shouldly;
using Toolbox.Settings;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Persistence;

public class XmlSchemaValidationTest : IClassFixture<BasicFixture>
{
    private static int _errorsCount;
    private static ITestOutputHelper _testOutputHelper;
    private IConfiguration _configuration;

    public XmlSchemaValidationTest(BasicFixture basicFixture, ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _configuration = basicFixture.Configuration;
    }

    [Theory]
    [Trait("UnitTest", "XmlSchema")]
    [InlineData("XmlStorageFile", "Latin1")]
    public void ShouldValidateSchema(string xmlFileKey, string encoding)
    {
        _errorsCount = 0;
        new Validator(_configuration).Validate(xmlFileKey, encoding);

        _errorsCount.ShouldBe(0);
    }

    private class Validator(IConfiguration configuration)
    {
        public void Validate(string xmlFileKey, string encoding)
        {
            XmlReaderSettings xmlReaderSettings = new();
            xmlReaderSettings.Schemas.Add("https://github.com/postb99/poesieland/ns",
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