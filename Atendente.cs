namespace RestauranteConcorrente;

sealed class Atendente
{
    readonly int _id;
    readonly BlockingCollection<Pedido> _fila;
    readonly Func<int, Pedido?> _criarPedido;
    readonly Action<Pedido> _aoNaoAtender;
    readonly CancellationToken _cancelamento;

    public Atendente(
        int id,
        BlockingCollection<Pedido> fila,
        Func<int, Pedido?> criarPedido,
        Action<Pedido> aoNaoAtender,
        CancellationToken cancelamento)
    {
        _id = id;
        _fila = fila;
        _criarPedido = criarPedido;
        _aoNaoAtender = aoNaoAtender;
        _cancelamento = cancelamento;
    }

    string Nome => $"Atendente {_id}";

    public void Trabalhar()
    {
        Log.Escrever(Nome, "começou o expediente");

        while (!_cancelamento.IsCancellationRequested)
        {
            var pedido = _criarPedido(_id);
            if (pedido is null)
                break;

            if (!Enfileirar(pedido))
            {
                Log.Escrever(Nome, $"{pedido} não atendido (fechamento)");
                _aoNaoAtender(pedido);
                break;
            }

            Log.Escrever(Nome, $"{pedido} entrou na fila");
            Thread.Sleep(Random.Shared.Next(20, 80));
        }

        Log.Escrever(Nome, "parou de anotar pedidos");
    }

    bool Enfileirar(Pedido pedido)
    {
        if (_fila.TryAdd(pedido, 0))
            return true;

        Log.Escrever(Nome, $"fila cheia ({_fila.BoundedCapacity}/{_fila.BoundedCapacity}), esperando para enfileirar {pedido}");

        while (!_cancelamento.IsCancellationRequested)
        {
            if (_fila.TryAdd(pedido, 50))
                return true;
        }

        return false;
    }
}
