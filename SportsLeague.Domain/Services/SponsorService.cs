using System.Net.Mail;
using Microsoft.Extensions.Logging;
using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Interfaces.Repositories;
using SportsLeague.Domain.Interfaces.Services;

namespace SportsLeague.Domain.Services;

public class SponsorService : ISponsorService
{
    private readonly ISponsorRepository _sponsorRepository;
    private readonly ITournamentRepository _tournamentRepository;
    private readonly ITournamentSponsorRepository _tournamentSponsorRepository;
    private readonly ILogger<SponsorService> _logger;

    public SponsorService(
        ISponsorRepository sponsorRepository,
        ITournamentRepository tournamentRepository,
        ITournamentSponsorRepository tournamentSponsorRepository,
        ILogger<SponsorService> logger)
    {
        _sponsorRepository = sponsorRepository;
        _tournamentRepository = tournamentRepository;
        _tournamentSponsorRepository = tournamentSponsorRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<Sponsor>> GetAllAsync()
    {
        _logger.LogInformation("Retrieving all sponsors");
        return await _sponsorRepository.GetAllAsync();
    }

    public async Task<Sponsor?> GetByIdAsync(int id)
    {
        _logger.LogInformation("Retrieving sponsor with ID {SponsorId}", id);

        var sponsor = await _sponsorRepository.GetByIdAsync(id);

        if (sponsor == null) 
        {
            _logger.LogWarning("Sponsor with ID {SponsorId} not found", id);
        }

        return sponsor;
    }

    public async Task<Sponsor> CreateAsync(Sponsor sponsor)
    {
        ValidateEmail(sponsor.ContactEmail);

        var existingSponsor = await _sponsorRepository.GetByNameAsync(sponsor.Name);
        if (existingSponsor != null)
        {
            _logger.LogWarning("Sponsor with name '{SponsorName}' already exists", sponsor.Name);
            throw new InvalidOperationException(
                $"Ya existe un patrocinador con el nombre '{sponsor.Name}'");
        }
        _logger.LogInformation("Creating sponsor: '{SponsorName}'", sponsor.Name);
        return await _sponsorRepository.CreateAsync(sponsor);
    }

    public async Task UpdateAsync(int id, Sponsor sponsor)
    {
        var existingSponsor = await _sponsorRepository.GetByIdAsync(id);
        if (existingSponsor == null)
        {
            _logger.LogWarning("Sponsor with ID {SponsorId} not found for update", id);
            throw new KeyNotFoundException(
                $"No se encontró el patrocinador con ID {id}");
        }

        ValidateEmail(sponsor.ContactEmail);

        if (!string.Equals(existingSponsor.Name, sponsor.Name, StringComparison.OrdinalIgnoreCase))
        {
            var sponsorWithSameName = await _sponsorRepository.GetByNameAsync(sponsor.Name);
            if (sponsorWithSameName != null && sponsorWithSameName.Id != id)
            {
                throw new InvalidOperationException(
                    $"Ya existe un patrocinador con el nombre '{sponsor.Name}'");
            }
        }

        existingSponsor.Name = sponsor.Name;
        existingSponsor.ContactEmail = sponsor.ContactEmail;
        existingSponsor.Phone = sponsor.Phone;
        existingSponsor.WebsiteUrl = sponsor.WebsiteUrl;
        existingSponsor.Category = sponsor.Category;

        _logger.LogInformation("Updating sponsor with ID {SponsorId}", id);
        await _sponsorRepository.UpdateAsync(existingSponsor);
    }

    public async Task DeleteAsync(int id)
    {
        var exists = await _sponsorRepository.ExistsAsync(id);
        if (!exists)
        {
            _logger.LogWarning("Sponsor with ID {SponsorId} not found for deletion", id);
            throw new KeyNotFoundException(
                $"No se encontró el patrocinador con ID {id}");
        }

        _logger.LogInformation("Deleting sponsor with ID: {SponsorId}", id);
        await _sponsorRepository.DeleteAsync(id);
    }

    public async Task<IEnumerable<Tournament>> GetTournamentsBySponsorAsync(int sponsorId)
    {
        var sponsorExists = await _sponsorRepository.ExistsAsync(sponsorId);
        if (!sponsorExists)
        {
            throw new KeyNotFoundException(
                $"No se encontró el patrocinador con ID {sponsorId}");
        }

        var relations = await _tournamentSponsorRepository.GetBySponsorIdAsync(sponsorId);

        return relations.Select(ts => ts.Tournament);
    }

    public async Task<TournamentSponsor> LinkToTournamentAsync(int sponsorId, int tournamentId, decimal contractAmount)
    {
        var sponsorExists = await _sponsorRepository.ExistsAsync(sponsorId);
        if (!sponsorExists)
        {
            throw new KeyNotFoundException(
                $"No se encontró el patrocinador con ID {sponsorId}");
        }

        var tournamentExists = await _tournamentRepository.ExistsAsync(tournamentId);
        if (!tournamentExists)
        {
            throw new KeyNotFoundException(
                $"No se encontró el torneo con ID {tournamentId}");
        }

        if (contractAmount <= 0)
        {
            throw new InvalidOperationException(
                "El monto del contrato debe ser mayor que 0");
        }

        var existingRelation = await _tournamentSponsorRepository
            .GetByTournamentAndSponsorAsync(tournamentId, sponsorId);

        if (existingRelation != null)
        {
            throw new InvalidOperationException(
                "Este patrocinador ya está vinculado a este torneo");
        }

        var tournamentSponsor = new TournamentSponsor
        {
            SponsorId = sponsorId,
            TournamentId = tournamentId,
            ContractAmount = contractAmount,
            JoinedAt = DateTime.UtcNow
        };

        _logger.LogInformation(
            "Linking sponsor {SponsorId} to tournament {TournamentId}",
            sponsorId, tournamentId);

        return await _tournamentSponsorRepository.CreateAsync(tournamentSponsor);
    }

    public async Task UnlinkFromTournamentAsync(int sponsorId, int tournamentId)
    {
        var sponsorExists = await _tournamentSponsorRepository.ExistsAsync(sponsorId);
        if (!sponsorExists)
        {
            throw new KeyNotFoundException(
                $"No se encontró el patrocinador con ID {sponsorId}");
        }

        var tournamentExists = await _tournamentRepository.ExistsAsync(tournamentId);
        if (!tournamentExists)
        {
            throw new KeyNotFoundException(
                $"No se encontró el torneo con ID {tournamentId}");
        }

        var existingRelation = await _tournamentSponsorRepository
            .GetByTournamentAndSponsorAsync(tournamentId, sponsorId);

        if (existingRelation == null)
        {
            throw new KeyNotFoundException(
                "No existe una vinculación entre este patrocinador y este torneo");
        }

        _logger.LogInformation(
            "Unlinking sponsor {SponsorId} from tournament {TournamentId}",
            sponsorId, tournamentId);

        await _tournamentSponsorRepository.DeleteAsync(existingRelation.Id);
    }

    private static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException(
                "El correo electrónico es obligatorio");
        }

        try
        {
            var mailAddress = new MailAddress(email);

            if (!string.Equals(mailAddress.Address, email, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "El email de contacto no tiene un formato válido");
            }
        }
        catch
        {
            throw new InvalidOperationException(
                "El email de contacto no tiene un formato válido");
        }
    }
}
