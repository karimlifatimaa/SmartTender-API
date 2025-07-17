using Microsoft.AspNetCore.Mvc;
using SmartTender.Application.Interfaces;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using SmartTender.Application.DTOs;
using SmartTender.Application.Common;

namespace SmartTender_API.Controllers
{
    [Authorize(Roles = Role.Admin)]
    [ApiController]
    [Route("api/[controller]")]
    public class TenderController : ControllerBase
    {
        private readonly ITenderService _tenderService;

        public TenderController(ITenderService tenderService)
        {
            _tenderService = tenderService;
        }

        [Authorize(Roles = Role.Admin)]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PaginationParams paginationParams)
        {
            var result = await _tenderService.GetAllTendersAsync(paginationParams);
            return Ok(result);
        }

        [Authorize(Roles = Role.Admin)]
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