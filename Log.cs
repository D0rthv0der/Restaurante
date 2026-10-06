namespace RestauranteConcorrente;

static class Log
{
    static Stopwatch Relogio = Stopwatch.StartNew();

    public static bool Silencioso { get; set; }

    public static void Reiniciar() => Relogio = Stopwatch.StartNew();

    public static void Escrever(string quem, string mensagem)
    {
        if (Silencioso)
            return;

        var t = Relogio.Elapsed;
        Console.WriteLine($"[{t:mm\\:ss\\.fff}] {quem} · {mensagem}");
    }
}
