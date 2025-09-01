using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HubApi.Data;
using HubApi.Models;
using HubApi.DTOs;

namespace HubApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GatewayPartnersController : ControllerBase
    {
        private readonly OrderHubDbContext _context;

        public GatewayPartnersController(OrderHubDbContext context)
        {
            _context = context;
        }

        // GET: api/gatewaypartners
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GatewayPartner>>> GetGatewayPartners()
        {
            try
            {
                // First try without includes to see if basic query works
                var partners = await _context.GatewayPartners
                    .OrderBy(gp => gp.PartnerName)
                    .ToListAsync();
                
                return partners;
            }
            catch (Exception ex)
            {
                // Log the error and return a more specific error message
                return StatusCode(500, $"Database error: {ex.Message}");
            }
        }

        // GET: api/gatewaypartners/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<GatewayPartner>> GetGatewayPartner(Guid id)
        {
            var gatewayPartner = await _context.GatewayPartners
                .Include(gp => gp.GatewayAssignments)
                .ThenInclude(ga => ga.PaymentGateway)
                .FirstOrDefaultAsync(gp => gp.Id == id);

            if (gatewayPartner == null)
            {
                return NotFound();
            }

            return gatewayPartner;
        }

        // POST: api/gatewaypartners
        [HttpPost]
        public async Task<ActionResult<GatewayPartner>> CreateGatewayPartner(GatewayPartnerRequest request)
        {
            // Check if partner code already exists
            if (await _context.GatewayPartners.AnyAsync(gp => gp.PartnerCode == request.PartnerCode))
            {
                return BadRequest("Partner code already exists");
            }

            var gatewayPartner = new GatewayPartner
            {
                Id = Guid.NewGuid(),
                PartnerName = request.PartnerName,
                PartnerCode = request.PartnerCode,
                Description = request.Description,
                RevenueSharePercentage = request.RevenueSharePercentage,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.GatewayPartners.Add(gatewayPartner);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetGatewayPartner), new { id = gatewayPartner.Id }, gatewayPartner);
        }

        // PUT: api/gatewaypartners/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateGatewayPartner(Guid id, GatewayPartnerRequest request)
        {
            var gatewayPartner = await _context.GatewayPartners.FindAsync(id);
            if (gatewayPartner == null)
            {
                return NotFound();
            }

            // Check if partner code already exists for different partner
            if (await _context.GatewayPartners.AnyAsync(gp => gp.PartnerCode == request.PartnerCode && gp.Id != id))
            {
                return BadRequest("Partner code already exists");
            }

            gatewayPartner.PartnerName = request.PartnerName;
            gatewayPartner.PartnerCode = request.PartnerCode;
            gatewayPartner.Description = request.Description;
            gatewayPartner.RevenueSharePercentage = request.RevenueSharePercentage;
            gatewayPartner.IsActive = request.IsActive;
            gatewayPartner.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GatewayPartnerExists(id))
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

        // DELETE: api/gatewaypartners/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGatewayPartner(Guid id)
        {
            var gatewayPartner = await _context.GatewayPartners
                .Include(gp => gp.GatewayAssignments)
                .FirstOrDefaultAsync(gp => gp.Id == id);

            if (gatewayPartner == null)
            {
                return NotFound();
            }

            // Check if partner has active assignments
            if (gatewayPartner.GatewayAssignments.Any(ga => ga.IsActive))
            {
                return BadRequest("Cannot delete partner with active gateway assignments");
            }

            _context.GatewayPartners.Remove(gatewayPartner);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/gatewaypartners/assign
        [HttpPost("assign")]
        public async Task<ActionResult<GatewayPartnerAssignment>> AssignToGateway(GatewayPartnerAssignmentRequest request)
        {
            // Check if partner exists
            var partner = await _context.GatewayPartners.FindAsync(request.GatewayPartnerId);
            if (partner == null)
            {
                return BadRequest("Gateway partner not found");
            }

            // Check if payment gateway exists
            var paymentGateway = await _context.PaymentGatewayDetails.FindAsync(request.PaymentGatewayId);
            if (paymentGateway == null)
            {
                return BadRequest("Payment gateway not found");
            }

            // Check if assignment already exists
            var existingAssignment = await _context.GatewayPartnerAssignments
                .FirstOrDefaultAsync(ga => ga.GatewayPartnerId == request.GatewayPartnerId && 
                                         ga.PaymentGatewayId == request.PaymentGatewayId);

            if (existingAssignment != null)
            {
                // Update existing assignment
                existingAssignment.AssignmentPercentage = request.AssignmentPercentage;
                existingAssignment.IsActive = request.IsActive;
                existingAssignment.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new assignment
                var assignment = new GatewayPartnerAssignment
                {
                    Id = Guid.NewGuid(),
                    GatewayPartnerId = request.GatewayPartnerId,
                    PaymentGatewayId = request.PaymentGatewayId,
                    AssignmentPercentage = request.AssignmentPercentage,
                    IsActive = request.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.GatewayPartnerAssignments.Add(assignment);
            }

            await _context.SaveChangesAsync();

            return Ok();
        }

        // GET: api/gatewaypartners/assignments
        [HttpGet("assignments")]
        public async Task<ActionResult<IEnumerable<object>>> GetAssignments()
        {
            try
            {
                // Return basic assignment data without includes to avoid EF issues
                var assignments = await _context.GatewayPartnerAssignments
                    .OrderBy(ga => ga.Id)
                    .Select(ga => new
                    {
                        id = ga.Id,
                        gatewayPartnerId = ga.GatewayPartnerId,
                        paymentGatewayId = ga.PaymentGatewayId,
                        assignmentPercentage = ga.AssignmentPercentage,
                        isActive = ga.IsActive,
                        createdAt = ga.CreatedAt,
                        updatedAt = ga.UpdatedAt
                    })
                    .ToListAsync();
                
                return assignments;
            }
            catch (Exception ex)
            {
                // Log the error and return a more specific error message
                return StatusCode(500, $"Database error loading assignments: {ex.Message}");
            }
        }

        // DELETE: api/gatewaypartners/assignments/{id}
        [HttpDelete("assignments/{id}")]
        public async Task<IActionResult> DeleteAssignment(Guid id)
        {
            var assignment = await _context.GatewayPartnerAssignments.FindAsync(id);
            if (assignment == null)
            {
                return NotFound();
            }

            _context.GatewayPartnerAssignments.Remove(assignment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool GatewayPartnerExists(Guid id)
        {
            return _context.GatewayPartners.Any(e => e.Id == id);
        }
    }
}
