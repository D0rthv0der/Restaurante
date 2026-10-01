namespace RestauranteConcorrente;

sealed class Restaurante
{
    public const int TotalPedidos = 60;
    public const int NumeroAtendentes = 2;
    public const int NumeroCozinheiros = 4;
    public const int CapacidadeFila = 10;
    public const int EstoqueInicial = 40;

    public void Executar()
    {
        var fila = new BlockingCollection<Pedido>(CapacidadeFila);
        var estoque = new Estoque(EstoqueInicial);
        var cozinha = new Cozinha();
        var balcao = new Balcao();

        var recebidos = 0;
        var recusados = 0;
        var entregues = 0;
        var proximoNumero = 0;

        Pedido? CriarPedido(int atendenteId)
        {
            var numero = Interlocked.Increment(ref proximoNumero);
            if (numero > TotalPedidos)
                return null;

            Interlocked.Increment(ref recebidos);
            return new Pedido(numero, Cardapio.Aleatorio(), atendenteId);
        }

        var garcom = new Garcom(balcao, () => Interlocked.Increment(ref entregues));
        var tarefaGarcom = Task.Run(garcom.Trabalhar);

        var cozinheiros = Enumerable.Range(1, NumeroCozinheiros)
            .Select(id => new Cozinheiro(
                id,
                fila,
                estoque,
                cozinha,
                balcao,
                () => Interlocked.Increment(ref recusados)))
            .Select(cozinheiro => Task.Run(cozinheiro.Trabalhar))
            .ToArray();

        var atendentes = Enumerable.Range(1, NumeroAtendentes)
            .Select(id => new Atendente(id, fila, CriarPedido))
            .Select(atendente => Task.Run(atendente.Trabalhar))
            .ToArray();

        Task.WaitAll(atendentes);
        fila.CompleteAdding();

        Task.WaitAll(cozinheiros);
        balcao.Encerrar();
        tarefaGarcom.Wait();

        ImprimirResumo(recebidos, entregues, recusados, estoque);
    }

    static void ImprimirResumo(int recebidos, int entregues, int recusados, Estoque estoque)
    {
        Console.WriteLine();
        Console.WriteLine("===== RESUMO · Entrega 2 · Recursos compartilhados =====");
        Console.WriteLine($"Pedidos recebidos ............ {recebidos}");
        Console.WriteLine($"Entregues .................... {entregues}");
        Console.WriteLine($"Recusados (sem ingrediente) .. {recusados}");

        var estoqueFinal = string.Join(" | ", estoque.Snapshot().Select(item => $"{item.Nome} {item.Quantidade}"));
        Console.WriteLine($"Estoque final: {estoqueFinal}");

        var fechouContas = recebidos == entregues + recusados;
        Console.WriteLine(fechouContas
            ? "[OK] recebidos = entregues + recusados"
            : "[ERRO] recebidos = entregues + recusados");

        Console.WriteLine(estoque.TemNegativo()
            ? "[ERRO] nenhum ingrediente com estoque negativo"
            : "[OK] nenhum ingrediente com estoque negativo");
    }
}
