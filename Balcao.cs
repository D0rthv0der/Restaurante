namespace RestauranteConcorrente;

/// <summary>
/// Pratos prontos ficam numa fila concorrente. O sino (AutoResetEvent) acorda o garçom.
/// Vários Set seguidos viram um único sinal, então quem acorda precisa esvaziar o balcão.
/// </summary>
sealed class Balcao
{
    readonly ConcurrentQueue<Pedido> _prontos = new();
    readonly AutoResetEvent _sino = new(false);
    int _encerrado;

    public bool Encerrado => Volatile.Read(ref _encerrado) == 1;
    public bool Vazio => _prontos.IsEmpty;

    public void Colocar(Pedido pedido)
    {
        _prontos.Enqueue(pedido);
        _sino.Set();
    }

    public bool TentarRetirar(out Pedido? pedido) => _prontos.TryDequeue(out pedido);

    public void EsperarSino() => _sino.WaitOne();

    public void Encerrar()
    {
        Volatile.Write(ref _encerrado, 1);
        _sino.Set();
    }
}
