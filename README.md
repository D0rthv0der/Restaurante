# Restaurante Concorrente

Simulação de um restaurante em C# (.NET 10): atendentes geram pedidos, cozinheiros preparam ao mesmo tempo e um garçom entrega os pratos. Vários cozinheiros disputam fornos, tábua, faca e ingredientes.

## Integrantes

- [D0rthv0der](https://github.com/D0rthv0der)

## Como executar

É preciso ter o [.NET 10 SDK](https://dotnet.microsoft.com/download) instalado.

```bash
dotnet run
```

O menu abre no console. Para ir direto a uma opção:

```bash
dotnet run -- seguro
dotnet run -- race
dotnet run -- deadlock
dotnet run -- estoque
dotnet run -- comparar
```

## O que cada opção do menu faz

1. **Modo seguro.** Dois atendentes geram 60 pedidos. A fila segura no máximo 10. Quatro cozinheiros preparam ao mesmo tempo, com no máximo 2 pratos no forno. Tábua e faca são pegas sempre na ordem tábua e depois faca. O estoque reserva todos os ingredientes do prato de uma vez. O caixa soma o faturamento com `lock` e conta as vendas com `AddOrUpdate`. Depois de 15 segundos o gerente fecha: os atendentes param, cada cozinheiro termina o prato atual e o que sobrou na fila fica como não atendido. O relatório precisa mostrar os três `[OK]`.

2. **Bug: race condition no caixa.** Igual ao modo seguro, mas o faturamento é somado sem `lock`. Dois cozinheiros leem o mesmo valor, esperam 1 ms e gravam: o último apaga a soma do outro. O relatório mostra `[ERRO]` em `faturamento registrado = faturamento esperado`.

3. **Bug: deadlock nos utensílios.** A salada pega a tábua e depois a faca. O hambúrguer pega a faca e depois a tábua, com 100 ms entre os dois locks. O log para em `esperando a tábua` e `esperando a faca`, e o relatório não aparece. Encerre com Ctrl+C.

4. **Bug: estoque negativo.** O cozinheiro verifica se há ingrediente e só depois desconta, em dois passos. Dois cozinheiros podem ver que ainda existe 1 queijo e os dois descontarem. O estoque final fica negativo e o relatório mostra `[ERRO]` em `nenhum ingrediente com estoque negativo`.

5. **Comparação 1 x 4 cozinheiros.** Roda os mesmos 60 pedidos, sem fechamento, primeiro com 1 cozinheiro e depois com 4. Mostra os dois tempos. Com 4 cozinheiros o preparo termina antes, limitado pelos 2 fornos e pelo par tábua/faca.

Ordem sugerida na apresentação: opção 1, opção 2, opção 4, opção 3 (Ctrl+C) e opção 5. A comparação demora cerca de 1 minuto.

## Ferramentas de concorrência

| Ferramenta | Onde | Para que serve |
|---|---|---|
| `Task` | Atendentes, cozinheiros e garçom | Várias pessoas trabalhando ao mesmo tempo |
| `BlockingCollection` | Fila de pedidos, capacidade 10 | O atendente espera quando a fila enche |
| `SemaphoreSlim(2)` | Fornos | No máximo dois pratos assando |
| `lock` | Tábua, faca e faturamento | Um cozinheiro por utensílio; soma do caixa sem corrida |
| `ConcurrentDictionary` + `TryUpdate` | Estoque | Reservar os ingredientes numa operação atômica |
| `AddOrUpdate` | Vendas por prato | Contar vendas sem perder atualização |
| `ConcurrentQueue` | Balcão | Vários cozinheiros colocam pratos prontos ao mesmo tempo |
| `AutoResetEvent` | Sino | Acorda o garçom. Vários toques viram um só sinal, então ele esvazia o balcão inteiro |
| `CancellationTokenSource` | Gerente | Fecha o restaurante depois de 15 segundos |

## Ferramentas de IA

O código foi gerado com o Cursor. A apresentação e a explicação de por que cada ferramenta foi usada ficam com a dupla.
