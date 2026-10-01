namespace RestauranteConcorrente;

sealed class Atendente
{
    readonly int _id;
    readonly BlockingCollection<Pedido> _fila;
    readonly Func<int, Pedido?> _criarPedido;

    public Atendente(int id, BlockingCollection<Pedido> fila, Func<int, Pedido?> criarPedido)
    {
        _id = id;
        _fila = fila;
        _criarPedido = criarPedido;
    }

    string Nome => $"Atendente {_id}";

    public void Trabalhar()
    {
        Log.Escrever(Nome, "começou o expediente");

        while (true)
        {
            var pedido = _criarPedido(_id);
            if (pedido is null)
                break;

            if (!_fila.TryAdd(pedido, 0))
            {
                Log.Escrever(Nome, $"fila cheia ({_fila.BoundedCapacity}/{_fila.BoundedCapacity}), esperando para enfileirar {pedido}");
                _fila.Add(pedido);
            }

            Log.Escrever(Nome, $"{pedido} entrou na fila");
            Thread.Sleep(Random.Shared.Next(20, 80));
        }

        Log.Escrever(Nome, "parou de anotar pedidos");
    }
}
