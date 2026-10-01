namespace RestauranteConcorrente;

static class Log
{
    static readonly Stopwatch Relogio = Stopwatch.StartNew();

    public static void Escrever(string quem, string mensagem)
    {
        var t = Relogio.Elapsed;
        Console.WriteLine($"[{t:mm\\:ss\\.fff}] {quem} · {mensagem}");
    }
}
