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
    public class UserTests
    {

        private IspitContext _context;
        private ClanController userService;

        [OneTimeSetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<IspitContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;


            _context = new IspitContext(options);
            var testPassword = "testpassword";
            var salt = GenerateSalt();

            var initialData = new List<Clan>
            {
                new Clan{ ID = 1, Username = "user1", Password=HashPassword(Encoding.UTF8.GetBytes(testPassword), salt), Salt=salt, Email="user1@example.com", Ime="Ime1", Prezime="Prezime1", BrojClanskeKarte="123456", IsAdmin=false},
                new Clan{ ID = 2, Username = "user2", Password=HashPassword(Encoding.UTF8.GetBytes(testPassword), salt), Salt=salt, Email="user2@example.com", Ime="Ime2", Prezime="Prezime2", BrojClanskeKarte="123457", IsAdmin=false},
                new Clan{ ID = 3, Username = "user3", Password=HashPassword(Encoding.UTF8.GetBytes(testPassword), salt), Salt=salt, Email="user3@example.com", Ime="Ime3", Prezime="Prezime3", BrojClanskeKarte="123458", IsAdmin=false}

            };

            _context.Clanovi.AddRange(initialData);
            _context.SaveChanges();
            var usersCount = _context.Clanovi.Count();
            TestContext.WriteLine($"Initial data added to the database. Total users: {usersCount}");
            userService = new ClanController(_context);
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            _context.Dispose();
        }

        private byte[] GenerateSalt()
        {
            // Implement your salt generation method here
            byte[] salt = new byte[16]; // Example: Generate a random 16-byte salt
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        private byte[] HashPassword(byte[] password, byte[] salt)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA256(salt);
            return hmac.ComputeHash(password);
        }

        [Test]
        [Order(21)]
        [TestCase("john", "password", "John", "Doe", "john@example.com", false)]
        public async Task CreateUserSuccessMessage(string username, string password, string ime, string prezime, string email, bool admin)
        {
            var result = await userService.Register(username, ime, password, prezime, email, admin);

            // Assert
            var actionResult = result as OkObjectResult;
            Assert.That(actionResult, Is.Not.Null);
            Assert.That(actionResult.Value!.ToString(), Is.EqualTo("Registration successful."));
        }


        [Test]
        [Order(22)]
        [TestCase("user2", "newpassword", "John", "Doe", "john@example.com", false)]
        public async Task RegisterUsernameAlreadyExists(string username, string password, string ime, string prezime, string email, bool admin)
        {
            var result = await userService.Register(username, ime, password, prezime, email, admin);

            var actionResult = result as BadRequestObjectResult;
            Assert.That(actionResult, Is.Not.Null);
            Assert.That(actionResult.Value!.ToString(), Is.EqualTo("Username already exists."));
        }

        [Test]
        [Order(23)]
        [TestCase("newuser", "", "John", "Doe", "john@example.com", false)]
        public async Task RegisterPasswordCannotBeEmpty(string username, string password, string ime, string prezime, string email, bool admin)
        {
            var result = await userService.Register(username, ime, password, prezime, email, admin);

            var actionResult = result as BadRequestObjectResult;
            Assert.That(actionResult, Is.Not.Null);
            Assert.That(actionResult.Value!.ToString(), Is.EqualTo("Password cannot be empty."));
        }



        [Test]
        [Order(1)]
        [TestCase(2)]
        public void GetUserReturnSuccess(int id)
        {
            var result = userService.Get(id);
            var actionResult = result.Result as OkObjectResult;
            var clan = actionResult?.Value as Clan;

            Assert.That(result, Is.Not.Null);
            Assert.That(actionResult, Is.Not.Null);
            Assert.That(clan, Is.Not.Null);
            Assert.That(clan.ID, Is.EqualTo(id));
        }

        [Test]
        [Order(2)]
        [TestCase(56)]
        public void GetNullUser(int id)
        {
            var result = userService.Get(id);
            var actionResult = result.Result as OkObjectResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(actionResult, Is.Not.Null);
            Assert.That(actionResult.Value, Is.Null);
        }

        

        [Test]
        [Order(4)]
        public void GetAllUsersReturnArray()
        {
            // Act
            var result = userService.GetAll();
            var actionResult = result.Result as OkObjectResult;

            // Assert
            Assert.That(actionResult, Is.Not.Null);
            var users = actionResult.Value as List<Clan>; // Use List<Clan> instead of Clan[]
            Assert.That(users, Is.Not.Null);
            Assert.That(users.Count, Is.EqualTo(2));
            Assert.That($"{users[0].Ime} {users[0].Prezime}", Is.EqualTo("Ime2 Prezime2"));
            Assert.That($"{users[1].Ime} {users[1].Prezime}", Is.EqualTo("Ime3 Prezime3"));
            //Assert.That($"{users[2].Ime} {users[2].Prezime}", Is.EqualTo("Ime3 Prezime3"));
        }

        [Test]
        [Order(24)]
        public void GetUsersWhenDatabaseIsEmpty()
        {
            // Act
            _context.Clanovi.RemoveRange(_context.Clanovi);
            _context.SaveChanges();

            var result = userService.GetAll();
            var actionResult = result.Result as OkObjectResult;

            // Assert
            Assert.That(actionResult, Is.Not.Null);
            var users = actionResult.Value as List<Clan>; 
            Assert.That(users, Is.Not.Null);
            Assert.That(users.Count, Is.EqualTo(0));
        }

        [Test]
        [Order(1)]
        [TestCase(1, "Ime1", "Prezime1", "newemail@example.com")]
        public void PromeniClan_Success(int id, string ime, string prezime, string email)
        {
            // Act
            var result = userService.PromeniClan(id, ime, prezime, email) as OkObjectResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            var updatedClan = result.Value as Clan;
            Assert.That(updatedClan, Is.Not.Null);
            Assert.That(updatedClan.Ime, Is.EqualTo(ime));
            Assert.That(updatedClan.Prezime, Is.EqualTo(prezime));
            Assert.That(updatedClan.Email, Is.EqualTo(email));
        }

        [Test]
        [Order(2)]
        [TestCase(999, "NewIme", "NewPrezime", "newemail@example.com")]
        public void PromeniClan_NotFound(int id, string ime, string prezime, string email)
        {
            // Act
            var result = userService.PromeniClan(id, ime, prezime, email) as NotFoundObjectResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Value!.ToString(), Is.EqualTo("Clan nije pronađen."));
        }


       [Test]
        [Order(8)]
        [TestCase("user2", "testpassword")]
        public async Task LoginSuccess(string username, string password)
        {
            var result = await userService.Login(username, password);
            var user = result.Result as OkObjectResult;
            var clan=user!.Value as Clan;
            Assert.That(result, Is.Not.Null);
            Assert.That(clan, Is.Not.Null);
            Assert.That(clan!.Username, Is.EqualTo(username));
        }

        [Test]
        [Order(9)]
        [TestCase("Invaliduser", "testpassword")]
        public async Task LoginInvalidUsername(string username, string password)
        {
            var result = await userService.Login(username, password);
            var actionResult = result.Result as UnauthorizedObjectResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(actionResult, Is.Not.Null);
            Assert.That(actionResult.StatusCode, Is.EqualTo(401)); // Unauthorized
            Assert.That(actionResult.Value!.ToString(), Is.EqualTo("Invalid username or password."));
        }

           [Test]
        [Order(1)]
        [TestCase(1)]
        public void ObrisiClana_Success(int id)
        {
            // Act
            var result = userService.ObrisiClana(id) as NoContentResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.StatusCode, Is.EqualTo(StatusCodes.Status204NoContent));
            Assert.That(!_context.Clanovi.Any(c => c.ID == id), Is.True); // Verify that the clan has been removed
        }

        [Test]
        [Order(2)]
        [TestCase(999)] // Assuming 999 is not in the initial data
        public void ObrisiClana_NotFound(int id)
        {
            // Act
            var result = userService.ObrisiClana(id) as NotFoundResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
        }

    }
}