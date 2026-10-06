namespace RestauranteConcorrente;

/// <summary>
/// Faturamento protegido por lock. Vendas por prato usam AddOrUpdate.
/// No modo inseguro, dois cozinheiros leem o mesmo valor, dormem 1 ms e gravam:
/// o último apaga a soma do outro.
/// </summary>
sealed class Caixa
{
    readonly object _faturamentoLock = new();
    readonly ConcurrentDictionary<string, int> _vendas = new(StringComparer.Ordinal);
    readonly bool _protegerFaturamento;
    decimal _faturamento;
    int _dentro;

    public Caixa(bool protegerFaturamento)
    {
        _protegerFaturamento = protegerFaturamento;
    }

    public decimal FaturamentoRegistrado => _faturamento;

    public int VendasDe(string prato) => _vendas.TryGetValue(prato, out var quantidade) ? quantidade : 0;

    public void RegistrarVenda(Prato prato)
    {
        if (_protegerFaturamento)
        {
            lock (_faturamentoLock)
                _faturamento += prato.Preco;
        }
        else
        {
            SomarSemLock(prato.Preco);
        }

        _vendas.AddOrUpdate(prato.Nome, 1, static (_, quantidade) => quantidade + 1);
    }

    void SomarSemLock(decimal preco)
    {
        var presentes = Interlocked.Increment(ref _dentro);
        var atual = _faturamento;
        try
        {
            if (presentes == 1)
            {
                var inicio = Environment.TickCount64;
                while (Volatile.Read(ref _dentro) < 2 && Environment.TickCount64 - inicio < 800)
                    Thread.Sleep(1);
            }

            Thread.Sleep(1);
            _faturamento = atual + preco;
        }
        finally
        {
            Interlocked.Decrement(ref _dentro);
        }
    }
}
