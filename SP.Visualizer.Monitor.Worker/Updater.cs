using Microsoft.EntityFrameworkCore;
using SP.Visualizer.Monitor.Comuns.Helpers;
using SP.Visualizer.Monitor.Dal;
using SP.Visualizer.Monitor.Dal.Models;

namespace SP.Visualizer.Monitor.Worker;
internal class Updater : IDisposable
{
    private static Random _random = new();
    public static string RandomColor => string.Format("#{0:X6}", _random.Next(0x1000000));
    public GraphHelper Helper { get; set; }

    private readonly MonitorContext ctx;

    public Updater(GraphHelper helper, MonitorContext ctx)
    {
        _random = new Random();
        this.ctx = ctx;
        Helper = helper;
    }

    public async Task UpdateAccountsAsync()
    {
        var users = await Helper.GetUsersAsync();

        bool next;
        do
        {
            foreach (var user in users)
            {
                Account? acc = await ctx.Accounts.FirstOrDefaultAsync(st => st.Id == user.Id);

                if (acc == null)
                {
                    acc = new Account()
                    {
                        Id = user.Id,
                        Email = user.Mail,
                        Name = user.DisplayName,
                        Color = RandomColor
                    };

                    await ctx.Accounts.AddAsync(acc);
                }
                else
                {
                    acc.Name = user.DisplayName;
                    acc.Email = user.Mail;

                    if (acc.Color.Equals("#000", StringComparison.InvariantCultureIgnoreCase))
                        acc.Color = RandomColor;
                }
            }

            await ctx.SaveChangesAsync();

            next = users.NextPageRequest != null;

            if (next)
                users = await users.NextPageRequest.GetAsync();

        } while (next);
    }

    public async Task UpdateSitesAsync()
    {
        var sites = await Helper.GetSitesAsync();

        bool next;
        do
        {
            foreach (var item in sites)
            {
                Site? site = await ctx.Sites.FirstOrDefaultAsync(st => st.Id == item.Id);

                if (site == null)
                {
                    site = new()
                    {
                        Id = item.Id,
                        Name = item.DisplayName,
                        WebUrl = item.WebUrl
                    };

                    await ctx.Sites.AddAsync(site);
                }
                else
                {
                    site.Name = item.DisplayName;
                    site.WebUrl = item.WebUrl;
                }
            }

            await ctx.SaveChangesAsync();

            next = sites.NextPageRequest != null;

            if (next)
                sites = await sites.NextPageRequest.GetAsync();

        } while (next);
    }

    public async Task UpdateListsAsync()
    {
        Site[] sites = await ctx.Sites.ToArrayAsync();

        foreach (Site site in sites)
        {
            var lists = await Helper.GetListsAsync(site.Id);

            bool next;
            do
            {
                foreach (var item in lists)
                {
                    List? list = await ctx.Lists.FirstOrDefaultAsync(st => st.Id == item.Id);

                    if (list == null)
                    {
                        Microsoft.Graph.Drive drive = await Helper.GetDriveWithListAsync(site.Id, item.Id);

                        list = new()
                        {
                            Id = item.Id,
                            Name = item.DisplayName,
                            SiteId = site.Id,
                            DriveId = drive?.Id,
                            WebUrl = item.WebUrl,
                            Directory = Folder.RemoveSeparetors(item.WebUrl.Remove(0, site.WebUrl.Length))
                        };

                        await ctx.Lists.AddAsync(list);
                    }
                    else
                    {
                        list.Name = item.DisplayName;
                        list.WebUrl = item.WebUrl;
                        list.Directory = Folder.RemoveSeparetors(item.WebUrl.Remove(0, site.WebUrl.Length));
                    }

                }

                await ctx.SaveChangesAsync();

                next = lists.NextPageRequest != null;

                if (next)
                    lists = await lists.NextPageRequest.GetAsync();

            } while (next);
        }
    }

    public async Task UpdateItemsAsync()
    {
        List[] lists = await ctx.Lists.Include(li => li.Site).ToArrayAsync();

        foreach (List list in lists)
        {
            var items = await Helper.GetItemsAsync(list.Site.Id, list.Id);

            bool next;
            do
            {
                foreach (var item in items)
                {
                    string directory = item.WebUrl[list.Site.WebUrl.Length..];
                    string fileName = Folder.RemoveSeparetors(Path.GetFileName(directory));
                    directory = Folder.RemoveSeparetors(directory[..^fileName.Length]);

                    #region Add Folder
                    if (item.ContentType.Name == "Folder")
                    {
                        Folder? folder = await ctx.Folders.FirstOrDefaultAsync(fol => fol.Id == item.Id);
                        Folder? father = await ctx.Folders.FirstOrDefaultAsync(fol => fol.Directory == directory);

                        if (folder == null)
                        {
                            folder = new()
                            {
                                Id = item.Id,
                                Directory = Path.Combine(directory, fileName),
                                Name = fileName,
                                ListId = list.Id,
                                FatherId = father?.Id,
                            };

                            await ctx.Folders.AddAsync(folder);
                        }
                        else
                        {
                            folder.Name = fileName;
                            folder.Directory = Path.Combine(directory, fileName);
                        }

                        continue;
                    }
                    #endregion

                    Item? file = await ctx.Items.FirstOrDefaultAsync(it => it.Id == item.Id);
                    Folder? path = await ctx.Folders.FirstOrDefaultAsync(fol => fol.Directory == directory);

                    if (file == null)
                    {
                        var it = await Helper.GetItemAsync(list.DriveId, item.ETag.Replace("\"", string.Empty).Split(",")[0]);
                        file = new()
                        {
                            Id = item.Id,
                            Name = it.Name,
                            WebUrl = item.WebUrl,
                            Etag = item.ETag.Replace("\"", string.Empty),
                            MimeType = it.File.MimeType,
                            FolderId = path?.Id,
                            ListId = path == null ? list.Id : null
                        };

                        await ctx.Items.AddAsync(file);
                    }
                    else
                    {
                        file.Name = fileName;
                        file.FolderId = path?.Id;
                        file.ListId = path == null ? list.Id : null;
                        file.WebUrl = item.WebUrl;
                    }

                }

                await ctx.SaveChangesAsync();

                next = items.NextPageRequest != null;

                if (next)
                    items = await items.NextPageRequest.GetAsync();

            } while (next);
        }
    }
    public void Dispose()
    {
        ctx.Dispose();
        Helper.Dispose();
        GC.SuppressFinalize(this);
    }
}
