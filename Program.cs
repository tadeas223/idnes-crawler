ArticleCrawler crawler = new("https://www.idnes.cz/zpravy/revue/zajimavosti/siamska-dvojcata-abby-a-brittany-hensel-miminko-spekulace-manzel.A250902_082153_zajimavosti_potu", "test.json")
{
  Log = new Log(3),
  MaxSize = ((UInt64)3 * 1024 * 1024 * 1024),
  MaxDepthSize = 10000,
  MaxTasks = 10
};

await crawler.Run();
