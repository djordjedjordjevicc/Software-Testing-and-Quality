using NUnit.Framework;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using WebTemplate.Controllers;
using WebTemplate.Models;
using NUnit.Framework.Legacy;
using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;

namespace BackendTest
{
    [TestFixture]
    public class KnjigaTests
    {

        private IspitContext _context;
        private KnjigaController userService;

        [OneTimeSetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<IspitContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase2")
                .Options;
            
             _context = new IspitContext(options);

             var knjige = new List<Knjiga>
            {
                new Knjiga { ID = 1, Naslov = "Naslov1", Autor = "Autor1", ISBN = "123456789", GodinaIzdanja = 2000, Zanr = "Zanr1" },
                new Knjiga { ID = 2, Naslov = "Naslov2", Autor = "Autor2", ISBN = "987654321", GodinaIzdanja = 2010, Zanr = "Zanr2" },
                new Knjiga { ID = 3, Naslov = "Naslov3", Autor = "Autor3", ISBN = "987654322", GodinaIzdanja = 2011, Zanr = "Zanr3" }
            };

            _context.Knjige.AddRange(knjige);
            _context.SaveChanges();
            userService = new KnjigaController(_context);

        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            _context.Dispose();
        }

        [Test]
        [Order(1)]
        public void GetAllKnjige_ReturnsListOfKnjige()
        {
            var result = userService.GetAll();
            var actionResult = result.Result as OkObjectResult;

            // Assert
            Assert.That(actionResult, Is.Not.Null);
            var knjige = actionResult.Value as List<Knjiga>;
            Assert.That(knjige, Is.Not.Null);
            Assert.That(knjige.Count, Is.EqualTo(3));
            Assert.That($"{knjige[0].Naslov}", Is.EqualTo("Naslov1"));
            Assert.That($"{knjige[1].Naslov}", Is.EqualTo("Naslov2"));
            Assert.That($"{knjige[2].Naslov}", Is.EqualTo("Naslov3"));
        }

        [Test]
        [Order(24)]
        public void GetBooksWhenDatabaseIsEmpty()
        {
            // Act
            _context.Knjige.RemoveRange(_context.Knjige);
            _context.SaveChanges();

            var result = userService.GetAll();
            var actionResult = result.Result as OkObjectResult;

            // Assert
            Assert.That(actionResult, Is.Not.Null);
            var knjige = actionResult.Value as List<Knjiga>; 
            Assert.That(knjige, Is.Not.Null);
            Assert.That(knjige.Count, Is.EqualTo(0));
        }

        [Test]
        [Order(11)]
        [TestCase(2)]
        public void GetBookReturnSuccess(int id)
        {
            var result = userService.Get(id);
            var actionResult = result.Result as OkObjectResult;
            var knjiga = actionResult?.Value as Knjiga;

            Assert.That(result, Is.Not.Null);
            Assert.That(actionResult, Is.Not.Null);
            Assert.That(knjiga, Is.Not.Null);
            Assert.That(knjiga.ID, Is.EqualTo(id));
        }

