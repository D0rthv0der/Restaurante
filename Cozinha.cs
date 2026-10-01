namespace RestauranteConcorrente;

/// <summary>
/// Fornos: no máximo 2 pratos ao mesmo tempo (SemaphoreSlim).
/// Tábua e faca: um lock cada, sempre pegos na ordem tábua e depois faca.
/// </summary>
sealed class Cozinha
{
    public const int FornosMaximos = 2;

    readonly SemaphoreSlim _fornos = new(FornosMaximos, FornosMaximos);
    int _fornosEmUso;

    readonly object _tabua = new();
    readonly object _faca = new();

    public void Assar(Action preparar, Action<int> aoEntrar, Action<int> aoSair)
    {
        _fornos.Wait();
        var ocupacao = Interlocked.Increment(ref _fornosEmUso);
        try
        {
            aoEntrar(ocupacao);
            preparar();
        }
        finally
        {
            var restante = Interlocked.Decrement(ref _fornosEmUso);
            aoSair(restante);
            _fornos.Release();
        }
    }

    public void UsarTabuaEFaca(Action preparar, Action aoPegar, Action aoLiberar)
    {
        lock (_tabua)
        {
            lock (_faca)
            {
                try
                {
                    aoPegar();
                    preparar();
                }
                finally
                {
                    aoLiberar();
                }
            }
        }
    }
}
