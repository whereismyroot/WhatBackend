using CharlieBackend.Core.FileModels;
using Swashbuckle.AspNetCore.Filters;
using System;
using System.Collections.Generic;
using System.Text;

namespace CharlieBackend.Api.SwaggerExamples.ImportController
{
    internal class ImportDataFromFileResponse : IExamplesProvider<List<StudentGroupFile>>
    {
        public List<StudentGroupFile> GetExamples()
        {
            return new List<StudentGroupFile>
            {
                new StudentGroupFile
                {
                    Id = "42",
                    Name = "Making of logo",
                    StartDate = new DateTime(2015, 7, 20),
                    FinishDate = new DateTime(2016, 8, 20)
                },
                new StudentGroupFile
                {
                    Id = "45",
                    Name = "Home design",
                    StartDate = new DateTime(2016, 8, 20),
                    FinishDate = new DateTime(2017, 9, 20)
                }
            };
        }
    }
}
