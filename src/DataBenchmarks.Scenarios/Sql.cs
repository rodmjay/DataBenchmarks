namespace DataBenchmarks.Scenarios;

/// Hand-written SQL shared by the AdoNet, Dapper and EfRawSql approaches. Table and column names
/// are lower snake_case on every engine, so these statements are portable as written; anything
/// that is not portable goes through IEngine.Dialect.
public static class Sql
{
    public const string OrderById =
        "SELECT id, customer_id, ordered_at, status, total FROM orders WHERE id = @id";

    public const string OrdersWithLinesByCustomer =
        """
        SELECT o.id, o.customer_id, o.ordered_at, o.status, o.total,
               l.id, l.order_id, l.product_id, l.quantity, l.unit_price
        FROM orders o
        JOIN order_lines l ON l.order_id = o.id
        WHERE o.customer_id = @customerId
        ORDER BY o.id, l.id
        """;
}
