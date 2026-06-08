using Umea.se.EstateService.Logic.Search;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Shared.Search;
using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Test.Search;

public class InMemorySearchServiceScoringTests
{
    private static PythagorasDocument Doc(
        int id,
        string name,
        NodeType type = NodeType.Building,
        string? popularName = null,
        decimal? grossArea = null,
        int numChildren = 0,
        BusinessTypeModel? businessType = null,
        AddressModel? address = null)
        => new()
        {
            Id = id,
            Type = type,
            Name = name,
            PopularName = popularName,
            GrossArea = grossArea,
            NumChildren = numChildren,
            BusinessType = businessType,
            Address = address,
            Ancestors = [],
            UpdatedAt = DateTimeOffset.UtcNow,
            RankScore = id
        };

    [Fact]
    public void DocumentCount_ReflectsIndexedDocuments()
    {
        InMemorySearchService service = new([Doc(1, "Alpha"), Doc(2, "Beta")]);

        service.DocumentCount.ShouldBe(2);
    }

    [Fact]
    public void Search_EmptyQuery_ReturnsAllDocumentsAsBrowse()
    {
        InMemorySearchService service = new([Doc(1, "Alpha"), Doc(2, "Beta"), Doc(3, "Gamma")]);

        List<SearchResult> results = [.. service.Search("")];

        results.Select(r => r.Item.Id).ShouldBe([1, 2, 3], ignoreOrder: true);
    }

    [Fact]
    public void Search_EmptyQuery_RanksEstateAboveBuildingViaTypeBoost()
    {
        InMemorySearchService service = new([Doc(2, "Bldg", NodeType.Building), Doc(1, "Est", NodeType.Estate)]);

        List<SearchResult> results = [.. service.Search("")];

        results[0].Item.Type.ShouldBe(NodeType.Estate);
    }

    [Fact]
    public void Search_RespectsMaxResults()
    {
        InMemorySearchService service = new(
        [
            Doc(1, "Alpha"), Doc(2, "Beta"), Doc(3, "Gamma"), Doc(4, "Delta"), Doc(5, "Epsilon")
        ]);

        List<SearchResult> results = [.. service.Search("", new QueryOptions(MaxResults: 2))];

        results.Count.ShouldBe(2);
    }

    [Fact]
    public void Search_FilterByTypes_ReturnsOnlyMatchingTypes()
    {
        InMemorySearchService service = new(
        [
            Doc(1, "An Estate", NodeType.Estate),
            Doc(2, "A Building", NodeType.Building),
            Doc(3, "A Room", NodeType.Room)
        ]);

        List<SearchResult> results = [.. service.Search("", new QueryOptions(FilterByTypes: [NodeType.Building]))];

        results.ShouldHaveSingleItem().Item.Type.ShouldBe(NodeType.Building);
    }

    [Fact]
    public void Search_FilterByBusinessTypes_ReturnsOnlyMatching()
    {
        InMemorySearchService service = new(
        [
            Doc(1, "School House", businessType: new BusinessTypeModel { Id = 5, Name = "Skola" }),
            Doc(2, "Office House", businessType: new BusinessTypeModel { Id = 3, Name = "Kontor" })
        ]);

        List<SearchResult> results = [.. service.Search("", new QueryOptions(FilterByBusinessTypes: [5]))];

        results.ShouldHaveSingleItem().Item.Id.ShouldBe(1);
    }

    [Fact]
    public void Search_FilterByBusinessTypes_NoMatches_ReturnsEmpty()
    {
        InMemorySearchService service = new(
        [
            Doc(1, "School", businessType: new BusinessTypeModel { Id = 5, Name = "Skola" })
        ]);

        List<SearchResult> results = [.. service.Search("", new QueryOptions(FilterByBusinessTypes: [99]))];

        results.ShouldBeEmpty();
    }

    [Fact]
    public void Search_NoMatch_ReturnsEmpty()
    {
        InMemorySearchService service = new([Doc(1, "Library"), Doc(2, "Office")]);

        service.Search("zzzzzzz").ShouldBeEmpty();
    }

    [Fact]
    public void Search_NullOptions_UsesDefaults()
    {
        InMemorySearchService service = new([Doc(1, "Library")]);

        service.Search("library").ShouldNotBeEmpty();
    }

    [Fact]
    public void Search_PrefixMatch_FindsByPrefix()
    {
        InMemorySearchService service = new([Doc(1, "Library"), Doc(2, "Office")]);

        List<SearchResult> results = [.. service.Search("libr")];

        results.ShouldContain(r => r.Item.Id == 1);
    }

    [Fact]
    public void Search_FuzzyMatch_FindsDespiteTypo()
    {
        InMemorySearchService service = new([Doc(1, "Library"), Doc(2, "Office")]);

        List<SearchResult> results = [.. service.Search("librari")];

        results.ShouldContain(r => r.Item.Id == 1);
    }

