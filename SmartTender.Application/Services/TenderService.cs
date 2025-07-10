using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SmartTender.Application.DTOs;
using SmartTender.Application.Interfaces;
using SmartTender.Domain;
using SmartTender.Infrastructure;

namespace SmartTender.Application.Services
{
    public class TenderService : ITenderService
    {
        private readonly SmartTenderDbContext _db;
        private readonly IMapper _mapper;
        private readonly IHttpClientFactory _httpClientFactory;

        public TenderService(SmartTenderDbContext db, IMapper mapper, IHttpClientFactory httpClientFactory)
        {
            _db = db;
            _mapper = mapper;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<List<TenderDto>> GetAllTendersAsync()
        {
            var tenders = await _db.Tenders
                .Include(t => t.TenderCategories).ThenInclude(tc => tc.Category)
                .Include(t => t.BomLines)
                .Include(t => t.ContactPersons)
                .Include(t => t.Announcements)
                .ToListAsync();
            return _mapper.Map<List<TenderDto>>(tenders);
        }

        public async Task<TenderDto> GetTenderByIdAsync(int id)
        {
            var tender = await _db.Tenders
                .Include(t => t.TenderCategories).ThenInclude(tc => tc.Category)
                .Include(t => t.BomLines)
                .Include(t => t.ContactPersons)
                .Include(t => t.Announcements)
                .FirstOrDefaultAsync(t => t.Id == id);
            return _mapper.Map<TenderDto>(tender);
        }

        public async Task SyncTendersFromRemoteAsync()
        {
            var client = _httpClientFactory.CreateClient();
            var remoteUrl = "https://etender.gov.az/api/events?EventType=2&PageSize=10000&PageNumber=1&EventStatus=1&Keyword=&buyerOrganizationName=&PrivateRfxId=&publishDateFrom=&publishDateTo=&AwardedparticipantName=&AwardedparticipantVoen=&DocumentViewType=";
            var response = await client.GetFromJsonAsync<RemoteTenderListResponse>(remoteUrl);
            if (response?.items == null) return;

            foreach (var remoteTender in response.items)
            {
                var exists = await _db.Tenders.AnyAsync(t => t.EventId == remoteTender.eventId);
                if (!exists)
                {
                    var tender = new Tender
                    {
                        // Id set edilmir, EF Core özü verir
                        RfxId = remoteTender.privateRfxId,
                        EventId = remoteTender.eventId,
                        TenderName = remoteTender.eventName,
                        OrganizationName = remoteTender.buyerOrganizationName,
                        OrganizationVoen = null, // Sonradan doldurulacaq
                        EnvelopeDate = 0, // Sonradan doldurulacaq
                        EndDate = remoteTender.endDate != null ? DateTimeToUnix(remoteTender.endDate) : 0,
                        PublishDate = remoteTender.publishDate != null ? DateTimeToUnix(remoteTender.publishDate) : 0,
                        StartDate = 0, // Sonradan doldurulacaq
                        BudgetCategoryCode = null,
                        Address = null,
                        CpvCode = null,
                        EventType = remoteTender.eventType,
                        IsRedirectionAvailable = false,
                        MinNumberOfSuppliers = 0,
                        EstimatedAmount = 0
                    };
                    _db.Tenders.Add(tender);
                }
            }
            await _db.SaveChangesAsync();
        }

        private long DateTimeToUnix(string dateTime)
        {
            if (DateTime.TryParse(dateTime, out var dt))
                return ((DateTimeOffset)dt).ToUnixTimeSeconds();
            return 0;
        }

        public class RemoteTenderListResponse
        {
            public List<RemoteTenderItem> items { get; set; }
        }
        public class RemoteTenderItem
        {
            public int eventId { get; set; }
            public int eventType { get; set; }
            public int eventStatus { get; set; }
            public string buyerOrganizationName { get; set; }
            public string eventName { get; set; }
            public string publishDate { get; set; }
            public string endDate { get; set; }
            public bool hasNewVersion { get; set; }
            public int privateRfxId { get; set; }
        }
    }
} 