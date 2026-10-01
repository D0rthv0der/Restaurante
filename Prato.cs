namespace RestauranteConcorrente;

sealed class Prato
{
    public Prato(
        string nome,
        decimal preco,
        int tempoPreparoMs,
        bool usaForno,
        bool usaTabuaEFaca,
        IReadOnlyList<string> ingredientes)
    {
        Nome = nome;
        Preco = preco;
        TempoPreparoMs = tempoPreparoMs;
        UsaForno = usaForno;
        UsaTabuaEFaca = usaTabuaEFaca;
        Ingredientes = ingredientes;
    }

    public string Nome { get; }
    public decimal Preco { get; }
    public int TempoPreparoMs { get; }
    public bool UsaForno { get; }
    public bool UsaTabuaEFaca { get; }
    public IReadOnlyList<string> Ingredientes { get; }
}
