using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Logic.Handlers.WorkOrder;

public class WorkOrderCategoryClassifier(
    WorkOrderCategoryProvider categoryProvider,
    IChatClient chatClient,
    ILogger<WorkOrderCategoryClassifier> logger) : IWorkOrderCategoryClassifier
{
    private const string SystemPrompt =
        """
        Du är en kategoriserare för felanmälningar och arbetsordrar inom svensk fastighetsförvaltning.
        Du får en lista med tillgängliga kategorier och en beskrivning av ett ärende.
        Välj de mest relevanta kategorierna och rangordna dem efter hur väl de matchar beskrivningen.
        Returnera ENBART bladkategorier (kategorier utan underkategorier).
        Svara med en JSON-array av objekt med CategoryId, CategoryName och Confidence (0.0–1.0).
        """;

    public async Task<IReadOnlyList<WorkOrderCategorySuggestion>> ClassifyAsync(
        string description, int workOrderTypeId, CancellationToken ct = default)
    {
        IReadOnlyList<WorkOrderCategoryOption> leaves = categoryProvider.GetLeafCategoriesForType(workOrderTypeId);
        if (leaves.Count == 0)
        {
            logger.LogWarning("No categories found for work order type {WorkOrderTypeId}", workOrderTypeId);
            return [];
        }

        string categoryTree = string.Join('\n', leaves.Select(leaf => $"- {leaf.Name} (id: {leaf.Id})"));
        string userMessage = $"""
            Tillgängliga kategorier:
            {categoryTree}

            Beskrivning av ärendet:
            {description}
            """;

        logger.LogDebug("Classifying work order with {CategoryCount} available categories", leaves.Count);

        ChatResponse<List<WorkOrderCategorySuggestion>> result = await chatClient.GetResponseAsync<List<WorkOrderCategorySuggestion>>(
            [
                new ChatMessage(ChatRole.System, SystemPrompt),
                new ChatMessage(ChatRole.User, userMessage)
            ],
            cancellationToken: ct);

        List<WorkOrderCategorySuggestion> suggestions = result.Result ?? [];

        return [.. suggestions.OrderByDescending(s => s.Confidence)];
    }
}
