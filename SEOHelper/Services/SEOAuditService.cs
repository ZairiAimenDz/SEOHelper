using HtmlAgilityPack;
using SEOHelper.Models;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace SEOHelper.Service
{
    public class SEOAuditService : ISEOAuditService
    {
        // Constants
        const int MaxLoadTime = 2;
        const int MaxURLLength = 80;
        const int MaxTitleLength = 70;
        const int MaxHeadingLength = 80;
        const int MaxMetaDescLength = 165;
        const int MinMetaDescLength = 80;
        const int MinContentLength = 110;//In Words
        public async Task<WebsiteAuditResult> AllPageAudit(string URL, string PrimaryKeyword, string[]? Keywords)
        {
            using HttpClient httpclient = new();
            string sitemapURL = URL + "/sitemap.xml";

            XmlDocument urldoc = new XmlDocument();
            var sitemap = await httpclient.GetStringAsync(sitemapURL);
            if (string.IsNullOrEmpty(sitemap))
            {
                return new() { HasSiteMap = false, URL = URL };
            }

            WebsiteAuditResult result = new() { URL = URL, HasSiteMap = true };
            urldoc.LoadXml(sitemap);
            XmlNodeList xmlSitemapList = urldoc.GetElementsByTagName("url");
            foreach (XmlNode node in xmlSitemapList)
            {
                if (node["loc"] is not null)
                {
                    try
                    {
                        var audit = await OnePageAudit(node["loc"].InnerText, PrimaryKeyword, Keywords);
                        if (audit is not null) 
                            result.PagesAudit.Add(audit);
                    }
                    catch (Exception ex)
                    { Console.WriteLine(ex); }
                }
            }

            foreach (var page in result.PagesAudit)
            {
                if(page.OnPageSEO.TitleErrors.Title.Length > 0)
                    page.DuplicateTitle = result.PagesAudit.Where(p => p.OnPageSEO.TitleErrors.Title == page.OnPageSEO.TitleErrors.Title).Count() > 1;
                if (page.DuplicateTitle)
                    AddError(page, "High", "Moderate", "Title", "Title is Duplicated");

                if (page.OnPageSEO.HeadingErrors.Heading.Length > 0) 
                    page.DuplicateHeading = result.PagesAudit.Where(p => p.OnPageSEO.HeadingErrors.Heading== page.OnPageSEO.HeadingErrors.Heading).Count() > 1;
                if (page.DuplicateHeading)
                    AddError(page, "High", "Moderate", "Title", "Title is Duplicated");

                if (page.OnPageSEO.MetaDescriptionErrors.MetaDescription.Length > 0) 
                    page.DuplicateMeta = result.PagesAudit.Where(p => p.OnPageSEO.MetaDescriptionErrors.MetaDescription== page.OnPageSEO.MetaDescriptionErrors.MetaDescription).Count() > 1;
                if (page.DuplicateMeta)
                    AddError(page, "High", "Moderate", "Title", "Title is Duplicated");

            }

            result.AvgPageScore = (double)result.PagesAudit.Sum(r => r.YourScore) * 100.0 / result.PagesAudit.Sum(r => r.TotalScore);
            return result;
        }

        public async Task<PageAuditResult> OnePageAudit(string URL, string PrimaryKeyword, string[]? Keywords)
        {
            var result = new PageAuditResult() { URL = URL };
            using HttpClient httpclient = new() { BaseAddress = new Uri(URL) };

            // Basic SEO
            var basic = new BasicSEO();
            // Has HTTPS Enabled

            result.TotalScore += 3;
            if (httpclient.BaseAddress.Scheme == "https")
            {
                basic.HTTPS = true;
                result.YourScore += 3;
            }
            else
                AddError(result, "HIGH", "HTTPS", "Your Website Doesn't have HTTPS which is recommended", "Moderate");


            // Checking For Temporary Redirects 

            result.TotalScore += 3;
            var response = await httpclient.GetAsync(URL);
            if (response.StatusCode == HttpStatusCode.TemporaryRedirect)
            {
                result.TemporaryRedirect = true;
                AddError(result, "HIGH", "HEAVY", "Website", "The Website Has A Temporary Redirect");
            }
            else
            {
                result.YourScore += 3;
            }

            // Counting the Loading Speed Of HTML & HTML Size

            Stopwatch timer = new Stopwatch();
            string data;
            timer.Start();
            try
            {
                data = await httpclient.GetStringAsync(URL);
            }
            catch
            {
                return new()
                {
                    Works = false,
                    SEOErrors = new()
                    {
                        new()
                        {
                            ErrorImpact = "VERY HIGH",
                            ErrorWeight = "HEAVY",
                            ErrorPlace = "Website",
                            ErrorText = "The Website Doens't Work"
                        }
                    }
                };
            }
            timer.Stop();

            var doc = new HtmlDocument();
            doc.LoadHtml(data);

            HashSet<string> KeywordsList = GetAllkeywords(Keywords, doc);
            TimeSpan timeTaken = timer.Elapsed;

            CheckLoadingTime(result, basic, data, timeTaken);
            await CheckSSLCertificate(URL, result, basic);
            CheckDoctype(result, basic, data);
            CheckMobileResponsivness(result, basic, data);
            CheckEncoding(result, basic, data);
            CheckMinimalJS(result, doc);
            CheckURLErrors(URL, PrimaryKeyword, result, httpclient, basic);
            CheckSEBlock(result, basic, doc);

            // On-Page SEO Errors 

            TitleErrors titleErrors = TitleErrorsCheck(PrimaryKeyword, result, doc, KeywordsList);
            HeadingErrors headingErrors = HeadingErrorsCheck(PrimaryKeyword, result, doc, KeywordsList);
            SubHeadingErrors subHeadingErrors = SubHeadingErrorsCheck(PrimaryKeyword, result, doc, KeywordsList);
            MetaDescriptionErrors metaDescriptionErrors = MetaDescErrorsCheck(PrimaryKeyword, result, doc, KeywordsList);
            ImageErrors imgErrors = ImageErrorsCheck(PrimaryKeyword, result, doc, KeywordsList);


            // Content Errors

            ContentErrors contentErrors = new();
            ContentErrorCheck(PrimaryKeyword, result, doc, KeywordsList, contentErrors);

            // Comment If You Want To Test Faster
            //await CheckForURLError(URL, result, httpclient, doc, contentErrors);




            // Final Result

            result.Keywords = KeywordsList;
            result.BasicSEO = basic;
            result.OnPageSEO = new()
            {
                TitleErrors = titleErrors,
                HeadingErrors = headingErrors,
                SubHeadingErrors = subHeadingErrors,
                MetaDescriptionErrors = metaDescriptionErrors,
                ImageErrors = imgErrors,
                ContentErrors = contentErrors,
            };

            // Returning The Final Result

            return result;
        }

        private async Task CheckForURLError(string URL, PageAuditResult result, HttpClient httpclient, HtmlDocument doc, ContentErrors contentErrors)
        {
            result.TotalScore += 11;
            var links = doc.DocumentNode.SelectNodes("//a");
            string currentwebsite = httpclient.BaseAddress.Host;
            var internalLinks = links.Where(a => a.Attributes.Contains("href")).Where(a => CheckTextForWord(a.Attributes["href"].Value, currentwebsite)).ToList();
            var externalLinks = links.Except(internalLinks).Where(a => a.Attributes.Contains("href")).ToList();
            if (internalLinks.Any())
            {
                result.YourScore += 1;
                foreach (var link in internalLinks)
                {
                    ALink route = new()
                    {
                        RouteLink = link.Attributes["href"].Value,
                        HasHTTPs = link.Attributes["href"].Value.Contains("https"),
                        Works = (await httpclient.GetAsync(URL)).IsSuccessStatusCode
                    };
                    contentErrors.InternalRoutes.Add(route);
                }
                if (contentErrors.InternalRoutes.All(il => il.Works))
                    result.YourScore += 2;
                if (contentErrors.InternalRoutes.All(il => il.HasHTTPs))
                    result.YourScore += 3;

            }
            else
                AddError(result, "MEDIUM", "MODERATE", "Links", "You Don't have any Internal Links");

            if (externalLinks.Any())
            {
                result.YourScore += 1;
                foreach (var link in externalLinks)
                {
                    ALink route = new()
                    {
                        RouteLink = link.Attributes["href"].Value,
                        HasHTTPs = link.Attributes["href"].Value.Contains("https"),
                        Works = (await httpclient.GetAsync(URL)).IsSuccessStatusCode
                    };
                    contentErrors.ExternalRoutes.Add(route);
                }
                if (contentErrors.ExternalRoutes.All(il => il.Works))
                    result.YourScore += 2;
                if (contentErrors.InternalRoutes.All(il => il.HasHTTPs))
                    result.YourScore += 2;
            }
            else
                AddError(result, "MEDIUM", "MODERATE", "Links", "You Don't have any External Links");
        }

        private void ContentErrorCheck(string PrimaryKeyword, PageAuditResult result, HtmlDocument doc, HashSet<string> KeywordsList, ContentErrors contentErrors)
        {

            result.TotalScore += 7;
            var content = doc.DocumentNode.SelectNodes("//p");
            if (content != null)
            {
                contentErrors.ContentLength = content.Sum(p => p.InnerText.Split(" ").Count());
                if (contentErrors.ContentLength > MinContentLength)
                    result.YourScore += 2;
                else
                    AddError(result, "MEDIUM", "LIGHT", "Content", $"Content is Less than The Recommended {MinContentLength} Words ");


                contentErrors.HasKeywords = content.Any(p => KeywordsList.Any(k => CheckTextForWord(p.InnerText, k)) || CheckTextForWord(p.InnerHtml, PrimaryKeyword));
                if (contentErrors.HasKeywords)
                    result.YourScore += 2;
                else
                    AddError(result, "MEDIUM", "MODERATE", "Content", "The Content doesn't contain any keyword");

                contentErrors.DuplicateContent = content.DistinctBy(p => p.InnerText).Count() != content.Count();
                if (contentErrors.DuplicateContent)
                    AddError(result, "MEDIUM", "LIGHT", "Content", "Content is Duplicated At Least Once");
                else
                    result.YourScore += 1;

                contentErrors.PrimaryKeywordInFirstP = CheckTextForWord(content.First().InnerText, PrimaryKeyword);
                if (contentErrors.PrimaryKeywordInFirstP)
                    result.YourScore++;
                contentErrors.PrimaryKeywordInLastP = CheckTextForWord(content.Last().InnerText, PrimaryKeyword);
                if (contentErrors.PrimaryKeywordInLastP)
                    result.YourScore++;
            }
            else
            {
                AddError(result, "High", "HEAVY", "Content", "This Page Doesn't Have Any Content Or P Tags");
            }
        }

        private static ImageErrors ImageErrorsCheck(string PrimaryKeyword, PageAuditResult result, HtmlDocument doc, HashSet<string> KeywordsList)
        {
            ImageErrors imgErrors = new();
            result.TotalScore += 6;
            if (doc.DocumentNode.SelectNodes("//img") is not null)
            {
                imgErrors.ImagesExist = doc.DocumentNode.SelectNodes("//img").Any();
                if (imgErrors.ImagesExist)
                    result.YourScore += 2;
                else
                    result.SEOErrors.Add(new()
                    {
                        ErrorWeight = "HEAVY",
                        ErrorText = "Your Website Doesn't Have Any Images",
                        ErrorPlace = "Images",
                        ErrorImpact = "MEDIUM"
                    });

                imgErrors.AllImgsHasAlt = doc.DocumentNode.SelectNodes("//img").All(i => i.Attributes.Contains("alt"));
                if (imgErrors.AllImgsHasAlt)
                    result.YourScore += 2;
                else
                    result.SEOErrors.Add(new()
                    {
                        ErrorWeight = "MODERATE",
                        ErrorText = "All Images Must Have Alt Attributes",
                        ErrorPlace = "Images",
                        ErrorImpact = "MEDIUM"
                    });

                imgErrors.AltContainsKeywords = doc.DocumentNode.SelectNodes("//img").Where(i => i.Attributes.Contains("alt"))
                    .Any(i => KeywordsList.ToList().Any(k => CheckTextForWord(i.Attributes["alt"].Value, k)) || CheckTextForWord(i.Attributes["alt"].Value, PrimaryKeyword));
                if (imgErrors.AltContainsKeywords)
                    result.YourScore += 2;
                else
                    result.SEOErrors.Add(new()
                    {
                        ErrorWeight = "MODERATE",
                        ErrorText = "Images Alts Don't Contain ANy Keywords",
                        ErrorPlace = "Images",
                        ErrorImpact = "MEDIUM"
                    });
            }
            else
            {
                result.SEOErrors.Add(new()
                {
                    ErrorWeight = "HEAVY",
                    ErrorText = "Your Website Doesn't Have Any Images",
                    ErrorPlace = "Images",
                    ErrorImpact = "MEDIUM"
                });
            }
            return imgErrors;
        }

        private static MetaDescriptionErrors MetaDescErrorsCheck(string PrimaryKeyword, PageAuditResult result, HtmlDocument doc, HashSet<string> KeywordsList)
        {
            MetaDescriptionErrors metaDescriptionErrors = new();
            result.TotalScore += 13;
            var metas = doc.DocumentNode.SelectNodes("//meta[@name=\"description\"]");
            if (metas != null)
            {
                var metadescs = metas.ToList();
                if (metadescs.Count() == 0)
                {
                    result.SEOErrors.Add(new()
                    {
                        ErrorImpact = "HIGH",
                        ErrorPlace = "Meta Description",
                        ErrorText = "The Web Page Doesn't have A Meta Description",
                        ErrorWeight = "HEAVY"
                    });
                }
                else if (metadescs.Count() == 1)
                {
                    metaDescriptionErrors.Exists = true;
                    if (metadescs.First().Attributes.Contains("content"))
                    {
                        metaDescriptionErrors.MetaDescription = metadescs.First().Attributes["content"].Value;
                        result.YourScore += 5;
                    }
                    // Meta Length

                    metaDescriptionErrors.Length = metaDescriptionErrors.MetaDescription.Length;
                    if (metaDescriptionErrors.Length > MaxMetaDescLength || metaDescriptionErrors.Length < MinMetaDescLength)
                        result.SEOErrors.Add(new()
                        {
                            ErrorImpact = "MEDIUM",
                            ErrorPlace = "Meta Description",
                            ErrorText = $"The Meta Description Length is not within the recommended {MinMetaDescLength} - {MaxMetaDescLength} characters",
                            ErrorWeight = "MODERATE",
                        });
                    else
                        result.YourScore += 2;

                    // Has Main Keyword

                    metaDescriptionErrors.HasPrimaryKeyword = CheckTextForWord(metaDescriptionErrors.MetaDescription, PrimaryKeyword);
                    if (metaDescriptionErrors.HasPrimaryKeyword)
                        result.YourScore += 2;
                    else
                        result.SEOErrors.Add(new()
                        {
                            ErrorImpact = "HIGH",
                            ErrorPlace = "Meta Description",
                            ErrorText = "The Meta Description Doesn't Contain the Primary Keyword",
                            ErrorWeight = "MODERATE"
                        });

                    // Has Other Keywords

                    if (KeywordsList.Count() > 0)
                        metaDescriptionErrors.HasKeywords = KeywordsList.All(k => metaDescriptionErrors.HasKeywords || CheckTextForWord(metaDescriptionErrors.MetaDescription, k));
                    if (metaDescriptionErrors.HasKeywords)
                        result.YourScore += 1;
                    else
                        result.SEOErrors.Add(new()
                        {
                            ErrorImpact = "LOW",
                            ErrorPlace = "Meta Description",
                            ErrorText = "The Meta Description Doesn't Contain any Of The Other Keywords ( Besides The Primary )",
                            ErrorWeight = "MODERATE"
                        });
                }
                else
                {
                    result.YourScore += 1;
                    result.SEOErrors.Add(new()
                    {
                        ErrorImpact = "HIGH",
                        ErrorPlace = "Meta Description",
                        ErrorText = "There is more than 1 Meta Description Which is not Recommended ",
                        ErrorWeight = "MODERATE"
                    });
                    metaDescriptionErrors.Exists = true;
                    metaDescriptionErrors.MoreThanOne = true;
                }
            }
            else
            {
                AddError(result, "High", "Heavy", "Meta Tags", "This Page Doesn't Contain Any Meta tags");
            }
            return metaDescriptionErrors;
        }

        private static SubHeadingErrors SubHeadingErrorsCheck(string PrimaryKeyword, PageAuditResult result, HtmlDocument doc, HashSet<string> KeywordsList)
        {
            result.TotalScore += 5;
            SubHeadingErrors subHeadingErrors = new();
            if (doc.DocumentNode.SelectNodes("//h2")!= null)
            {
                var headings = doc.DocumentNode.SelectNodes("//h2").ToList();
                result.YourScore += 2;
                subHeadingErrors.Exists = true;
                if (headings.Any(h => KeywordsList.ToList().Any(k => CheckTextForWord(h.InnerText, k)) || CheckTextForWord(h.InnerText, PrimaryKeyword)))
                    result.YourScore += 3;
                else
                    result.SEOErrors.Add(new()
                    {
                        ErrorImpact = "HIGH",
                        ErrorPlace = "Sub Headings",
                        ErrorText = "Subheadings Don't Contain Any Keywords",
                        ErrorWeight = "MODERATE"
                    });

                headings.ForEach(h => subHeadingErrors.SubHeadings.Add(new()
                {
                    HasKeywords = KeywordsList.ToList().Any(k => CheckTextForWord(h.InnerText, k)) || CheckTextForWord(h.InnerText, PrimaryKeyword),
                    Level = 2,
                    Heading = h.InnerHtml
                }));
            }
            else
                result.SEOErrors.Add(new()
                {
                    ErrorImpact = "MEDIUM",
                    ErrorWeight = "MODERATE",
                    ErrorPlace = "Sub-Headings",
                    ErrorText = "The Web page Doesn't Containt Any Subheadings"
                });
            return subHeadingErrors;
        }

        private static HashSet<string> GetAllkeywords(string[]? Keywords, HtmlDocument doc)
        {
            HashSet<string> KeywordsList = new();
            try
            {
                var kws = doc.DocumentNode
                   .SelectNodes("//meta")
                   .Where(n => n.Attributes.Contains("name"))
                   .Where(n => n.Attributes["name"].Value.ToLower() == "keywords");
                if (kws.Any())
                    kws.First().Attributes["content"].Value.Split(',').ToList().ForEach(k => KeywordsList.Add(k));
            }
            catch
            { }
            if (Keywords is not null)
                Keywords.ToList().ForEach(k => KeywordsList.Add(k));
            return KeywordsList;
        }

        private static void CheckLoadingTime(PageAuditResult result, BasicSEO basic, string data, TimeSpan timeTaken)
        {
            basic.LoadingTime = timeTaken.TotalSeconds;
            basic.HTMLSize = data.Length;
            result.TotalScore += 3;
            if (timeTaken.TotalSeconds > MaxLoadTime)
            {
                result.SEOErrors.Add(new()
                {
                    ErrorPlace = "Loading Time",
                    ErrorImpact = "HIGH",
                    ErrorWeight = "HEAVY",
                    ErrorText = $"Loading time Is {timeTaken.TotalSeconds} which is over the max recommended {MaxLoadTime}"
                });
            }
            else
                result.YourScore += 3;
        }

        private static async Task CheckSSLCertificate(string URL, PageAuditResult result, BasicSEO basic)
        {
            result.TotalScore += 3;
            X509Certificate2 certificate = null;
            var httpClientHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, cert, __, ___) =>
                {
                    certificate = new X509Certificate2(cert.GetRawCertData());
                    return true;
                }
            };
            var httpClient = new HttpClient(httpClientHandler);
            await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, URL));
            if (certificate != null)
            {
                basic.HasSSL = true;
                basic.SSLCertificateExpiration = DateTime.Parse(certificate.GetExpirationDateString());
                if (basic.SSLCertificateExpiration <= DateTime.Now)
                    result.SEOErrors.Add(new()
                    {
                        ErrorImpact = "Heavy",
                        ErrorWeight = "Moderate",
                        ErrorPlace = "SSL Certificate",
                        ErrorText = $"SSL Certificate Has Expired in {basic.SSLCertificateExpiration.ToString("dd-mm-yyyy")}"
                    });
                else
                    result.YourScore += 3;
            }
            else
            {
                result.SEOErrors.Add(new()
                {
                    ErrorImpact = "Heavy",
                    ErrorWeight = "Moderate",
                    ErrorPlace = "SSL Certificate",
                    ErrorText = "You Don't Have an SSL Certificate"
                });
            }
        }

        private static void CheckDoctype(PageAuditResult result, BasicSEO basic, string data)
        {
            result.TotalScore++;
            if (data.Contains("<!DOCTYPE html>"))
            {
                basic.DoctypeDecalred = true;
                result.YourScore++;
            }
            else
            {
                result.SEOErrors.Add(new()
                {
                    ErrorImpact = "LOW",
                    ErrorPlace = "DOCTYPE",
                    ErrorText = "DOCTYPE is not declared",
                    ErrorWeight = "Moderate"
                });
            }
        }

        private static void CheckMobileResponsivness(PageAuditResult result, BasicSEO basic, string data)
        {
            result.TotalScore++;
            if (data.Contains("meta name=\"viewport\""))
            {
                basic.MobileResponsive = true;
                result.YourScore++;
            }
            else
            {
                result.SEOErrors.Add(new()
                {
                    ErrorImpact = "MEDIUM",
                    ErrorPlace = "Mobile Responsivness",
                    ErrorText = "Web Page is Not Mobile Responsive",
                    ErrorWeight = "MODERATE"
                });
            }
        }

        private static void CheckEncoding(PageAuditResult result, BasicSEO basic, string data)
        {
            result.TotalScore++;
            if (data.Contains("meta charset"))
            {
                basic.EncodingDeclared = true;
                result.YourScore++;
            }
            else
            {
                result.SEOErrors.Add(new()
                {
                    ErrorImpact = "LOW",
                    ErrorPlace = "Encoding",
                    ErrorText = "Character Encoding is not Declared",
                    ErrorWeight = "Moderate"
                });
            }
        }

        private static void CheckMinimalJS(PageAuditResult result, HtmlDocument doc)
        {
            result.TotalScore++;
            var all = doc.DocumentNode.SelectNodes("//script");
            if (all != null && all.Any())
            {
                var minimal_js = all.Where(s => s.HasAttributes).Where(s => s.Attributes.Contains("src"))
                    .ToList().All(s => s.Attributes["src"].Value.Contains("min"));
                if (minimal_js)
                    result.YourScore++;
                else
                    result.SEOErrors.Add(new()
                    {
                        ErrorImpact = "LOW",
                        ErrorPlace = "Javascript Files",
                        ErrorText = "Some Javascript Files are not minimal",
                        ErrorWeight = "LIGHT"
                    });
            }
        }

        private static void CheckURLErrors(string URL, string PrimaryKeyword, PageAuditResult result, HttpClient web1, BasicSEO basic)
        {
            result.TotalScore += 4;
            var relativeurl = string.Join("", web1.BaseAddress.Segments.Skip(1));
            basic.URLLength = relativeurl.Length;
            if (relativeurl.Length > MaxURLLength)
                result.SEOErrors.Add(new()
                {
                    ErrorImpact = "MEDIUM",
                    ErrorPlace = "URL Length",
                    ErrorText = "URL length is Over The Recommended 80 Characters",
                    ErrorWeight = "LIGHT"
                });
            else
                result.YourScore += 2;

            basic.PrimaryKeywordinURL = CheckTextForWord(URL, PrimaryKeyword);
            if (basic.PrimaryKeywordinURL)
                result.YourScore += 2;
            else
                result.SEOErrors.Add(new()
                {
                    ErrorImpact = "MEDIUM",
                    ErrorPlace = "URL",
                    ErrorText = "URL Doesn't Contain the Primary Keyword",
                    ErrorWeight = "MODERATE"
                });
        }

        private static void CheckSEBlock(PageAuditResult result, BasicSEO basic, HtmlDocument doc)
        {
            result.TotalScore += 3;
            if (doc.DocumentNode.SelectNodes("//meta") != null)
            {
                var noindex = doc.DocumentNode.SelectNodes("//meta").Where(m => m.Attributes.Contains("content") && m.Attributes["content"].Value == "noindex");
                if (noindex.Any())
                {
                    basic.BlockedFromSE = true;
                    result.SEOErrors.Add(new()
                    {
                        ErrorImpact = "HIGH",
                        ErrorPlace = "meta",
                        ErrorText = "The Website is blocked from SE, remove the noindex meta tag",
                        ErrorWeight = "HEAVY"
                    });
                }
                else
                    result.YourScore += 3;
            }
        }

        private static TitleErrors TitleErrorsCheck(string PrimaryKeyword, PageAuditResult result, HtmlDocument doc, HashSet<string> KeywordsList)
        {
            TitleErrors titleErrors = new();

            // Checking For Title Errors

            result.TotalScore += 11;
            var titles = doc.DocumentNode
                .SelectNodes("//head/title");
            if (titles is null)
            {
                result.SEOErrors.Add(new()
                {
                    ErrorImpact = "HIGH",
                    ErrorPlace = "Title",
                    ErrorText = "The Web Page Doesn't have A Title",
                    ErrorWeight = "HEAVY"
                });
                titleErrors.TitleExists = false;
                titleErrors.HasMainKeyword = false;
            }
            else if (titles.Count() == 1)
            {
                result.YourScore += 5;
                // Title Length
                titleErrors.Title = titles.First().InnerText;
                titleErrors.TitleLength = titles.First().InnerText.Length;
                if (titleErrors.TitleLength > MaxTitleLength)
                    result.SEOErrors.Add(new()
                    {
                        ErrorImpact = "MEDIUM",
                        ErrorPlace = "Title",
                        ErrorText = $"The Title Length is more than the recommended {MaxTitleLength} characters",
                        ErrorWeight = "MODERATE",
                    });
                else
                    result.YourScore += 2;

                // Has Main Keyword

                titleErrors.HasMainKeyword = CheckTextForWord(titles.First().InnerText, PrimaryKeyword);
                if (titleErrors.HasMainKeyword)
                    result.YourScore += 3;
                else
                    result.SEOErrors.Add(new()
                    {
                        ErrorImpact = "HIGH",
                        ErrorPlace = "Title",
                        ErrorText = "The Title Doesn't Contain the Primary Keyword",
                        ErrorWeight = "HEAVY"
                    });

                // Has Other Keywords

                if (KeywordsList.Count() > 0)
                    titleErrors.HasKeywords = KeywordsList.All(k => titleErrors.HasKeywords || CheckTextForWord(titles.First().InnerText, k));
                if (titleErrors.HasKeywords)
                    result.YourScore += 1;
                else
                    result.SEOErrors.Add(new()
                    {
                        ErrorImpact = "HIGH",
                        ErrorPlace = "Title",
                        ErrorText = "The Title Doesn't Contain at Least One Of The Other Keywords ( Besides The Primary )",
                        ErrorWeight = "MODERATE"
                    });
            }
            else
            {
                result.YourScore += 1;
                result.SEOErrors.Add(new()
                {
                    ErrorImpact = "HIGH",
                    ErrorPlace = "Title",
                    ErrorText = "There is more than 1 Title Which is not Recommended :" + string.Join(',', titles.Select(t => t.InnerText)),
                    ErrorWeight = "HEAVY"
                });
                titleErrors.TitleExists = true;
                titleErrors.MoreThanOne = true;
                titleErrors.HasMainKeyword = false;
            }

            return titleErrors;
        }

        private static HeadingErrors HeadingErrorsCheck(string PrimaryKeyword, PageAuditResult result, HtmlDocument doc, HashSet<string> KeywordsList)
        {
            HeadingErrors headingErrors = new();
            result.TotalScore += 9;
            var headings = doc.DocumentNode.SelectNodes("//h1") != null ? doc.DocumentNode
                .SelectNodes("//h1")
                .ToList() : new();
            if (headings.Count() == 0)
            {
                result.SEOErrors.Add(new()
                {
                    ErrorImpact = "HIGH",
                    ErrorPlace = "Heading",
                    ErrorText = "The Web Page Doesn't have A Heading",
                    ErrorWeight = "HEAVY"
                });
                headingErrors.Exists = false;
                headingErrors.HasPrimaryKeyword = false;
            }
            else if (headings.Count() == 1)
            {
                headingErrors.Exists = true;
                result.YourScore += 4;
                // Heading Length
                headingErrors.Heading = headings.First().InnerText;
                headingErrors.HeadingLength = headings.First().InnerText.Length;
                if (headingErrors.HeadingLength > MaxHeadingLength)
                    result.SEOErrors.Add(new()
                    {
                        ErrorImpact = "MEDIUM",
                        ErrorPlace = "Heading",
                        ErrorText = $"The Heading Length is more than the recommended {MaxHeadingLength} characters",
                        ErrorWeight = "MODERATE",
                    });
                else
                    result.YourScore += 2;

                // Has Main Keyword

                headingErrors.HasPrimaryKeyword = CheckTextForWord(headings.First().InnerText, PrimaryKeyword);
                if (headingErrors.HasPrimaryKeyword)
                    result.YourScore += 2;
                else
                    result.SEOErrors.Add(new()
                    {
                        ErrorImpact = "HIGH",
                        ErrorPlace = "Heading",
                        ErrorText = "The Heading Doesn't Contain the Primary Keyword",
                        ErrorWeight = "MODERATE"
                    });

                // Has Other Keywords

                if (KeywordsList.Count() > 0)
                    headingErrors.HasKeywords = KeywordsList.All(k => headingErrors.HasKeywords || CheckTextForWord(headings.First().InnerText, k));
                if (headingErrors.HasKeywords)
                    result.YourScore += 1;
                else
                    result.SEOErrors.Add(new()
                    {
                        ErrorImpact = "LOW",
                        ErrorPlace = "Heading",
                        ErrorText = "The Heading Doesn't Contain any Of The Other Keywords ( Besides The Primary )",
                        ErrorWeight = "MODERATE"
                    });
            }
            else
            {
                result.YourScore += 1;
                result.SEOErrors.Add(new()
                {
                    ErrorImpact = "HIGH",
                    ErrorPlace = "Heading",
                    ErrorText = "There is more than 1 Heading Which is not Recommended :" + string.Join(',', headings.Select(t => t.InnerText)),
                    ErrorWeight = "HEAVY"
                });
                headingErrors.Exists = true;
                headingErrors.MoreThanOne = true;
            }

            return headingErrors;
        }


        static bool CheckTextForWord(string Text, string Keyword)
        {
            return Text.ToLower().Contains(Keyword.ToLower());
        }

        public static void AddError(PageAuditResult result, string Impact, string Weight, string Place, string ErrMessage)
        {
            result.SEOErrors.Add(new()
            {
                ErrorImpact = Impact,
                ErrorPlace = Place,
                ErrorText = ErrMessage,
                ErrorWeight = Weight
            });
        }
    }
}