using System.Globalization;
using RestauranteConcorrente;

Console.OutputEncoding = System.Text.Encoding.UTF8;

if (args.Length > 0)
{
    ExecutarOpcao(args[0]);
    return;
}

while (true)
{
    Console.WriteLine();
    Console.WriteLine("===== Restaurante Concorrente =====");
    Console.WriteLine("1. Modo seguro");
    Console.WriteLine("2. Bug: race condition no caixa");
    Console.WriteLine("3. Bug: deadlock nos utensílios");
    Console.WriteLine("4. Bug: estoque negativo");
    Console.WriteLine("5. Comparação 1 x 4 cozinheiros");
    Console.WriteLine("0. Sair");
    Console.Write("> ");

    switch (Console.ReadLine()?.Trim())
    {
        case "1":
            ExecutarOpcao("seguro");
            break;
        case "2":
            ExecutarOpcao("race");
            break;
        case "3":
            Console.WriteLine("O programa vai travar de propósito. Quando o log parar, encerre com Ctrl+C.");
            ExecutarOpcao("deadlock");
            break;
        case "4":
            ExecutarOpcao("estoque");
            break;
        case "5":
            Comparar();
            break;
        case "0":
            return;
        default:
            Console.WriteLine("Opção inválida.");
            break;
    }
}

static void ExecutarOpcao(string opcao)
{
    var restaurante = new Restaurante();
    switch (opcao)
    {
        case "seguro":
            restaurante.Executar(new Opcoes(Modo.Seguro, Restaurante.NumeroCozinheiros, Restaurante.FechamentoPadrao, false, null));
            break;
        case "race":
            restaurante.Executar(new Opcoes(Modo.RaceCaixa, Restaurante.NumeroCozinheiros, Restaurante.FechamentoPadrao, false, null));
            break;
        case "deadlock":
            restaurante.Executar(new Opcoes(Modo.DeadlockUtensilios, Restaurante.NumeroCozinheiros, null, false, null));
            break;
        case "estoque":
            restaurante.Executar(new Opcoes(Modo.EstoqueNegativo, Restaurante.NumeroCozinheiros, null, false, null));
            break;
        case "comparar":
            Comparar();
            break;
        default:
            Console.WriteLine("Use: seguro, race, deadlock, estoque ou comparar.");
            break;
    }
}

static void Comparar()
{
    var pedidos = Cardapio.Sortear(Restaurante.TotalPedidos, semente: 42);
    var cultura = new CultureInfo("pt-BR");
    Console.WriteLine();
    Console.WriteLine("Comparação · 60 pedidos, sem fechamento");

    var um = Medir(1, pedidos);
    var varios = Medir(Restaurante.NumeroCozinheiros, pedidos);

    Console.WriteLine($"1 cozinheiro ...... {um.TotalSeconds.ToString("0.0", cultura)} s");
    Console.WriteLine($"{Restaurante.NumeroCozinheiros} cozinheiros ..... {varios.TotalSeconds.ToString("0.0", cultura)} s");
}

static TimeSpan Medir(int cozinheiros, IReadOnlyList<Prato> pedidos)
{
    var tempo = Stopwatch.StartNew();
    new Restaurante().Executar(new Opcoes(Modo.Seguro, cozinheiros, null, true, pedidos));
    tempo.Stop();
    return tempo.Elapsed;
}
