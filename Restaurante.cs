using System.Globalization;

namespace RestauranteConcorrente;

sealed record Opcoes(
    Modo Modo,
    int Cozinheiros,
    TimeSpan? Fechamento,
    bool Silencioso,
    IReadOnlyList<Prato>? PedidosFixos);

sealed class Restaurante
{
    public const int TotalPedidos = 60;
    public const int NumeroAtendentes = 2;
    public const int NumeroCozinheiros = 4;
    public const int CapacidadeFila = 10;
    public const int EstoqueInicial = 40;
    public static readonly TimeSpan FechamentoPadrao = TimeSpan.FromSeconds(15);

    static readonly CultureInfo PtBr = new("pt-BR");

    public void Executar(Opcoes opcoes)
    {
        var silenciosoAntes = Log.Silencioso;
        Log.Silencioso = opcoes.Silencioso;
        Log.Reiniciar();
        var tempo = Stopwatch.StartNew();

        try
        {
            Rodar(opcoes, tempo);
        }
        finally
        {
            Log.Silencioso = silenciosoAntes;
        }
    }

    void Rodar(Opcoes opcoes, Stopwatch tempo)
    {
        var fila = new BlockingCollection<Pedido>(CapacidadeFila);
        var estoque = new Estoque(EstoqueInicial);
        var cozinha = new Cozinha();
        var balcao = new Balcao();
        var caixa = new Caixa(protegerFaturamento: opcoes.Modo != Modo.RaceCaixa);
        var entregues = new ConcurrentBag<Pedido>();

        var recebidos = 0;
        var recusados = 0;
        var naoAtendidos = 0;
        var proximoNumero = 0;

        using var fechamento = opcoes.Fechamento is { } prazo
            ? new CancellationTokenSource(prazo)
            : null;
        var token = fechamento?.Token ?? CancellationToken.None;

        if (fechamento is not null)
        {
            fechamento.Token.Register(() =>
                Log.Escrever("Gerente", $"restaurante fechado após {opcoes.Fechamento!.Value.TotalSeconds:0} s"));
        }

        Pedido? CriarPedido(int atendenteId)
        {
            var numero = Interlocked.Increment(ref proximoNumero);
            if (numero > TotalPedidos)
                return null;

            Interlocked.Increment(ref recebidos);
            var prato = opcoes.PedidosFixos is null
                ? Cardapio.Aleatorio()
                : opcoes.PedidosFixos[numero - 1];
            return new Pedido(numero, prato, atendenteId);
        }

        var garcom = new Garcom(balcao, pedido => entregues.Add(pedido));
        var tarefaGarcom = Task.Run(garcom.Trabalhar);

        var cozinheiros = Enumerable.Range(1, opcoes.Cozinheiros)
            .Select(id => new Cozinheiro(
                id,
                fila,
                estoque,
                cozinha,
                balcao,
                caixa,
                opcoes.Modo,
                () => Interlocked.Increment(ref recusados),
                token))
            .Select(cozinheiro => Task.Run(cozinheiro.Trabalhar))
            .ToArray();

        var atendentes = Enumerable.Range(1, NumeroAtendentes)
            .Select(id => new Atendente(
                id,
                fila,
                CriarPedido,
                _ => Interlocked.Increment(ref naoAtendidos),
                token))
            .Select(atendente => Task.Run(atendente.Trabalhar))
            .ToArray();

        Task.WaitAll(atendentes);
        fila.CompleteAdding();
        Task.WaitAll(cozinheiros);

        while (fila.TryTake(out var pedido))
        {
            Interlocked.Increment(ref naoAtendidos);
            Log.Escrever("Gerente", $"{pedido} não atendido (fechamento)");
        }

        balcao.Encerrar();
        tarefaGarcom.Wait();
        tempo.Stop();

        if (!opcoes.Silencioso)
            ImprimirRelatorio(opcoes, recebidos, entregues, recusados, naoAtendidos, caixa, estoque, tempo.Elapsed);
    }

    static void ImprimirRelatorio(
        Opcoes opcoes,
        int recebidos,
        ConcurrentBag<Pedido> entregues,
        int recusados,
        int naoAtendidos,
        Caixa caixa,
        Estoque estoque,
        TimeSpan tempo)
    {
        var totalEntregues = entregues.Count;
        decimal esperado = 0;
        foreach (var pedido in entregues)
            esperado += pedido.Prato.Preco;

        var registrado = caixa.FaturamentoRegistrado;
        var vendas = string.Join(" | ", Cardapio.Pratos.Select(prato => $"{prato.Nome} {caixa.VendasDe(prato.Nome)}"));
        var estoqueFinal = string.Join(" | ", estoque.Snapshot().Select(item => $"{item.Nome} {item.Quantidade}"));
        var fechamento = opcoes.Fechamento is null
            ? "sem"
            : $"{opcoes.Fechamento.Value.TotalSeconds:0} s";

        Console.WriteLine();
        Console.WriteLine("===== RELATÓRIO · Restaurante Concorrente =====");
        Console.WriteLine($"Modo: {NomeModo(opcoes.Modo)} | Cozinheiros: {opcoes.Cozinheiros} | Fornos: {Cozinha.FornosMaximos} | Fechamento: {fechamento}");
        Console.WriteLine($"Pedidos recebidos ............ {recebidos,3}");
        Console.WriteLine($"Entregues .................... {totalEntregues,3}");
        Console.WriteLine($"Recusados (sem ingrediente) .. {recusados,3}");
        Console.WriteLine($"Não atendidos (fechamento) ... {naoAtendidos,3}");
        Console.WriteLine($"Vendas: {vendas}");
        Console.WriteLine($"Faturamento esperado ......... {esperado.ToString("C", PtBr)}");
        Console.WriteLine($"Faturamento registrado ....... {registrado.ToString("C", PtBr)}");
        Console.WriteLine($"Estoque final: {estoqueFinal}");

        var contasOk = recebidos == totalEntregues + recusados + naoAtendidos;
        var faturamentoOk = registrado == esperado;
        var estoqueOk = !estoque.TemNegativo();

        Console.WriteLine(contasOk
            ? "[OK] recebidos = entregues + recusados + não atendidos"
            : "[ERRO] recebidos = entregues + recusados + não atendidos");
        Console.WriteLine(faturamentoOk
            ? "[OK] faturamento registrado = faturamento esperado"
            : "[ERRO] faturamento registrado = faturamento esperado");
        Console.WriteLine(estoqueOk
            ? "[OK] nenhum ingrediente com estoque negativo"
            : "[ERRO] nenhum ingrediente com estoque negativo");
        Console.WriteLine($"Tempo total: {tempo.TotalSeconds.ToString("0.0", PtBr)} s");
    }

    static string NomeModo(Modo modo) => modo switch
    {
        Modo.Seguro => "SEGURO",
        Modo.RaceCaixa => "RACE NO CAIXA",
        Modo.DeadlockUtensilios => "DEADLOCK",
        Modo.EstoqueNegativo => "ESTOQUE NEGATIVO",
        _ => modo.ToString().ToUpperInvariant()
    };
}
