using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterGameCorpus.Application
{
    public static class ExceptionsDictionary
    {
        public static HashSet<string> STOP_WORDS = new HashSet<string> {
            "and",
            "the",
            "with",
            "for",
            "of",
            "in",
            "on",
            "a",
            "to",
            "is",
            "was",
            "it's",
            "is an",
            "how",
            "him",
            "she",
            "hers",
            "you do",
            "why",
            "and an",
            "by a",
            "are the",
            "himself",
            "the game",
            "game and",
            "a new"
        };
        public static HashSet<string> FILLERS = new HashSet<string> {
            "video",
            "developed",
            "published",
            "released",
            "review",
            "critics",
            "website",
            "metacritic",
            "playstation",
            "xbox",
            "windows",
            "nintendo",
            "switch",
            "version",
            "inspired",
            "remastered",
            "series",
            "definitive",
            "updated",
            "created by",
            "engine",
            "based on the",
            "franchise"
        };
    }
}
