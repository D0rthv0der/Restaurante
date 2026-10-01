namespace RestauranteConcorrente;

sealed class Cozinheiro
{
    readonly int _id;
    readonly BlockingCollection<Pedido> _fila;
    readonly Estoque _estoque;
    readonly Cozinha _cozinha;
    readonly Balcao _balcao;
    readonly Action _aoRecusar;

    public Cozinheiro(
        int id,
        BlockingCollection<Pedido> fila,
        Estoque estoque,
        Cozinha cozinha,
        Balcao balcao,
        Action aoRecusar)
    {
        _id = id;
        _fila = fila;
        _estoque = estoque;
        _cozinha = cozinha;
        _balcao = balcao;
        _aoRecusar = aoRecusar;
    }

    string Nome => $"Cozinheiro {_id}";

    public void Trabalhar()
    {
        Log.Escrever(Nome, "começou o expediente");

        foreach (var pedido in _fila.GetConsumingEnumerable())
        {
            Log.Escrever(Nome, $"{pedido} retirado da fila");

            if (!_estoque.TentarReservar(pedido.Prato.Ingredientes, out var faltando))
            {
                Log.Escrever(Nome, $"{pedido} recusado (sem ingrediente: {faltando})");
                _aoRecusar();
                continue;
            }

            Log.Escrever(Nome, $"{pedido} ingredientes reservados ({string.Join(", ", pedido.Prato.Ingredientes)})");

            void Preparar() => Thread.Sleep(pedido.Prato.TempoPreparoMs);

            if (pedido.Prato.UsaForno)
            {
                _cozinha.Assar(
                    Preparar,
                    ocupacao => Log.Escrever(Nome, $"{pedido} entrou no forno ({ocupacao}/{Cozinha.FornosMaximos})"),
                    restante => Log.Escrever(Nome, $"{pedido} saiu do forno ({restante}/{Cozinha.FornosMaximos})"));
            }
            else if (pedido.Prato.UsaTabuaEFaca)
            {
                _cozinha.UsarTabuaEFaca(
                    Preparar,
                    () => Log.Escrever(Nome, $"{pedido} pegou tábua e faca"),
                    () => Log.Escrever(Nome, $"{pedido} liberou tábua e faca"));
            }
            else
            {
                Preparar();
            }

            _balcao.Colocar(pedido);
            Log.Escrever(Nome, $"{pedido} colocado no balcão");
        }

        Log.Escrever(Nome, "encerrou o expediente");
    }
}
