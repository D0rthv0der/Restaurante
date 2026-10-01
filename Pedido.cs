namespace RestauranteConcorrente;

sealed class Pedido
{
    public Pedido(int numero, Prato prato, int atendenteId)
    {
        Numero = numero;
        Prato = prato;
        AtendenteId = atendenteId;
    }

    public int Numero { get; }
    public Prato Prato { get; }
    public int AtendenteId { get; }

    public override string ToString() => $"{Prato.Nome} #{Numero}";
}
