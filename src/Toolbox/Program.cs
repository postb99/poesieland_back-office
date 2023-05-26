using System.Text;
using System.Xml.Serialization;
using Microsoft.Extensions.Configuration;
using Toolbox.Xml;

var configurationBuilder = new ConfigurationBuilder();
configurationBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
var configuration = configurationBuilder.Build();

var xmlDocPath = Path.Combine(Directory.GetCurrentDirectory(), configuration["XmlStorageFile"]);
using var streamReader = new StreamReader(xmlDocPath, Encoding.Latin1);

var xmlSerializer = new XmlSerializer(typeof(Root));
var content = xmlSerializer.Deserialize(streamReader);

Console.WriteLine(content);



  
