using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Task1.Views
{
    public class UploadFile : PageModel
    {
        private readonly ILogger<UploadFile> _logger;

        public UploadFile(ILogger<UploadFile> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }
    }
}