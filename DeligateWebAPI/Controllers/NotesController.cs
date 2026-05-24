using DeligateWebAPI.Models;
using Microsoft.AspNetCore.Mvc;
using DeligateWebAPI.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;

namespace DeligateWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NotesController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<NotesController> _logger;

        public NotesController(ILogger<NotesController> logger)
        {
            _logger = logger;
        }

        //[HttpGet(Name = "GetNotes")]
        //public IEnumerable<Item> Get()
        //{
        //    return Enumerable.Range(1, 5).Select(index => new Item
        //    {
        //        Name = "",
        //        Description = "",
               
        //    })
        //    .ToArray();
        //}

        //[HttpGet]
        //[Route("api/Employee/Index")]
        //public IEnumerable<Item> Index()
        //{
        //    return objemployee.GetAllEmployees();
        //}

        //[HttpPost]
        //[Route("api/Employee/Create")]
        //public int Create(TblEmployee employee)
        //{
        //    return objemployee.AddEmployee(employee);
        //}

        //[HttpGet]
        //[Route("api/Employee/Details/{id}")]
        //public TblEmployee Details(int id)
        //{
        //    return objemployee.GetEmployeeData(id);
        //}

        //[HttpPut]
        //[Route("api/Employee/Edit")]
        //public int Edit(TblEmployee employee)
        //{
        //    return objemployee.UpdateEmployee(employee);
        //}

        //[HttpDelete]
        //[Route("api/Employee/Delete/{id}")]
        //public int Delete(int id)
        //{
        //    return objemployee.DeleteEmployee(id);
        //}

        //[HttpGet]
        //[Route("api/Employee/GetCityList")]
        //public IEnumerable<TblCities> Details()
        //{
        //    return objemployee.GetCities();
        //}
    }


public static class ItemEndpoints
{
	public static void MapItemEndpoints (this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/Item").WithTags(nameof(Item));

        group.MapGet("/", async (ApplicationDbContext db) =>
        {
            return await db.Items.ToListAsync();
        })
        .WithName("GetAllItems")
        .WithOpenApi();

        group.MapGet("/{id}", async Task<Results<Ok<Item>, NotFound>> (int id, ApplicationDbContext db) =>
        {
            return await db.Items.AsNoTracking()
                .FirstOrDefaultAsync(model => model.Id == id)
                is Item model
                    ? TypedResults.Ok(model)
                    : TypedResults.NotFound();
        })
        .WithName("GetItemById")
        .WithOpenApi();

        group.MapPut("/{id}", async Task<Results<Ok, NotFound>> (int id, Item item, ApplicationDbContext db) =>
        {
            var affected = await db.Items
                .Where(model => model.Id == id)
                .ExecuteUpdateAsync(setters => setters
                  .SetProperty(m => m.Id, item.Id)
                  .SetProperty(m => m.Name, item.Name)
                  .SetProperty(m => m.Description, item.Description)
                  );
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("UpdateItem")
        .WithOpenApi();

        group.MapPost("/", async (Item item, ApplicationDbContext db) =>
        {
            db.Items.Add(item);
            await db.SaveChangesAsync();
            return TypedResults.Created($"/api/Item/{item.Id}",item);
        })
        .WithName("CreateItem")
        .WithOpenApi();

        group.MapDelete("/{id}", async Task<Results<Ok, NotFound>> (int id, ApplicationDbContext db) =>
        {
            var affected = await db.Items
                .Where(model => model.Id == id)
                .ExecuteDeleteAsync();
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("DeleteItem")
        .WithOpenApi();
    }
}}
