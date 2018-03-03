using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ChannelsDB.API.Model;
using ChannelsDB.Core.Utils;
using ChannelsDB.Core.Models.Annotations;
using System.IO;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace ChannelsDB.API.Controllers
{
    [Route("[controller]")]
    public class UploadAnnotationsController : Controller
    {
        private readonly Config config;
        private DbManager m = null;

        public UploadAnnotationsController(IOptions<Config> optionsAccessor)
        {
            config = optionsAccessor.Value;
            m = new DbManager(config);

        }


        [HttpPost("Mole")]
        [Consumes("application/json")]
        public ActionResult TransferFromMole([FromBody] MoleTransfer moleData)
        {
            var id = Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", "");
            ApiResponse response;

            if (moleData == null)
            {
                response = new ApiResponse()
                {
                    Status = "Error",
                    Msg = "Data have not been transferred in a correct format."
                };
            }

            System.IO.File.WriteAllText(Path.Combine(config.MoleTransfers, $"{id}.json"), JsonConvert.SerializeObject(moleData, Formatting.Indented));

            response = new ApiResponse()
            {
                Status = "OK",
                Msg = $"Annotation was sucesfully stored. Thank you! Accession ID: {id}. Keep for further communications."
            };

            return Content(response.ToJson(), "aplication/json");
        }


        [HttpPost]
        [Consumes("multipart/form-data", "application/json")]
        public ActionResult Post(string s)
        {
            ApiResponse r = null;

            try
            {
                var id = Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", "");
                var uploadDir = Path.Combine(config.UserUploads, id);

                var upload = new ApiUpload()
                {
                    PdbId = Request.Form["pdbid"],
                    Email = Request.Form["email"],
                    ResidueAnnotations = JsonConvert.DeserializeObject<ResidueAnnotation[]>(Request.Form["residues"]),
                    TunnelAnnotations = JsonConvert.DeserializeObject<TunnelAnnotations[]>(Request.Form["channels"])
                };


                Directory.CreateDirectory(uploadDir);
                System.IO.File.WriteAllText(Path.Combine(uploadDir, "user_annotations.json"), JsonConvert.SerializeObject(upload, Formatting.Indented));


                foreach (var item in Request.Form.Files)
                {
                    var savePath = Path.Combine(uploadDir, item.FileName);
                    using (var fileStream = new FileStream(savePath, FileMode.Create))
                    {
                        item.CopyTo(fileStream);
                    }
                }

            }
            catch (Exception e)
            {
                r = new ApiResponse() { Status = "Error", Msg = $"Something went wrong {e.Message}." };
            }

            r = new ApiResponse() { Status = "OK", Msg = $"Data were succesfully stored." };

            return Content(r.ToJson(), "application/json");
        }

    }
}