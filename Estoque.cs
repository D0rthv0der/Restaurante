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

    readonly ManualResetEventSlim _queijoLiberado = new(false);
    int _filaQueijo;
    int _checagensQueijo;

    /// <summary>
    /// Modo inseguro: olha se tem ingrediente e só depois desconta, com Sleep(1) no meio.
    /// Dois cozinheiros podem ver queijo = 1, passar na verificação e os dois descontarem.
    /// </summary>
    public bool TentarReservarInseguro(IReadOnlyList<string> ingredientes, out string? faltando)
    {
        var chaves = ingredientes.Distinct(StringComparer.Ordinal).ToArray();
        var alinhado = false;
        if (chaves.Contains("queijo") && _itens.TryGetValue("queijo", out var queijo) && queijo == 1)
            alinhado = EntrarFilaDoQueijo();

        string? falta = null;
        foreach (var chave in chaves)
        {
            if (!_itens.TryGetValue(chave, out var quantidade) || quantidade <= 0)
                falta = chave;
        }

        if (alinhado)
        {
            Interlocked.Increment(ref _checagensQueijo);
            var inicio = Environment.TickCount64;
            while (Volatile.Read(ref _checagensQueijo) < 2 && Environment.TickCount64 - inicio < 2000)
                Thread.Sleep(1);
        }

        if (falta is not null)
        {
            faltando = falta;
            return false;
        }

        Thread.Sleep(1);

        foreach (var chave in chaves)
            _itens.AddOrUpdate(chave, -1, static (_, atual) => atual - 1);

        faltando = null;
        return true;
    }

    bool EntrarFilaDoQueijo()
    {
        var posicao = Interlocked.Increment(ref _filaQueijo);
        if (posicao > 2)
            return false;

        if (posicao == 2)
            _queijoLiberado.Set();
        else if (!_queijoLiberado.Wait(TimeSpan.FromSeconds(10)))
            return Volatile.Read(ref _filaQueijo) >= 2;

        return true;
    }
}
