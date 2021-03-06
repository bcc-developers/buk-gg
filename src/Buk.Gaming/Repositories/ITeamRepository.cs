using Buk.Gaming.Models;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;


namespace Buk.Gaming.Repositories
{
    public interface ITeamRepository
    {
        // TEAMS
        Task<Team> GetTeamAsync(string teamId);

        Task<Team[]> GetTeamsAsync();

        Task<Team[]> GetMyTeamsAsync(string playerId);

        Task<Team[]> GetTeamsInGameAsync(string gameId);

        Task<Team> AddTeamAsync(User requester, Team team);

        Task<SanityResult<Team>> UpdateTeamAsync(User requester, Team team);

        Task<bool> DeleteTeamAsync(User requester, Team team);

        Task<Game[]> GetGamesAsync();

        // // MEMBERS
        // Task<Member> GetMemberAsync(Player player);

        // Task<Member> AddMemberAsync(User requester, string teamId, Player player);

        // Task<Member> UpdateMemberAsync(User requester, string teamId, Player player);

        // Task<bool> RemoveMemberAsync(User requester, string teamId, Player player);
    }
}