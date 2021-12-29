using HtmlAgilityPack;
using SEOHelper.Models;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography.X509Certificates;

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
        const int MinContentLength = 300;
        const int MaxSentenceLength = 30;
        public Task<WebsiteAuditResult> AllPageAudit(string URL, string PrimaryKeyword, string[]? Keywords)
        {
            return null;
        }

        public async Task<PageAuditResult> OnePageAudit(string URL, string PrimaryKeyword, string[]? Keywords)
        {
            var result = new PageAuditResult() { URL = URL };
            using HttpClient web1 = new() { BaseAddress = new Uri(URL) };

            // Basic SEO
            var basic = new BasicSEO();
            // Has HTTPS Enabled

            result.TotalScore += 3;
            if (web1.BaseAddress.Scheme == "https")
            {
                basic.HTTPS = true;
                result.YourScore += 3;
            }
            else
                AddError(result, "HIGH", "HTTPS", "Your Website Doesn't have HTTPS which is recommended", "Moderate");


            // Checking For Temporary Redirects 

            result.TotalScore += 3;
            var response = await web1.GetAsync(URL);
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
                data = await web1.GetStringAsync(URL);
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
            CheckURLErrors(URL, PrimaryKeyword, result, web1, basic);
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

        private void ContentErrorCheck(string PrimaryKeyword, PageAuditResult result, HtmlDocument doc, HashSet<string> KeywordsList, ContentErrors contentErrors)
        {

            result.TotalScore += 7;
            var content = doc.DocumentNode.SelectNodes("//p");
            contentErrors.ContentLength = content.Sum(p => p.InnerText.Split(" ").Count());
            if (contentErrors.ContentLength > MinContentLength)
                result.YourScore += 2;
            else
                AddError(result, "MEDIUM", "LIGHT", "Content", $"Content is Less than The Recommended {MinContentLength} Words ");


            contentErrors.HasKeywords = content.Any(p => KeywordsList.Any(k => CheckTextForKeywords(p.InnerText, k)) || CheckTextForKeywords(p.InnerHtml, PrimaryKeyword));
            if (contentErrors.HasKeywords)
                result.YourScore += 2;
            else
                AddError(result, "MEDIUM", "MODERATE", "Content", "The Content doesn't contain any keyword");

            contentErrors.DuplicateContent = content.DistinctBy(p => p.InnerText).Count() != content.Count();
            if (contentErrors.DuplicateContent)
                AddError(result, "MEDIUM", "LIGHT", "Content", "Content is Duplicated At Least Once");
            else
                result.YourScore += 1;

            contentErrors.PrimaryKeywordInFirstP = CheckTextForKeywords(content.First().InnerText, PrimaryKeyword);
            if (contentErrors.PrimaryKeywordInFirstP)
                result.YourScore++;
            contentErrors.PrimaryKeywordInLastP = CheckTextForKeywords(content.Last().InnerText, PrimaryKeyword);
            if (contentErrors.PrimaryKeywordInLastP)
                result.YourScore++;
        }

        private static ImageErrors ImageErrorsCheck(string PrimaryKeyword, PageAuditResult result, HtmlDocument doc, HashSet<string> KeywordsList)
        {
            ImageErrors imgErrors = new();
            result.TotalScore += 6;
            imgErrors.ImagesExist = doc.DocumentNode.SelectNodes("//img").Count() > 0;
            if (imgErrors.ImagesExist)
                result.YourScore += 2;
            else
                result.SEOErrors.Add(new() { 
                    ErrorWeight = "HEAVY",
                    ErrorText= "Your Website Doesn't Have Any Images",
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
                .Any(i => KeywordsList.ToList().Any(k => CheckTextForKeywords(i.Attributes["alt"].Value, k)) || CheckTextForKeywords(i.Attributes["alt"].Value, PrimaryKeyword));
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

            return imgErrors;
        }

        private static MetaDescriptionErrors MetaDescErrorsCheck(string PrimaryKeyword, PageAuditResult result, HtmlDocument doc, HashSet<string> KeywordsList)
        {
            MetaDescriptionErrors metaDescriptionErrors = new();
            result.TotalScore += 13;
            var metadescs = doc.DocumentNode.SelectNodes("//meta").Where(s => s.HasAttributes).Where(s => s.Attributes.Contains("name"))
                .Where(s => s.Attributes["name"].Value.ToLower() == "description").ToList();
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
                metaDescriptionErrors.MetaDescription = metadescs.First().Attributes["content"].Value;
                result.YourScore += 5;
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

                metaDescriptionErrors.HasPrimaryKeyword = CheckTextForKeywords(metaDescriptionErrors.MetaDescription, PrimaryKeyword);
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
                    metaDescriptionErrors.HasKeywords = KeywordsList.All(k => metaDescriptionErrors.HasKeywords || CheckTextForKeywords(metaDescriptionErrors.MetaDescription, k));
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

            return metaDescriptionErrors;
        }

        private static SubHeadingErrors SubHeadingErrorsCheck(string PrimaryKeyword, PageAuditResult result, HtmlDocument doc, HashSet<string> KeywordsList)
        {
            result.TotalScore += 5;
            SubHeadingErrors subHeadingErrors = new();
            var headings = doc.DocumentNode.SelectNodes("//h2").ToList();
            if (headings.Count > 0)
            {
                result.YourScore += 2;
                subHeadingErrors.Exists = true;
                if (headings.Any(h => KeywordsList.ToList().Any(k => CheckTextForKeywords(h.InnerText, k)) || CheckTextForKeywords(h.InnerText, PrimaryKeyword)))
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
                    HasKeywords = KeywordsList.ToList().Any(k => CheckTextForKeywords(h.InnerText, k)) || CheckTextForKeywords(h.InnerText, PrimaryKeyword),
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
               .Where(n => n.Attributes["name"].Value.ToLower() == "keywords").First();
                kws.Attributes["content"].Value.Split(',').ToList().ForEach(k => KeywordsList.Add(k));
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
            var minimal_js = all.Where(s => s.HasAttributes).Where(s => s.Attributes.Contains("src"))
                .ToList().All(s=>s.Attributes["src"].Value.Contains("min"));
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

            basic.PrimaryKeywordinURL = CheckTextForKeywords(URL, PrimaryKeyword);
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

        private static TitleErrors TitleErrorsCheck(string PrimaryKeyword, PageAuditResult result, HtmlDocument doc, HashSet<string> KeywordsList)
        {
            TitleErrors titleErrors = new();

            // Checking For Title Errors

            result.TotalScore += 11;
            var titles = doc.DocumentNode
                .SelectNodes("//head/title")
                .ToList();
            if (titles.Count() == 0)
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

                titleErrors.HasMainKeyword = CheckTextForKeywords(titles.First().InnerText, PrimaryKeyword);
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
                    titleErrors.HasKeywords = KeywordsList.All(k => titleErrors.HasKeywords || CheckTextForKeywords(titles.First().InnerText, k));
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
            var headings = doc.DocumentNode
                .SelectNodes("//h1")
                .ToList();
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

                headingErrors.HasPrimaryKeyword = CheckTextForKeywords(headings.First().InnerText, PrimaryKeyword);
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
                    headingErrors.HasKeywords = KeywordsList.All(k => headingErrors.HasKeywords || CheckTextForKeywords(headings.First().InnerText, k));
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

        
        static bool CheckTextForKeywords(string Text, string Keyword)
        {
            return Text.ToLower().Contains(Keyword.ToLower());
        }

        public void AddError(PageAuditResult result, string Impact, string Weight, string Place, string ErrMessage)
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