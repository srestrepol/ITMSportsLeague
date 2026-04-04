using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SportsLeague.API.DTOs.Request;
using SportsLeague.API.DTOs.Response;
using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Interfaces.Services;
using System.Security.Cryptography.X509Certificates;

namespace SportsLeague.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SponsorController : ControllerBase
{
    private readonly ISponsorService _sponsorService;
    private readonly IMapper _mapper;
    private readonly ILogger<SponsorController> _logger;

    public SponsorController(
        ISponsorService sponsorService,
        IMapper mapper,
        ILogger<SponsorController> logger)
    {
        _sponsorService = sponsorService;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SponsorResponseDTO>>> GetAll()
    {
        var sponsors = await _sponsorService.GetAllAsync();
        var sponsorDTOs = _mapper.Map<IEnumerable<SponsorResponseDTO>>(sponsors);
        return Ok(sponsorDTOs);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SponsorResponseDTO>> GetById(int id)
    {
        var sponsor = await _sponsorService.GetByIdAsync(id);
        if (sponsor == null)
            return NotFound(new { message = $"Patrocinador con ID {id} no encontrado" });

        var sponsorDTO = _mapper.Map<SponsorResponseDTO>(sponsor);
        return Ok(sponsorDTO);
    }

    [HttpPost]
    public async Task<ActionResult<SponsorResponseDTO>> Create(SponsorRequestDTO dto)
    {
        try
        {
            var sponsor = _mapper.Map<Sponsor>(dto);
            var createdSponsor = await _sponsorService.CreateAsync(sponsor);
            var responseDto = _mapper.Map<SponsorResponseDTO>(createdSponsor);

            return CreatedAtAction(
                nameof(GetById), 
                new { id = responseDto.Id }, 
                responseDto);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, SponsorRequestDTO dto)
    {
        try
        {
            var sponsor = _mapper.Map<Sponsor>(dto);
            await _sponsorService.UpdateAsync(id, sponsor);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            await _sponsorService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("{id}/tournaments")]
    public async Task<ActionResult<IEnumerable<TournamentResponseDTO>>> GetTournaments(int id)
    {
        try
        {
            var tournaments = await _sponsorService.GetTournamentsBySponsorAsync(id);
            var tournamentDTO = _mapper.Map<IEnumerable<TournamentResponseDTO>>(tournaments);
            return Ok(tournamentDTO);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/tournaments")]
    public async Task<ActionResult<TournamentSponsorResponseDTO>> LinkToTournament(int id, TournamentSponsorRequestDTO dto)
    {
        try
        {
            var relation = await _sponsorService.LinkToTournamentAsync(id, dto.TournamentId, dto.ContractAmount);

            var tournaments = await _sponsorService.GetTournamentsBySponsorAsync(id);
            var linkedTournament = tournaments.FirstOrDefault(t => t.Id == dto.TournamentId);
            var sponsor = await _sponsorService.GetByIdAsync(id);

            var responseDto = new TournamentSponsorResponseDTO
            {
                Id = relation.Id,
                TournamentId = relation.TournamentId,
                TournamentName = linkedTournament?.Name ?? string.Empty,
                SponsorId = relation.SponsorId,
                SponsorName = sponsor?.Name ?? string.Empty,
                ContractAmount = relation.ContractAmount,
                JoinedAt = relation.JoinedAt
            };

            return CreatedAtAction(
                nameof(GetTournaments),
                new { id },
                responseDto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}/tournaments/{tid}")]
    public async Task<ActionResult> UnlinkFromTournament(int id, int tid)
    {
        try
        {
            await _sponsorService.UnlinkFromTournamentAsync(id, tid);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
