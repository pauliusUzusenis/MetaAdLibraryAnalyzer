using System.Diagnostics.Metrics;
using System;
using System.Net.Http.Json;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Net.Http;

var pageCounts = new Dictionary<string, int>();
var iteration = 0;
var numOfIterations = 10;
var apiKey = "HnJD445Aq8YobyD2dN1MSRX2";
var query = "trauma";

await GetPageCounts("");

foreach (var kvp in pageCounts.AsEnumerable().Where(x => x.Value >= 4).OrderByDescending(x => x.Value))
{
    Console.WriteLine($"PageId: {kvp.Key} Active ad count: {kvp.Value}");
}
Console.ReadLine();

async Task GetPageCounts(string nextPageToken)
{
    iteration++;
    Console.WriteLine($"Iteration: {iteration}");

    var client = new HttpClient();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    var url = "https://www.searchapi.io/api/v1/search";

    // var nextPageParameter = !string.IsNullOrEmpty(nextPageToken) ? $"&next_page_token={nextPageToken}" : "";
    //var url = $"https://www.searchapi.io/api/v1/search?engine=meta_ad_library&ad_type=all&country=ALL&is_targeted_country=false&content_languages[0]=en&media_type=all&active_status=active&api_key={apiKey}&q={query}{nextPageParameter}";
    //search_type=page
    //source=page-transparency-widget
    //view_all_page_id=726451997220616


    //curl - X POST https://www.searchapi.io/api/v1/search \
    //    -H "Authorization: Bearer YOUR_API_KEY" \
    //    -H "Content-Type: application/json" \
    //    -d '{"engine": "meta_ad_library", "q": "nike", "next_page_token": "..."}'



    var contentObj = new Object();
    if (!string.IsNullOrEmpty(nextPageToken))
    {
        contentObj = new
                        {
                            Authorization = $"Bearer {apiKey}",
                            d = new
                            {
                                engine = "meta_ad_library",
                                ad_type = "ALL",
                                country = "ALL",
                                //is_targeted_country = false,
                                //content_languages = "en",
                                media_type = "ALL",
                                active_status = "active",
                                api_key = apiKey,
                                q = query,
                                next_page_token = nextPageToken
                            }
                        };
    }
    else
    {
        contentObj = new
                        {
                            Authorization = $"Bearer {apiKey}",
                            d = new
                            {
                                engine = "meta_ad_library",
                                ad_type = "ALL",
                                country = "ALL",
                                //is_targeted_country = false,
                                //content_languages = "en",
                                media_type = "ALL",
                                active_status = "active",
                                api_key = apiKey,
                                q = query
                            }
                        };
    }

    var content = JsonContent.Create(contentObj);
    var responseJson = await client.PostAsync(url, content);
    responseJson.EnsureSuccessStatusCode();
    var jsonString = await responseJson.Content.ReadAsStringAsync();
    var doc = JsonSerializer.Deserialize<JsonElement>(jsonString);
    var ads = doc.GetProperty("ads").EnumerateArray();

    foreach (var ad in ads)
    {
        var pageId = ad.GetProperty("page_id").GetString();
        if (
            ad.TryGetProperty("is_active", out JsonElement isActive) &&
            isActive.GetBoolean()
        )
        {
            pageCounts.TryGetValue(pageId, out int count);
            pageCounts[pageId] = count + 1;
        }
    }

    if (doc.TryGetProperty("pagination", out var paginationObject) &&
        paginationObject.ValueKind == JsonValueKind.Object &&
        paginationObject.TryGetProperty("next_page_token", out JsonElement nextPageTokenElement) &&
        iteration < numOfIterations
    )
    {
        Thread.Sleep(1000);
        await GetPageCounts(nextPageTokenElement.ToString());
    }
}