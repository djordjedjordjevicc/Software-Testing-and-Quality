using System.Text.Json.Serialization;

namespace WebTemplate.Controllers;

[ApiController]
[Route("[controller]")]
public class IzdavanjeController : ControllerBase
{
    public IspitContext Context { get; set; }

    public IzdavanjeController(IspitContext context)
    {
        Context = context;
    }


    [HttpPost("IzdajKnjigu/{idKnjiga}/{idClan}")]
    public ActionResult<Izdavanje> Izdaj(int idKnjiga, int idClan)
    {
        var knjiga = Context.Knjige.Find(idKnjiga);
        if (knjiga == null)
        {
            return Ok(new { error = "Knjiga ne postoji." });
        }

        var clan = Context.Clanovi.Find(idClan);
        if (clan == null)
        {
            return Ok(new { error = "Clan nije pronadjen." });
        }

        var rezervacija = Context.Rezervacije.FirstOrDefault(r => r.Knjiga!.ID == idKnjiga);
        if (rezervacija == null)
        {
            var novaRezervacija = new Rezervacija
            {
                Knjiga = knjiga,
                Clan = clan,
                DatumRezervacije = DateTime.Now,
                DatumIzdavanja = DateTime.Now,
                DatumVracanja = DateTime.Now.AddDays(14) // Datum vraćanja dan pre rezervacije
            };

            Context.Rezervacije.Add(novaRezervacija);
            Context.SaveChanges();

            var izdavanje = new Izdavanje
            {
                Knjiga = knjiga,
                Clan = clan,
                DatumIzdavanja = DateTime.Now,
                Rezervacija = novaRezervacija
            };

            Context.Izdavanja.Add(izdavanje);
            Context.SaveChanges();
            return Ok(izdavanje);
        }
        else if (rezervacija.Clan != null && rezervacija.Clan.ID == idClan)
        {
            if (rezervacija.DatumIzdavanja.Date == DateTime.Now.Date)
            {
                var izdavanje = new Izdavanje
                {
                    Knjiga = knjiga,
                    Clan = clan,
                    DatumIzdavanja = DateTime.Now,
                    Rezervacija = rezervacija
                };

                Context.Izdavanja.Add(izdavanje);
                Context.SaveChanges();

                return Ok(izdavanje);
            }
            else
            {
                return Ok(new { error = "Nije moguce izdati knjigu jer datum izdavanja ne podudara se sa datumom rezervacije ili je knjiga rezervisana za drugog clana." });
            }
        }
        else
        {
            var datumIzdavanja = rezervacija.DatumIzdavanja.Date;
            var datumSada = DateTime.Now.Date;
            var razlikaUDanima = (datumIzdavanja - datumSada).Days;

            if (razlikaUDanima <= 14)
            {
                return Ok(new { error = $"Nije moguce izdati knjigu jer je rezervacija za drugog clana i datumi se poklapaju u narednih {razlikaUDanima} dana." });
            }

            var novaRezervacija = new Rezervacija
            {
                Knjiga = knjiga,
                Clan = clan,
                DatumRezervacije = DateTime.Now,
                DatumIzdavanja = rezervacija.DatumIzdavanja,
                DatumVracanja = rezervacija.DatumIzdavanja.AddDays(-1) // Datum vraćanja dan pre rezervacije
            };

            Context.Rezervacije.Add(novaRezervacija);
            Context.SaveChanges();

            var izdavanje = new Izdavanje
            {
                Knjiga = knjiga,
                Clan = clan,
                DatumIzdavanja = DateTime.Now,
                Rezervacija = novaRezervacija
            };

            Context.Izdavanja.Add(izdavanje);
            Context.SaveChanges();

            return Ok(izdavanje);
        }
    }




    [HttpGet("GetAll")]
    public ActionResult<IEnumerable<Izdavanje>> GetAll()
    {
        return Context.Izdavanja
            .Include(i => i.Knjiga)
            .Include(i => i.Clan)
            .Include(i => i.Rezervacija)
            .ToList();
    }


    [HttpGet("GetIzdavanje/{id}")]
    public ActionResult<Izdavanje> Get(int id)
    {
        var izdavanje = Context.Izdavanja
            .Include(i => i.Knjiga)
            .Include(i => i.Clan)
            .Include(i => i.Rezervacija)
            .FirstOrDefault(i => i.ID == id);

        if (izdavanje == null)
        {
            return NotFound();
        }

        return izdavanje;
    }

    [HttpPut("VratiKnjigu/{id}")]
    public IActionResult VratiKnjigu(int id)
    {
        var izdavanje = Context.Izdavanja.Include(i => i.Rezervacija).FirstOrDefault(i => i.ID == id);

        if (izdavanje == null)
        {
            return NotFound("Izdavanje nije pronadjeno.");
        }

        if (izdavanje.DatumVracanja != null)
        {
            return BadRequest("Knjiga je vec vracena.");
        }

        // Provera da li postoji rezervacija povezana sa izdavanjem
        var rezervacija = izdavanje.Rezervacija;
        if (rezervacija == null)
        {
            return BadRequest("Ne postoji rezervacija uz ovo izdavanje.");
        }

        // Brišemo rezervaciju
        Context.Rezervacije.Remove(rezervacija);

        // Označavamo datum vraćanja knjige
        izdavanje.DatumVracanja = DateTime.Now;

        Context.SaveChanges();

        return Ok("Knjiga je uspešno vraćena.");
    }


    [HttpPut("PromeniIzdavanje/{id}")]
    public IActionResult PromeniIzdavanje(int id, Izdavanje izdavanje)
    {
        if (id != izdavanje.ID)
        {
            return BadRequest("ID u putanji se ne poklapa sa ID-em izdavanja.");
        }

        var existingIzdavanje = Context.Izdavanja.Find(id);

        if (existingIzdavanje == null)
        {
            return NotFound("Izdavanje nije pronađeno.");
        }

        // Ažuriranje svih atributa izdavanja
        existingIzdavanje.Knjiga = izdavanje.Knjiga;
        existingIzdavanje.Clan = izdavanje.Clan;
        existingIzdavanje.DatumIzdavanja = izdavanje.DatumIzdavanja;
        existingIzdavanje.DatumVracanja = izdavanje.DatumVracanja;

        try
        {
            Context.SaveChanges();
            // Include related entities before returning
            Context.Entry(existingIzdavanje).Reference(i => i.Knjiga).Load();
            Context.Entry(existingIzdavanje).Reference(i => i.Clan).Load();
            return Ok(existingIzdavanje);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!Context.Izdavanja.Any(i => i.ID == id))
            {
                return NotFound("Izdavanje nije pronađeno.");
            }
            else
            {
                throw;
            }
        }
    }

    [HttpDelete("ObrisiIzdavanje/{id}")]
    public IActionResult Delete(int id)
    {
        var izdavanje = Context.Izdavanja.Find(id);

        if (izdavanje == null)
        {
            return NotFound("Izdavanje nije pronađeno.");
        }

        Context.Izdavanja.Remove(izdavanje);
        Context.SaveChanges();

        return NoContent();
    }




}