﻿using System.Text;
using System.Xml.Serialization;
using Microsoft.Extensions.Configuration;
using Toolbox.Xml;

namespace Toolbox;

public class Engine
{
    private IConfiguration _configuration;
    public Root Data { get; private set; }
    
    public Engine(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Load()
    {
        var xmlDocPath = Path.Combine(Directory.GetCurrentDirectory(), _configuration["XmlStorageFile"]);
        using var streamReader = new StreamReader(xmlDocPath, Encoding.Latin1);

        var xmlSerializer = new XmlSerializer(typeof(Root));
        Data = xmlSerializer.Deserialize(streamReader) as Root;
    }
}