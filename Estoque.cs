namespace RestauranteConcorrente;

/// <summary>
/// Reserva todos os ingredientes do prato numa operação só.
/// TryUpdate é o compare-and-swap: só desconta se o valor ainda for o que lemos.
/// Se faltar algum, nada é descontado. Se outro cozinheiro ganhar a corrida,
/// devolvemos o que já saiu e tentamos de novo. O estoque nunca fica negativo.
/// </summary>
sealed class Estoque
{
    readonly ConcurrentDictionary<string, int> _itens = new(StringComparer.Ordinal);

    public Estoque(int quantidadeInicial)
    {
        foreach (var ingrediente in new[] { "massa", "queijo", "tomate", "carne", "alface", "pão" })
            _itens[ingrediente] = quantidadeInicial;
    }

    public bool TentarReservar(IReadOnlyList<string> ingredientes, out string? faltando)
    {
        var chaves = ingredientes
            .Distinct(StringComparer.Ordinal)
            .OrderBy(nome => nome, StringComparer.Ordinal)
            .ToArray();

        while (true)
        {
            var fotos = new int[chaves.Length];
            for (var i = 0; i < chaves.Length; i++)
            {
                if (!_itens.TryGetValue(chaves[i], out var quantidade) || quantidade <= 0)
                {
                    faltando = chaves[i];
                    return false;
                }

                fotos[i] = quantidade;
            }

            var reservados = 0;
            for (var i = 0; i < chaves.Length; i++)
            {
                if (_itens.TryUpdate(chaves[i], fotos[i] - 1, fotos[i]))
                {
                    reservados++;
                    continue;
                }

                for (var j = 0; j < reservados; j++)
                    _itens.AddOrUpdate(chaves[j], 1, (_, atual) => atual + 1);

                break;
            }

            if (reservados == chaves.Length)
            {
                faltando = null;
                return true;
            }
        }
    }

    public IReadOnlyList<(string Nome, int Quantidade)> Snapshot()
    {
        return _itens
            .OrderBy(par => par.Key, StringComparer.Ordinal)
            .Select(par => (par.Key, par.Value))
            .ToArray();
    }

    public bool TemNegativo() => _itens.Any(par => par.Value < 0);
}
