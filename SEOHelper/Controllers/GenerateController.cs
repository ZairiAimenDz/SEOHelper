 using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SEOHelper.Service;

namespace SEOHelper.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GenerateController : ControllerBase
    {
        private readonly IKeywordService service;
        private readonly ITitleService titleService;
        private readonly IContentGenerationService generationService;

        public GenerateController(IKeywordService keyService
            ,ITitleService titleService
            ,IContentGenerationService generationService)
        {
            this.service = keyService;
            this.titleService = titleService;
            this.generationService = generationService;
        }

        [HttpGet("Keywords")]
        public ActionResult Keywords([FromQuery]string[]? keywords,
            [FromQuery]string? url
            ,[FromQuery]string? location)
        {
            if(keywords.Length ==0 && String.IsNullOrEmpty(url))
            {
                return BadRequest();
            }

            return Ok(service.GetKeywords(url,keywords,location));
        }

        [HttpGet("Title")]
        public ActionResult GetTitle([FromQuery]string[] keywords
            ,[FromQuery] string[]? Post 
            ,[FromQuery] string[]? Pre)
        {
            if (keywords.Length == 0)
            {
                return BadRequest();
            }

            return Ok(titleService.GetTitles(keywords,Post,Pre));
        }

        [HttpGet("URL")]
        public ActionResult GetUrl([FromQuery]string baseURL
            ,[FromQuery]string[] keywords,
            [FromQuery]string?Title)
        {
            if (keywords.Length == 0 && string.IsNullOrEmpty(baseURL))
            {
                return BadRequest();
            }

            return Ok(titleService.GetURL(baseURL,keywords,Title));
        }

        [HttpGet("Heading")]
        public ActionResult GetHeading([FromQuery]string[] keywords
            ,[FromQuery]string[]? Post
            ,[FromQuery]string[]? Pre)
        {
            return Ok(titleService.GetHeadings(keywords,Post,Pre));
        }

        [HttpGet("MetaDescription")]
        public ActionResult GetMetaDescription([FromQuery] string[] keywords,[FromQuery]string? content)
        {
            return Ok(generationService.GenerateMeta(keywords,content));
        }

        [HttpGet("Content")]
        public async Task<ActionResult> GetContent([FromQuery]string[] keywords)
        {
            return Ok(await generationService.GenerateContent(keywords));
        }

        [HttpGet("Hashtags")]
        public ActionResult GetHashtags([FromQuery]string[] keywords)
        {
            return Ok(service.GetHashtags(keywords));
        }
    }
}
