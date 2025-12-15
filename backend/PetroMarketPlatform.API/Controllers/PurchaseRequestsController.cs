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
    public class PurchaseRequestsController : ControllerBase
    {
        private readonly PetroContext _context;
        public PurchaseRequestsController(PetroContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> GetPurchaseRequests()
        {
            var reqs = await _context.PurchaseRequests.ToListAsync();
            return Ok(reqs);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PurchaseRequest>> GetPurchaseRequest(int id)
        {
            var pr = await _context.PurchaseRequests.FindAsync(id);
            return pr == null ? NotFound() : pr;
        }

        [HttpPost]
        public async Task<ActionResult<PurchaseRequest>> PostPurchaseRequest(PurchaseRequest pr)
        {
            _context.PurchaseRequests.Add(pr);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetPurchaseRequest), new { id = pr.PurchaseRequestId }, pr);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutPurchaseRequest(int id, PurchaseRequest pr)
        {
            if (id != pr.PurchaseRequestId) return BadRequest();
            _context.Entry(pr).State = EntityState.Modified;

            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) {
                if (!_context.PurchaseRequests.Any(e => e.PurchaseRequestId == id)) return NotFound();
                throw;
            }
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePurchaseRequest(int id)
        {
            var pr = await _context.PurchaseRequests.FindAsync(id);
            if (pr == null) return NotFound();
            _context.PurchaseRequests.Remove(pr);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
