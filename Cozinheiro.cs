namespace RestauranteConcorrente;

sealed class Cozinheiro
{
    readonly int _id;
    readonly BlockingCollection<Pedido> _fila;
    readonly Estoque _estoque;
    readonly Cozinha _cozinha;
    readonly Balcao _balcao;
    readonly Caixa _caixa;
    readonly Modo _modo;
    readonly Action _aoRecusar;
    readonly CancellationToken _cancelamento;

    public Cozinheiro(
        int id,
        BlockingCollection<Pedido> fila,
        Estoque estoque,
        Cozinha cozinha,
        Balcao balcao,
        Caixa caixa,
        Modo modo,
        Action aoRecusar,
        CancellationToken cancelamento)
    {
        _id = id;
        _fila = fila;
        _estoque = estoque;
        _cozinha = cozinha;
        _balcao = balcao;
        _caixa = caixa;
        _modo = modo;
        _aoRecusar = aoRecusar;
        _cancelamento = cancelamento;
    }

    string Nome => $"Cozinheiro {_id}";

    public void Trabalhar()
    {
        Log.Escrever(Nome, "começou o expediente");

        while (!_cancelamento.IsCancellationRequested)
        {
            if (!_fila.TryTake(out var pedido, 100))
            {
                if (_fila.IsCompleted)
                    break;
                continue;
            }

            Log.Escrever(Nome, $"{pedido} retirado da fila");
            Preparar(pedido);
        }

        Log.Escrever(Nome, "encerrou o expediente");
    }

    void Preparar(Pedido pedido)
    {
        string? faltando;
        var reservou = _modo == Modo.EstoqueNegativo
            ? _estoque.TentarReservarInseguro(pedido.Prato.Ingredientes, out faltando)
            : _estoque.TentarReservar(pedido.Prato.Ingredientes, out faltando);

        if (!reservou)
        {
            Log.Escrever(Nome, $"{pedido} recusado (sem ingrediente: {faltando})");
            _aoRecusar();
            return;
        }

        Log.Escrever(Nome, $"{pedido} ingredientes reservados ({string.Join(", ", pedido.Prato.Ingredientes)})");

        void Cozinhar() => Thread.Sleep(pedido.Prato.TempoPreparoMs);

        if (pedido.Prato.UsaForno)
        {
            _cozinha.Assar(
                Cozinhar,
                ocupacao => Log.Escrever(Nome, $"{pedido} entrou no forno ({ocupacao}/{Cozinha.FornosMaximos})"),
                restante => Log.Escrever(Nome, $"{pedido} saiu do forno ({restante}/{Cozinha.FornosMaximos})"));
        }
        else if (pedido.Prato.UsaTabuaEFaca && _modo == Modo.DeadlockUtensilios)
        {
            var hamburguer = pedido.Prato.Nome == "Hambúrguer";
            _cozinha.UsarTabuaEFacaInseguro(
                hamburguer,
                Cozinhar,
                () => Log.Escrever(Nome, hamburguer
                    ? $"{pedido} pegou a faca, esperando a tábua"
                    : $"{pedido} pegou a tábua, esperando a faca"),
                () => Log.Escrever(Nome, $"{pedido} pegou tábua e faca"),
                () => Log.Escrever(Nome, $"{pedido} liberou tábua e faca"));
        }
        else if (pedido.Prato.UsaTabuaEFaca)
        {
            _cozinha.UsarTabuaEFaca(
                Cozinhar,
                () => Log.Escrever(Nome, $"{pedido} pegou tábua e faca"),
                () => Log.Escrever(Nome, $"{pedido} liberou tábua e faca"));
        }
        else
        {
            Cozinhar();
        }

        _caixa.RegistrarVenda(pedido.Prato);
        _balcao.Colocar(pedido);
        Log.Escrever(Nome, $"{pedido} colocado no balcão");
    }
}
