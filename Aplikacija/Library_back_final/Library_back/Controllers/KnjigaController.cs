namespace WebTemplate.Controllers;

[ApiController]
[Route("[controller]")]
public class KnjigaController : ControllerBase
{
    public IspitContext Context { get; set; }

    public KnjigaController(IspitContext context)
    {
        Context = context;
    }

    [HttpGet("GetAll")]
    public ActionResult<List<Knjiga>> GetAll()
    {
        try
        {
            var knjige = Context.Knjige.ToList();
            if (knjige == null || !knjige.Any())
            {
                return Ok(new List<Knjiga>());
            }
            return Ok(knjige);
        }
        catch (Exception ex)
        {
            // Log exception if necessary
            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred while fetching data: {ex.Message}");
        }
    }

    [HttpGet("{id}")]
    public ActionResult<Knjiga> Get(int id)
    {
        var knjiga = Context.Knjige.Find(id);

        if (knjiga == null)
        {
            return NotFound();
        }

        return Ok(knjiga);
    }

    [HttpPost("Add")]
    public ActionResult<Knjiga> Add([FromBody] Knjiga knjiga)
    {
        if (knjiga == null)
        {
            return BadRequest("Knjiga ne može biti null.");
        }

        if (string.IsNullOrEmpty(knjiga.Naslov) || string.IsNullOrEmpty(knjiga.Autor))
        {
            return BadRequest("Naslov i Autor su obavezni.");
        }

        try
        {
            Context.Knjige.Add(knjiga);
            Context.SaveChanges();
            return Ok(knjiga); // Used CreatedAtAction for better REST practice
        }
        catch (DbUpdateException ex)
        {
            // Log exception if necessary
            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred while saving the book: {ex.Message}");
        }
    }
    [HttpPost("DodajKnjigu/{naslov}/{autor}/{isbn}/{godinaIzdanja}/{zanr}")]
    public async Task<ActionResult> DodajKnjigu(string naslov, string autor, string isbn, int godinaIzdanja, string zanr)
    {
        // Create a new instance of Knjiga with the provided parameters
        if (await Context.Knjige.AnyAsync(c => c.Naslov == naslov))
        {
            return BadRequest(" Book already exists.");
        }
        var knjiga = new Knjiga
        {
            Naslov = naslov,
            Autor = autor,
            ISBN = isbn,
            GodinaIzdanja = godinaIzdanja,
            Zanr = zanr
        };


        Context.Knjige.Add(knjiga);
        await Context.SaveChangesAsync();


        return Ok(knjiga.ID);
    }

    [HttpPut("PromeniKnjigu{id}")]
    public IActionResult PromeniKnjigu(int id, Knjiga knjiga)
    {
        if (id != knjiga.ID)
        {
            return BadRequest("ID u putanji se ne poklapa sa ID-em knjige.");
        }

        var existingKnjiga = Context.Knjige.Find(id);

        if (existingKnjiga == null)
        {
            return NotFound("Knjiga nije pronađena.");
        }

        // Ažuriranje svih atributa knjige
        existingKnjiga.Naslov = knjiga.Naslov;
        existingKnjiga.Autor = knjiga.Autor;
        existingKnjiga.ISBN = knjiga.ISBN;
        existingKnjiga.GodinaIzdanja = knjiga.GodinaIzdanja;
        existingKnjiga.Zanr = knjiga.Zanr;
        // Dodati ostale atribute koje želite da menjate

        try
        {
            Context.SaveChanges();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!Context.Knjige.Any(k => k.ID == id))
            {
                return NotFound("Knjiga nije pronađena.");
            }
            else
            {
                throw;
            }
        }

    // Vratiti ažuriranu knjigu kao odgovor
        return Ok(existingKnjiga);
    }
    [HttpPut("PromeniKnjigu/{id}/{naslov?}/{autor?}/{isbn?}/{godinaIzdanja?}/{zanr?}")]
    public IActionResult PromeniKnjigu(int id, string naslov = null, string autor = null, string isbn = null, int? godinaIzdanja = null, string zanr = null)
    {
        var existingKnjiga = Context.Knjige.Find(id);

        if (existingKnjiga == null)
        {
            return NotFound("Knjiga nije pronađena.");
        }

        // Ažuriranje atributa ako su prosleđeni
        if (!string.IsNullOrEmpty(naslov))
            existingKnjiga.Naslov = naslov;

        if (!string.IsNullOrEmpty(autor))
            existingKnjiga.Autor = autor;

        if (!string.IsNullOrEmpty(isbn))
            existingKnjiga.ISBN = isbn;

        if (godinaIzdanja.HasValue)
            existingKnjiga.GodinaIzdanja = godinaIzdanja.Value;

        if (!string.IsNullOrEmpty(zanr))
            existingKnjiga.Zanr = zanr;

        try
        {
            Context.SaveChanges();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!Context.Knjige.Any(k => k.ID == id))
            {
                return NotFound("Knjiga nije pronađena.");
            }
            else
            {
                throw;
            }
        }

        // Vratiti ažuriranu knjigu kao odgovor
        return Ok(existingKnjiga);
    }


    [HttpDelete("ObrisiKnjigu/{id}")]
    public IActionResult ObrisiKnjigu(int id)
    {
        var knjiga = Context.Knjige.Find(id);

        if (knjiga == null)
        {
            return NotFound();
        }

        Context.Knjige.Remove(knjiga);
        Context.SaveChanges();

        return NoContent();
    }

}
