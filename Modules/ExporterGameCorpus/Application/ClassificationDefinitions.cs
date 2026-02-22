using ExporterGameCorpus.Domain;
using ExporterGameCorpus.Domain.ValueObject;
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
                    Id = ClassificationId.RUN_BASED,
                    RequiredTags = new List<string>
                    {
                        "roguelike",
                        "roguelite",
                        "procedural generation"
                    },
                    RequiredGenres = new List<string>
                    {
                        "roguelike",
                        "roguelite",
                        "procedural generation"
                    },
                },
                new ClassificationDefinition
                {
                    Id = ClassificationId.HORROR,
                    RequiredTags = new List<string>
                    {
                        "horror",
                        "psychological horror",
                        "psychological-horror",
                        "survival horror",
                        "survival-horror"
                    },
                    RequiredGenres = new List<string>
                    {
                        "horror",
                        "psychological horror",
                        "psychological-horror",
                        "survival horror",
                        "survival-horror"
                    },
                }
            };
    }
}
