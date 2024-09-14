using CodeMechanic.Diagnostics;
using CodeMechanic.FileSystem;
using DotNetTools.SharpGrabber;
using DotNetTools.SharpGrabber.Grabbed;

// using CodeMechanic.Scraper;
// using CodeMechanic.Scriptures;

var builder = WebApplication.CreateBuilder(args);
DotEnv.Load();
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
//
// var grabber = GrabberBuilder.New()
//     .UseDefaultServices()
//     .AddYouTube()
//     .AddVimeo()
//     .Build();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
// var html_scraper = new HtmlScraperService();
// var teachings_svc = new WordPressReaderService();

// string url = "https://ammoseek.com/ammo/224-valkyrie";
// 

string url = "https://books.toscrape.com/";


app.MapGet("/tpot/download", async (int per_page, int page) =>
{
    if (per_page <= 0) throw new ArgumentOutOfRangeException(nameof(per_page));
    if (page <= 0) throw new ArgumentOutOfRangeException(nameof(page));

    // string cwd = Directory.GetCurrentDirectory();
    // string folder = "teachings";
    // string save_dir = Path.Combine(cwd, folder);

    // var results = await teachings_svc.GetPaginatedRangeOfTeachings(per_page, page);

    // results.FirstOrDefault().Dump("first record");
    // return results;

    // results.Dump("results");
});


app.MapGet("ammo/scrape/anglesharp", async () =>
{
    // var results = await html_scraper.ScrapeHtmlFromAnglesharp<AmmoseekRow>(url);
    // results.Dump("results");
});

app.MapGet("ammo/scrape/hap", async () =>
{
    // List<AmmoseekRow> records = new(0);
    // var response = await html_scraper
    //     .ScrapeHtmlTable<AmmoseekRow>(url);
    // response.Dump("response");
    // return records;
});

app.MapGet("/ammo/scrape/httpclient", async () =>
{
    // List<AmmoseekRow> records = new(0);
    // var response = await html_scraper.ScrapeHtmlFromHttpClient<AmmoseekRow>(url);
    // response.Dump("response");
    // return records;
}).WithOpenApi();

app.MapGet("videos/download",
    async (string url, string? format, string? handler) =>
    {
        var downloader = new SharpGrapperService();

        await downloader.Download(url);

        return "done";
    });


app.Run();