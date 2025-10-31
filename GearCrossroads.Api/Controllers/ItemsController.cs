using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GearCrossroads.Api.Data;
using GearCrossroads.Api.Models;

[ApiController]
[Route("api/[controller]")]
public class ItemsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ItemsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? q)
    {
        var query = _db.Items.AsQueryable();
        if (!string.IsNullOrEmpty(q))
            query = query.Where(i => i.Name.Contains(q));
        var items = await query.Take(50).ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id}/users")]
    public async Task<IActionResult> UsersForItem(int id)
    {
        var users = await _db.SetupItems
            .Where(si => si.ItemId == id)
            .Include(si => si.Setup)
                .ThenInclude(s => s.Owner)
            .Select(si => new {
                si.Setup.Id,
                si.Setup.Title,
                OwnerId = si.Setup.OwnerId,
                OwnerName = si.Setup.Owner.DisplayName
            })
            .Distinct()
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Item item)
    {
        _db.Items.Add(item);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = item.Id }, item);
    }
}
