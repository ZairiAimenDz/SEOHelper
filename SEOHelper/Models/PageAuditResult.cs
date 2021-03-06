namespace SEOHelper.Models
{
    public class WebsiteAuditResult
    {
        public string URL { get; set; } = "";
        public double AvgPageScore { get; set; }
        public List<PageAuditResult> PagesAudit { get; set; } = new();
        public bool HasSiteMap { get; set; }
    }
    public class PageAuditResult
    {
        public string URL { get; set; } = "";
        public int TotalScore { get; set; } = 0;
        public int YourScore { get; set; } = 0;
        public bool DuplicateMeta { get; set; }
        public bool DuplicateHeading { get; set; }
        public bool DuplicateTitle { get; set; }
        public bool Works { get; set; }
        public bool TemporaryRedirect { get; set; }
        public HashSet<string> Keywords { get; set; } = new();
        public List<SEOError> SEOErrors { get; set; } = new();
        public BasicSEO BasicSEO { get; set; } = new();
        public OnPageSEO OnPageSEO { get; set; } = new();
        public SocialMediaSEO SocialMediaSEO { get; set; } = new(); 
    }

    public class SocialMediaSEO
    {
        public OpenGraphErrors OpenGraphErrors { get; set; } = new();
        public TwitterCardsErrors TwitterCardsErrors { get; set; } = new();
        public AppleMobileErrors AppleMobileErrors { get; set; } = new();
    }
    public class OpenGraphErrors
    {
        // Obligatory :
        public bool HasTitle { get; set; }
        public bool HasType { get; set; }
        public bool HasImage { get; set; }
        public bool HasURL { get; set; }
        // Optional 
        public bool HasDescription { get; set; }
        public bool HasLocale { get; set; }

        public bool ObligatoryAllTrue()
        {
            return HasTitle && HasType && HasImage && HasURL && HasDescription;
        }

    }
    public class TwitterCardsErrors {
        public bool HasCard { get; set; }
        public bool HasSite { get; set; }
        public bool HasTitle { get; set; }
        public bool HasImage { get; set; }
        public bool HasDescription { get; set; }
        public bool AllTrue()
        {
            return HasImage && HasCard && HasSite && HasTitle && HasDescription;
        }
    }
    public class AppleMobileErrors {
        public bool HasTitle { get; set; }
        public bool HasImage { get; set; }
        public bool HasCapableDef { get; set; }
        public bool HasBarStyle { get; set; }
        public bool HasFormatDetection { get; set; }

        public bool AllTrue()
        {
            return HasTitle && HasImage && HasCapableDef && HasBarStyle && HasFormatDetection;
        }

    }

    public class OnPageSEO
    {
        public TitleErrors TitleErrors { get; set; } = new();
        public HeadingErrors HeadingErrors { get; set; } = new();
        public SubHeadingErrors SubHeadingErrors { get; set; } = new();
        public MetaDescriptionErrors MetaDescriptionErrors { get; set; } = new();
        public ContentErrors ContentErrors { get; set; } = new();
        public ImageErrors ImageErrors{ get; set; } = new();
    }

    public class ImageErrors
    {
        public bool ImagesExist { get; set; }
        public bool AllImgsHasAlt { get; set; }
        public bool AltContainsKeywords { get; set; }
    }

    public class ContentErrors
    {
        public List<ALink> InternalRoutes { get; set; } = new();
        public List<ALink> ExternalRoutes { get; set; } = new();
        public int ContentLength { get; set; }
        public bool HasKeywords { get; set; }
        public bool PrimaryKeywordInFirstP { get; set; }
        public bool PrimaryKeywordInLastP { get; set; }
        public bool DuplicateContent { get; set; }
    }

    public class ALink
    {
        public string RouteLink { get; set; } = "";
        public bool HasHTTPs { get; set; }
        public bool Works { get; set; }
    }

    public class MetaDescriptionErrors
    {
        public string MetaDescription { get; set; } = "";
        public bool Exists { get; set; }
        public int Length { get; set; }
        public bool HasKeywords { get; set; }
        public bool HasPrimaryKeyword { get; set; }
        public bool MoreThanOne { get; set; }
        public bool DuplicateExist { get; set; }
    }

    public class SubHeadingErrors
    {
        public bool Exists { get; set; }
        public List<SubHeading> SubHeadings { get; set; } = new();
    }

    public class SubHeading
    {
        public string Heading { get; set; } = "";
        public int Level { get; set; }
        public bool HasKeywords { get; set; }
    }

    public class HeadingErrors
    {
        public string Heading { get; set; } = "";
        public int HeadingLength { get; set; }
        public bool Exists { get; set; }
        public bool MoreThanOne { get; set; }
        public bool HasKeywords { get; set; }
        public bool HasPrimaryKeyword { get; set; }
    }

    public class TitleErrors
    {
        public string Title { get; set; } = "";
        public int TitleLength { get; set; }
        public bool TitleExists { get; set; } = true;
        public bool MoreThanOne { get; set; }
        public bool HasKeywords { get; set; } = false;
        public bool HasMainKeyword { get; set; } = true;
    }

    public class BasicSEO
    {
        public double LoadingTime { get; set; }
        public bool HTTPS { get; set; }
        public bool HasSSL { get; set; }
        public long HTMLSize { get; set; }
        public DateTime SSLCertificateExpiration { get; set; }
        public bool PrimaryKeywordinURL { get; set; }
        public int URLLength { get; set; }
        public bool MinimalJS { get; set; }
        public bool MobileResponsive { get; set; }
        public bool BlockedFromSE { get; set; }
        public bool EncodingDeclared { get; set; }
        public bool DoctypeDecalred { get; set; }
    }

    public class SEOError
    {
        public string ErrorText { get; set; } = "";
        public string ErrorPlace { get; set; } = "";
        public string ErrorWeight { get; set; } = "LIGHT";
        public string ErrorImpact { get; set; } = "LOW";
    }
}
