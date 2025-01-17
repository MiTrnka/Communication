using Microsoft.AspNetCore.Mvc;
namespace RESTAPI.Controllers;

[ApiController, Route("/api/cats")]
public class CatsController : ControllerBase
{
    //Testovaci data pro API
    public class Cat
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
    private static List<Cat> cats = [new Cat {Id = 0, Name = "Blecha"}, new Cat { Id = 1, Name = "Kapina" }, new Cat { Id = 2, Name = "Mourek" }];


    //Vrati vsechny kocky
    [HttpGet("")]
    public ActionResult<List<Cat>> Get()
    {
        //Vrati http 200 s JSONem kocek v body
        return this.Ok(cats);
    }

    //Vrati kocku dle Id (od nuly vcetne), pokud kocka s danym Id neni, vrati se 404
    [HttpGet("{Id:int:min(0)}")]
    public ActionResult<Cat> Get(int Id)
    {
        var cat = cats.Find(c => c.Id == Id);
        if (cat == null) return this.NotFound(); //Vrátí http 404 bez kocky v body

        //Vrati http 200 s JSONem kocky v body
        return this.Ok(cat);
    }

    //Vytvori kocku
    [HttpPost]
    public ActionResult<Cat> Post(Cat cat)
    {
        //Vrátí 400
        if (!this.ModelState.IsValid) return this.BadRequest();

        cats.Add(cat);
        //Vrati http 201, v hlavicce url bude url na toto API a v těle bude json prave zalozena kocky
        return this.CreatedAtAction(nameof(this.Post), cat);
    }

    //Aktualizuje kocku
    [HttpPut("{Id:int:min(0)}")]
    //asynchronni varianta: public async Task<IActionResult> Put(int Id, Cat updatedCat)
    public IActionResult Put(int Id, Cat updatedCat)
    {
        var cat = cats.FirstOrDefault(c => c.Id == Id);
        if (cat == null) return this.NotFound(); //Vrátí http 404

        //Vrátí 400
        if (!this.ModelState.IsValid) return this.BadRequest();

        cat.Name = updatedCat.Name;

        //Vrati http 204
        return this.NoContent();
    }


    [HttpDelete("{Id:int}")]
    //asynchronni varianta public async Task<IActionResult> Delete(int Id)
    public IActionResult Delete(int Id)
    {
        var cat = cats.FirstOrDefault(c => c.Id == Id);
        if (cat == null) return this.NoContent();

        cats.Remove(cat);
        return this.NoContent();
    }
}
