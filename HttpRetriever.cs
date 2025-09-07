using System.Net;
using System.Text;

public class HttpRetriever
{
  private string cookie = "dCMP=mafra=1111,all=1,reklama=1,part=0,cpex=1,google=1,gemius=1,id5=1,next=0000,onlajny=0000,jenzeny=0000," +
      "databazeknih=0000,autojournal=0000,skodahome=0000,skodaklasik=0000,groupm=1,piano=1,seznam=1,geozo=0," +
      "czaid=1,click=1,verze=2"; 
  public string Url;
  HttpClient client;
  
  public HttpRetriever(string url) 
  {
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    Url = url;

    HttpClientHandler handler = new HttpClientHandler(); 
    handler.CookieContainer ??= new CookieContainer();
    client = new HttpClient(handler)
    {
      Timeout = TimeSpan.FromSeconds(10)
    };
  }

  public async Task<string> GetHtmlAsync(string url) 
  {
    var request = new HttpRequestMessage(HttpMethod.Get, url);
    request.Headers.Add("Cookie", cookie);

    var response = await client.SendAsync(request);
    
    var html = await response.Content.ReadAsStringAsync();
    return html;
  }
}
