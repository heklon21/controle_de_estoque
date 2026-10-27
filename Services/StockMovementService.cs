namespace ControleEstoque.Services;

public sealed class StockMovementService(StockInventory inventory)
{
    private readonly List<StockMovement> _movements = [];

    public IReadOnlyList<StockMovement> Movements => _movements;

    public WeeklyCmv CalculatePreviousWeekCmv(
        DateTime referenceDate,
        decimal? purchases,
        decimal? revenue)
    {
        var daysSinceTuesday = ((int)referenceDate.DayOfWeek - (int)DayOfWeek.Tuesday + 7) % 7;
        var thisWeekStart = referenceDate.Date.AddDays(-daysSinceTuesday);
        var periodStart = thisWeekStart.AddDays(-7);
        var periodEndExclusive = thisWeekStart;

        var movementChangesAfterPeriodStart = _movements
            .Where(movement => movement.OccurredAt >= periodStart)
            .Sum(GetSignedValue);
        var movementChangesAfterPeriodEnd = _movements
            .Where(movement => movement.OccurredAt >= periodEndExclusive)
            .Sum(GetSignedValue);
        var startingInventory = inventory.TotalValue - movementChangesAfterPeriodStart;
        var endingInventory = inventory.TotalValue - movementChangesAfterPeriodEnd;
        decimal? costOfGoodsSold = purchases is decimal purchaseValue
            ? startingInventory + purchaseValue - endingInventory
            : null;
        decimal? percentage = costOfGoodsSold is decimal cost && revenue is > 0
            ? cost / revenue.Value * 100
            : null;

        return new WeeklyCmv(
            periodStart,
            periodEndExclusive.AddDays(-1),
            startingInventory,
            purchases,
            endingInventory,
            revenue,
            costOfGoodsSold,
            percentage);
    }

    public bool TryCreate(
        DateTime occurredAt,
        bool isEntry,
        string reason,
        string? observation,
        IReadOnlyCollection<StockMovementLineDraft> lines,
        out string error)
    {
        if (lines.Count == 0)
        {
            error = "Adicione pelo menos um insumo à movimentação.";
            return false;
        }

        if (lines.Any(line => line.ItemId == Guid.Empty || line.Quantity <= 0))
        {
            error = "Selecione um insumo e informe uma quantidade maior que zero para cada linha.";
            return false;
        }

        var quantities = new Dictionary<Guid, long>();
        foreach (var line in lines)
        {
            quantities.TryGetValue(line.ItemId, out var currentQuantity);
            quantities[line.ItemId] = currentQuantity + line.Quantity;
        }

        if (!inventory.TryApplyMovement(quantities, isEntry, out error))
        {
            return false;
        }

        var movementLines = quantities
            .Select(entry =>
            {
                var item = inventory.Items.First(stockItem => stockItem.Id == entry.Key);
                return new StockMovementLine(
                    item.Id,
                    item.Name,
                    item.CategoryId,
                    item.Value,
                    (int)entry.Value);
            })
            .ToArray();

        _movements.Insert(
            0,
            new StockMovement(
                Guid.NewGuid(),
                occurredAt,
                isEntry,
                reason,
                observation?.Trim() ?? string.Empty,
                movementLines));

        error = string.Empty;
        return true;
    }

    private static decimal GetSignedValue(StockMovement movement) =>
        movement.IsEntry ? movement.TotalValue : -movement.TotalValue;
}

public sealed record WeeklyCmv(
    DateTime PeriodStart,
    DateTime PeriodEnd,
    decimal StartingInventory,
    decimal? Purchases,
    decimal EndingInventory,
    decimal? Revenue,
    decimal? CostOfGoodsSold,
    decimal? Percentage);

public sealed record StockMovement(
    Guid Id,
    DateTime OccurredAt,
    bool IsEntry,
    string Reason,
    string Observation,
    IReadOnlyList<StockMovementLine> Lines)
{
    public long TotalQuantity => Lines.Sum(line => (long)line.Quantity);

    public decimal TotalValue => Lines.Sum(line => line.UnitValue * line.Quantity);
}

public sealed record StockMovementLine(
    Guid ItemId,
    string ItemName,
    Guid CategoryId,
    decimal UnitValue,
    int Quantity);

public sealed record StockMovementLineDraft(Guid ItemId, int Quantity);
