ArticleCrawler crawler = new("https://www.idnes.cz/zpravy/revue/zajimavosti/siamska-dvojcata-abby-a-brittany-hensel-miminko-spekulace-manzel.A250902_082153_zajimavosti_potu", "test.json")
{
  Log = new Log(3),
  MaxSize = ((UInt64)1 * 1024 * 1024),
  MaxDepthSize = 50,
  MaxTasks = 32
};

await crawler.Run();