    [Fact]
    public void Search_FuzzyDisabledWithoutPrefixOrContains_TypoReturnsEmpty()
    {
        InMemorySearchService service = new([Doc(1, "Library"), Doc(2, "Office")]);

        QueryOptions options = new(EnablePrefix: false, EnableFuzzy: false, EnableContains: false);
        List<SearchResult> results = [.. service.Search("librari", options)];

        results.ShouldBeEmpty();
    }

    [Fact]
    public void Search_ExactNameMatch_RanksHighest()
    {
        InMemorySearchService service = new(
        [
            Doc(1, "Parking House"),
            Doc(2, "Park")
        ]);

        List<SearchResult> results = [.. service.Search("park")];

        results[0].Item.Id.ShouldBe(2);
    }

    [Fact]
    public void Search_Browse_LargerAreaRanksAboveZeroArea()
    {
        InMemorySearchService service = new(
        [
            Doc(1, "Empty Plot", grossArea: 0m),
            Doc(2, "Big Estate", grossArea: 5000m)
        ]);

        List<SearchResult> results = [.. service.Search("")];

        results[0].Item.Id.ShouldBe(2);
    }

    [Fact]
    public void Search_Browse_MoreChildrenRanksHigher()
    {
        InMemorySearchService service = new(
        [
            Doc(1, "Leaf Estate", NodeType.Estate, numChildren: 0),
            Doc(2, "Parent Estate", NodeType.Estate, numChildren: 16)
        ]);

        List<SearchResult> results = [.. service.Search("")];

        results[0].Item.Id.ShouldBe(2);
    }

    [Fact]
    public void Search_TieScores_FallsBackToNameOrdering()
    {
        InMemorySearchService service = new(
        [
            Doc(1, "Bravo", NodeType.Building),
            Doc(2, "Alpha", NodeType.Building)
        ]);

        List<SearchResult> results = [.. service.Search("")];

        results[0].Item.Name.ShouldBe("Alpha");
    }

    [Fact]
    public void SearchWithDiagnostics_PopulatesMetadata()
    {
        InMemorySearchService service = new([Doc(1, "Library"), Doc(2, "Office")]);
        QueryOptions options = new(MaxResults: 7);

        (IEnumerable<SearchResult> results, SearchDiagnostics diagnostics) =
            service.SearchWithDiagnostics("library", options);

        results.ShouldNotBeEmpty();
        diagnostics.OriginalQuery.ShouldBe("library");
        diagnostics.QueryTokens.ShouldContain("library");
        diagnostics.TokenExpansions.ShouldContainKey("library");
        diagnostics.CandidateDocumentCount.ShouldBeGreaterThan(0);
        diagnostics.TopScoreBreakdowns.ShouldNotBeEmpty();
        diagnostics.AppliedOptions.MaxResults.ShouldBe(7);
        diagnostics.ElapsedMilliseconds.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void SearchWithDiagnostics_EmptyQuery_BrowsesAllAsCandidates()
    {
        InMemorySearchService service = new([Doc(1, "Alpha"), Doc(2, "Beta")]);

        (_, SearchDiagnostics diagnostics) = service.SearchWithDiagnostics("");

        diagnostics.QueryTokens.ShouldBeEmpty();
        diagnostics.CandidateDocumentCount.ShouldBe(service.DocumentCount);
    }

    [Fact]
    public void SearchWithDiagnostics_NoMatch_EmptyResultsWithZeroCandidates()
    {
        InMemorySearchService service = new([Doc(1, "Library")]);

        (IEnumerable<SearchResult> results, SearchDiagnostics diagnostics) =
            service.SearchWithDiagnostics("zzzzzz");

        results.ShouldBeEmpty();
        diagnostics.CandidateDocumentCount.ShouldBe(0);
    }

    [Fact]
    public void SearchWithDiagnostics_MultiTokenAddress_AwardsSameFieldBonus()
    {
        PythagorasDocument doc = Doc(
            1,
            "Branch",
            address: new AddressModel("Skolgatan 31", "901 84", "Umeå", string.Empty, string.Empty));
        InMemorySearchService service = new([doc]);

        (_, SearchDiagnostics diagnostics) = service.SearchWithDiagnostics("skolgatan 31");

        diagnostics.TopScoreBreakdowns.ShouldContain(b => b.DocId == 1);
        DocumentScoreBreakdown breakdown = diagnostics.TopScoreBreakdowns.First(b => b.DocId == 1);
        breakdown.SameFieldMultiTokenBonus.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void FromJsonFile_LoadsDocumentsAndSearches()
    {
        string path = Path.GetTempFileName();
        File.WriteAllText(path, "[{\"Id\":1,\"Type\":1,\"Name\":\"Library\",\"PopularName\":\"Central\"}]");

        try
        {
            InMemorySearchService service = InMemorySearchService.FromJsonFile(path);

            service.DocumentCount.ShouldBe(1);
            service.Search("library").ShouldContain(r => r.Item.Id == 1);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
