using ExporterGameCorpus.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterGameCorpus.Application
{
    public static class ClassificationDefinitions
    {
        public static List<ClassificationDefinition> All =
            new List<ClassificationDefinition>
            {
                new ClassificationDefinition
                {
                    Id = "RUN-BASED",
                    RequiredTags = new List<string>
                    {
                        "roguelike",
                        "roguelite",
                        "procedural generation"
                    }
                },
                new ClassificationDefinition
                {
                    Id = "HORROR",
                    RequiredTags = new List<string>
                    {
                        "horror",
                        "psychological horror",
                        "survival horror"
                    }
                }
            };
    }
}
