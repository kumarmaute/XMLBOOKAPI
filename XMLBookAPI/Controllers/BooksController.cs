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
    public async Task<IActionResult> GetValidBooksAsync()
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

        string? title = null, author = null, genre = null, yearText = null, publisher = null;

        while (await reader.ReadAsync())
        {
            if (reader.NodeType == XmlNodeType.Element && reader.Name == "book")
            {
                title = author = genre = yearText = publisher = null;

                while (await reader.ReadAsync())
                {
                    if (reader.NodeType == XmlNodeType.EndElement &&
                        reader.Name == "book")
                        break;

                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        var elementName = reader.Name;

                        await reader.ReadAsync(); // move to Text node

                        if (reader.NodeType != XmlNodeType.Text)
                            continue;

                        var value = reader.Value;

                        switch (elementName)
                        {
                            case "title": title = value; break;
                            case "author": author = value; break;
                            case "genre": genre = value; break;
                            case "year": yearText = value; break;
                            case "publisher": publisher = value; break;
                        }
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
                    Title = title!,
                    Author = author!,
                    Genre = genre!,
                    Year = year,
                    Publisher = publisher!
                });
            }
        }

        return Ok(response);
    }
}
