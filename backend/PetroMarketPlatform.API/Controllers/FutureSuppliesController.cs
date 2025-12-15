using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetroMarketPlatform.API.Data;
using PetroMarketPlatform.API.Models;

namespace PetroMarketPlatform.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FutureSuppliesController : ControllerBase
    {
        private readonly PetroContext _context;
        public FutureSuppliesController(PetroContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> GetFutureSupplies()
        {
            var supplies = await _context.FutureSupplies.ToListAsync();
            return Ok(supplies);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<FutureSupply>> GetFutureSupply(int id)
        {
            var fs = await _context.FutureSupplies.FindAsync(id);
            return fs == null ? NotFound() : fs;
        }

        [HttpPost]
        public async Task<ActionResult<FutureSupply>> PostFutureSupply(FutureSupply fs)
        {
            _context.FutureSupplies.Add(fs);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetFutureSupply), new { id = fs.FutureSupplyId }, fs);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutFutureSupply(int id, FutureSupply fs)
        {
            if (id != fs.FutureSupplyId) return BadRequest();
            _context.Entry(fs).State = EntityState.Modified;

            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) {
                if (!_context.FutureSupplies.Any(e => e.FutureSupplyId == id)) return NotFound();
                throw;
            }
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFutureSupply(int id)
        {
            var fs = await _context.FutureSupplies.FindAsync(id);
            if (fs == null) return NotFound();
            _context.FutureSupplies.Remove(fs);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
