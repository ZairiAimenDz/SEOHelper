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
        const double MaxLoadTime = 2;
        const double MaxURLLength = 80;
        const double MaxTitleLength = 70;
        const double MaxHeadingLength = 80;

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
            {
                result.SEOErrors.Add(new()
                {
                    ErrorImpact = "HIGH",
                    ErrorPlace = "HTTPS",
                    ErrorText = "Your Website Doesn't have HTTPS which is recommended",
                    ErrorWeight = "Moderate"
                });
            }

            // Checking For Temporary Redirects 

            result.TotalScore += 3; 
            var response = await web1.GetAsync(URL);
            if(response.StatusCode == HttpStatusCode.TemporaryRedirect)
            {
                result.TemporaryRedirect = true;
                result.SEOErrors.Add(new()
                {
                    ErrorImpact = "HIGH",
                    ErrorPlace = "Website",
                    ErrorWeight = "HEAVY",
                    ErrorText = "The Website Has A Temporary Redirect"
                });
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
                    Works =false,
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
            TimeSpan timeTaken = timer.Elapsed;
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

            // SSL Certificate Expiration

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

            // Checking For Doctype

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

            // Checking For Mobile Responsivness 

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

            // Checking For Encoding 

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

            // Minimal Javascript 

            result.TotalScore++;
            bool minimal_js = doc.DocumentNode.SelectNodes("//script").Where(s => s.Attributes.Contains("src"))
                    .All(s => s.Attributes["src"].ToString().ToLower().Contains("min"));
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

            // Checking For Primary keyword In URL & URL Length

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

            // Check if Blocked From Seach Engine

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

            // On-Page SEO Errors 
            // 1 - Title Errors :

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
                    result.SEOErrors.Add(new() {
                        ErrorImpact = "HIGH",
                        ErrorPlace = "Title",
                        ErrorText = "The Title Doesn't Contain the Primary Keyword",
                        ErrorWeight= "HEAVY"
                    });

                // Has Other Keywords
                
                if(Keywords != null)
                    Keywords.All(k => titleErrors.HasKeywords || CheckTextForKeywords(titles.First().InnerText, k));
                if (titleErrors.HasKeywords)
                    result.YourScore += 1;
                else
                    result.SEOErrors.Add(new() {
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
                    ErrorText = "There is more than 1 Title Which is not Recommended :"+string.Join(',',titles.Select(t=>t.InnerText)),
                    ErrorWeight = "HEAVY"
                });
                titleErrors.TitleExists = true;
                titleErrors.MoreThanOne = true;
                titleErrors.HasMainKeyword = false;
            }

            // Main Heading Errors

            HeadingErrors headingErrors = new();
            result.TotalScore += 9;
            var headings= doc.DocumentNode
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

                if (Keywords != null)
                    Keywords.All(k => headingErrors.HasKeywords || CheckTextForKeywords(headings.First().InnerText, k));
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
                headingErrors.Exists= true;
                headingErrors.MoreThanOne= true;
            }


            // Assigning The Results To The Result Object
            result.BasicSEO = basic;
            result.OnPageSEO = new() {
                TitleErrors = titleErrors,
                HeadingErrors = headingErrors,
            };
            // Returning The Final Result
            return result;
        }
        public Task<WebsiteAuditResult> AllPageAudit(string URL, string PrimaryKeyword, string[]? Keywords)
        {
            return null;
        }

        static bool CheckTextForKeywords(string Text, string Keyword)
        {
            return Text.ToLower().Contains(Keyword.ToLower());
        }
    }
}
