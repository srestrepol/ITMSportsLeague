using SportsLeague.Domain.Entities;

namespace SportsLeague.Domain.Interfaces.Repositories;

public interface ITournamentSponsorRepository : IGenericRepository<TournamentSponsor>
{
    Task<TournamentSponsor?> GetByTournamentAndSponsorAsync(int tournamentId, int sponsorId);
    Task<IEnumerable<TournamentSponsor>> GetByTournamentIdAsync(int tournametId);
    Task<IEnumerable<TournamentSponsor>> GetBySponsorIdAsync(int sponsorId);
}
