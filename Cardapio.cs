namespace RestauranteConcorrente;

static class Cardapio
{
    public static IReadOnlyList<Prato> Pratos { get; } =
    [
        new("Pizza", 45.00m, 800, usaForno: true, usaTabuaEFaca: false, ["massa", "queijo", "tomate"]),
        new("Lasanha", 38.00m, 1000, usaForno: true, usaTabuaEFaca: false, ["massa", "queijo", "carne"]),
        new("Salada", 22.00m, 400, usaForno: false, usaTabuaEFaca: true, ["alface", "tomate"]),
        new("Hambúrguer", 30.00m, 500, usaForno: false, usaTabuaEFaca: true, ["pão", "carne", "queijo"]),
    ];

    public static Prato Aleatorio() => Pratos[Random.Shared.Next(Pratos.Count)];

    public static IReadOnlyList<Prato> Sortear(int quantidade, int semente)
    {
        var aleatorio = new Random(semente);
        var pedidos = new Prato[quantidade];
        for (var i = 0; i < quantidade; i++)
            pedidos[i] = Pratos[aleatorio.Next(Pratos.Count)];
        return pedidos;
    }
}
