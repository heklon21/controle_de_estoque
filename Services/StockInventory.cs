namespace ControleEstoque.Services;

public sealed class StockInventory
{
    private readonly List<StockItem> _items = [];
    private readonly List<StockCategory> _categories = [];

    public IReadOnlyList<StockItem> Items => _items;

    public IReadOnlyList<StockCategory> Categories => _categories;

    public decimal TotalValue => _items.Sum(item => item.Value * item.Quantity);

    public long TotalQuantity => _items.Sum(item => (long)item.Quantity);

    public bool AddCategory(string name)
    {
        var normalizedName = name.Trim();
        if (_categories.Any(category =>
                string.Equals(category.Name, normalizedName, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        _categories.Add(new StockCategory(Guid.NewGuid(), normalizedName));
        return true;
    }

    public void Add(string name, Guid categoryId, decimal value)
    {
        var category = _categories.FirstOrDefault(item => item.Id == categoryId)
            ?? throw new InvalidOperationException("A categoria selecionada não está cadastrada.");

        _items.Add(new StockItem(Guid.NewGuid(), name, category.Id, category.Name, value, 0));
    }

    public bool TryApplyMovement(IReadOnlyDictionary<Guid, long> quantities, bool isEntry, out string error)
    {
        var updates = new List<(int Index, int Quantity)>();

        foreach (var (itemId, amount) in quantities)
        {
            var index = _items.FindIndex(item => item.Id == itemId);
            if (index < 0)
            {
                error = "Um dos insumos selecionados não está mais cadastrado.";
                return false;
            }

            if (amount <= 0)
            {
                error = "A quantidade movimentada deve ser maior que zero.";
                return false;
            }

            var currentQuantity = _items[index].Quantity;
            var updatedQuantity = isEntry ? currentQuantity + amount : currentQuantity - amount;
            if (updatedQuantity < 0)
            {
                error = $"Estoque insuficiente para o insumo \"{_items[index].Name}\".";
                return false;
            }

            if (updatedQuantity > int.MaxValue)
            {
                error = $"A quantidade do insumo \"{_items[index].Name}\" excede o limite permitido.";
                return false;
            }

            updates.Add((index, (int)updatedQuantity));
        }

        foreach (var (index, quantity) in updates)
        {
            _items[index] = _items[index] with { Quantity = quantity };
        }

        error = string.Empty;
        return true;
    }

    public bool Remove(Guid id) =>
        _items.RemoveAll(item => item.Id == id) > 0;
}

public sealed record StockCategory(Guid Id, string Name);

public sealed record StockItem(Guid Id, string Name, Guid CategoryId, string CategoryName, decimal Value, int Quantity);
