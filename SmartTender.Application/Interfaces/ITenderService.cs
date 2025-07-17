using System.Collections.Generic;
using System.Threading.Tasks;
using SmartTender.Application.DTOs;

namespace SmartTender.Application.Interfaces
{
    public interface ITenderService
    {
        Task<List<TenderDto>> GetAllTendersAsync(PaginationParams paginationParams);
        Task<TenderDto> GetTenderByIdAsync(int id);
        Task SyncTendersFromRemoteAsync();
    }
} 