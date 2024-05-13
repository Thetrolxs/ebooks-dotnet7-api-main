using ebooks_dotnet7_api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<DataContext>(opt => opt.UseInMemoryDatabase("ebooks"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
var app = builder.Build();

var ebooks = app.MapGroup("api/ebook");

// TODO: Add more routes
ebooks.MapPost("/", CreateEBookAsync);
ebooks.MapGet("?genre={genre}&author={author}&format={format}", GetAllBooksAvailability);
ebooks.MapPut("/{id}", updateeBook);
ebooks.MapPut("/{id}/change-availability", ChangeAvailability);
ebooks.MapPut("/{id}/increment-stock", UpdateStock);
ebooks.MapPost("/purchase", PurchaseEbook);
ebooks.MapDelete("/{id}", DeleteEbook);

app.Run();

// TODO: Add more methods
async Task<IResult> CreateEBookAsync(EBook eBook, DataContext context)
{
    var ebook = await context.EBooks.FirstOrDefaultAsync(e=> e.Title == eBook.Title & e.Author == eBook.Author);
    if(ebook == eBook) return TypedResults.BadRequest("El libro ya existe");

    eBook.IsAvailable = true;
    eBook.Stock = 0;
    
    context.EBooks.Add(eBook);
    await context.SaveChangesAsync();

    return TypedResults.Created($"/api/ebook/{eBook.Id}",ebook);
}

//obtener todos los libros electronicos posibles
async Task<IResult> GetAllBooksAvailability(DataContext context)
{
    return TypedResults.Ok(await context.EBooks.Where(e => e.IsAvailable).ToListAsync());
}

//actualizar libro electronico
async Task<IResult> updateeBook(int id,EBook eBookInput, DataContext context)
{
    var existBook = await context.EBooks.FindAsync(id);
    if(existBook == null)
    {
        return TypedResults.NotFound();
    }
    
    existBook.Title = eBookInput.Title;
    existBook.Genre = eBookInput.Genre;
    existBook.Author = eBookInput.Author;
    existBook.Format = eBookInput.Format;
    existBook.Price = eBookInput.Price;

    await context.SaveChangesAsync();
    return Results.NoContent();
}

//Cambiar disponibilidad de un libro electronico
async Task<IResult> ChangeAvailability(int id, DataContext context)
{
    var ebook = await context.EBooks.FindAsync(id);
    if(ebook is null)
    {
        return TypedResults.NotFound();
    }

    ebook.IsAvailable = !ebook.IsAvailable;

    return TypedResults.NoContent();
}

//Incrementar stock de un libro
async Task<IResult> UpdateStock(int id, int stock, DataContext context)
{
    var existBook = await context.EBooks.FindAsync(id);
    if(existBook is null)
    {
        return TypedResults.NotFound();
    }

    existBook.Stock += stock;
    await context.SaveChangesAsync();

    return TypedResults.NoContent();
}

//Comprar libro
async Task<IResult> PurchaseEbook(int id, int cant, int pay, DataContext context)
{
    var existBook = await context.EBooks.FindAsync(id);

    if(existBook is null) return TypedResults.NotFound();

    if(existBook.Stock>=cant)
    {
        var totalPrice = existBook.Price*cant;
        if(totalPrice == pay)
        {
            return TypedResults.Ok("Compra existosa");
        }
        else if(totalPrice > pay)
        {
            return TypedResults.BadRequest("La cantidad a pagar es menor al total de la compra");
        }
        else if(totalPrice < pay)
        {
            var vuelto = totalPrice - pay;
            return TypedResults.Ok("Su vuelvo es" + vuelto);
        }
    }

    return TypedResults.BadRequest("No se encuentra la cantidad deseada");
}

async Task<IResult> DeleteEbook(int id, DataContext context)
{
    var ebook = await context.EBooks.FindAsync(id);
    
    if(ebook is null) return TypedResults.NotFound();

    context.EBooks.Remove(ebook);
    await context.SaveChangesAsync();

    return TypedResults.NoContent();
}

