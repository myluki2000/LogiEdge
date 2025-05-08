using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogiEdge.BaseService.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogiEdge.BaseService.Services
{
    public class SettingsService
    {
        private readonly IDbContextFactory<ApplicationDbContext> applicationDbContextFactory;

        internal SettingsService(IDbContextFactory<ApplicationDbContext> applicationDbContextFactory)
        {
            this.applicationDbContextFactory = applicationDbContextFactory;
        }

        public async Task<List<KeyValuePair<string, string>>> GetAllSettingsAsync()
        {
            await using ApplicationDbContext context = await applicationDbContextFactory.CreateDbContextAsync();
            return await context.Settings.Select(s => new KeyValuePair<string, string>(s.Key, s.Value)).ToListAsync();
        }

        public async Task<string?> GetSettingAsync(string key)
        {
            await using ApplicationDbContext context = await applicationDbContextFactory.CreateDbContextAsync();
            SettingsEntry? setting = await context.Settings.FindAsync(key);
            return setting?.Value;
        }

        public async Task SetSettingAsync(string key, string value)
        {
            await using ApplicationDbContext context = await applicationDbContextFactory.CreateDbContextAsync();

            SettingsEntry? setting = await context.Settings.FindAsync(key);

            if(setting == null)
                context.Settings.Add(new SettingsEntry(key, value));
            else
                setting.Value = value;

            await context.SaveChangesAsync();
        }
    }
}
