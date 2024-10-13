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
    public class IzdavanjeTests
    {

        private IspitContext _context;
        private IzdavanjeController userService;

        [OneTimeSetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<IspitContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase4")
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
                DatumIzdavanja = DateTime.Now.AddDays(0),
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
            var rezervacija3 = new Rezervacija
            {
                ID = 3,
                Knjiga = knjiga2,
                Clan = clan2,
                DatumRezervacije = DateTime.Now,
                DatumIzdavanja = DateTime.Now.AddDays(23),
                DatumVracanja = DateTime.Now.AddDays(14+23)
            };

            
            _context.Rezervacije.AddRange(rezervacija1, rezervacija2, rezervacija3);

            var izdavanje1 = new Izdavanje
            {
                ID = 1,
                Knjiga = knjiga1,
                Clan = clan1,
                DatumIzdavanja = DateTime.Now.AddDays(1),
                DatumVracanja = null, // Not returned yet
                Rezervacija = rezervacija1
            };

            var izdavanje2 = new Izdavanje
            {
                ID = 2,
                Knjiga = knjiga3,
                Clan = clan2,
                DatumIzdavanja = DateTime.Now,
                DatumVracanja = DateTime.Now.AddDays(14),
                Rezervacija = null
            };

            _context.Izdavanja.AddRange(izdavanje1, izdavanje2);

            _context.SaveChanges();
            userService = new IzdavanjeController(_context);
            
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            _context.Dispose();
        }

        [Test]
        [Order(1)]
        public void Izdaj_BookAndMemberExist_NoReservation_ReturnsOk()
        {
            // Arrange
            int idKnjiga = 3; // Book with ID 3
            int idClan = 3;   // Member with ID 3

            // Act
            var result = userService.Izdaj(idKnjiga, idClan) as ActionResult<Izdavanje>;

            // Assert
            ClassicAssert.NotNull(result, "The result should not be null.");

            // Check if the result is of type OkObjectResult
            var okResult = result.Result as OkObjectResult;
            ClassicAssert.NotNull(okResult, "The result should be OkObjectResult.");
            ClassicAssert.AreEqual(StatusCodes.Status200OK, okResult!.StatusCode, "The status code should be 200 OK.");

            var izdavanje = okResult.Value as Izdavanje;
            ClassicAssert.NotNull(izdavanje, "The response should contain the issued book.");
            ClassicAssert.AreEqual(idKnjiga, izdavanje!.Knjiga!.ID, "The issued book ID should match.");
            ClassicAssert.AreEqual(idClan, izdavanje.Clan!.ID, "The issuing member ID should match.");
        }

        [Test]
        [Order(2)]
        public void Izdaj_BookAndMemberExist_ReservationExists_SameMemberAndDate_ReturnsOk()
        {
            // Arrange
            int idKnjiga = 1; // Book with ID 1
            int idClan = 1;   // Member with ID 1

            // Act
            var actionResult = userService.Izdaj(idKnjiga, idClan);
            
            // Assert
            var result = actionResult.Result as OkObjectResult;
            ClassicAssert.NotNull(result, "The result should be OkObjectResult.");
            ClassicAssert.AreEqual(StatusCodes.Status200OK, result!.StatusCode, "The status code should be 200 OK.");
            
            var izdavanje = result.Value as Izdavanje;
            ClassicAssert.NotNull(izdavanje, "The response should contain the issued book.");
            ClassicAssert.AreEqual(idKnjiga, izdavanje!.Knjiga!.ID, "The issued book ID should match.");
            ClassicAssert.AreEqual(idClan, izdavanje.Clan!.ID, "The issuing member ID should match.");
        }

        [Test]
        [Order(1)]
        public void Izdaj_BookDoesNotExist_ReturnsBadRequest()
        {
            // Arrange
            int idKnjiga = 999; // Non-existent book ID
            int idClan = 1;    // Existing member

            // Act
            var result = userService.Izdaj(idKnjiga, idClan) as ActionResult<Izdavanje>;

            // Assert
            ClassicAssert.NotNull(result, "The result should not be null.");
            
            if (result.Result is BadRequestObjectResult badRequestResult)
            {
                ClassicAssert.AreEqual(StatusCodes.Status400BadRequest, badRequestResult.StatusCode, "The status code should be 400 BadRequest.");
                ClassicAssert.AreEqual("Knjiga ne postoji.", badRequestResult.Value, "The error message should indicate that the book does not exist.");
            }
            else
            {
                Assert.Fail("Expected BadRequestObjectResult but got a different result type.");
            }
        }

        [Test]
        [Order(2)]
        public void Izdaj_MemberDoesNotExist_ReturnsBadRequest()
        {
            // Arrange
            int idKnjiga = 1; // Existing book
            int idClan = 999; // Non-existent member ID

            // Act
            var result = userService.Izdaj(idKnjiga, idClan) as ActionResult<Izdavanje>;

            // Assert
            ClassicAssert.NotNull(result, "The result should be BadRequestObjectResult.");
            if (result.Result is BadRequestObjectResult badRequestResult)
            {
                ClassicAssert.AreEqual(StatusCodes.Status400BadRequest, badRequestResult.StatusCode, "The status code should be 400 BadRequest.");
                ClassicAssert.AreEqual("Clan nije pronadjen.", badRequestResult.Value, "The error message should indicate that the book does not exist.");
            }
            else
            {
                Assert.Fail("Expected BadRequestObjectResult but got a different result type.");
            }
            
        }

        [Test]
        [Order(5)]
        public void Izdaj_BookAndMemberExist_ReservationExists_DifferentDate_ReturnsBadRequest()
        {
            // Arrange
            int idKnjiga = 2; // Book with ID 1
            int idClan = 2;   // Member with ID 2 (who has a reservation for this book)

            // Act
            var result = userService.Izdaj(idKnjiga, idClan) as ActionResult<Izdavanje>;

            // Assert
            ClassicAssert.NotNull(result, "The result should not be null.");
            
            if (result.Result is BadRequestObjectResult badRequestResult)
            {
                ClassicAssert.AreEqual(StatusCodes.Status400BadRequest, badRequestResult.StatusCode, "The status code should be 400 BadRequest.");
                ClassicAssert.AreEqual("Nije moguce izdati knjigu jer datum izdavanja ne podudara se sa datumom rezervacije ili je knjiga rezervisana za drugog clana.", badRequestResult.Value, "The error message should indicate that the issue is due to mismatched reservation dates or another member's reservation.");
            }
            else
            {
                Assert.Fail("Expected BadRequestObjectResult but got a different result type.");
            }
        }

        [Test]
        [Order(6)]
        public void Izdaj_BookAndMemberExist_ReservationExists_DifferentMemberInNext14Days_ReturnsBadRequest()
        {
            // Arrange
            int idKnjiga = 2; // Book with ID 2
            int idClan = 3;   // Member with ID 3

            // Act
            var result = userService.Izdaj(idKnjiga, idClan) as ActionResult<Izdavanje>;

            // Assert
            ClassicAssert.NotNull(result, "The result should not be null.");

            if (result.Result is BadRequestObjectResult badRequestResult)
            {
                ClassicAssert.AreEqual(StatusCodes.Status400BadRequest, badRequestResult.StatusCode, "The status code should be 400 BadRequest.");
                ClassicAssert.IsTrue(badRequestResult.Value!.ToString()!.StartsWith("Nije moguce izdati knjigu jer je rezervacija za drugog clana"), "The error message should indicate that the reservation is for another member in the next 14 days.");
            }
            else
            {
                Assert.Fail("Expected BadRequestObjectResult but got a different result type.");
            }
        }

        [Test]
        [Order(20)]
        public void GetAll_NoRecords_ReturnsEmptyList()
        {
            // Arrange
            // Clear the context to ensure no records are present
            _context.Izdavanja.RemoveRange(_context.Izdavanja);
            _context.SaveChanges();

            // Act
            var result = userService.GetAll() as ActionResult<List<Izdavanje>>;

            // Assert
            ClassicAssert.NotNull(result, "The result should not be null.");
            var okResult = result.Result as OkObjectResult;
            ClassicAssert.NotNull(okResult, "The result should be OkObjectResult.");
            ClassicAssert.AreEqual(StatusCodes.Status200OK, okResult!.StatusCode, "The status code should be 200 OK.");
            var izdavanjaList = okResult.Value as List<Izdavanje>;
            ClassicAssert.NotNull(izdavanjaList, "The response value should be a List<Izdavanje>.");
            ClassicAssert.IsEmpty(izdavanjaList!, "The list should be empty.");
        }

        [Test]
        [Order(2)]
        public void GetAll_RecordsExist_ReturnsRecordsWithRelatedEntities()
        {   // Act
            var result = userService.GetAll() as ActionResult<List<Izdavanje>>;

            // Assert
            ClassicAssert.NotNull(result, "The result should not be null.");
            var okResult = result.Result as OkObjectResult;
            ClassicAssert.NotNull(okResult, "The result should be OkObjectResult.");
            ClassicAssert.AreEqual(StatusCodes.Status200OK, okResult!.StatusCode, "The status code should be 200 OK.");
            var izdavanjaList = okResult.Value as List<Izdavanje>;
            ClassicAssert.NotNull(izdavanjaList, "The response value should be a List<Izdavanje>.");
            ClassicAssert.AreEqual(4, izdavanjaList!.Count, "The list should contain one record.");
            var retrievedIzdavanje = izdavanjaList.First();
           
        }

        [Test]
        [Order(2)]
        public void Get_RecordDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            // Ensure the context does not contain the record with ID 999
            // Act
            var result = userService.Get(999) as ActionResult<Izdavanje>;

            // Assert
            ClassicAssert.NotNull(result, "The result should not be null.");
            var notFoundResult = result.Result as NotFoundResult;
            ClassicAssert.NotNull(notFoundResult, "The result should be NotFoundResult.");
            ClassicAssert.AreEqual(StatusCodes.Status404NotFound, notFoundResult!.StatusCode, "The status code should be 404 NotFound.");
        }

        [Test]
        [Order(1)]
        public void Get_RecordExists_ReturnsIzdavanje()
        {
            // Arrange
            var expectedId = 1;
            // Ensure that the record with ID 1 exists
            var existingRecord = _context.Izdavanja.Find(expectedId);
            ClassicAssert.NotNull(existingRecord, "The record with ID 1 should exist in the database.");

            // Act
            var result = userService.Get(expectedId) as ActionResult<Izdavanje>;

            // Assert
            ClassicAssert.NotNull(result, "The result should not be null.");
            var okResult = result.Result as OkObjectResult;
            ClassicAssert.NotNull(okResult, "The result should be OkObjectResult.");
            ClassicAssert.AreEqual(StatusCodes.Status200OK, okResult!.StatusCode, "The status code should be 200 OK.");
            
            var retrievedIzdavanje = okResult.Value as Izdavanje;
            ClassicAssert.NotNull(retrievedIzdavanje, "The response value should be of type Izdavanje.");
            ClassicAssert.AreEqual(expectedId, retrievedIzdavanje!.ID, "The retrieved record ID should match.");
        }

        [Test]
        [Order(1)]
        public void VratiKnjigu_BookExistsAndNotReturned_ReturnsOk()
        {
            // Arrange
            int id = 1; // Valid Izdavanje ID with an associated reservation

            // Act
            var result = userService.VratiKnjigu(id) as OkObjectResult;

            // Assert
            ClassicAssert.NotNull(result, "The result should not be null.");
            ClassicAssert.AreEqual(StatusCodes.Status200OK, result!.StatusCode, "The status code should be 200 OK.");
            ClassicAssert.AreEqual("Knjiga je uspešno vraćena.", result.Value, "The response message should indicate success.");

            // Verify that the book return date has been set
            var izdavanje = _context.Izdavanja.Find(id);
            ClassicAssert.NotNull(izdavanje, "The Izdavanje should be found in the database.");
            ClassicAssert.NotNull(izdavanje!.DatumVracanja, "The book return date should be set.");
        }

        [Test]
        [Order(2)]
        public void VratiKnjigu_BookAlreadyReturned_ReturnsBadRequest()
        {
            // Arrange
            int id = 2; // Izdavanje with already returned book

            // Act
            var result = userService.VratiKnjigu(id) as BadRequestObjectResult;

            // Assert
            ClassicAssert.NotNull(result, "The result should not be null.");
            ClassicAssert.AreEqual(StatusCodes.Status400BadRequest, result!.StatusCode, "The status code should be 400 BadRequest.");
            ClassicAssert.AreEqual("Knjiga je vec vracena.", result.Value, "The error message should indicate that the book has already been returned.");
        }

        [Test]
        [Order(3)]
        public void VratiKnjigu_IzdavanjeNotFound_ReturnsNotFound()
        {
            // Arrange
            int id = 999; // Non-existent Izdavanje ID

            // Act
            var result = userService.VratiKnjigu(id) as NotFoundObjectResult;

            // Assert
            ClassicAssert.NotNull(result, "The result should not be null.");
            ClassicAssert.AreEqual(StatusCodes.Status404NotFound, result!.StatusCode, "The status code should be 404 NotFound.");
            ClassicAssert.AreEqual("Izdavanje nije pronadjeno.", result.Value, "The error message should indicate that the Izdavanje was not found.");
        }
        
        [Test]
        [Order(18)]
        public void Delete_ValidId_ReturnsNoContent()
        {
            // Arrange
            int id = 1; // Valid Izdavanje ID

            // Ensure the record with ID 1 exists in the context
            var existingIzdavanje = _context.Izdavanja.Find(id);
            ClassicAssert.NotNull(existingIzdavanje, "Test setup failed: Izdavanje with ID 1 should exist.");

            // Act
            var result = userService.Delete(id) as NoContentResult;

            // Assert
            ClassicAssert.NotNull(result, "The result should not be null.");
            ClassicAssert.AreEqual(StatusCodes.Status204NoContent, result!.StatusCode, "The status code should be 204 NoContent.");

            // Verify that the Izdavanje has been removed
            var izdavanje = _context.Izdavanja.Find(id);
            ClassicAssert.IsNull(izdavanje, "The Izdavanje should be removed from the database.");
        }

        [Test]
        [Order(2)]
        public void Delete_InvalidId_ReturnsNotFound()
        {
            // Arrange
            int id = 999; // Non-existent Izdavanje ID

            // Act
            var result = userService.Delete(id) as NotFoundObjectResult;

            // Assert
            ClassicAssert.NotNull(result, "The result should not be null.");
            ClassicAssert.AreEqual(StatusCodes.Status404NotFound, result!.StatusCode, "The status code should be 404 NotFound.");
            ClassicAssert.AreEqual("Izdavanje nije pronađeno.", result.Value, "The error message should indicate that the Izdavanje was not found.");
        }

        


    }
}