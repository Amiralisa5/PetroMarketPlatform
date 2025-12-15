using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetroMarketPlatform.API.Data;
using PetroMarketPlatform.API.Models;

namespace PetroMarketPlatform.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommoditiesController : ControllerBase
    {
        private readonly PetroContext _context;

        public CommoditiesController(PetroContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Commodity>>> GetCommodities()
        {
            return await _context.Commodities.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Commodity>> GetCommodity(int id)
        {
            var commodity = await _context.Commodities.FindAsync(id);
            if (commodity == null)
            {
                return NotFound();
            }
            return commodity;
        }

        [HttpPost]
        public async Task<ActionResult<Commodity>> PostCommodity(Commodity commodity)
        {
            _context.Commodities.Add(commodity);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCommodity), new { id = commodity.CommodityId }, commodity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutCommodity(int id, Commodity commodity)
        {
            if (id != commodity.CommodityId)
            {
                return BadRequest();
            }

            _context.Entry(commodity).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Commodities.Any(e => e.CommodityId == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCommodity(int id)
        {
            var commodity = await _context.Commodities.FindAsync(id);
            if (commodity == null)
            {
                return NotFound();
            }

            _context.Commodities.Remove(commodity);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