        [Test]
        [Order(2)]
        [TestCase(65)]
        public void GetBookNotFound(int id)
        {
            var result = userService.Get(id);
            var actionResult = result.Result as NotFoundResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(actionResult, Is.Not.Null);
            Assert.That(actionResult.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
        }

        [Test]
        [Order(3)]
        [TestCase("Nova Knjiga", "Novi Autor", "1112233445566", 2024, "Nova Vrsta")]
        public void AddBookSuccess(string naslov, string autor, string isbn, int godinaIzdanja, string zanr)
        {
            // Arrange
            var knjiga = new Knjiga
            {
                Naslov = naslov,
                Autor = autor,
                ISBN = isbn,
                GodinaIzdanja = godinaIzdanja,
                Zanr = zanr
            };

            // Act
            var result = userService.Add(knjiga);
            var actionResult = result.Result as OkObjectResult;
            var addedBook = actionResult?.Value as Knjiga;

            // Assert
            Assert.That(actionResult, Is.Not.Null);
            Assert.That(addedBook, Is.Not.Null);
            Assert.That(addedBook.ID, Is.GreaterThan(0)); // ID should be auto-generated
            Assert.That(addedBook.Naslov, Is.EqualTo(naslov));
            Assert.That(addedBook.Autor, Is.EqualTo(autor));
            Assert.That(addedBook.ISBN, Is.EqualTo(isbn));
            Assert.That(addedBook.GodinaIzdanja, Is.EqualTo(godinaIzdanja));
            Assert.That(addedBook.Zanr, Is.EqualTo(zanr));
        }

        [Test]
        [Order(44)]
        [TestCase("", "", "1112233445566", 2024, "Nova Vrsta")]
        public void AddBookFailsWhenRequiredFieldsAreEmpty(string naslov, string autor, string isbn, int godinaIzdanja, string zanr)
        {
            // Arrange
            var knjiga = new Knjiga
            {
                Naslov = naslov,
                Autor = autor,
                ISBN = isbn,
                GodinaIzdanja = godinaIzdanja,
                Zanr = zanr
            };

            // Act
            var result = userService.Add(knjiga);
            var actionResult = result.Result as BadRequestObjectResult;
            var errorMessage = actionResult?.Value as string;

            // Assert
            Assert.That(actionResult, Is.Not.Null);
            Assert.That(actionResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
            Assert.That(errorMessage, Does.Contain("Naslov i Autor su obavezni."));
        }

                [Test]
        [Order(1)]
        [TestCase(4, "Updated Title", "Updated Author", "111222333", 2024, "Updated Genre")]
        public void PromeniKnjigu_SuccessfulUpdate_ReturnsOk(int id, string naslov, string autor, string isbn, int godinaIzdanja, string zanr)
        {
            // Arrange
            var existingKnjiga = new Knjiga
            {
                ID = id,
                Naslov = "Original Title",
                Autor = "Original Author",
                ISBN = "123456789",
                GodinaIzdanja = 2000,
                Zanr = "Original Genre"
            };

            _context.Knjige.Add(existingKnjiga);
            _context.SaveChanges();

            var updatedKnjiga = new Knjiga
            {
                ID = id,
                Naslov = naslov,
                Autor = autor,
                ISBN = isbn,
                GodinaIzdanja = godinaIzdanja,
                Zanr = zanr
            };

            var controller = new KnjigaController(_context);

            // Act
            var result = controller.PromeniKnjigu(id, updatedKnjiga);
            var actionResult = result as OkObjectResult;
            var updatedBook = actionResult?.Value as Knjiga;

            // Assert
            Assert.That(actionResult, Is.Not.Null);
            Assert.That(actionResult.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
            Assert.That(updatedBook, Is.Not.Null);
            Assert.That(updatedBook.Naslov, Is.EqualTo(naslov));
            Assert.That(updatedBook.Autor, Is.EqualTo(autor));
            Assert.That(updatedBook.ISBN, Is.EqualTo(isbn));
            Assert.That(updatedBook.GodinaIzdanja, Is.EqualTo(godinaIzdanja));
            Assert.That(updatedBook.Zanr, Is.EqualTo(zanr));
        }

        [Test]
        [Order(22)]
        [TestCase(1, 6, "Updated Title", "Updated Author", "111222333", 2024, "Updated Genre")]
        public void PromeniKnjigu_IDMismatch_ReturnsBadRequest(int routeId, int bookId, string naslov, string autor, string isbn, int godinaIzdanja, string zanr)
        {
            // Arrange
            var existingKnjiga = new Knjiga
            {
                ID = bookId,
                Naslov = "Original Title",
                Autor = "Original Author",
                ISBN = "123456789",
                GodinaIzdanja = 2000,
                Zanr = "Original Genre"
            };

            _context.Knjige.Add(existingKnjiga);
            _context.SaveChanges();

            var updatedKnjiga = new Knjiga
            {
                ID = bookId,
                Naslov = naslov,
                Autor = autor,
                ISBN = isbn,
                GodinaIzdanja = godinaIzdanja,
                Zanr = zanr
            };

            var controller = new KnjigaController(_context);

            // Act
            var result = controller.PromeniKnjigu(routeId, updatedKnjiga);
            var actionResult = result as BadRequestObjectResult;

            // Assert
            Assert.That(actionResult, Is.Not.Null);
            Assert.That(actionResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
            Assert.That(actionResult.Value, Is.EqualTo("ID u putanji se ne poklapa sa ID-em knjige."));
        }
        
        [Test]
        [Order(3)]
        [TestCase(8, "Updated Title", "Updated Author", "111222333", 2024, "Updated Genre")]
        public void PromeniKnjigu_BookNotFound_ReturnsNotFound(int id, string naslov, string autor, string isbn, int godinaIzdanja, string zanr)
        {
            // Arrange
            var updatedKnjiga = new Knjiga
            {
                ID = id,
                Naslov = naslov,
                Autor = autor,
                ISBN = isbn,
                GodinaIzdanja = godinaIzdanja,
                Zanr = zanr
            };

            var controller = new KnjigaController(_context);

            // Act
            var result = controller.PromeniKnjigu(id, updatedKnjiga);
            var actionResult = result as NotFoundObjectResult;

            // Assert
            Assert.That(actionResult, Is.Not.Null);
            Assert.That(actionResult.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
            Assert.That(actionResult.Value, Is.EqualTo("Knjiga nije pronađena."));
        }

        [Test]
        [Order(1)]
        [TestCase(4)]
        public void ObrisiKnjigu_SuccessfulDeletion_ReturnsNoContent(int id)
        {
            var result = userService.ObrisiKnjigu(id);
            var actionResult = result as NoContentResult;

            // Assert
            Assert.That(actionResult, Is.Not.Null);
            Assert.That(actionResult.StatusCode, Is.EqualTo(StatusCodes.Status204NoContent));
        }

        [Test]
        [Order(2)]
        [TestCase(8)]
        public void ObrisiKnjigu_BookNotFound_ReturnsNotFound(int id)
        {
            // Arrange
            var controller = new KnjigaController(_context);

            // Act
            var result = controller.ObrisiKnjigu(id);
            var actionResult = result as NotFoundResult;

            // Assert
            Assert.That(actionResult, Is.Not.Null);
            Assert.That(actionResult.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
        }

    }
}