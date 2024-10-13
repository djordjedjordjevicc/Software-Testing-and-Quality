using System.Security.Cryptography;

namespace WebTemplate.Controllers;

[ApiController]
[Route("[controller]")]
public class ClanController : ControllerBase
{
    public IspitContext Context { get; set; }


    public ClanController(IspitContext context)
    {
        Context = context;
        
    }

    [HttpGet]
    public ActionResult<List<Clan>> GetAll()
    {
        try
        {
            var users = Context.Clanovi.ToList();
            if (users == null || !users.Any())
            {
                return Ok(new List<Clan>());
            }
            return Ok(users);
        }
        catch (Exception ex)
        {
            // Log exception if necessary
            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred while fetching data: {ex.Message}");
        }
    }





    [HttpGet("GetClan/{id}")]
    public ActionResult<Clan> Get(int id)
    {
        var clan = Context.Clanovi.Find(id);

        if (clan == null)
        {
            return Ok(null);
        }

        return Ok(clan);
    }


    [HttpPut("PromeniClan/{id}/{ime}/{prezime}/{email}")]
    public IActionResult PromeniClan(int id, string ime, string prezime, string email)
    {

        var existingClan = Context.Clanovi.Find(id);

        if (existingClan == null)
        {
            return NotFound("Knjiga nije pronađena.");
        }

        existingClan.Ime = ime;
        existingClan.Prezime = prezime;
        existingClan.Email = email;

        try
        {
            Context.SaveChanges();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!Context.Knjige.Any(k => k.ID == id))
            {
                return NotFound("Clan nije pronađen.");
            }
            else
            {
                throw;
            }
        }
        return Ok(existingClan);
    }

    [HttpDelete("ObrisiClana/{id}")]
    public IActionResult ObrisiClana(int id)
    {
        var clan = Context.Clanovi.Find(id);

        if (clan == null)
        {
            return NotFound();
        }

        Context.Clanovi.Remove(clan);
        Context.SaveChanges();

        return NoContent();
    }

    [HttpPost("Register/{username}/{password}/{ime}/{prezime}/{email}/{admin}")]
    public async Task<ActionResult> Register(string username,string ime, string password, string prezime, string email, bool admin) 
    {
        if (await Context.Clanovi.AnyAsync(c => c.Username == username))
        {
            return BadRequest("Username already exists.");
        }

        string brojClanskeKarte="";

        if(!admin)
        {
            brojClanskeKarte=GenerateUniqueMembershipCardNumber();;
        }
        
    
        if (string.IsNullOrEmpty(password))
        {
            return BadRequest("Password cannot be empty.");
        }
        // Hash the password before saving
        byte[] salt = GenerateSalt(); // Implement your salt generation method
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[] hashedPassword = HashPassword(passwordBytes, salt); // Implement your password hashing method

        var clan=new Clan
        {
            Username=username,
            Password=hashedPassword,
            Salt=salt,
            Ime=ime,
            Prezime=prezime,
            Email=email,
            BrojClanskeKarte=brojClanskeKarte,
            IsAdmin=admin
        };

        Context.Clanovi.Add(clan);
        await Context.SaveChangesAsync();

        return Ok("Registration successful.");
    }


    private string GenerateUniqueMembershipCardNumber()
    {
            // Implement a method to generate a unique membership card number
        string cardNumber;
        do
        {
            cardNumber = Guid.NewGuid().ToString().Substring(0, 8); // Example generation logic
        } while (Context.Clanovi.Any(c => c.BrojClanskeKarte == cardNumber));

        return cardNumber;
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

    [HttpGet("Login/{username}/{password}")]
    public async Task<ActionResult<Clan>> Login(string username, string password)
    {
        var clan = await Context.Clanovi.FirstOrDefaultAsync(c => c.Username == username);

        if (clan == null)
        {
            return Unauthorized("Invalid username or password.");
        }

        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        if(clan.Salt!=null && clan.Password!=null)
        {
            byte[] hashedPassword = HashPassword(passwordBytes, clan.Salt);
            if (!hashedPassword.SequenceEqual(clan.Password))
            {
                return Unauthorized("Invalid username or password.");
            }
        }
        return Ok(clan);
    }

}