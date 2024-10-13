namespace WebTemplate.Controllers;

[ApiController]
[Route("[controller]")]
public class RezervacijaController : ControllerBase
{
    public IspitContext Context { get; set; }


    public RezervacijaController(IspitContext context)
    {
        Context = context;
        
    }

    [HttpPost("DodajRezervaciju/{idClan}/{idKnjiga}/{datumIzdavanja}")]
    public IActionResult DodajRezervaciju(int idClan, int idKnjiga, DateTime datumIzdavanja)
    {
        var knjiga = Context.Knjige.Find(idKnjiga);
        if (knjiga == null)
        {
            return BadRequest("Knjiga ne postoji.");
        }

        var clan = Context.Clanovi.Find(idClan);
        if (clan == null)
        {
            return BadRequest("Clan nije pronadjen.");
        }

        var postojecaRezervacija = Context.Rezervacije.FirstOrDefault(r => r.Knjiga!.ID == idKnjiga);
        if (postojecaRezervacija != null)
        {
            if (DateTime.Now == postojecaRezervacija.DatumRezervacije.Date)
            {
                return BadRequest("Vec postoji rezervacija za ovu knjigu sa istim datumom rezervacije.");
            }

            if (datumIzdavanja < postojecaRezervacija.DatumVracanja.Date)
            {
                return BadRequest("Datum rezervacije za uzimanje knjige ne moze biti pre datuma vracanja postojece rezervacije.");
            }
        }
        var novaRezervacija = new Rezervacija
        {
            Knjiga = knjiga,
            Clan = clan,
            DatumRezervacije = DateTime.Now,
            DatumIzdavanja = datumIzdavanja,
            DatumVracanja = datumIzdavanja.AddDays(14)
        };
        Context.Rezervacije.Add(novaRezervacija);
        Context.SaveChanges();

        Context.Entry(novaRezervacija).Reference(r => r.Knjiga).Load();
        Context.Entry(novaRezervacija).Reference(r => r.Clan).Load();

        return Ok(new { message = "Rezervacija je uspesno dodata.", id = novaRezervacija.ID });
    }


    [HttpGet("SveRezervacije")]
    public ActionResult<List<Rezervacija>> SveRezervacije()
    {
        try
        {
            var rezervacije = Context.Rezervacije.Include(r => r.Knjiga).Include(r => r.Clan).ToList();
            if (rezervacije == null || !rezervacije.Any())
            {
                return Ok(new List<Rezervacija>());
            }
            return Ok(rezervacije);
        }
        catch (Exception ex)
        {
            // Log exception if necessary
            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred while fetching data: {ex.Message}");
        }
    }



    [HttpGet("PronadjiRezervaciju/{id}")]
    public ActionResult<Rezervacija> PronadjiRezervaciju(int id)
    {
        var rezervacija = Context.Rezervacije.Include(r => r.Knjiga).Include(r => r.Clan).FirstOrDefault(r => r.ID == id);
        if (rezervacija == null)
        {
            return NotFound("Rezervacija nije pronađena.");
        }
        return Ok(rezervacija);
    }
    [HttpGet("PronadjiRezervacijeZaClana/{idClan}")]
    public IActionResult PronadjiRezervacijeZaClana(int idClan)
    {
        // Pronađi sve rezervacije za datog člana
        var rezervacije = Context.Rezervacije
                                .Include(r => r.Knjiga)
                                .Where(r => r.Clan.ID == idClan)
                                .ToList();

        // Provera da li su rezervacije pronađene
        if (!rezervacije.Any())
        {
            return NotFound("Nema rezervacija za datog člana.");
        }

        // Vraćanje podataka o rezervisanim knjigama
        var rezervisaneKnjige = rezervacije.Select(r => new
        {
            r.Knjiga.ID,
            r.Knjiga.Naslov,
            r.Knjiga.Autor,
            r.Knjiga.ISBN,
            r.Knjiga.GodinaIzdanja,
            r.Knjiga.Zanr,
            Rezervisano = true // Oznaka da je knjiga rezervisana
        }).ToList();

        return Ok(rezervisaneKnjige);
    }

    [HttpPut("AzurirajRezervaciju/{id}")]
    public IActionResult AzurirajRezervaciju(int id, Rezervacija novaRezervacija)
    {
        var staraRezervacija = Context.Rezervacije.Find(id);
        if (staraRezervacija == null)
        {
            return NotFound("Rezervacija nije pronađena.");
        }

        // Provera da li već postoji rezervacija za datu knjigu i da li se preklapa sa novim datumima
        var preklapanjeRezervacija = Context.Rezervacije
        .Any(r =>
            r.ID != id &&
            r.Knjiga!.ID == novaRezervacija.Knjiga!.ID &&
            !(novaRezervacija.DatumIzdavanja.Date >= r.DatumVracanja.Date || novaRezervacija.DatumVracanja.Date <= r.DatumIzdavanja.Date));
        if (preklapanjeRezervacija)
        {
            return BadRequest("Novi vremenski interval rezervacije se preklapa sa postojećom rezervacijom za istu knjigu.");
        }

    // Ažuriranje svojstava stare rezervacije sa svojstvima nove rezervacije
        staraRezervacija.DatumIzdavanja = novaRezervacija.DatumIzdavanja;
        staraRezervacija.DatumVracanja = novaRezervacija.DatumVracanja;
        staraRezervacija.Knjiga = novaRezervacija.Knjiga;
        staraRezervacija.Clan = novaRezervacija.Clan;

        Context.SaveChanges();

        return Ok("Rezervacija je uspešno ažurirana.");
    }


    [HttpDelete("ObrisiRezervaciju/{id}")]
    public IActionResult ObrisiRezervaciju(int id)
    {
        var rezervacija = Context.Rezervacije.Find(id);
        if (rezervacija == null)
        {
            return NotFound("Rezervacija nije pronađena.");
        }

        // Pronađi sva izdanja koja koriste ovu rezervaciju
        var izdavanja = Context.Izdavanja.Where(i => i.Rezervacija!.ID == id).ToList();
        
        // Postavi referencu na rezervaciju kao null
        foreach (var izdavanje in izdavanja)
        {
            izdavanje.Rezervacija = null;
        }
        
        // Sačuvaj promene pre brisanja rezervacije
        Context.SaveChanges();

        // Sada obriši rezervaciju
        Context.Rezervacije.Remove(rezervacija);
        Context.SaveChanges();

        return Ok("Rezervacija je uspešno obrisana.");
    }
    [HttpDelete("ObrisiRezervaciju/{idClan}/{idKnjiga}")]
    public IActionResult ObrisiRezervaciju(int idClan, int idKnjiga)
    {
        var rezervacija = Context.Rezervacije
            .Include(r => r.Knjiga)
            .Include(r => r.Clan)
            .FirstOrDefault(r => r.Clan.ID == idClan && r.Knjiga.ID == idKnjiga);

        if (rezervacija == null)
        {
            return NotFound(new { message = "Rezervacija nije pronađena." });
        }

        var izdavanja = Context.Izdavanja.Where(i => i.Rezervacija!.ID == rezervacija.ID).ToList();

        foreach (var izdavanje in izdavanja)
        {
            izdavanje.Rezervacija = null;
        }

        Context.SaveChanges();

        Context.Rezervacije.Remove(rezervacija);
        Context.SaveChanges();

        return Ok(new { message = "Rezervacija je uspešno obrisana." });
    }

}