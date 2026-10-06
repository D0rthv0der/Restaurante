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

    int _saladasNoPar;
    int _hamburgueresNoPar;

    /// <summary>
    /// Modo inseguro: Salada pega tábua e depois faca; Hambúrguer pega faca e depois tábua.
    /// O Sleep(100) entre os dois locks faz os cozinheiros se cruzarem e travarem.
    /// </summary>
    public void UsarTabuaEFacaInseguro(
        bool hamburguer,
        Action preparar,
        Action aoPegarPrimeiro,
        Action aoPegarSegundo,
        Action aoLiberar)
    {
        AlinharParOposto(hamburguer);

        if (!hamburguer)
        {
            lock (_tabua)
            {
                aoPegarPrimeiro();
                Thread.Sleep(100);
                lock (_faca)
                {
                    try
                    {
                        aoPegarSegundo();
                        preparar();
                    }
                    finally
                    {
                        aoLiberar();
                    }
                }
            }

            return;
        }

        lock (_faca)
        {
            aoPegarPrimeiro();
            Thread.Sleep(100);
            lock (_tabua)
            {
                try
                {
                    aoPegarSegundo();
                    preparar();
                }
                finally
                {
                    aoLiberar();
                }
            }
        }
    }

    void AlinharParOposto(bool hamburguer)
    {
        var ordem = hamburguer
            ? Interlocked.Increment(ref _hamburgueresNoPar)
            : Interlocked.Increment(ref _saladasNoPar);

        if (ordem != 1)
            return;

        var inicio = Environment.TickCount64;
        while (Volatile.Read(ref _saladasNoPar) == 0 || Volatile.Read(ref _hamburgueresNoPar) == 0)
        {
            if (Environment.TickCount64 - inicio > 20000)
                return;
            Thread.Sleep(5);
        }
    }
}
