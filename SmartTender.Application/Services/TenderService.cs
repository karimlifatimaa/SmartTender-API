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
using System;
using System.Text.Json;

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

        public async Task<List<TenderDto>> GetAllTendersAsync(PaginationParams paginationParams)
        {
            var tenders = await _db.Tenders
                .Include(t => t.TenderCategories).ThenInclude(tc => tc.Category)
                .Include(t => t.BomLines)
                .Include(t => t.ContactPersons)
                .Include(t => t.Announcements)
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
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
                .FirstOrDefaultAsync(t => t.EtenderId == id);
            return _mapper.Map<TenderDto>(tender);
        }

        public async Task SyncTendersFromRemoteAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var remoteUrl = "https://etender.gov.az/api/events?EventType=2&PageSize=100&PageNumber=1&EventStatus=1&Keyword=&buyerOrganizationName=&PrivateRfxId=&publishDateFrom=&publishDateTo=&AwardedparticipantName=&AwardedparticipantVoen=&DocumentViewType=";
                var response = await client.GetFromJsonAsync<RemoteTenderListResponse>(remoteUrl);
                if (response?.items == null) return;

                int addedCount = 0;
                foreach (var remoteTender in response.items)
                {
                    var exists = await _db.Tenders.AnyAsync(t => t.EventId == remoteTender.eventId);
                    if (!exists)
                    {
                        var tender = new Tender
                        {
                            EtenderId = remoteTender.eventId,
                            RfxId = remoteTender.privateRfxId,
                            EventId = remoteTender.eventId,
                            TenderName = remoteTender.eventName,
                            OrganizationName = remoteTender.buyerOrganizationName,
                            OrganizationVoen = null,
                            EnvelopeDate = 0,
                            EndDate = remoteTender.endDate != null ? DateTimeToUnix(remoteTender.endDate) : 0,
                            PublishDate = remoteTender.publishDate != null ? DateTimeToUnix(remoteTender.publishDate) : 0,
                            StartDate = 0,
                            BudgetCategoryCode = null,
                            Address = null,
                            CpvCode = null,
                            EventType = remoteTender.eventType,
                            IsRedirectionAvailable = false,
                            MinNumberOfSuppliers = 0,
                            EstimatedAmount = 0,
                            BomLines = new List<BomLine>(),
                            ContactPersons = new List<ContactPerson>(),
                            Announcements = new List<Announcement>(),
                            TenderCategories = new List<TenderCategory>()
                        };

                        _db.Tenders.Add(tender); 
                        await _db.SaveChangesAsync(); 

                        var bomUrl = $"https://etender.gov.az/api/events/{remoteTender.eventId}/bomLines?PageSize=1000&PageNumber=1";
                        var bomResponse = await client.GetAsync(bomUrl);
                        if (bomResponse.IsSuccessStatusCode)
                        {
                            var bomJson = await bomResponse.Content.ReadAsStringAsync();
                            var bomObj = JsonDocument.Parse(bomJson);
                            if (bomObj.RootElement.TryGetProperty("items", out var bomItems))
                            {
                                foreach (var bom in bomItems.EnumerateArray())
                                {
                                    int quantity = 0;
                                    var quantityProp = bom.GetProperty("quantity");
                                    if (quantityProp.ValueKind == JsonValueKind.Number)
                                    {
                                        if (!quantityProp.TryGetInt32(out quantity))
                                            quantity = (int)quantityProp.GetDecimal();
                                    }
                                    else if (quantityProp.ValueKind == JsonValueKind.String)
                                    {
                                        var str = quantityProp.GetString();
                                        if (!int.TryParse(str, out quantity))
                                        {
                                            if (decimal.TryParse(str, out var dec))
                                                quantity = (int)dec;
                                        }
                                    }
                                    var bomLine = new BomLine
                                    {
                                         TenderId = tender.EtenderId, // TenderId-ni set et
                                        //TenderId = tender.Id,
                                        Name = bom.GetProperty("name").GetString(),
                                        Description = bom.GetProperty("description").GetString(),
                                        UnitOfMeasure = bom.GetProperty("unitOfMeasure").GetString(),
                                        Quantity = quantity,
                                        CategoryCode = bom.GetProperty("categoryCode").GetString()
                                    };
                                    tender.BomLines.Add(bomLine);
                                    _db.BomLines.Add(bomLine);
                                    await _db.SaveChangesAsync();
                                }
                            }
                        }

                        var contactUrl = $"https://etender.gov.az/api/events/{remoteTender.eventId}/contact-persons";
                        var contactResponse = await client.GetAsync(contactUrl);
                        if (contactResponse.IsSuccessStatusCode)
                        {
                            var contactJson = await contactResponse.Content.ReadAsStringAsync();
                            var contactArr = JsonDocument.Parse(contactJson).RootElement;
                            if (contactArr.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var contact in contactArr.EnumerateArray())
                                {
                                    var contactPerson = new ContactPerson
                                    {
                                        TenderId = tender.EtenderId, 
                                        //TenderId = tender.Id,
                                        FullName = contact.GetProperty("fullName").GetString(),
                                        Contact = contact.GetProperty("contact").GetString(),
                                        Position = contact.GetProperty("position").GetString(),
                                        PhoneNumber = contact.GetProperty("phoneNumber").GetString()
                                    };
                                    tender.ContactPersons.Add(contactPerson);
                                    _db.ContactPersons.Add(contactPerson);
                                    await _db.SaveChangesAsync();
                                }
                            }
                        }

                        var announcementUrl = $"https://etender.gov.az/api/events/{remoteTender.eventId}/announcements";
                        var announcementResponse = await client.GetAsync(announcementUrl);
                        if (announcementResponse.IsSuccessStatusCode)
                        {
                            var announcementJson = await announcementResponse.Content.ReadAsStringAsync();
                            var announcementObj = JsonDocument.Parse(announcementJson);
                            int announcementVersion = 0;
                            if (announcementObj.RootElement.TryGetProperty("announcementVersion", out var versionProp))
                            {
                                if (versionProp.ValueKind == JsonValueKind.Number)
                                {
                                    announcementVersion = versionProp.GetInt32();
                                }
                                else if (versionProp.ValueKind == JsonValueKind.String && int.TryParse(versionProp.GetString(), out var parsed))
                                {
                                    announcementVersion = parsed;
                                }
                            }
                            if (announcementObj.RootElement.TryGetProperty("announcements", out var annArr))
                            {
                                foreach (var ann in annArr.EnumerateArray())
                                {
                                    var announcement = new Announcement
                                    {
                                        TenderId = tender.EtenderId, // TenderId-ni set et
                                        //TenderId = tender.Id,
                                        AnnouncementVersion = announcementVersion,
                                        Text = ann.GetProperty("text").GetString()
                                    };
                                    tender.Announcements.Add(announcement);
                                    _db.Announcements.Add(announcement);
                                    await _db.SaveChangesAsync();
                                }
                            }
                        }

                        if (remoteTender.categoryCodes.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var cat in remoteTender.categoryCodes.EnumerateArray())
                            {
                                var code = cat.GetString();
                                if (!string.IsNullOrWhiteSpace(code))
                                {
                                    var category = await _db.Categories.FirstOrDefaultAsync(c => c.Code == code);
                                    if (category == null)
                                    {
                                        category = new Category { Code = code, Name = code };
                                        _db.Categories.Add(category);
                                        await _db.SaveChangesAsync();
                                    }
                                    var tenderCategory = new TenderCategory { CategoryId = category.Id, TenderId = tender.Id, Tender = tender };
                                    tender.TenderCategories.Add(tenderCategory);
                                    _db.TenderCategories.Add(tenderCategory);
                                    await _db.SaveChangesAsync();
                                }
                            }
                        }

                       // _db.Tenders.Add(tender);
                        //await _db.SaveChangesAsync();
                        addedCount++;
                        Console.WriteLine($"Yeni tender əlavə olundu: EventId={remoteTender.eventId}, Name={remoteTender.eventName}");
                    }
                }
                await _db.SaveChangesAsync();
                Console.WriteLine($"Tender sync bitdi. Yeni əlavə olunan tender sayı: {addedCount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Tender sync ERROR: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
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
            public JsonElement categoryCodes { get; set; }

            public string? awardedParticipantName { get; set; }
            public string? awardedParticipantVoen { get; set; }
            public int documentViewType { get; set; }
            public int actualVersionId { get; set; }
        }
    }
} 