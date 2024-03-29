﻿using System.Globalization;
using FluentAssertions;
using Toolbox;
using Toolbox.Domain;
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

    [Fact(Skip = "Applied")]
    public void ReworkAndSave()
    {

        foreach (var season in _data.Seasons)
        {
            season.Poems.ForEach(p => p.Id = $"{p.Title.UnaccentedCleaned()}_{season.Id}");
        }
        _engine.Save();
    }
    
    [Theory(Skip = "Applied")]
    [InlineData("Amitié", "Amour")]
    [InlineData("Portraits", "Philosophie")]
    [InlineData("Enfance", "Philosophie")]
    [InlineData("Divers", "Philosophie")]
    [InlineData("Ville", "Ombres et lumières")]
    public void AddCategoryToSubCategory(string categoryBecomingSubCategory, string addedCategory)
    {
        var poems = _data.Seasons.SelectMany(x => x.Poems).Where(x => x.Categories.Any(x => x.Name == categoryBecomingSubCategory));
        foreach (var poem in poems)
        {
            foreach (var category in poem.Categories)
            {
                if (category.Name == categoryBecomingSubCategory)
                {
                    category.Name = addedCategory;
                    category.SubCategories = new List<string>{categoryBecomingSubCategory};
                }
            }
        }
        _engine.Save();
    }
}