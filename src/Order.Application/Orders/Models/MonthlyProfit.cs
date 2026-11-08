namespace Order.Application.Orders.Models;

public class MonthlyProfit
{
    public int Year { get; set; }

    public int Month { get; set; }

    public decimal Profit { get; set; } // decimal is the best option for money
}
