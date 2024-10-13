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
    public class RezervacijaTests
    {

        private IspitContext _context;
        private RezervacijaController userService;

        [OneTimeSetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<IspitContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase3")
                .Options;
            
             _context = new IspitContext(options);

            var knjiga1 = new Knjiga { ID = 1, Naslov = "Knjiga 1", Autor = "Autor 1", ISBN = "1234567890", GodinaIzdanja = 2022, Zanr = "Fiction" };
            var knjiga2 = new Knjiga { ID = 2, Naslov = "Knjiga 2", Autor = "Autor 2", ISBN = "0987654321", GodinaIzdanja = 2021, Zanr = "Non-Fiction" };
            var knjiga3 = new Knjiga { ID = 3, Naslov = "Knjiga 3", Autor = "Autor 3", ISBN = "0987654323", GodinaIzdanja = 2023, Zanr = "Non-Fiction" };
            _context.Knjige.AddRange(knjiga1, knjiga2, knjiga3);

            // Add test data for Clanovi (Members)
            var clan1 = new Clan { ID = 1, Username = "clan1", Password = new byte[0], Salt = new byte[0], Email = "clan1@example.com", Ime = "Ime1", Prezime = "Prezime1", BrojClanskeKarte = "12345", IsAdmin = false };
            var clan2 = new Clan { ID = 2, Username = "clan2", Password = new byte[0], Salt = new byte[0], Email = "clan2@example.com", Ime = "Ime2", Prezime = "Prezime2", BrojClanskeKarte = "67890", IsAdmin = false };
            var clan3 = new Clan { ID = 3, Username = "clan3", Password = new byte[0], Salt = new byte[0], Email = "clan3@example.com", Ime = "Ime3", Prezime = "Prezime3", BrojClanskeKarte = "67892", IsAdmin = false };
            _context.Clanovi.AddRange(clan1, clan2, clan3);

            // Add test data for Rezervacije (Reservations)
            var rezervacija1 = new Rezervacija
            {
                ID = 1,
                Knjiga = knjiga1,
                Clan = clan1,
                DatumRezervacije = DateTime.Now,
                DatumIzdavanja = DateTime.Now.AddDays(1),
                DatumVracanja = DateTime.Now.AddDays(14)
            };

            var rezervacija2 = new Rezervacija
            {
                ID = 2,
                Knjiga = knjiga2,
                Clan = clan2,
                DatumRezervacije = DateTime.Now,
                DatumIzdavanja = DateTime.Now.AddDays(3),
                DatumVracanja = DateTime.Now.AddDays(14)
            };

            
            _context.Rezervacije.AddRange(rezervacija1, rezervacija2);

            _context.SaveChanges();
            userService=new RezervacijaController(_context);
            
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            _context.Dispose();
        }

        [Test]
        [Order(2)]
        public void DodajRezervaciju_ValidData_ReturnsOk()
        {
            // Arrange
            int idClan = 2;
            int idKnjiga = 3;
            DateTime datumIzdavanja = DateTime.Now.AddDays(1);

            // Act
            var result = userService.DodajRezervaciju(idClan, idKnjiga, datumIzdavanja) as OkObjectResult;

            // Assert
            ClassicAssert.IsNotNull(result, "The result should not be null.");
            ClassicAssert.AreEqual(StatusCodes.Status200OK, result!.StatusCode, "The status code should be 200 OK.");
            ClassicAssert.AreEqual("Rezervacija je uspesno dodata.", result!.Value, "The response message should indicate successful reservation.");
            ClassicAssert.AreEqual(3, _context.Rezervacije.Count(), "The reservation count should be 3.");
        }

        [Test]
        [Order(3)]
        public void DodajRezervaciju_BookNotFound_ReturnsBadRequest()
        {
            // Arrange
            int idClan = 2;
            int idKnjiga = 999; // ID that does not exist
            DateTime datumIzdavanja = DateTime.Now.AddDays(1);

            // Act
            var result = userService.DodajRezervaciju(idClan, idKnjiga, datumIzdavanja) as BadRequestObjectResult;

            // Assert
            ClassicAssert.IsNotNull(result, "The result should not be null.");
            ClassicAssert.AreEqual(StatusCodes.Status400BadRequest, result!.StatusCode, "The status code should be 400 Bad Request.");
            ClassicAssert.AreEqual("Knjiga ne postoji.", result!.Value, "The response message should indicate that the book does not exist.");
        }

        [Test]
        [Order(4)]
        public void DodajRezervaciju_MemberNotFound_ReturnsBadRequest()
        {
            // Arrange
            int idClan = 999; // ID that does not exist
            int idKnjiga = 3;
            DateTime datumIzdavanja = DateTime.Now.AddDays(1);

            // Act
            var result = userService.DodajRezervaciju(idClan, idKnjiga, datumIzdavanja) as BadRequestObjectResult;

            // Assert
            ClassicAssert.IsNotNull(result, "The result should not be null.");
            ClassicAssert.AreEqual(StatusCodes.Status400BadRequest, result!.StatusCode, "The status code should be 400 Bad Request.");
            ClassicAssert.AreEqual("Clan nije pronadjen.", result!.Value, "The response message should indicate that the member was not found.");
        }

        [Test]
        [Order(1)]
        public void DodajRezervaciju_ReservationDateBeforeExistingReturnDate_ReturnsBadRequest()
        {
            // Arrange
            int idClan = 2;
            int idKnjiga = 1; // Book ID that already has a reservation with an active return date
            DateTime datumIzdavanja = DateTime.Now.AddDays(1); // Before the existing return date

            // Act
            var result = userService.DodajRezervaciju(idClan, idKnjiga, datumIzdavanja) as BadRequestObjectResult;

            // Assert
            ClassicAssert.IsNotNull(result, "The result should not be null.");
            ClassicAssert.AreEqual(StatusCodes.Status400BadRequest, result!.StatusCode, "The status code should be 400 Bad Request.");
            ClassicAssert.AreEqual("Datum rezervacije za uzimanje knjige ne moze biti pre datuma vracanja postojece rezervacije.", result!.Value, "The response message should indicate that the reservation date cannot be before the existing reservation's return date.");
        }

        [Test]
        [Order(1)]
        public void GetAllReservations_ReturnsListOfReservations()
        {
            var result = userService.SveRezervacije() as ActionResult<List<Rezervacija>>;
            var okResult = result.Result as OkObjectResult;
            
            // Assert
            ClassicAssert.IsNotNull(okResult, "The result should be OkObjectResult.");
            var rezervacijeResult = okResult!.Value as List<Rezervacija>;
            ClassicAssert.IsNotNull(rezervacijeResult, "The value should not be null.");
            ClassicAssert.AreEqual(2, rezervacijeResult!.Count, "The number of reservations should be 2.");
        }

        [Test]
        [Order(24)]
        public void GetAllReservations_WhenDatabaseIsEmpty_ReturnsEmptyList()
        {
            // Clear the database
            _context.Rezervacije.RemoveRange(_context.Rezervacije);
            _context.SaveChanges();

            // Act
            var result = userService.SveRezervacije() as ActionResult<List<Rezervacija>>;
            var okResult = result.Result as OkObjectResult;
            
            // Assert
            ClassicAssert.NotNull(okResult, "The result should be OkObjectResult.");
            var rezervacijeResult = okResult!.Value as List<Rezervacija>;
            ClassicAssert.NotNull(rezervacijeResult, "The value should not be null.");
            ClassicAssert.AreEqual(0, rezervacijeResult!.Count, "The number of reservations should be 0.");
        }

        [Test]
        [Order(1)]
        [TestCase(1)]
        public void PronadjiRezervaciju_ExistingId_ReturnsOk(int existingId)
        {
            // Act
            var result = userService.PronadjiRezervaciju(existingId) as ActionResult<Rezervacija>;
            var actionResult = result?.Result as OkObjectResult;
            var rezervacija = actionResult?.Value as Rezervacija;

            // Assert
            ClassicAssert.NotNull(result, "The result should not be null.");
            ClassicAssert.NotNull(actionResult, "The action result should not be null.");
            ClassicAssert.NotNull(rezervacija, "The reservation should not be null.");
            ClassicAssert.AreEqual(existingId, rezervacija?.ID, "The reservation ID should match.");
        }

        [Test]
        [Order(1)]
        [TestCase(999)]
        public void PronadjiRezervaciju_NonExistingId_ReturnsNotFound(int nonExistingId)
        {
            // Act
            var result = userService.PronadjiRezervaciju(nonExistingId) as ActionResult<Rezervacija>;
            var notFoundResult = result?.Result as NotFoundObjectResult;

            // Assert
            ClassicAssert.NotNull(result, "The result should not be null.");
            ClassicAssert.NotNull(notFoundResult, "The result should be a NotFoundObjectResult.");
            ClassicAssert.AreEqual(StatusCodes.Status404NotFound, notFoundResult?.StatusCode, "The status code should be 404 Not Found.");
            ClassicAssert.AreEqual("Rezervacija nije pronađena.", notFoundResult?.Value, "The response message should indicate that the reservation was not found.");
        }

        [Test]
        [Order(15)]
        [TestCase(1, 1, "2024-08-11T21:30:11", "2024-08-21T21:30:11")]
        public void AzurirajRezervaciju_ValidData_ReturnsOk(int id, int knjigaId, DateTime datumIzdavanja, DateTime datumVracanja)
        {
            // Arrange
            var novaRezervacija = new Rezervacija
            {
                Knjiga = _context.Knjige.Find(knjigaId),
                DatumIzdavanja = datumIzdavanja,
                DatumVracanja = datumVracanja,
                Clan = _context.Clanovi.Find(1)
            };

            // Act
            var result = userService.AzurirajRezervaciju(id, novaRezervacija) as OkObjectResult;

            // Assert
            ClassicAssert.NotNull(result, "The result should be OkObjectResult.");
            ClassicAssert.AreEqual(StatusCodes.Status200OK, result!.StatusCode, "The status code should be 200 OK.");
            ClassicAssert.AreEqual("Rezervacija je uspešno ažurirana.", result.Value, "The response message should indicate successful update.");
            
            var updatedRezervacija = _context.Rezervacije.Find(id);
            ClassicAssert.NotNull(updatedRezervacija, "The updated reservation should not be null.");
            ClassicAssert.AreEqual(knjigaId, updatedRezervacija!.Knjiga!.ID, "The book ID should be updated.");
            ClassicAssert.AreEqual(datumIzdavanja, updatedRezervacija.DatumIzdavanja, "The issue date should be updated.");
            ClassicAssert.AreEqual(datumVracanja, updatedRezervacija.DatumVracanja, "The return date should be updated.");
        }

        [Test]
        [Order(10)]
        [TestCase(999)]
        public void AzurirajRezervaciju_NonExistingId_ReturnsNotFound(int id)
        {
            // Arrange
            var novaRezervacija = new Rezervacija
            {
                Knjiga = _context.Knjige.First(),
                DatumIzdavanja = DateTime.Now.AddDays(1),
                DatumVracanja = DateTime.Now.AddDays(14),
                Clan = _context.Clanovi.First()
            };

            // Act
            var result = userService.AzurirajRezervaciju(id, novaRezervacija) as NotFoundObjectResult;

            // Assert
            ClassicAssert.NotNull(result, "The result should be NotFoundObjectResult.");
            ClassicAssert.AreEqual(StatusCodes.Status404NotFound, result!.StatusCode, "The status code should be 404 Not Found.");
            ClassicAssert.AreEqual("Rezervacija nije pronađena.", result.Value, "The response message should indicate that the reservation was not found.");
        }



        [Test]
        [Order(25)]
        [TestCase(1)]
        public void ObrisiRezervaciju_ValidId_ReturnsOk(int id)
        {
            // Arrange: Ensure there is a reservation to delete
            var rezervacija = new Rezervacija
            {
                ID = id,
                Knjiga = _context.Knjige.First(), // Ensure the book exists
                Clan = _context.Clanovi.First(),
                DatumIzdavanja = DateTime.Now,
                DatumVracanja = DateTime.Now.AddDays(7)
            };
            _context.Rezervacije.Add(rezervacija);
            _context.SaveChanges();

            // Act
            var controller = new RezervacijaController(_context);
            var result = controller.ObrisiRezervaciju(id) as OkObjectResult;

            // Assert
            ClassicAssert.NotNull(result, "The result should be OkObjectResult.");
            ClassicAssert.AreEqual(StatusCodes.Status200OK, result!.StatusCode, "The status code should be 200 OK.");
            ClassicAssert.AreEqual("Rezervacija je uspešno obrisana.", result.Value, "The response message should indicate successful deletion.");

            // Verify the reservation is deleted
            var deletedRezervacija = _context.Rezervacije.Find(id);
            ClassicAssert.IsNull(deletedRezervacija, "The reservation should be null after deletion.");
        }

        [Test]
        [Order(26)]
        [TestCase(999)] // Assuming 999 is an ID that does not exist
        public void ObrisiRezervaciju_InvalidId_ReturnsNotFound(int id)
        {
            // Arrange
            var controller = new RezervacijaController(_context);

            // Act
            var result = controller.ObrisiRezervaciju(id) as NotFoundObjectResult;

            // Assert
            ClassicAssert.NotNull(result, "The result should be NotFoundObjectResult.");
            ClassicAssert.AreEqual(StatusCodes.Status404NotFound, result!.StatusCode, "The status code should be 404 Not Found.");
            ClassicAssert.AreEqual("Rezervacija nije pronađena.", result.Value, "The response message should indicate that the reservation was not found.");
        }



    }
}