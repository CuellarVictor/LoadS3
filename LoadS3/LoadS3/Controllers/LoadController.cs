using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LoadS3.Controllers
{

    [ApiController]
    [Route("load")]
    public class LoadController : Controller
    {
        private static IConfiguration Configuration;


        public LoadController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        [HttpPost, Route("uploadfile")]
        public async Task<ResultModel> UploadFile()
        {
            ResultModel result = new ResultModel();
            result.Result = "ERROR";

            try
            {
                //Get File
                var file = Request.Form.Files[0];


                //Se valida si la variable "file" tiene algun archivo
                if (file.Length > 0)
                {


                    //Get Loat Path
                    string pathFile = Configuration["PathList:LoadPath"];

                    //build file name complete
                    string fileName = DateTime.Now.ToString("yyyy:MM:dd:HH:mm:ss").Replace(":", "") + "_" + file.FileName;

                    string filePahtComplete = Path.Combine(pathFile, fileName);

                    //Se crea una variable FileStream para carlo en la ruta definida
                    using (var stream = new FileStream(filePahtComplete, FileMode.Create))
                    {
                        file.CopyTo(stream);
                    }

                    #region Upoad File S3

                    try
                    {
                        var client = new AmazonS3Client(Configuration["S3:S3AccessKeyId"],
                           Configuration["S3:S3SecretAccessKey"],
                           RegionEndpoint.GetBySystemName(Configuration["S3:S3RegionEndpoint"]));

                        PutObjectRequest putRequest = new PutObjectRequest
                        {
                            BucketName = Configuration["S3:S3BucketName"],
                            Key = fileName,
                            FilePath = pathFile + fileName,
                            //ContentType = "text/plain",
                            StorageClass = Configuration["S3:S3StorageClass"]
                        };

                        var loadResult = await client.PutObjectAsync(putRequest);

                        Thread.Sleep(1000);

                        result.Detail = Configuration["S3:UrlFileS3Uploaded"] + fileName;
                        result.Result = "OK";

                        //Delete input file
                        if (System.IO.File.Exists(pathFile + fileName))
                        {
                            try
                            {
                                System.IO.File.Delete(pathFile + fileName);
                            }
                            catch (Exception ex)
                            {
                               
                            }
                        }
                    }
                    catch (AmazonS3Exception amazonS3Exception)
                    {
                        if (amazonS3Exception.ErrorCode != null &&
                            (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId")
                            ||
                            amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                        {
                            throw new Exception("Check the provided AWS Credentials.");
                        }
                        else
                        {
                            throw new Exception("Error occurred: " + amazonS3Exception.Message);
                        }
                    }

                    #endregion

                }
                else
                {
                    result.Detail = "No se ha cargado archivo";
                }

                
            }
            catch (Exception ex)
            {
                result.Detail = ex.ToString();
            }

            return result;

        }
    }


  }
