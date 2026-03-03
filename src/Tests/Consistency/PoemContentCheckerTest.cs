using System.Text.RegularExpressions;
using Toolbox.Domain;
using Xunit;

namespace Tests.Consistency;

public class PoemContentCheckerTest(WithRealDataFixture fixture) : IClassFixture<WithRealDataFixture>
{
    private Regex _regex = new("[^ ][:;!?]");

    [Fact]
    [Trait("UnitTest", "ContentConsistencyCheck")]
    public void CheckMissingSpaces()
    {
        using var outputFileStream = File.Open("PoemsWithMissingSpaces.txt", FileMode.Create);
        using var streamWriter = new StreamWriter(outputFileStream);
        object fileLock = new object();
        
        Parallel.ForEach(fixture.Data.Seasons, season =>
        {
            foreach (var poem in season.Poems)
            {
                CheckPoemContent(poem, streamWriter, fileLock);
            }
        });
    }

    private void CheckPoemContent(Poem poem, StreamWriter sw, object fileLock)
    {
        foreach (var verse in poem.Paragraphs.SelectMany(x => x.Verses))
        {
            if (!_regex.IsMatch(verse)) continue;
            lock (fileLock)
            {
                sw.WriteLine(poem.Id);
                break;
            }
        }
    }
}