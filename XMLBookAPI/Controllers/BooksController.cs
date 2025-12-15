using Microsoft.AspNetCore.Mvc;
using System.Xml;
using XMLBookAPI.Models;
using XmlBooksApi.Models;

namespace XmlBooksApi.Controllers;

[ApiController]
[Route("books")]
public class BooksController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    public BooksController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpGet("valid")] 
    public async Task<IActionResult> GetValidBooksAsync(CancellationToken cancellationToken)
    {
        var response = new BookResponse();
        var filePath = Path.Combine(_env.ContentRootPath, "books.xml");

        if (!System.IO.File.Exists(filePath))
            return NotFound("books.xml NOT FOUND");

        var settings = new XmlReaderSettings
        {
            IgnoreWhitespace = true,
            IgnoreComments = true,
            Async = true
        };

        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            4096,
            useAsync: true);

        using var reader = XmlReader.Create(stream, settings);

        // Move to first book
        if (!reader.ReadToDescendant("book"))
            return Ok(response);

        do
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var bookReader = reader.ReadSubtree();
            await bookReader.ReadAsync();

            string? title = null, author = null, genre = null, yearText = null, publisher = null;

            while (await bookReader.ReadAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (bookReader.NodeType != XmlNodeType.Element)
                    continue;

                switch (bookReader.Name)
                {
                    case "title":
                        title = await bookReader.ReadElementContentAsStringAsync();
                        break;
                    case "author":
                        author = await bookReader.ReadElementContentAsStringAsync();
                        break;
                    case "genre":
                        genre = await bookReader.ReadElementContentAsStringAsync();
                        break;
                    case "year":
                        yearText = await bookReader.ReadElementContentAsStringAsync();
                        break;
                    case "publisher":
                        publisher = await bookReader.ReadElementContentAsStringAsync();
                        break;
                }
            }

            // Validation
            if (!int.TryParse(yearText, out var year) || year <= 0)
            {
                response.InvalidBooks.Add(new InvalidBook
                {
                    Title = title,
                    Reason = "Invalid year"
                });
                continue;
            }

            if (!Uri.TryCreate(publisher, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp &&
                 uri.Scheme != Uri.UriSchemeHttps))
            {
                response.InvalidBooks.Add(new InvalidBook
                {
                    Title = title,
                    Reason = "Invalid publisher"
                });
                continue;
            }

            response.ValidBooks.Add(new Book
            {
                Title = title,
                Author = author,
                Genre = genre,
                Year = year,
                Publisher = publisher
            });

        } while (reader.ReadToNextSibling("book"));

        return Ok(response);
    }

}
