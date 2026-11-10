namespace client_sql.Services;

public static class SeedData
{
    public static readonly (string Nome, string Categoria, double Preco, int Estoque, int Disponivel)[] Produtos =
    [
        ("Notebook Dell Inspiron 15", "Informática", 3499.90, 12, 1),
        ("Mouse Logitech G203", "Informática", 149.90, 45, 1),
        ("Teclado Mecânico Redragon", "Informática", 299.90, 8, 1),
        ("Monitor 24\" Full HD", "Informática", 899.00, 0, 1),
        ("Cadeira Ergonômica Office", "Móveis", 1299.00, 6, 1),
        ("Mesa de Escritório 1.20m", "Móveis", 549.00, 3, 0),
        ("Estante de Livros Madeira", "Móveis", 429.00, 10, 1),
        ("Dom Casmurro - Machado de Assis", "Livros", 39.90, 20, 1),
        ("Clean Code - Robert C. Martin", "Livros", 129.00, 5, 1),
        ("Harry Potter e a Pedra Filosofal", "Livros", 54.90, 15, 0),
        ("Controle DualSense PS5", "Games", 449.90, 7, 1),
        ("Jogo FIFA 24 - PS5", "Games", 249.00, 0, 1),
        ("Caneta Esferográfica Azul", "Papelaria", 2.50, 100, 1),
        ("Caderno Espiral 100 folhas", "Papelaria", 15.90, 30, 1),
        ("Grampeador de Mesa", "Papelaria", 24.90, 18, 1)
    ];
}
