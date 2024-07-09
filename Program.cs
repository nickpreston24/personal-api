using CodeMechanic.Diagnostics;
using CodeMechanic.FileSystem;
using CodeMechanic.Scraper;
using CodeMechanic.Scriptures;

var builder = WebApplication.CreateBuilder(args);
DotEnv.Load();
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
var html_scraper = new HtmlScraperService();
var teachings_svc = new WordPressReaderService();

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

    var results = await teachings_svc.GetPaginatedRangeOfTeachings(per_page, page);

    // results.FirstOrDefault().Dump("first record");
    return results;

    // results.Dump("results");
});


app.MapGet("ammo/scrape/anglesharp", async () =>
{
    var results = await html_scraper.ScrapeHtmlFromAnglesharp<AmmoseekRow>(url);
    results.Dump("results");
});

app.MapGet("ammo/scrape/hap", async () =>
{
    List<AmmoseekRow> records = new(0);
    var response = await html_scraper
        .ScrapeHtmlTable<AmmoseekRow>(url);
    response.Dump("response");
    return records;
});

app.MapGet("/ammo/scrape/httpclient", async () =>
{
    List<AmmoseekRow> records = new(0);
    var response = await html_scraper.ScrapeHtmlFromHttpClient<AmmoseekRow>(url);
    response.Dump("response");
    return records;
}).WithOpenApi();


app.MapGet("youtube/download", async (string url, string format) =>
{

});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}


public class AmmoseekRow
{
    public string retailer { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public string brand { get; set; } = string.Empty;
    public string caliber { get; set; } = string.Empty;
    public string grains { get; set; } = string.Empty;
    public string limits { get; set; } = string.Empty;
    public string casing { get; set; } = string.Empty;
    public string is_new { get; set; } = string.Empty;
    public string price { get; set; } = string.Empty;
    public string rounds { get; set; } = string.Empty;
    public string price_per_round { get; set; } = string.Empty;
    public string shipping_rating { get; set; } = string.Empty;
    public string last_update { get; set; } = string.Empty; // last time Ammoseek updated this row.

    // Admin properties
    public string environment { get; set; } = ""; // Dev or prod
    public DateTimeOffset created_at { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset last_updated_at { get; set; } = DateTimeOffset.Now; // last time I updated this row!

    public string last_updated_by { get; set; } = string.Empty;
    public string created_by { get; set; } = string.Empty;
}