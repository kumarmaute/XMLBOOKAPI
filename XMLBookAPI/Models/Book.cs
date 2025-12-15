namespace XmlBooksApi.Models;

public sealed class Book
{
    public string? Title { get; init; }
    public string? Author { get; init; }
    public string? Genre { get; init; }
    public int Year { get; init; }
    public string? Publisher { get; init; }
}
