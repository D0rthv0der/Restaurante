namespace RestauranteConcorrente;

sealed class Garcom
{
    readonly Balcao _balcao;
    readonly Action<Pedido> _aoEntregar;

    public Garcom(Balcao balcao, Action<Pedido> aoEntregar)
    {
        _balcao = balcao;
        _aoEntregar = aoEntregar;
    }

    public void Trabalhar()
    {
        Log.Escrever("Garçom", "a postos, esperando o sino");

        while (true)
        {
            _balcao.EsperarSino();

            while (_balcao.TentarRetirar(out var pedido))
            {
                Log.Escrever("Garçom", $"{pedido} entregue");
                _aoEntregar(pedido!);
            }

            if (_balcao.Encerrado && _balcao.Vazio)
                break;
        }

        Log.Escrever("Garçom", "expediente encerrado");
    }
}
