using XmlBooksApi.Models;

namespace XMLBookAPI.Models
{
    public sealed class BookResponse
    {
        public List<Book> ValidBooks { get; } = new();
        public List<InvalidBook> InvalidBooks { get; } = new();
    }
}
