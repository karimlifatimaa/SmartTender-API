using Microsoft.AspNetCore.Mvc;
using SmartTender.Application.Interfaces;
using System.Threading.Tasks;

namespace SmartTender_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TenderController : ControllerBase
    {
        private readonly ITenderService _tenderService;

        public TenderController(ITenderService tenderService)
        {
            _tenderService = tenderService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _tenderService.GetAllTendersAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _tenderService.GetTenderByIdAsync(id);
            if (result == null)
                return NotFound();
            return Ok(result);
        }
    }
} 